#r "nuget: FluentAssertions, 5.6.0"
#load "../Dotnet.Build/Command.csx"
#load "../Dotnet.Build/DotNet.csx"
#load "nuget:ScriptUnit, 0.1.3"
#load "TestUtils.csx"


using FluentAssertions;
using static ScriptUnit;
using static FileUtils;
using System.Xml.Linq;

//gst await AddTestsFrom<DotNetTests>().AddFilter(m => m.IsDefined(typeof(OnlyThisAttribute), true)).Execute();

//await AddTestsFrom<DotNetTests>().Execute();

public class DotNetTests
{
    public void ShouldBuildProject()
    {
        using (var projectFolder = new DisposableFolder())
        {
            Command.Execute("dotnet", $"new console -o {projectFolder.Path}");
            DotNet.Build(projectFolder.Path);
        }
    }


    public void ShouldPackProjectWithCommitHash()
    {
        using (var projectFolder = new DisposableFolder())
        {
            Command.Execute("dotnet", $"new console -o {projectFolder.Path}");
            DotNet.Pack(projectFolder.Path, "123");
        }
    }

    public void ShouldExecuteTests()
    {
        using (var projectFolder = new DisposableFolder())
        {
            Command.Execute("dotnet", $"new xunit -o {projectFolder.Path}");
            DotNet.Test(projectFolder.Path);
        }
    }

    //[OnlyThis]
    public void ShouldAnalyzeCodeCoverageUsingCoverletAndReportGenerator()
    {
        using (var solutionFolder = new DisposableFolder())
        {
            var srcFolder = CreateDirectory(solutionFolder.Path, "src");
            var buildFolder = CreateDirectory(solutionFolder.Path, "build");
            var artifactsFolder = CreateDirectory(buildFolder, "Artifacts");
            var projectFolder = CreateDirectory(srcFolder, "SampleProject");
            var testFolder = CreateDirectory(srcFolder, "SampleProjects.Tests");
            Command.Execute("dotnet", $"new classlib", projectFolder);
            Command.Execute("dotnet", $"new xunit", testFolder);
            Command.Execute("dotnet", "add package coverlet.collector", testFolder);
            Command.Execute("dotnet", "add reference ../SampleProject", testFolder);
            DotNet.TestWithCodeCoverage(testFolder, artifactsFolder, 100);
        }
    }

    //[OnlyThis]
    public void ShouldReport100PercentCodeCoverageWithCodeAnnotatedWithGeneratedCodeAttribute()
    {
        using (var solutionFolder = new DisposableFolder())
        {
            var srcFolder = CreateDirectory(solutionFolder.Path, "src");
            var buildFolder = CreateDirectory(solutionFolder.Path, "build");
            var artifactsFolder = CreateDirectory(buildFolder, "Artifacts");
            var projectFolder = CreateDirectory(srcFolder, "SampleProject");
            var testFolder = CreateDirectory(srcFolder, "SampleProjects.Tests");
            Command.Execute("dotnet", $"new classlib", projectFolder);
            Command.Execute("dotnet", $"new xunit", testFolder);
            Command.Execute("dotnet", "add package coverlet.collector", testFolder);
            Command.Execute("dotnet", "add reference ../SampleProject", testFolder);
            var sourceFile = File.CreateText(Path.Combine(projectFolder, "MyClass.cs"));
            sourceFile.WriteLine("using System.CodeDom.Compiler;");
            sourceFile.WriteLine("using System.Diagnostics.CodeAnalysis;");
            sourceFile.WriteLine("[GeneratedCode(\"\", \"\")]");
            sourceFile.WriteLine("public class MyClass { public void MyMethod() { Console.WriteLine(42);} }");
            sourceFile.WriteLine("[ExcludeFromCodeCoverage]");
            sourceFile.WriteLine("public class MyClass2 { public void MyMethod() { Console.WriteLine(42);} }");
            sourceFile.Close();
            DotNet.TestWithCodeCoverage(testFolder, artifactsFolder, 100);
        }
    }

