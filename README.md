# simple-rankings-server

simple-rankings-server is distributed under MIT License.

## CI Status
|||
:---|:---
|Github Actions|[![](https://github.com/wraikny/simple-rankings-server/workflows/CI/badge.svg)](https://github.com/wraikny/simple-rankings-server/actions?workflow=CI)|
<!---
|Travis CI|[![](https://travis-ci.org/wraikny/simple-rankings-server.svg?branch=master)](https://travis-ci.org/wraikny/simple-rankings-server)|
|AppVeyor|[![](https://ci.appveyor.com/api/projects/status/5vtyb8v9twdpteb6?svg=true)](https://ci.appveyor.com/project/wraikny/simple-rankings-server)|
--->

## Libraries

- [System.Data.SQLite](https://system.data.sqlite.org/index.html),[Microsoft Public License](https://opensource.org/licenses/ms-pl)
- [FSharp.Interop.Dynamic](https://github.com/fsprojects/FSharp.Interop.Dynamic), [Apache License 2.0](http://www.apache.org/licenses/)
- [Dapper](https://github.com/StackExchange/Dapper), [Apache License 2.0](http://www.apache.org/licenses/)
- [Suave](https://suave.io/), [Apache License 2.0](http://www.apache.org/licenses/)
- [FSharp.Json](https://github.com/vsapronov/FSharp.Json), [Apache License 2.0](http://www.apache.org/licenses/)


## Benefical URLs
- https://suave.io
- https://suave.io/restful.html
- https://blog.recyclebin.jp/archives/4495
- https://qiita.com/masato44gm/items/dffb8281536ad321fb08
- https://github.com/SuaveIO/suave/blob/master/examples/Example/Program.fs
- http://pocketberserker.hatenablog.com/entry/2017/02/15/184210
- https://www.nekoni.net/Blog/Article/nekonidotnet-admin-development-part6-first-part


## Requirements
.NET Core 3.0  
https://dotnet.microsoft.com/download  

```shell
$ dotnet --version
3.0.100
```

## CLI

### Restoring after Clone
```shell
$ dotnet tool restore
```

### Build
```shell
$ dotnet fake build # Build all projects as Release
$ # or
$ dotnet build --project src/SampleApp [-c {Debug|Release}]
```

### Run
```shell
$ dotnet run --project src/SampleApp [-c {Debug|Release}]
```

### Tests
```shell
$ dotnet fake build -t Test
$ #or
$ dotnet run --project tests/SampleTest
```

## References
### [Paket](https://fsprojects.github.io/Paket/index.html)  
Each project requires `paket.references` file.

After updating [paket.dependencies](/paket.dependencies):
```shell
$ dotnet paket install
```

To Update Versions of Libraries,
```shell
$ dotnet paket update
```

### [FAKE](https://fake.build/)  
Scripting at [build.fsx](/build.fsx).  

```shell
$ dotnet fake build -t Clean # Run "Clean" Target
$ dotnet fake build # Run Default Taret
```

### Create Project
```shell
$ # Application
$ dotnet new console -lang=f# -o src/SampleApp
$ echo 'FSharp.Core' > src/SampleApp/paket.references
$ paket install

$ # Library
$ dotnet new classlib -lang=f# -o src/SampleLib
$ echo 'FSharp.Core' > src/SampleLib/paket.references
$ paket install
```

### Create Test Project
```shell
$ dotnet new console -lang=f# -o tests/SampleTest
$ echo -e 'FSharp.Core\nExpecto\nExpecto.FsCheck' > tests/SampleTest/paket.references

$ paket install # Add reference of Paket to .fsproj file
```
and then, Add **Project Name** to [build.fsx](/build.fsx).

### Create Solution
```shell
$ dotnet new sln
$ dotnet sln add src/SampleApp
$ dotnet sln add src/SampleLib
```

### Update Tool
```shell
$ dotnet fake build -t Tool
```
and then, commit [.config/dotnet-tools.json](/.config/dotnet-tools.json).
