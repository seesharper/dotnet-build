#r "nuget: AwesomeAssertions, 9.3.0"
#load "../Dotnet.Build/Logger.csx"
#load "nuget:ScriptUnit, 0.2.0"

using AwesomeAssertions;
using static ScriptUnit;

//await AddTestsFrom<LoggerTests>().Execute();

public class LoggerTests
{
    public void ShouldLogToStandardError()
    {
        Logger.Log("This is a log message");
        TestContext.StandardError.Should().Be("This is a log message" + Environment.NewLine);
    }
}

