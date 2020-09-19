#r "paket:
source https://api.nuget.org/v3/index.json
nuget Fake.DotNet.Cli
nuget Fake.IO.FileSystem
nuget Fake.Core.Target
nuget Fake.DotNet.Testing.Expecto
//"

#load ".fake/build.fsx/intellisense.fsx"
#r "netstandard"

open Fake.Core
open Fake.DotNet
open Fake.DotNet.Testing
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.Core.TargetOperators

let testProjects = []

Target.create "Test" (fun _ ->
  [ for x in testProjects ->
      sprintf "tests/%s/bin/Release/**/%s.dll" x x
  ] |> function
  | [] ->
    printfn "There is no test project"
  | x::xs ->
    Seq.fold (++) (!! x) xs
    |> Expecto.run id
)

let dotnet cmd arg = DotNet.exec id cmd arg |> ignore

Target.create "Tool" (fun _ ->
  dotnet "tool" "update paket"
  dotnet "tool" "update fake-cli"
)

Target.create "Clean" (fun _ ->
  !! "src/**/bin"
  ++ "src/**/obj"
  ++ "tests/**/bin"
  ++ "tests/**/obj"
  ++ "sample/**/bin"
  ++ "sample/**/obj"
  |> Shell.cleanDirs
)

Target.create "Build" (fun _ ->
  !! "src/**/*.*proj"
  ++ "tests/**/*.*proj"
  ++ "sample/**/*.*proj"
  |> Seq.iter (DotNet.build id)
)

Target.create "Publish" (fun _ ->
  "src/SimpleRankingsServer/SimpleRankingsServer.fsproj"
  |> DotNet.publish (fun p ->
    { p with
        Runtime = Some "linux-x64"
        Configuration = DotNet.BuildConfiguration.Release
        SelfContained = Some true
        MSBuildParams = {
          p.MSBuildParams with
            Properties =
              ("PublishSingleFile", "true")
              :: ("PublishTrimmed", "true")
              :: p.MSBuildParams.Properties
        }
        OutputPath = "publish/linux-x64" |> Some
    }
  )
)

Target.create "All" ignore

"Clean"
  ==> "Build"
  ==> "All"

Target.runOrDefault "All"
