open Suave
open Suave.Filters
open Suave.Operators
open Suave.Successful
open Suave.RequestErrors
open Suave.Json
open Suave.Authentication
open Suave.Logging

open System
open System.Runtime.Serialization
open System.Collections.Generic
open System.Text

open FSharp.Json
open Model

module ValueOption =
  let inline ofChoice x =
    x |> function
    | Choice1Of2 a -> ValueSome a
    | _ -> ValueNone

let logger =

  LiterateConsoleTarget(
    name = [|"SimpleRankingsServer"|],
    minLevel = LogLevel.Verbose,
    options = Literate.LiterateOptions.create(),
    outputTemplate = "[{level}] {timestampUtc:o} {message} [{source}]{exceptions}"
  ) :> Logger

module Endpoint =
  let private jsonConfig = JsonConfig.create(allowUntyped = true)

  let inline pathTable dbName f =
    Writers.setMimeType "application/json; charset=utf-8"
    >=> path (sprintf "/api/%s" dbName) >=> f()

  let select dbName tableMap connStr =
    pathTable dbName (fun () ->
      request(fun x ->
        try
          ( x.queryParam "table"|> ValueOption.ofChoice
          , x.queryParam "orderBy" |> ValueOption.ofChoice
          )
          |> function
          | ValueNone, _ ->
            sprintf "missing 'table' field"
            |> BAD_REQUEST
          | _, ValueSome s
            when s <> Database.IdKey &&
              (Map.tryFind s tableMap
              |> Option.map((=) Text)
              |> Option.defaultValue true)
              ->

            sprintf "'orderBy': '%s' is invalid" s
            |> BAD_REQUEST

          | ValueSome tableName, orderBy ->
            { table = tableName
              orderBy = orderBy
              limit = x.queryParam "limit" |> ValueOption.ofChoice |> ValueOption.map int
              isDescending =
                x.queryParam "isDescending" |> ValueOption.ofChoice |> ValueOption.map Boolean.Parse
            }
            |> Database.select connStr tableMap
            |> Json.serializeEx jsonConfig
            |> OK
        with e ->
          let s = sprintf "%A:%s" (e.GetType()) e.Message
          BAD_REQUEST s
      ) >=> logStructured logger logFormatStructured
    )

  let insert dbName (tableMap: Model.TableConfig) connStr =
    pathTable dbName (fun () ->
      try
        mapJsonWith
          (Encoding.UTF8.GetString >> Json.deserializeEx jsonConfig)
          (Json.serialize >> Encoding.UTF8.GetBytes)
          (fun (param : Insert) ->
            [|
              for table in tableMap do
                if not <| param.values.ContainsKey(table.Key) then
                  yield table.Key
            |]
            |> function
            | [||] ->
              let date = DateTime.UtcNow
              let id = Database.insert connStr param.table tableMap date param
              { InsertResult.id = id }
            | xs ->
              failwithf "Keys %A are needed." xs
          )
      with e ->
        let s = sprintf "%A:%s" (e.GetType()) e.Message
        BAD_REQUEST s
    ) >=> logStructured logger logFormatStructured

let app (config: Model.Config) (connStrDict: IReadOnlyDictionary<string, string>) =
  choose [
    for dbName, gameConfig in Map.toSeq config.games ->
      authenticateBasic ((=) (gameConfig.username, gameConfig.password)) <| choose [
        for tableName, keys in Map.toSeq gameConfig.tables do
          yield GET >=> Endpoint.select dbName keys (connStrDict.[dbName])
          yield POST >=> Endpoint.insert dbName keys (connStrDict.[dbName])
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

  { defaultConfig with bindings = [ httpBinding ]; logger = logger }


[<EntryPoint>]
let main _ =
  let config = Config.Load @"config.json"

  if not <| IO.Directory.Exists config.directory then
    IO.Directory.CreateDirectory config.directory
    |> printfn "Directory created: %A"

  let connStrDict = Dictionary<string, string>()

  for (dbname, gameConfig) in config.games |> Map.toSeq do
    let dbPath = sprintf "%s/%s.db" config.directory dbname
    let connStr = (Database.createConfig dbPath).ToString()
    connStrDict.[dbname] <- connStr

    Database.createTables connStr gameConfig.tables

  app config connStrDict
  #if DEBUG
  |> startWebServer { defaultConfig with logger = logger }
  #else
  |> startWebServer (conf config.port)
  #endif

  0
