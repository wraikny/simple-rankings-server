module Model

open FSharp.Json

[<Struct>]
type TableType =
  | Int
  | Float
  | Text
with
  member inline x.ToSql() =
    x |> function
    | Int -> "int"
    | Float -> "real"
    | Text -> "text"

type TableConfig = Map<string, TableType>

type GameConfig = {
  username : string
  password : string
  tables: Map<string, TableConfig>
}

type Config = {
  port : uint16
  directory : string
  games : Map<string, GameConfig>
} with
  static member inline Load path =
    System.IO.File.ReadAllText path
    |> Json.deserialize<Config>

type Record = {
  id : int64
  userId : string
  utcDate : string
  values : Map<string, obj>
}

type Select = {
  table : string
  orderBy : string voption
  isDescending : bool voption
  limit : int voption
}

type Insert = {
  table: string
  userId : string
  values : Map<string, obj>
}

type InsertResult = { id : int64 }