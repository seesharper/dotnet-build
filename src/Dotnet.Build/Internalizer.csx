#load "FileUtils.csx"
#load "Logger.csx"
using System.Text.RegularExpressions;
public static class Internalizer
{
    public static void Internalize(string pathToProjectFolder, params string[] exceptTheseTypes)
    {
        var sourceFiles = Directory.GetFiles(pathToProjectFolder, "*.cs", SearchOption.AllDirectories);
        foreach (var sourceFile in sourceFiles)
        {
            InternalizeFile(sourceFile, exceptTheseTypes);
        }
    }

    private static void InternalizeFile(string pathToSourceFile, string[] exceptTheseTypes)
    {
        Logger.Log($"Internalizing {pathToSourceFile}");
        var source = FileUtils.ReadFile(pathToSourceFile);
               
        string negativeLookahead = string.Empty;                        
        if (exceptTheseTypes != null)
        {
            foreach (var type in exceptTheseTypes)
            {
                negativeLookahead += string.Format("(?!{0})", type);
            }
        }
        
        // Make all public classes internal
        source = Regex.Replace(source, "public (.*class |struct |interface |enum )" + negativeLookahead, "internal $1");
                        
        // Exclude classes from code coverage
        source = Regex.Replace(source, @"(([^\S\r\n]*)internal.*class.*)", "$2[ExcludeFromCodeCoverage]\r\n$1");
                                
        FileUtils.WriteFile(pathToSourceFile, source);
    }
}