#! "netcoreapp2.0"
#load "../Dotnet.Build.Tests/CommandTests.csx"
#load "../Dotnet.Build.Tests/GitTests.csx"
#load "nuget:ScriptUnit, 0.1.1"

using static ScriptUnit; 

// await AddTestsFrom<CommandTests>().AddTestsFrom<GitTests>().Execute();