    public void ShouldReport100PercentCodeCoverageForSolutionWithCoverageSplitAcrossMultipleProjects()
    {
        using (var rootFolder = new DisposableFolder())
        {
            BuildContext.CodeCoverageThreshold = 100;
            var srcFolder = CreateDirectory(rootFolder.Path, "src");
            var buildFolder = CreateDirectory(rootFolder.Path, "build");
            var artifactsFolder = CreateDirectory(buildFolder, "Artifacts");
            var firstProjectFolder = CreateDirectory(srcFolder, "SampleProject1");
            var secondProjectFolder = CreateDirectory(srcFolder, "SampleProject2");
            var firstTestProjectFolder = CreateDirectory(srcFolder, "SampleProject1.Tests");
            var secondTestProjectFolder = CreateDirectory(srcFolder, "SampleProject2.Tests");
            Command.Execute("dotnet", "new classlib", firstProjectFolder);
            Command.Execute("dotnet", "new classlib", secondProjectFolder);
            Command.Execute("dotnet", "add reference ../SampleProject1", secondProjectFolder);
            Command.Execute("dotnet", "new xunit", firstTestProjectFolder);
            Command.Execute("dotnet", "add package coverlet.collector", firstTestProjectFolder);
            Command.Execute("dotnet", "add reference ../SampleProject1", firstTestProjectFolder);
            Command.Execute("dotnet", "new xunit", secondTestProjectFolder);
            Command.Execute("dotnet", "add package coverlet.collector", secondTestProjectFolder);
            Command.Execute("dotnet", "add reference ../SampleProject2", secondTestProjectFolder);
            Command.Execute("dotnet", "new sln", srcFolder);
            Command.Execute("dotnet", "sln add SampleProject1", srcFolder);
            Command.Execute("dotnet", "sln add SampleProject2", srcFolder);
            Command.Execute("dotnet", "sln add SampleProject1.Tests", srcFolder);
            Command.Execute("dotnet", "sln add SampleProject2.Tests", srcFolder);
            var templatesFolder = Path.Combine(GetScriptFolder(), "templates");
            var firstSourceFilePath = Path.Combine(firstProjectFolder, "FirstTestableClass.cs");
            Copy(Path.Combine(templatesFolder, "FirstTestableClass.template"), firstSourceFilePath);
            var secondSourceFilePath = Path.Combine(secondProjectFolder, "SecondTestableClass.cs");
            Copy(Path.Combine(templatesFolder, "SecondTestableClass.template"), secondSourceFilePath);
            var firstTestSourceFilePath = Path.Combine(firstTestProjectFolder, "FirstTestableClassTests.cs");
            Copy(Path.Combine(templatesFolder, "FirstTestableClassTests.template"), firstTestSourceFilePath);
            var secondTestSourceFilePath = Path.Combine(secondTestProjectFolder, "SecondTestableClassTests.cs");
            Copy(Path.Combine(templatesFolder, "SecondTestableClassTests.template"), secondTestSourceFilePath);

            DotNet.TestSolutionWithCodeCoverage(Path.Combine(srcFolder, "src.sln"), artifactsFolder, 100);
        }
    }

    public void ShouldExecuteScriptTests()
    {
        using (var projectFolder = new DisposableFolder())
        {
            var pathToScriptUnitTests = Path.Combine(projectFolder.Path, "ScriptUnitTests.csx");
            Copy(Path.Combine(GetScriptFolder(), "templates", "ScriptUnitTests.template"), pathToScriptUnitTests);
            DotNet.Test(pathToScriptUnitTests);
        }
    }

    public void ShouldPublishProject()
    {
        using (var projectFolder = new DisposableFolder())
        {
            Command.Execute("dotnet", $"new console -o {projectFolder.Path}");
            DotNet.Publish(projectFolder.Path);
        }
    }


    public void ShouldPublishToGivenFolder()
    {
        using (var projectFolder = new DisposableFolder())
        {
            using (var outputFolder = new DisposableFolder())
            {
                Command.Execute("dotnet", $"new console -o {projectFolder.Path}");
                DotNet.Publish(projectFolder.Path, outputFolder.Path);
                Directory.GetFiles(outputFolder.Path, "*.dll").Should().NotBeEmpty();
            }
        }
    }

    //[OnlyThis]
    public void ShouldPackWithTagVersionWhenVersionAttributeIsMissing()
    {
        using (var projectFolder = new DisposableFolder())
        {
            Command.Execute("dotnet", $"new classlib -n TestLib -o {projectFolder.Path}", projectFolder.Path);
            DotNet.Pack(projectFolder.Path, projectFolder.Path);

            string pathToNugetFile = FindFile(projectFolder.Path, "*.nupkg");
            Path.GetFileName(pathToNugetFile).Should().Be($"TestLib.{BuildContext.LatestTag}.nupkg");
        }
    }

