#! "netcoreapp2.0"
#r "nuget: FluentAssertions, 4.19.4"
#load "../Dotnet.Build/FileUtils.csx"
#load "nuget:ScriptUnit, 0.1.3"
#load "TestUtils.csx"

using FluentAssertions;
using static FileUtils;
using static ScriptUnit;

//await AddTestsFrom<FileUtilsTests>().Execute();

public class FileUtilsTests
{
    public void ShouldCopyFolderContents()
    {
        using(var sourceFolder = new DisposableFolder())
        {
            File.WriteAllText(Path.Combine(sourceFolder.Path, "Test.txt"),"Test");
            using (var destinationFolder = new DisposableFolder())
            {
                Copy(sourceFolder.Path, destinationFolder.Path);
                File.Exists(Path.Combine(destinationFolder.Path,"Test.txt")).Should().BeTrue();                                
            }
        }
    }
    
    public void ShouldCopyFile()
    {
        const string sourceFileName = "Source.txt";
        const string destinationFileName = "Destination.txt";
        using(var sourceFolder = new DisposableFolder())
        {
            File.WriteAllText(Path.Combine(sourceFolder.Path, sourceFileName),"Test");
            using (var destinationFolder = new DisposableFolder())
            {
                var sourcePath = Path.Combine(sourceFolder.Path,sourceFileName);
                var destinationPath = Path.Combine(destinationFolder.Path,destinationFileName);
                Copy(sourcePath, destinationPath);
                File.Exists(Path.Combine(destinationFolder.Path,destinationFileName)).Should().BeTrue();                                
            }
        }
    }
}