using System.IO.Compression;
using System.Runtime.CompilerServices;



public enum PathType
{
    File,
    Directory
}

public static class FileUtils
{
    public static string GetScriptPath([CallerFilePath] string path = null) => path;
    public static string GetScriptFolder([CallerFilePath] string path = null) => Path.GetDirectoryName(path);


    public static void Zip(string sourceDirectoryName, string pathToZipfile)
    {
        ZipFile.CreateFromDirectory(sourceDirectoryName, pathToZipfile);
    }

    public static string ReadFile(string pathToFile)
    {
        using (var fileStream = new FileStream(pathToFile, FileMode.Open, FileAccess.Read))
        {
            using (var reader = new StreamReader(fileStream))
            {
                return reader.ReadToEnd();
            }
        }
    }

    public static void WriteFile(string pathToFile, string content)
    {
        using (var fileStream = new FileStream(pathToFile, FileMode.Create))
        {
            using (var writer = new StreamWriter(fileStream))
            {
                writer.Write(content);
            }
        }

    }

    public static PathType GetPathType(string path)
    {
        FileAttributes attr = File.GetAttributes(path);

        if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
        {
            return PathType.Directory;
        }

        return PathType.File;
    }

    public static void Copy(string sourcePath, string targetPath, string[] excludeFolders = null)
    {
        if (excludeFolders == null)
        {
            excludeFolders = Array.Empty<string>();
        }
        var sourcePathType = GetPathType(sourcePath);
        if (sourcePathType == PathType.File)
        {
            var targetFolder = Path.GetDirectoryName(targetPath);
            Directory.CreateDirectory(targetFolder);
            File.Copy(sourcePath, targetPath, true);
        }
        else
        {
            Directory.CreateDirectory(targetPath);

            foreach (var file in Directory.GetFiles(sourcePath))
            {
                File.Copy(file, Path.Combine(targetPath, Path.GetFileName(file)));
            }

            foreach (var directory in Directory.GetDirectories(sourcePath))
            {                
                var directoryName = Path.GetFileName(directory);
                if (!excludeFolders.Contains(directoryName))
                {
                    Copy(directory, Path.Combine(targetPath, Path.GetFileName(directory)));
                }                
            }
        }
    }

    public static string CreateDirectory(params string[] paths)
    {
        var pathToDirectory = Path.Combine(paths);
        RemoveDirectory(pathToDirectory);
        Directory.CreateDirectory(pathToDirectory);
        return pathToDirectory;
    }

    public static void RemoveDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            return;
        }
        NormalizeAttributes(path);

        foreach (string directory in Directory.GetDirectories(path))
        {
            RemoveDirectory(directory);
        }

        try
        {
            Directory.Delete(path, true);
        }
        catch (IOException)
        {
            Directory.Delete(path, true);
        }
        catch (UnauthorizedAccessException)
        {
            Directory.Delete(path, true);
        }

        void NormalizeAttributes(string directoryPath)
        {
            string[] filePaths = Directory.GetFiles(directoryPath);
            string[] subdirectoryPaths = Directory.GetDirectories(directoryPath);

            foreach (string filePath in filePaths)
            {
                File.SetAttributes(filePath, FileAttributes.Normal);
            }
            foreach (string subdirectoryPath in subdirectoryPaths)
            {
                NormalizeAttributes(subdirectoryPath);
            }
            File.SetAttributes(directoryPath, FileAttributes.Normal);
        }
    }

    public class DisposableFolder : IDisposable
    {
        public DisposableFolder()
        {
            var tempFolder = System.IO.Path.GetTempPath();
            this.Path = System.IO.Path.Combine(tempFolder, System.IO.Path.GetFileNameWithoutExtension(System.IO.Path.GetTempFileName()));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            FileUtils.RemoveDirectory(Path);
        }
    }
}