#r "nuget: AwesomeAssertions, 9.3.0"
#load "../Dotnet.Build/FileUtils.csx"
#load "nuget:ScriptUnit, 0.1.3"
#load "TestUtils.csx"

using AwesomeAssertions;
using static FileUtils;
using static ScriptUnit;
using DisposableFolder = FileUtils.DisposableFolder;
// await AddTestsFrom<FileUtilsTests>().AddFilter(m => m.IsDefined(typeof(OnlyThisAttribute), true)).Execute();
// await AddTestsFrom<FileUtilsTests>().Execute();

public class FileUtilsTests
{
    public void ShouldCopyFolderContents()
    {
        using (var sourceFolder = new DisposableFolder())
        {
            File.WriteAllText(Path.Combine(sourceFolder.Path, "Test.txt"), "Test");
            using (var destinationFolder = new DisposableFolder())
            {
                Copy(sourceFolder.Path, destinationFolder.Path);
                File.Exists(Path.Combine(destinationFolder.Path, "Test.txt")).Should().BeTrue();
            }
        }
    }

    // [OnlyThis]
    public void ShouldCopyFile()
    {
        const string sourceFileName = "Source.txt";
        const string destinationFileName = "Destination.txt";
        using (var sourceFolder = new DisposableFolder())
        {
            File.WriteAllText(Path.Combine(sourceFolder.Path, sourceFileName), "Test");
            using (var destinationFolder = new DisposableFolder())
            {
                var sourcePath = Path.Combine(sourceFolder.Path, sourceFileName);
                var destinationPath = Path.Combine(destinationFolder.Path, destinationFileName);
                Copy(sourcePath, destinationPath);
                File.Exists(Path.Combine(destinationFolder.Path, destinationFileName)).Should().BeTrue();
            }
        }
    }

    [OnlyThis]
    public void ShouldCopyFileIntoTargetDirectory()
    {
        const string sourceFileName = "Source.txt";
        using (var sourceFolder = new DisposableFolder())
        {
            File.WriteAllText(Path.Combine(sourceFolder.Path, sourceFileName), "Test");
            using (var destinationFolder = new DisposableFolder())
            {
                var sourcePath = Path.Combine(sourceFolder.Path, sourceFileName);
                Copy(sourcePath, destinationFolder.Path);
                File.Exists(Path.Combine(destinationFolder.Path, sourceFileName)).Should().BeTrue();
            }
        }
    }

    public void ShouldNotCopyExcludedFolders()
    {
        using (var sourceFolder = new DisposableFolder())
        {
            Directory.CreateDirectory(Path.Combine(sourceFolder.Path, "ExcludedFolder"));
            Directory.CreateDirectory(Path.Combine(sourceFolder.Path, "IncludedFolder"));

            using (var destinationFolder = new DisposableFolder())
            {
                Copy(sourceFolder.Path, destinationFolder.Path, new[] { "ExcludedFolder" });
                Directory.Exists(Path.Combine(destinationFolder.Path, "ExcludedFolder")).Should().BeFalse();
                Directory.Exists(Path.Combine(destinationFolder.Path, "IncludedFolder")).Should().BeTrue();
            }
        }
    }

    public void ShouldFindFile()
    {
        using (var sourceFolder = new DisposableFolder())
        {
            File.WriteAllText(Path.Combine(sourceFolder.Path, "Test.txt"), "Test");
            var result = FindFile(sourceFolder.Path, "Test.txt");
            result.Should().Be(Path.Combine(sourceFolder.Path, "Test.txt"));
        }
    }
}