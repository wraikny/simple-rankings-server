type Sample1 = {
  Score1 : int
  Score2 : float
  Name : string
}

let [<Literal>] Url = @"http://localhost:8080/api/Sample1"
let [<Literal>] Username = "sample"
let [<Literal>] Password = "sample"

open System

[<EntryPoint>]
let main _ =
  use client = new SimpleRankingsServer.Client(Url, Username, Password)
  let userId = Guid.NewGuid()

  async {
    let sample = { Score1 = 90; Score2 = 111.1; Name = "taremimi" }
    let! result = client.AsyncInsert(userId, sample)

    printfn "%A" result

    let! data = client.AsyncSelect<Sample1>("Score1", limit = 7)
    for x in data do
      printfn "%A" x
  } |> Async.RunSynchronously

  Console.ReadLine() |> ignore

  0
