#! "netcoreapp2.0"
#r "nuget: FluentAssertions, 4.19.4"
#load "../Dotnet.Build/Command.csx"
#load "nuget:ScriptUnit, 0.1.1"


using FluentAssertions;
using static ScriptUnit; 

await AddTestsFrom<CommandTests>().Execute();

public class CommandTests
{
    public void ShouldCaptureStandardOut()
    {
        var result = Command.Execute("dotnet","--info");
        result.StandardOut.Should().Contain(".NET Command Line Tools");
    }

    public void ShouldCaptureStandardError()
    {        
        var result = Command.Execute("dotnet", "invalidargument");                
        result.StandardError.Should().Contain("No executable found matching command \"dotnet-invalidargument\"");
    }

    public void ShouldReturnZeroExitCode()
    {
        var result = Command.Execute("dotnet","--info");
        result.ExitCode.Should().Be(0);
    }

    public void ShouldReturnNonZeroExitCode()
    {
        var result = Command.Execute("dotnet", "invalidargument");                
        result.ExitCode.Should().NotBe(0);
    }

    public void ShouldDumpStandardOut()
    {
        Command.Execute("dotnet","--info").Dump();
        TestContext.StandardOut.Should().Contain(".NET Command Line Tools");
    }

    public void ShouldDumpStandardError()
    {
        Command.Execute("dotnet", "invalidargument").Dump();
        TestContext.StandardError.Should().Contain("No executable found matching command \"dotnet-invalidargument\"");
    }  

    public void ShouldNotThrowWhenExitCodeIsSuccessful()
    {
        var result = Command.Execute("dotnet","--info");
        result.Invoking(r => r.EnsureSuccessfulExitCode()).ShouldNotThrow();
    }

    // public void ShouldThrowWhenExitCodeIsSuccessful()
    // {
    //     var result = Command.Execute("dotnet", "invalidargument");  
    //     result.Invoking(r => r.EnsureSuccessfulExitCode()).ShouldThrow<InvalidOperationException>();
    // }
}

