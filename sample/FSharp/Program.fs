type Sample1 = {
  Score1 : int
  Score2 : float
  Name : string
}

let [<Literal>] Url = @"http://localhost:8080/api/SampleDB"
let [<Literal>] Username = "sample"
let [<Literal>] Password = "sample"

let client = new SimpleRankingsServer.Client(Url, Username, Password)
let userId = System.Guid.NewGuid()

[<EntryPoint>]
let main _ =
  async {
    let sample = { Score1 = 290; Score2 = 111.1; Name = "taremimi" }
    let! result = client.AsyncInsert("SampleTable", userId, sample)

    printfn "%A" result

    return! client.AsyncSelect<Sample1>("SampleTable", orderBy = "Score1", limit = 7)
  }
  |> Async.Catch
  |> Async.RunSynchronously
  |> printfn "%A"
  0
