#r "nuget: AwesomeAssertions, 9.3.0"
#load "../Dotnet.Build/Command.csx"
#load "nuget:ScriptUnit, 0.2.0"
#load "TestUtils.csx"

using AwesomeAssertions;
using static ScriptUnit;

//await AddTestsFrom<CommandTests>().AddFilter(m => m.IsDefined(typeof(OnlyThisAttribute), true)).Execute();

//await AddTestsFrom<CommandTests>().Execute();

public class CommandTests
{
    public void ShouldCaptureStandardOut()
    {
        var result = Command.Capture("dotnet", "--info");
        result.StandardOut.Should().NotBeEmpty();
        TestContext.StandardOut.Should().BeEmpty();
    }


    public async Task ShouldCaptureStandardOutAsync()
    {
        var result = await Command.CaptureAsync("dotnet", "--info");
        result.StandardOut.Should().NotBeEmpty();
        TestContext.StandardOut.Should().BeEmpty();
    }


    public void ShouldExecuteAndOutputStandardOut()
    {
        Command.Execute("dotnet", "--info");
        TestContext.StandardOut.Should().NotBeEmpty();
    }
    public void ShouldCaptureStandardError()
    {
        var result = Command.Capture("dotnet", "invalidargument");
        result.StandardError.Should().NotBeEmpty();
    }

    public void ShouldReturnZeroExitCode()
    {
        var result = Command.Capture("dotnet", "--info");
        result.ExitCode.Should().Be(0);
    }


    public void ShouldReturnNonZeroExitCode()
    {
        var result = Command.Capture("dotnet", "invalidargument");
        result.ExitCode.Should().NotBe(0);
    }


    public void ShouldDumpStandardOut()
    {
        Command.Capture("dotnet", "--info").Dump();
        TestContext.StandardOut.Should().NotBeEmpty();
    }


    public void ShouldDumpStandardError()
    {
        Command.Capture("dotnet", "invalidargument").Dump();
        TestContext.StandardError.Should().NotBeEmpty();
    }

    [OnlyThis]
    public void ShouldNotThrowWhenExitCodeIsSuccessful()
    {
        var result = Command.Capture("dotnet", "--info");
        result.Invoking(r => r.EnsureSuccessfulExitCode()).Should().NotThrow();
    }

    // public void ShouldThrowWhenExitCodeIsSuccessful()
    // {
    //     var result = Command.Execute("dotnet", "invalidargument");
    //     result.Invoking(r => r.EnsureSuccessfulExitCode()).ShouldThrow<InvalidOperationException>();
    // }
}

