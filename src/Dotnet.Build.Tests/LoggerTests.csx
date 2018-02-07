#! "netcoreapp2.0"
#r "nuget: FluentAssertions, 4.19.4"
#load "../Dotnet.Build/Logger.csx"
#load "nuget:ScriptUnit, 0.1.1"

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

