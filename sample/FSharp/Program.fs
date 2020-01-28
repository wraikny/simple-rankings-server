// Learn more about F# at http://fsharp.org

module SimpleRankingsServer =
  open System
  open System.Text
  open System.Collections.Generic
  open System.Net.Http
  open System.ComponentModel
  open FSharp.Json

  type DateTimeString() =
    interface ITypeTransform with
      member x.targetType() = (fun _ -> typeof<string>) ()
      member x.toTargetType value = (fun (v: obj) ->
        (v :?> DateTime).ToString "yyyy/MM/dd HH:mm:ss" :> obj) value
      
      member x.fromTargetType value = (fun (v: obj) ->
        DateTime.ParseExact(v:?> string, "yyyy/MM/dd HH:mm:ss", null) :> obj) value

  type Data<'a> = {
    id : int64
    userId : Guid
    values : 'a

    [<JsonField(Transform=typeof<DateTimeString>)>]
    utcDate : DateTime
  }

  #if !DEBUG
  [<EditorBrowsable(EditorBrowsableState.Never)>]
  #endif
  type InsertParam<'a> = {
    userId : Guid
    values : 'a
  }

  #if !DEBUG
  [<EditorBrowsable(EditorBrowsableState.Never)>]
  #endif
  type InsertResult = { id : int64 }
  
  let private client = new HttpClient()

  let private jsonConfig = JsonConfig.create(allowUntyped = true)

  type Client =
    static member Insert (url: string, userId, data) : Async<int64> =
      async {
        let json = Json.serializeEx jsonConfig { userId = userId; values = data }
        use content = new StringContent(json, Encoding.UTF8, @"application/json")

        let! result = client.PostAsync(url, content) |> Async.AwaitTask
        let! resString = result.Content.ReadAsStringAsync() |> Async.AwaitTask

        if result.IsSuccessStatusCode then
          return Json.deserialize<InsertResult>(resString).id
        else
          return failwithf "%A:%s" result.StatusCode resString
      }

    static member Select (url: string, ?orderBy : string, ?isDescending : bool, ?limit : int) : Async<Data<'a>[]> =
      async {
        let param = Dictionary<string, string>()
        let inline add s x =
          x |> Option.iter(fun y -> param.Add(s, y.ToString()))
        
        orderBy |> add "orderBy"
        isDescending |> add "isDescending"
        limit |> add "limit"

        let! paramStr = (new FormUrlEncodedContent(param)).ReadAsStringAsync() |> Async.AwaitTask
        let! result = client.GetAsync(sprintf "%s?%s" url paramStr) |> Async.AwaitTask
        let! resString = result.Content.ReadAsStringAsync() |> Async.AwaitTask

        if result.IsSuccessStatusCode then
          return Json.deserializeEx<Data<'a>[]> jsonConfig resString
        else
          return failwithf "%A:%s" result.StatusCode resString
      }


type Sample1 = {
  Score1 : int
  Score2 : float
  Name : string
}

[<Literal>]
let Url = @"http://localhost:8080/v1/Sample1"

open System
open SimpleRankingsServer

[<EntryPoint>]
let main _ =
  async {
    let userId = Guid.NewGuid()

    let sample = { Score1 = 90; Score2 = 111.1; Name = "taremimi" }
    let! result = Client.Insert(Url, userId, sample)

    printfn "%A" result

    let! data = Client.Select<Sample1>(Url, "Score1", limit = 7)
    for x in data do
      printfn "%A" x
  } |> Async.RunSynchronously

  0 // return an integer exit code
