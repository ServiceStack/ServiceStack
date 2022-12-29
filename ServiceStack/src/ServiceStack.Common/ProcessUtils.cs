using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using ServiceStack.Text;

namespace ServiceStack
{
    /// <summary>
    /// Async Process Helper
    /// - https://gist.github.com/Indigo744/b5f3bd50df4b179651c876416bf70d0a
    /// </summary>
    public static class ProcessUtils
    {
        /// <summary>
        /// .NET Core / Win throws Win32Exception (193): The specified executable is not a valid application for this OS platform.
        /// This method converts it to a cmd.exe /c execute to workaround this 
        /// </summary>
        public static ProcessStartInfo ConvertToCmdExec(this ProcessStartInfo startInfo)
        {
            var to = new ProcessStartInfo {
                FileName = Env.IsWindows 
                    ? "cmd.exe"
                    : "/bin/bash",
                WorkingDirectory = startInfo.WorkingDirectory,
                Arguments = Env.IsWindows
                    ? $"/c \"\"{startInfo.FileName}\" {startInfo.Arguments}\""
                    : $"-c \"{startInfo.FileName} {startInfo.Arguments}\"",
            };
            return to;
        }

        /// <summary>
        /// Returns path of executable if exists within PATH
        /// </summary>
        public static string FindExePath(string exeName)
        {
            try
            {
                var p = new Process
                {
                    StartInfo =
                    {
                        UseShellExecute = false,
                        FileName = Env.IsWindows 
                            ? "where"  //Win 7/Server 2003+
                            : "which", //macOS / Linux
                        Arguments = exeName,
                        RedirectStandardOutput = true
                    }
                };
                p.Start();
                var output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();

                if (p.ExitCode == 0)
                {
                    // just return first match
                    var fullPath = output.Substring(0, output.IndexOf(Environment.NewLine, StringComparison.Ordinal));
                    if (!string.IsNullOrEmpty(fullPath))
                    {
                        return fullPath;
                    }
                }
            }
            catch {}               
            return null;
        }
        
        /// <summary>
        /// Run the command with the OS's command runner 
        /// </summary>
        public static string RunShell(string arguments, string workingDir=null)
        {
            if (string.IsNullOrEmpty(arguments))
                throw new ArgumentNullException(nameof(arguments));

            if (Env.IsWindows)
            {
                var cmdArgs = "/C " + arguments; 
                return Run("cmd.exe", cmdArgs, workingDir);
            }
            else
            {
                var escapedArgs = arguments.Replace("\"", "\\\"");
                var cmdArgs = $"-c \"{escapedArgs}\"";
                return Run("/bin/bash", cmdArgs, workingDir);
            }
        }
        
        /// <summary>
        /// Run the process and return the Standard Output, any Standard Error output will throw an Exception
        /// </summary>
        public static string Run(string fileName, string arguments=null, string workingDir=null)
        {
            var process = CreateProcess(fileName, arguments, workingDir);
            using (process)
            {
                process.StartInfo.RedirectStandardError = true;
                process.Start();

                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();

                process.WaitForExit();
                process.Close();

                if (!string.IsNullOrEmpty(error))
                    throw new Exception($"`{fileName} {process.StartInfo.Arguments}` command failed, stderr: " + error + ", stdout:" + output);

                return output;
            }
        }

        public static Process CreateProcess(string fileName, string arguments, string workingDir)
        {
            var process = new Process
            {
                StartInfo =
                {
                    FileName = fileName,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                }
            };
            if (arguments != null)
                process.StartInfo.Arguments = arguments;

            if (workingDir != null)
                process.StartInfo.WorkingDirectory = workingDir;
            return process;
        }
        
        /// <summary>
        /// Run the command with the OS's command runner 
        /// </summary>
        public static async Task RunShellAsync(string arguments, string workingDir=null, int? timeoutMs = null,
            Action<string> onOut=null, Action<string> onError=null)
        {
            if (string.IsNullOrEmpty(arguments))
                throw new ArgumentNullException(nameof(arguments));

            if (Env.IsWindows)
            {
                var cmdArgs = "/C " + arguments; 
                await RunAsync(CreateProcess("cmd.exe", cmdArgs, workingDir).StartInfo, timeoutMs, onOut, onError);
            }
            else
            {
                var escapedArgs = arguments.Replace("\"", "\\\"");
                var cmdArgs = $"-c \"{escapedArgs}\"";
                await RunAsync(CreateProcess("/bin/bash", cmdArgs, workingDir).StartInfo, timeoutMs, onOut, onError);
            }
        }

