open Suave
open Suave.Filters
open Suave.Operators
open Suave.Successful
open Suave.RequestErrors
open Suave.Json
open Suave.Authentication
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

  let inline pathTable version tableName f =
    Writers.setMimeType "application/json; charset=utf-8"
    >=> path (sprintf "/%s/%s" version tableName) >=> f()

  let select version tableName tableMap connStr =
    pathTable version tableName (fun () ->
      request(fun x ->
        try
          x.queryParam "orderBy"
          |> ValueOption.ofChoice
          |> function
          | ValueSome s
            when s <> Database.IdKey &&
              (Map.tryFind s tableMap
              |> Option.map((=) Text)
              |> Option.defaultValue true)
              ->

            sprintf "orderBy '%s' is invalid key" s
            |> BAD_REQUEST
          
          | orderBy ->
            { table = tableName
              orderBy = orderBy
              limit = x.queryParam "limit" |> ValueOption.ofChoice |> ValueOption.map int
              isDescending =
                x.queryParam "isDescending" |> function
                | Choice1Of2 x ->
                  Boolean.TryParse x
                  |> function
                  | true, t -> t
                  | _, _ -> failwithf "Unexpected value in isDescending '%s'" x
                | Choice2Of2 _ -> true
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

  let insert version tableName tableMap connStr =
    pathTable version tableName (fun () ->
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
              let id = Database.insert connStr tableName tableMap date param
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
    for tableName, tableConfig in Map.toSeq config.tables do
      authenticateBasic ((=) (tableConfig.username, tableConfig.password)) <| choose [
        GET >=> Endpoint.select config.version tableName tableConfig.keys connStr
        POST >=> Endpoint.insert config.version tableName tableConfig.keys connStr
      ]
  ]

open System.Net
open System.Net.Sockets
open Suave.Sockets

let conf (port: uint16) =
  let socketBinding : Sockets.SocketBinding =
    let ip : IPAddress =
      Dns.GetHostName()
      |>  Dns.GetHostAddresses
      |> Seq.find(fun x -> x.AddressFamily = AddressFamily.InterNetwork)
    { ip = ip; port = port }

  let httpBinding : Http.HttpBinding =
    { scheme = HTTP
      socketBinding = socketBinding }

  { defaultConfig with bindings = [ httpBinding ] }


[<EntryPoint>]
let main _ =
  let config = Config.Load @"config.json"
  let connStr = (Database.createConfig config.databasePath).ToString()

  Database.createTables connStr config.tables

  app config connStr
  #if DEBUG
  |> startWebServer defaultConfig
  #else
  |> startWebServer (conf config.port)
  #endif

  0
