module SimpleRankingsServer

open System
open System.Text
open System.Collections.Generic
open System.Net.Http
open System.Net.Http.Headers
open FSharp.Json
open System.ComponentModel

type DateTimeString() =
  interface ITypeTransform with
    member __.targetType() = (fun _ -> typeof<string>) ()
    member __.toTargetType value = (fun (v: obj) ->
      (v :?> DateTime).ToString "yyyy/MM/dd HH:mm:ss" :> obj) value
    
    member __.fromTargetType value = (fun (v: obj) ->
      DateTime.ParseExact(v:?> string, "yyyy/MM/dd HH:mm:ss", null) :> obj) value

type Data<'a> = {
  id : int64
  userId : Guid
  values : 'a

  [<JsonField(Transform=typeof<DateTimeString>)>]
  utcDate : DateTime
}

module Internal =
  type InsertParam<'a> = {
    table : string
    userId : Guid
    values : 'a
  }

  type InsertResult = { id : int64 }

open Internal

let private jsonConfig = JsonConfig.create(allowUntyped = true)

type Client(url : string, usrename, password) =
  let client = new HttpClient()

  do
    let parameter = Convert.ToBase64String(Encoding.UTF8.GetBytes(sprintf "%s:%s" usrename password));
    client.DefaultRequestHeaders.Authorization <- AuthenticationHeaderValue("Basic", parameter);

  interface IDisposable with
    member __.Dispose() = client.Dispose()

  member __.AsyncInsert (tableName, userId, data) : Async<int64> =
    async {
      let json = Json.serializeEx jsonConfig { table = tableName; userId = userId; values = data }
      use content = new StringContent(json, Encoding.UTF8, @"application/json")

      let! result = client.PostAsync(url, content) |> Async.AwaitTask
      let! resString = result.Content.ReadAsStringAsync() |> Async.AwaitTask

      if result.IsSuccessStatusCode then
        return Json.deserialize<InsertResult>(resString).id
      else
        return failwithf "%A:%s" result.StatusCode resString
    }

  member __.AsyncSelect (tableName: string, ?orderBy : string, ?isDescending : bool, ?limit : int) : Async<Data<'a>[]> =
    async {
      let param = Dictionary<string, string>()
      let inline add s x =
        x |> Option.iter(fun y -> param.Add(s, y.ToString()))

      param.Add("table", tableName)
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