        /// <summary>
        /// Run a Process asynchronously, returning  entire captured process output, whilst streaming stdOut, stdErr callbacks
        /// </summary>
        public static async Task<ProcessResult> RunAsync(ProcessStartInfo startInfo, int? timeoutMs = null,
            Action<string> onOut=null, Action<string> onError=null)
        {
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            
            using var process = new Process 
            {
                StartInfo = startInfo, 
                EnableRaisingEvents = true,
            };
            
            // List of tasks to wait for a whole process exit
            var processTasks = new List<Task>();

            // === EXITED Event handling ===
            var processExitEvent = new TaskCompletionSource<object>();
            process.Exited += (sender, args) => {
                processExitEvent.TrySetResult(true);
            };
            processTasks.Add(processExitEvent.Task);

            long callbackTicks = 0;

            // === STDOUT handling ===
            var stdOutBuilder = StringBuilderCache.Allocate();
            var stdOutCloseEvent = new TaskCompletionSource<bool>();
            process.OutputDataReceived += (s, e) => {
                if (e.Data == null)
                {
                    stdOutCloseEvent.TrySetResult(true);
                }
                else
                {
                    stdOutBuilder.AppendLine(e.Data);
                    if (onOut != null)
                    {
                        var swCallback = Stopwatch.StartNew();
                        onOut(e.Data);
                        callbackTicks += swCallback.ElapsedTicks;
                    }
                }
            };

            processTasks.Add(stdOutCloseEvent.Task);

            // === STDERR handling ===
            var stdErrBuilder = StringBuilderCacheAlt.Allocate();
            var stdErrCloseEvent = new TaskCompletionSource<bool>();
            process.ErrorDataReceived += (s, e) => {
                if (e.Data == null)
                {
                    stdErrCloseEvent.TrySetResult(true);
                }
                else
                {
                    stdErrBuilder.AppendLine(e.Data);
                    if (onError != null)
                    {
                        var swCallback = Stopwatch.StartNew();
                        onError(e.Data);
                        callbackTicks += swCallback.ElapsedTicks;
                    }
                }
            };

            processTasks.Add(stdErrCloseEvent.Task);

            // === START OF PROCESS ===
            var sw = Stopwatch.StartNew();
            var result = new ProcessResult {
                StartAt = DateTime.UtcNow,
            };
            if (!process.Start())
            {
                result.ExitCode = process.ExitCode;
                return result;
            }

            // Reads the output stream first as needed and then waits because deadlocks are possible
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            // === ASYNC WAIT OF PROCESS ===

            // Process completion = exit AND stdout (if defined) AND stderr (if defined)
            var processCompletionTask = Task.WhenAll(processTasks);

            // Task to wait for exit OR timeout (if defined)
            var awaitingTask = timeoutMs.HasValue
                ? Task.WhenAny(Task.Delay(timeoutMs.Value), processCompletionTask)
                : Task.WhenAny(processCompletionTask);

            // Let's now wait for something to end...
            if ((await awaitingTask.ConfigureAwait(false)) == processCompletionTask)
            {
                // -> Process exited cleanly
                result.ExitCode = process.ExitCode;
            }
            else
            {
                // -> Timeout, let's kill the process
                try
                {
                    process.Kill();
                }
                catch
                {
                    // ignored
                }
            }

            // Read stdout/stderr
            result.EndAt = DateTime.UtcNow;
            if (callbackTicks > 0)
            {
                var callbackMs = (callbackTicks / Stopwatch.Frequency) * 1000;
                result.CallbackDurationMs = callbackMs;
                result.DurationMs = sw.ElapsedMilliseconds - callbackMs;
            }
            else
            {
                result.DurationMs = sw.ElapsedMilliseconds;
            }
            result.StdOut = StringBuilderCache.ReturnAndFree(stdOutBuilder);
            result.StdErr = StringBuilderCacheAlt.ReturnAndFree(stdErrBuilder);

            return result;
        }
        public static ProcessResult CreateErrorResult(Exception e)
        {
            return new ProcessResult {
                StartAt = DateTime.UtcNow,
                EndAt = DateTime.UtcNow,
                DurationMs = 0,
                CallbackDurationMs = 0,
                StdErr = e.ToString(),
                ExitCode = -1,
            };
        }
    }
    
    /// <summary>
    /// Run process result
    /// </summary>
    public class ProcessResult
    {
        /// <summary>
        /// Exit code
        /// <para>If NULL, process exited due to timeout</para>
        /// </summary>
        public int? ExitCode { get; set; } = null;

        /// <summary>
        /// Standard error stream
        /// </summary>
        public string StdErr { get; set; }

        /// <summary>
        /// Standard output stream
        /// </summary>
        public string StdOut { get; set; }
            
        /// <summary>
        /// UTC Start
        /// </summary>
        public DateTime StartAt { get; set; }
            
        /// <summary>
        /// UTC End
        /// </summary>
        public DateTime EndAt { get; set; }
            
        /// <summary>
        /// Duration (ms)
        /// </summary>
        public long DurationMs { get; set; }
            
        /// <summary>
        /// Duration (ms)
        /// </summary>
        public long? CallbackDurationMs { get; set; }
    }

}