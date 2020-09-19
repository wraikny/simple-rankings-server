cd /d %~dp0
dotnet tool restore
dotnet fake run build.fsx %*
