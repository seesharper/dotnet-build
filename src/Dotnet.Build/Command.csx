
using System.Diagnostics;
using System.Threading;
using System.Text.RegularExpressions;

public static class Command
{
    public static CommandResult Capture(string commandPath, string arguments, string workingDirectory = null, string stdin = null)
    {
        return CaptureAsync(commandPath, arguments, workingDirectory, stdin).GetAwaiter().GetResult();
    }

    public static async Task<CommandResult> CaptureAsync(string commandPath, string arguments, string workingDirectory = null, string stdin = null)
    {
        Error.WriteLine($"Executing command (CaptureAsync) {commandPath} {arguments} in working directory {workingDirectory}");

        var process = CreateProcess(commandPath, arguments, workingDirectory, stdin != null);

        var startProcessTaskResult = await StartProcessAsync(process, echo: false, stdin: stdin);
        var readStandardOutputTask = process.StandardOutput.ReadToEndAsync();
        var readStandardErrorTask = process.StandardError.ReadToEndAsync();

        await Task.WhenAll(readStandardOutputTask, readStandardErrorTask).ConfigureAwait(false);
        return new CommandResult(startProcessTaskResult, readStandardOutputTask.Result, readStandardErrorTask.Result);
    }

    public static async Task ExecuteAsync(string commandPath, string arguments, string workingDirectory = null, int success = 0, string stdin = null)
    {
        Error.WriteLine($"Executing command {commandPath} {arguments} in working directory {workingDirectory}");
        var process = CreateProcess(commandPath, arguments, workingDirectory, stdin != null);
        RedirectToConsole(process);
        var exitCode = await StartProcessAsync(process, echo: true, stdin: stdin);
        if (exitCode != success)
        {
            throw new InvalidOperationException($"The command {commandPath} {arguments} failed.");
        }
    }

    public static void Execute(string commandPath, string arguments, string workingDirectory = null, int success = 0, string stdin = null)
    {
        ExecuteAsync(commandPath, arguments, workingDirectory, success, stdin).Wait();
    }

    private static void RedirectToConsole(Process process)
    {
        process.OutputDataReceived += (o, a) => WriteStandardOut(a);
        process.ErrorDataReceived += (o, a) => WriteStandardError(a);
        void WriteStandardOut(DataReceivedEventArgs args)
        {
            if (args.Data != null)
            {
                Out.WriteLine(args.Data);
            }
        }

        void WriteStandardError(DataReceivedEventArgs args)
        {
            if (args.Data != null)
            {
                Error.WriteLine(args.Data);
            }
        }
    }

    private static Process CreateProcess(string commandPath, string arguments, string workingDirectory, bool redirectStdin = false)
    {
        var startInformation = new ProcessStartInfo($"{commandPath}");
        startInformation.CreateNoWindow = true;
        startInformation.Arguments = arguments;
        startInformation.RedirectStandardOutput = true;
        startInformation.RedirectStandardError = true;
        startInformation.RedirectStandardInput = redirectStdin;
        startInformation.UseShellExecute = false;
        startInformation.WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory;
        var process = new Process();
        process.StartInfo = startInformation;
        return process;
    }

    private static async Task<int> StartProcessAsync(Process process, bool echo, string stdin = null)
    {
        var tcs = new TaskCompletionSource<int>();
        process.Exited += (o, s) => tcs.SetResult(process.ExitCode);
        process.EnableRaisingEvents = true;
        process.Start();
        if (stdin != null)
        {
            await process.StandardInput.WriteLineAsync(stdin);
            process.StandardInput.Close();
        }
        if (echo)
        {
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }
        return await tcs.Task;
    }
}

public class CommandResult
{
    public CommandResult(int exitCode, string standardOut, string standardError)
    {
        ExitCode = exitCode;
        StandardOut = standardOut;
        StandardError = standardError;
    }
    public string StandardOut { get; }
    public string StandardError { get; }
    public int ExitCode { get; }

    public CommandResult Dump()
    {
        Out.Write(StandardOut);
        Error.Write(StandardError);
        return this;
    }

    public CommandResult EnsureSuccessfulExitCode(int success = 0)
    {
        if (ExitCode != success)
        {
            throw new InvalidOperationException(StandardError);
        }
        return this;
    }
}