    //[OnlyThis]
    public void ShouldPackWithVersionWhenVersionIsSpecified()
    {
        using (var projectFolder = new DisposableFolder())
        {
            Command.Execute("dotnet", $"new classlib -n TestLib -o {projectFolder.Path}", projectFolder.Path);


            var projectFile = FindFile(projectFolder.Path, "*.csproj");
            var document = XDocument.Load(projectFile);
            var propertyGroupElement = document.Descendants("PropertyGroup").Single();
            var isPublishableElement = new XElement("Version", "0.0.1");
            propertyGroupElement.Add(isPublishableElement);
            document.Save(projectFile);
            DotNet.Pack(projectFolder.Path, projectFolder.Path);

            string pathToNugetFile = FindFile(projectFolder.Path, "*.nupkg");


            Path.GetFileName(pathToNugetFile).Should().Be($"TestLib.0.0.1.nupkg");
        }
    }

    //[OnlyThis]
    public void ShouldPublishWhenProjectIsPublishable()
    {
        using (var solutionFolder = new DisposableFolder())
        {
            var srcFolder = CreateDirectory(solutionFolder.Path, "src");
            var buildFolder = CreateDirectory(solutionFolder.Path, "build");
            var oldRepoFolder = BuildContext.RepositoryFolder;
            BuildContext.RepositoryFolder = solutionFolder.Path;
            using (var outputFolder = new DisposableFolder())
            {
                Command.Execute("dotnet", $"new console -o {srcFolder}");
                var projectFile = FindFile(srcFolder, "*.csproj");
                var document = XDocument.Load(projectFile);
                var propertyGroupElement = document.Descendants("PropertyGroup").Single();
                var isPublishableElement = new XElement("IsPublishable", true);
                propertyGroupElement.Add(isPublishableElement);
                document.Save(projectFile);
                DotNet.Publish();

                Directory.GetFiles(BuildContext.GitHubArtifactsFolder, "*.dll").Should().NotBeEmpty();
            }

            BuildContext.RepositoryFolder = oldRepoFolder;
        }
    }


    //[OnlyThis]
    public void ShouldNotPublishWhenProjectIsPublishableIsSetToFalse()
    {
        using (var solutionFolder = new DisposableFolder())
        {
            var srcFolder = CreateDirectory(solutionFolder.Path, "src");
            var buildFolder = CreateDirectory(solutionFolder.Path, "build");
            var oldRepoFolder = BuildContext.RepositoryFolder;
            BuildContext.RepositoryFolder = solutionFolder.Path;
            using (var outputFolder = new DisposableFolder())
            {
                Command.Execute("dotnet", $"new console -o {srcFolder}");
                var projectFile = FindFile(srcFolder, "*.csproj");
                var document = XDocument.Load(projectFile);
                var propertyGroupElement = document.Descendants("PropertyGroup").Single();
                var isPublishableElement = new XElement("IsPublishable", false);
                propertyGroupElement.Add(isPublishableElement);
                document.Save(projectFile);
                DotNet.Publish();

                Directory.GetFiles(BuildContext.GitHubArtifactsFolder, "*.dll").Should().BeEmpty();
            }

            BuildContext.RepositoryFolder = oldRepoFolder;
        }
    }

    //[OnlyThis]
    public void ShouldNotPublishWhenProjectIsPublishablePropertyIsMissing()
    {
        using (var solutionFolder = new DisposableFolder())
        {
            var srcFolder = CreateDirectory(solutionFolder.Path, "src");
            var buildFolder = CreateDirectory(solutionFolder.Path, "build");
            var oldRepoFolder = BuildContext.RepositoryFolder;
            BuildContext.RepositoryFolder = solutionFolder.Path;
            using (var outputFolder = new DisposableFolder())
            {
                Command.Execute("dotnet", $"new console -o {srcFolder}");
                var projectFile = FindFile(srcFolder, "*.csproj");
                DotNet.Publish();

                Directory.GetFiles(BuildContext.GitHubArtifactsFolder, "*.dll").Should().BeEmpty();
            }

            BuildContext.RepositoryFolder = oldRepoFolder;
        }
    }
}
