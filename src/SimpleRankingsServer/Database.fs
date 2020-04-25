module Database

open System.Text
open System.Data
open System.Data.SQLite
open Dapper
open System
open FSharp.Interop.Dynamic

let inline createConfig dataSource =
  SQLiteConnectionStringBuilder(DataSource = dataSource)

let [<Literal>] IdKey = "Id"
let [<Literal>] UserIdKey = "UserId"
let [<Literal>] UTCDateKey = "UTCDate"

type Model.Select with
  member this.ToSql(tableMap : Map<_,_>) =
    let orderBy =
      this.orderBy |> function
      | ValueSome x when x = IdKey || tableMap.ContainsKey x ->
          sprintf "order by %s %s" x
            (this.isDescending
              |> function
              | ValueSome false -> "asc"
              | _ -> "desc"
            )
      | _ -> ""

    let limit =
      this.limit
      |> ValueOption.map (sprintf "limit %d")
      |> ValueOption.defaultValue ""
    
    sprintf "select * from %s %s %s" this.table orderBy limit

let private createTableSql (table: string) (keys: seq<string * Model.TableType>) =
  let sb = 
    StringBuilder(
      sprintf
        """
        create table if not exists [%s] (
        [%s] integer primary key autoincrement,
        [%s] text not null, [%s] text not null
        """
        table IdKey UserIdKey UTCDateKey
    )
  for key, ty in keys do
    sb.Append(sprintf ", [%s] %A not null" key ty) |> ignore

  sb.Append(")").ToString()


let createTables (connStr: string) (tables: Map<string, Model.TableConfig>) =
  use connection = new SQLiteConnection(connStr)
  connection.Open()
  use trans = connection.BeginTransaction()

  try
    for (tableName, tableConfig) in Map.toSeq tables do
      let sql = createTableSql tableName (Map.toSeq tableConfig)
      connection.Execute(sql, trans) |> ignore

    trans.Commit()
  with _ ->
    trans.Rollback()
    reraise()

open System.Collections.Concurrent

let insert (connStr: string) tableName (tableMap: Model.TableConfig) (utcDate: DateTime) (data: Model.Insert) : int64 =
  let keys = [| for (k, _) in tableMap |> Map.toSeq -> k |]

  let sql =
    sprintf "insert into %s(%s, %s, %s) values(@%s, @%s, %s)" tableName
      UserIdKey UTCDateKey (keys |> String.concat ", ")
      UserIdKey UTCDateKey (keys |> Seq.map(sprintf "@%s") |> String.concat ", ")

  let param =
    let p = DynamicParameters()
    p.Add(UserIdKey, data.userId)
    p.Add(UTCDateKey, utcDate.ToString "yyyy/MM/dd HH:mm:ss")
    for k in keys do p.Add(k, data.values |> Map.find k)
    box p

  use connection = new SQLiteConnection(connStr)
  connection.Open()
  use trans =  connection.BeginTransaction()

  try
    if connection.Execute(sql, param, trans) = 0 then
      failwith "Value has not benn inserted"
    
    let x =
      connection.Query(sprintf "select max(%s) from %s" IdKey tableName, trans)
      |> Seq.head
    
    trans.Commit()

    x?(sprintf "max(%s)" IdKey)
  with _ ->
    trans.Rollback()
    reraise()


let private selectTableMapMemo = ConcurrentDictionary<Model.Select, string>()

let select (connStr : string) (tableMap: Model.TableConfig) (data: Model.Select) : Model.Record[] =
  let sql =
    selectTableMapMemo.TryGetValue(data) |> function
    | true, x -> x
    | _ ->
      let sql = data.ToSql(tableMap)
      selectTableMapMemo.TryAdd(data, sql) |> ignore
      sql

  use connection = new SQLiteConnection(connStr)
  connection.Open()

  let xs = connection.Query(sql)

  [|for x in xs -> {
      id = x?(IdKey)
      userId = x?(UserIdKey)
      utcDate = x?(UTCDateKey)
      values = Map.map (fun key _ -> x?(key)) tableMap }|]
