#load "../Dotnet.Build.Tests/CommandTests.csx"
#load "../Dotnet.Build.Tests/GitTests.csx"
#load "../Dotnet.Build.Tests/DotNetTests.csx"
#load "../Dotnet.Build.Tests/LoggerTests.csx"
#load "../Dotnet.Build.Tests/FileUtilsTests.csx"
#load "../Dotnet.Build.Tests/NuGetTests.csx"
#load "../Dotnet.Build.Tests/BuildContextTests.csx"
#load "../Dotnet.Build.Tests/InternalizerTests.csx"
#load "../Dotnet.Build.Tests/GitHub-ReleaseManagerTests.csx"
#load "../Dotnet.Build/BuildEnvironment.csx"

#load "nuget:ScriptUnit, 0.1.3"

using static ScriptUnit;



var testRunner = AddTestsFrom<CommandTests>()
    .AddTestsFrom<DotNetTests>()
    .AddTestsFrom<GitTests>()
    .AddTestsFrom<LoggerTests>()
    .AddTestsFrom<FileUtilsTests>()
    .AddTestsFrom<NuGetTests>()
    .AddTestsFrom<InternalizerTests>()
    .AddTestsFrom<BuildContextTests>();
if (BuildEnvironment.IsSecure)
{
    testRunner = testRunner.AddTestsFrom<ReleaseManagerTests>();
}

return await testRunner.Execute();