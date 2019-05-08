#r "nuget: FluentAssertions, 5.6.0"
#load "../Dotnet.Build/Logger.csx"
#load "nuget:ScriptUnit, 0.1.3"

using FluentAssertions;
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

