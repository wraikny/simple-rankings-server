module Model

open FSharp.Json

[<Struct>]
type TableType =
  | Int
  | Float
  | Text
with
  member x.ToSql() =
    x |> function
    | Int -> "int"
    | Float -> "real"
    | Text -> "text"

type Config = {
  databasePath : string
  tables : Map<string, Map<string, TableType>>
} with
  static member Load path =
    System.IO.File.ReadAllText path
    |> Json.deserialize<Config>

type Record = {
  id : int64
  userId : string
  values : Map<string, obj>
}

type Select = {
  table : string
  orderBy : string voption
  isDescending : bool
  limit : int voption
}

type Insert = {
  userId : string
  values : Map<string, obj>
}

// type Insert = {
//   table : string
//   userId : string
//   values : Map<string, obj>
// }

type InsertResult = { id : int64 }