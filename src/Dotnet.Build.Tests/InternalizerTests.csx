#r "nuget: FluentAssertions, 5.6.0"
#load "../Dotnet.Build/FileUtils.csx"
#load "../Dotnet.Build/Internalizer.csx"
#load "nuget:ScriptUnit, 0.1.3"
#load "TestUtils.csx"

using static FileUtils;
using static ScriptUnit;
using FluentAssertions;

// await AddTestsFrom<InternalizerTests>().AddFilter(m => m.IsDefined(typeof(OnlyThisAttribute), true)).Execute();
// await AddTestsFrom<InternalizerTests>().Execute();
public class InternalizerTests
{
    public void ShouldInternalizeInterface()
    {
        string source =
        @"
        public interface Foo
        {

        }";

        Internalize(source).Should().Contain("internal interface Foo");
    }

    public void ShouldInternalizeClass()
    {
        string source =
        @"
        public class Foo
        {

        }";

        Internalize(source).Should().Contain("internal class Foo");
    }

    public void ShouldInternalizeStruct()
    {
        string source =
        @"
        public struct Foo
        {

        }";

        Internalize(source).Should().Contain("internal struct Foo");

    }

    public void ShouldInternalizeEnum()
    {
        string source =
        @"
        public enum Foo
        {

        }";

        Internalize(source).Should().Contain("internal enum Foo");

    }

    public void ShouldInternalizeDelegate()
    {
        string source = "public delegate object SomeDelegate()";

        Internalize(source).Should().Contain("internal delegate object SomeDelegate()");
    }

    [OnlyThis]
    public void ShouldAddExcludeFromCodeCoverageAttribute()
    {
        string source =
        @"
        public class Foo
        {

        }";

        Internalize(source).ReadLines()[1].Should().Contain("[ExcludeFromCodeCoverage]");
    }

    private string Internalize(string source)
    {
        using (var disposableFolder = new DisposableFolder())
        {
            string fileName = Path.Combine(disposableFolder.Path, "source.cs");
            WriteFile(fileName, source);
            Internalizer.Internalize(disposableFolder.Path);
            return ReadFile(fileName);
        }
    }
}