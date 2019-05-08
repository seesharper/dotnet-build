#load "../Dotnet.Build/FileUtils.csx"

using System.Collections.ObjectModel;

public static string[] ReadLines(this string value)
{
    Collection<string> result = new Collection<string>();
    var reader = new StringReader(value);
    while (reader.Peek() != -1)
    {
        result.Add(reader.ReadLine());
    }
    return result.ToArray();
}

public class OnlyThisAttribute : Attribute
{

}