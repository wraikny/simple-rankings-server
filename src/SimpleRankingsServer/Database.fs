module Database

open System.Text
open System.Data
open System.Data.SQLite
open Dapper
open System
open FSharp.Interop.Dynamic

let inline createConfig dataSource =
  SQLiteConnectionStringBuilder(DataSource = dataSource)

let inline getConnection (connectionString: string) = 
  new SQLiteConnection(connectionString)

let inline execute sql param (connection: IDbConnection) =
  connection.Execute(sql, param)

let inline query<'a> (sql: string) (connection: IDbConnection) : seq<'a> =
  connection.Query<'a>(sql)

let inline beginTransaction (connection: IDbConnection) =
  connection.BeginTransaction()

[<Literal>]
let IdKey = "Id"

[<Literal>]
let UserIdKey = "UserId"

[<Literal>]
let UTCDateKey = "UTCDate"

let createTables connStr (tables: Map<string, Map<string, Model.TableType>>) =
  let createSql (table: string) (keys: seq<string * Model.TableType>) =
    let sb = 
      StringBuilder()
        .Append(
          sprintf
            "create table if not exists [%s] ([%s] integer primary key autoincrement, [%s] text not null, [%s] text not null"
            table IdKey UserIdKey UTCDateKey
        )

    for (key, ty) in keys do
      sb.Append(sprintf ", [%s] %s not null" key (ty.ToSql())) |> ignore
    
    sb.Append(")").ToString()

  let connection = getConnection connStr
  connection.Open()
  use trans = beginTransaction connection

  try
    for (table, keys) in Map.toSeq tables do
      let sql = createSql table (Map.toSeq keys)
      connection.Execute(sql, trans) |> ignore

    trans.Commit()
  with _ ->
    trans.Rollback()
    reraise()

open System.Collections.Concurrent

let private insertTableMapMemo = ConcurrentDictionary<string, string[] * string>()

let insert connStr table (tableMap : Map<string, Model.TableType>) (utcDate: DateTime) (data: Model.Insert) : int64 =
  let keys, sql =
    insertTableMapMemo.TryGetValue table |> function
    | true, x -> x
    | _ ->
      let keys = [| for (k, _) in tableMap |> Map.toSeq -> k |]

      let sql =
        sprintf "insert into %s(%s, %s, %s) values(@%s, @%s, %s)" table
          UserIdKey UTCDateKey (keys |> String.concat ", ")
          UserIdKey UTCDateKey (keys |> Seq.map(sprintf "@%s") |> String.concat ", ")

      insertTableMapMemo.TryAdd(table, (keys, sql)) |> ignore

      keys, sql

  let param = DynamicParameters()
  param.Add(UserIdKey, data.userId)
  param.Add(UTCDateKey, utcDate.ToString "yyyy/MM/dd HH:mm:ss")

  for k in keys do
    param.Add(k, data.values |> Map.find k)

  let connection = getConnection connStr
  connection.Open()
  use trans = beginTransaction connection

  try
    connection.Execute(sql, param, trans) |> ignore
    let x =
      connection.Query(sprintf "select max(%s) from %s" IdKey table, trans)
      |> Seq.head
    trans.Commit()
    x?(sprintf "max(%s)" IdKey)
  with _ ->
    trans.Rollback()
    reraise()


let private selectTableMapMemo = ConcurrentDictionary<Model.Select, string>()

let select connStr (tableMap: Map<string, Model.TableType>) (data: Model.Select) : Model.Record[] =
  let sql =
    selectTableMapMemo.TryGetValue(data) |> function
    | true, x -> x
    | _ ->
      let sql =
        sprintf "select * from %s %s %s"
          data.table
          ( data.orderBy |> function
            | ValueSome x
              when x = IdKey
              || x = UserIdKey
              || tableMap.ContainsKey x ->
                sprintf "order by %s %s" x
                  (data.isDescending |> function | true -> "desc" | _ -> "asc")
            | _ -> "" )
          (data.limit |> ValueOption.map(sprintf "limit %d") |> ValueOption.defaultValue "")

      selectTableMapMemo.TryAdd(data, sql) |> ignore

      sql

  let connection = getConnection connStr
  connection.Open()
  use trans = beginTransaction connection

  try
    let xs = connection.Query(sql, trans)
    trans.Commit()

    [|for x in xs ->
        {
          id = x?(IdKey)
          userId = x?(UserIdKey)
          utcDate = x?(UTCDateKey)
          values =
            tableMap
            |> Map.map(fun key _ -> x?(key))
        }
    |]
  with _ ->
    trans.Rollback()
    reraise()
