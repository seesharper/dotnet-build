#! "netcoreapp2.0"
#load "../Dotnet.Build.Tests/CommandTests.csx"
#load "../Dotnet.Build.Tests/GitTests.csx"
#load "../Dotnet.Build.Tests/DotNetTests.csx"
#load "../Dotnet.Build.Tests/LoggerTests.csx"
#load "../Dotnet.Build.Tests/FileUtilsTests.csx"
#load "nuget:ScriptUnit, 0.1.3"

using static ScriptUnit; 

return await 
     AddTestsFrom<CommandTests>()
    .AddTestsFrom<DotNetTests>()
    .AddTestsFrom<GitTests>()
    .AddTestsFrom<LoggerTests>()
    .AddTestsFrom<FileUtilsTests>()
    .Execute();