#! "netcoreapp2.0"
#load "../src/Dotnet.Build/Command.csx"
#load "../src/Dotnet.Build/FileUtils.csx"
#load "../src/Dotnet.Build/NuGet.csx"

using static FileUtils;

var scriptFolder = GetScriptFolder();
var tempFolder = Path.Combine(scriptFolder,"tmp");
var contentFolder = CreateDirectory(tempFolder,"contentFiles","csx","any");

Copy(Path.Combine(scriptFolder,"..","src","Dotnet.Build"), contentFolder);
Copy(Path.Combine(scriptFolder,"Dotnet.Build.nuspec"),Path.Combine(tempFolder,"Dotnet.Build.nuspec"));

NuGet.Pack(tempFolder,Path.Combine(scriptFolder,"Artifacts","NuGet"));


// string pathToUnitTests = Path.Combine(scriptFolder,"..","src","ScriptUnit.Tests","ScriptUnitTests.csx");
// Command.Execute("dotnet", $"script {pathToUnitTests}");








