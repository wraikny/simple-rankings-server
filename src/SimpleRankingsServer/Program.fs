open Suave
open Suave.Filters
open Suave.Operators
open Suave.Successful
open Suave.RequestErrors
open Suave.Json
open System.Runtime.Serialization
open System
open FSharp.Json
open Model

module ValueOption =
  let inline ofChoice x =
    x |> function
    | Choice1Of2 a -> ValueSome a
    | _ -> ValueNone


module Endpoint =
  let private jsonConfig = JsonConfig.create(allowUntyped = true)

  let inline pathTable config f =
    Writers.setMimeType "application/json; charset=utf-8"
    >=> pathScan "/v1/%s" (fun table ->
      Map.tryFind table config.tables |> function
      | Some tableMap -> f table tableMap
      | _ ->
        sprintf "Table %s is not found" table
        |> NOT_FOUND
    )

  let select config connStr =
    pathTable config (fun table tableMap ->
      request(fun x ->
        try
          x.queryParam "orderBy"
          |> ValueOption.ofChoice
          |> function
          | ValueSome s
            when s <> Database.IdKey &&
              Map.tryFind s tableMap
              |> Option.map(fun t -> t = Int || t = Float)
              |> Option.defaultValue false
              ->

            sprintf "orderBy '%s' is invalid key" s
            |> BAD_REQUEST
          
          | orderBy ->
            { table = table
              orderBy = orderBy
              limit = x.queryParam "limit" |> ValueOption.ofChoice |> ValueOption.map int
              isDescending =
                x.queryParam "isDescending" |> function
                | Choice2Of2 _ | Choice1Of2 "true" -> true
                | Choice1Of2 "false" -> false
                | Choice1Of2 s -> failwithf "Unexpected value in isDescending '%s'" s
            }
            |> Database.select connStr tableMap
            |> Json.serializeEx jsonConfig
            |> OK
        with e ->
          let s = sprintf "%A:%s" (e.GetType()) e.Message
          eprintfn "%s" s
          BAD_REQUEST s
      )
    )

  open System.Text

  let insert config connStr =
    pathTable config (fun table tableMap ->
      try
        mapJsonWith
          (Encoding.UTF8.GetString >> Json.deserializeEx jsonConfig)
          (Json.serialize >> Encoding.UTF8.GetBytes)
          (fun (param : Insert) ->
            tableMap
            |> Map.toSeq
            |> Seq.filter(fun (k, _) -> not <| param.values.ContainsKey k)
            |> Seq.toArray
            |> function
            | [||] ->
              let date = DateTime.UtcNow
              let id = Database.insert connStr table tableMap date param
              { InsertResult.id = id }
            | xs ->
              failwithf "Keys %A are needed." xs
          )
      with e ->
        let s = sprintf "%A:%s" (e.GetType()) e.Message
        eprintfn "%s" s
        BAD_REQUEST s
    )

let app config connStr =
  choose [
    GET >=> Endpoint.select config connStr
    POST >=> Endpoint.insert config connStr
  ]

[<EntryPoint>]
let main _ =
  let config = Config.Load @"config.json"
  let connStr = (Database.createConfig config.databasePath).ToString()

  Database.createTables connStr config.tables

  app config connStr
  |> startWebServer defaultConfig

  0
