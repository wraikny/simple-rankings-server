open Suave
open Suave.Filters
open Suave.Operators
open Suave.Successful
open Suave.Json
open System.Runtime.Serialization
open System
open FSharp.Json

[<Struct>]
type TableType =
  | Int
  | Float
  | Text

type Config = {
  databasePath : string
  tables : Map<string, Map<string, TableType>>
}

type QueryData = {
  table : string
  sortBy : string option
  limit : int option
}

type ScoreData = {
  uniqueId : int option
  userId : Guid
  values : (string*obj)[]
}

type ResultData<'a> = {
  isSuccess : bool
  message : string
  value : 'a
}

let app =
  choose [
    GET >=>
      pathScan "/v1/%s" (fun table ->
        OK table
      )
    
    // POST >=>
    //   path "/json" >=> mapJson (fun (a:Foo) -> { bar = a.foo })
  ]

[<EntryPoint>]
let main _ =
  // startWebServer defaultConfig app
  0
