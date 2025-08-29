using HMCodingWeb.ViewModels;
using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace HMCodingWeb.Services
{
    public class RunProcessService
    {
        private readonly long _capacityLimit;

        public RunProcessService(long capacityLimit = 10485760)
        {
            _capacityLimit = capacityLimit;
        }

        private string GetCompilerPath(string compiler)
        {
            if (compiler == "g++")
            {
                return "g++";
            }
            else if (compiler == "python")
            {
                return "python3";
            }
            else if (compiler == "fpc")
            {
                return "fpc";
            }
            else if (compiler == "node")
            {
                return "node";
            }
            return "python";
        }

        public bool ClearTempFolder(long userId)
        {
            try
            {
                var userDirectory = Path.Combine("Temp", userId.ToString());
                if (Directory.Exists(userDirectory))
                {
                    // Xóa tất cả file
                    var files = Directory.GetFiles(userDirectory);
                    foreach (var file in files)
                    {
                        File.Delete(file);
                    }

                    // Xóa tất cả thư mục con
                    var directories = Directory.GetDirectories(userDirectory);
                    foreach (var dir in directories)
                    {
                        Directory.Delete(dir, true);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clearing temp folder: {ex.Message}");
                return false;
            }
        }


        public async Task<RunProcessViewModel> RunProcessWithInput(RunProcessViewModel model, bool compile = true)
        {
            try
            {
                model.SourceCode = model.SourceCode?.Replace("system", ""); // Remove system calls for security reasons
                // Check Temp Folder
                if (!Directory.Exists("Temp"))
                {
                    Directory.CreateDirectory("Temp");
                }
                var userDirectory = Path.Combine("Temp", model.UserId.ToString());
                if (!Directory.Exists(userDirectory))
                {
                    Directory.CreateDirectory(userDirectory);
                }

                /**************************************************************
                 * 
                 *                 COMPILE AND RUN C++ CODE
                 * 
                 **************************************************************/
                if (model.ProgramLanguageId == 1)
                {
                    model.FileName = model.FileName + ".cpp";
                    var sourceFilePath = Path.Combine(userDirectory, model.FileName);
                    await File.WriteAllTextAsync(sourceFilePath, model.SourceCode);

                    if (compile)
                    {
                        model = await CompileCppFile(model, userDirectory);
                        if (model.IsError == true)
                        {
                            return model;
                        }
                    }

                    var runProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = Path.Combine(userDirectory, Path.ChangeExtension(model.FileName ?? "", "exe")),
                            RedirectStandardInput = true,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            WorkingDirectory = userDirectory
                        }
                    };

                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    runProcess.Start();

                    var stdoutTask = runProcess.StandardOutput.ReadToEndAsync();
                    var stderrTask = runProcess.StandardError.ReadToEndAsync();
                    Task processTask = Task.CompletedTask;

                    if (!string.IsNullOrEmpty(model.InputFile))
                    {
                        var inputFilePath = Path.Combine(userDirectory, model.InputFile);
                        await File.WriteAllTextAsync(inputFilePath, model.Input);

                        if (runProcess.StartInfo.RedirectStandardInput)
                        {
                            runProcess.StartInfo.Arguments = $" < {inputFilePath}";
                        }
                        processTask = Task.Run(() => runProcess.WaitForExit());
                    }
                    else if (!string.IsNullOrEmpty(model.Input))
                    {
                        processTask = Task.Run(async () =>
                        {
                            await runProcess.StandardInput.WriteLineAsync(model.Input);
                            await runProcess.StandardInput.FlushAsync();
                            runProcess.StandardInput.Close();
                            runProcess.WaitForExit();
                        });
                    }
                    else
                    {
                        processTask = Task.Run(() =>
                        {
                            runProcess.StandardInput.Close();
                            runProcess.WaitForExit();
                        });
                    }

                    // === Monitor Memory Usage in parallel ===
                    var monitorTask = Task.Run(async () =>
                    {
                        while (!runProcess.HasExited)
                        {
                            try
                            {
                                runProcess.Refresh();
                                var memoryMB = runProcess.WorkingSet64 / (1024.0 * 1024.0);
                                model.MemoryUsed = model.MemoryUsed > (float)memoryMB ? model.MemoryUsed : (float)memoryMB;

                                if (model.MemoryLimit.HasValue && memoryMB > model.MemoryLimit.Value)
                                {
                                    runProcess.Kill();
                                    model.Error = "Memory limit exceeded";
                                    model.IsError = true;
                                    break;
                                }
                            }
                            catch { }
                            await Task.Delay(50); // check mỗi 50ms
                        }
                    });

                    // === Wait with runtime limit ===
                    if (await Task.WhenAny(processTask, Task.Delay(model.TimeLimit * 1000 ?? 1000)) == processTask)
                    {
                        stopwatch.Stop();
                        model.RunTime = (float)stopwatch.Elapsed.TotalSeconds;

                        if (!string.IsNullOrEmpty(model.OutputFile))
                        {
                            var outputFilePath = Path.Combine(userDirectory, model.OutputFile);
                            runProcess.StartInfo.RedirectStandardOutput = false;
                            runProcess.StartInfo.RedirectStandardError = false;
                            runProcess.StartInfo.Arguments += $" > {outputFilePath}";

                            await runProcess.WaitForExitAsync();

                            if (File.Exists(outputFilePath))
                            {
                                model.Output = await File.ReadAllTextAsync(outputFilePath);
                            }
                        }
                        else
                        {
                            model.Output = await stdoutTask;
                        }

                        var runErrors = await stderrTask;
                        if (!string.IsNullOrEmpty(runErrors))
                        {
                            model.Error = runErrors;
                            model.IsError = true;
                        }
                        else if (model.IsError != true) // không bị kill bởi memory
                        {
                            model.IsError = false;
                        }
                    }
                    else
                    {
                        try
                        {
                            runProcess.Kill();
                            runProcess.WaitForExit();
                        }
                        catch (Exception ex)
                        {
                            model.Error = $"Error terminating process: {ex.Message}";
                            model.IsError = true;
                            return model;
                        }

                        stopwatch.Stop();
                        model.RunTime = (float)stopwatch.Elapsed.TotalSeconds;
                        model.Error = "Time limit exceeded";
                        model.IsError = true;
                    }

                    // cập nhật PeakWorkingSet64 sau khi kết thúc
                    try
                    {
                        runProcess.Refresh();
                        var peakMB = runProcess.PeakWorkingSet64 / (1024.0 * 1024.0);
                        model.MemoryUsed = model.MemoryUsed > (float)peakMB ? model.MemoryUsed : (float)peakMB;
                    }
                    catch { }
                }
                /**************************************************************
                 * 
                 *                 RUN PYTHON CODE
                 * 
                 **************************************************************/
                else if (model.ProgramLanguageId == 2)
                {
                    // Python code execution logic
                    model.FileName = model.FileName + ".py";
                    var sourceFilePath = Path.Combine(userDirectory, model.FileName);
                    await File.WriteAllTextAsync(sourceFilePath, model.SourceCode, Encoding.UTF8);

                    var runProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "sudo",
                            Arguments = $"-u sandbox {GetCompilerPath("python")} \"{model.FileName}\"",
                            RedirectStandardInput = true,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            WorkingDirectory = userDirectory
                        }
                    };

                    runProcess.StartInfo.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";

                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    runProcess.Start();

                    var stdoutTask = runProcess.StandardOutput.ReadToEndAsync();
                    var stderrTask = runProcess.StandardError.ReadToEndAsync();
                    Task processTask;

                    if (!string.IsNullOrEmpty(model.InputFile))
                    {
                        var inputFilePath = Path.Combine(userDirectory, model.InputFile);
                        await File.WriteAllTextAsync(inputFilePath, model.Input, Encoding.UTF8);

                        if (runProcess.StartInfo.RedirectStandardInput)
                        {
                            runProcess.StartInfo.Arguments += $" < {inputFilePath}";
                        }
                        processTask = Task.Run(() => runProcess.WaitForExit());
                    }
                    else if (!string.IsNullOrEmpty(model.Input))
                    {
                        processTask = Task.Run(async () =>
                        {
                            await runProcess.StandardInput.WriteLineAsync(model.Input);
                            await runProcess.StandardInput.FlushAsync();
                            runProcess.StandardInput.Close();
                            runProcess.WaitForExit();
                        });
                    }
                    else
                    {
                        processTask = Task.Run(() =>
                        {
                            runProcess.StandardInput.Close();
                            runProcess.WaitForExit();
                        });
                    }

                    // === Monitor Memory Usage ===
                    var monitorTask = Task.Run(async () =>
                    {
                        while (!runProcess.HasExited)
                        {
                            try
                            {
                                runProcess.Refresh();
                                var memoryMB = runProcess.WorkingSet64 / (1024.0 * 1024.0);
                                model.MemoryUsed = model.MemoryUsed > (float)memoryMB ? model.MemoryUsed : (float)memoryMB;

                                if (model.MemoryLimit.HasValue && memoryMB > model.MemoryLimit.Value)
                                {
                                    runProcess.Kill();
                                    model.Error = "Memory limit exceeded";
                                    model.IsError = true;
                                    break;
                                }
                            }
                            catch { }
                            await Task.Delay(50); // check mỗi 50ms
                        }
                    });

                    // === Wait with runtime limit ===
                    if (await Task.WhenAny(processTask, Task.Delay(model.TimeLimit * 1000 ?? 1000)) == processTask)
                    {
                        stopwatch.Stop();
                        model.RunTime = (float)stopwatch.Elapsed.TotalSeconds;

                        if (!string.IsNullOrEmpty(model.OutputFile))
                        {
                            var outputFilePath = Path.Combine(userDirectory, model.OutputFile);

                            runProcess.StartInfo.RedirectStandardOutput = false;
                            runProcess.StartInfo.RedirectStandardError = false;
                            runProcess.StartInfo.Arguments += $" > {outputFilePath}";

                            await runProcess.WaitForExitAsync();

                            if (File.Exists(outputFilePath))
                            {
                                model.Output = await File.ReadAllTextAsync(outputFilePath, Encoding.UTF8);
                            }
                        }
                        else
                        {
                            model.Output = await stdoutTask;
                        }

                        var runErrors = await stderrTask;
                        if (!string.IsNullOrEmpty(runErrors))
                        {
                            model.Error = runErrors;
                            model.IsError = true;
                        }
                        else if (model.IsError != true) // không bị kill bởi memory
                        {
                            model.IsError = false;
                        }
                    }
                    else
                    {
                        try
                        {
                            runProcess.Kill();
                            runProcess.WaitForExit(); // Ensure the process has exited
                        }
                        catch (Exception ex)
                        {
                            model.Error = $"Error terminating process: {ex.Message}";
                            model.IsError = true;
                            return model;
                        }

                        stopwatch.Stop();
                        model.RunTime = (float)stopwatch.Elapsed.TotalSeconds;
                        model.Error = "Time limit exceeded";
                        model.IsError = true;
                    }

                    // === cập nhật Peak Working Set sau khi kết thúc ===
                    try
                    {
                        runProcess.Refresh();
                        var peakMB = runProcess.PeakWorkingSet64 / (1024.0 * 1024.0);
                        model.MemoryUsed = model.MemoryUsed > (float)peakMB ? model.MemoryUsed : (float)peakMB;
                    }
                    catch { }
                }
                /**************************************************************
                 * 
                 *                 COMPILE AND RUN PASCAL CODE
                 * 
                 **************************************************************/
                else if (model.ProgramLanguageId == 3)
                {
                    // Pascal code execution logic
                    model.FileName = model.FileName + ".pas";
                    var sourceFilePath = Path.Combine(userDirectory, model.FileName);
                    await File.WriteAllTextAsync(sourceFilePath, model.SourceCode);

                    // COMPILE CODE
                    var compileProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "sudo",
                            Arguments = $"-u sandbox {GetCompilerPath("fpc")} \"{model.FileName}\"",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            WorkingDirectory = userDirectory
                        }
                    };


                    var compileErrorsTask = compileProcess.StandardError.ReadToEndAsync();
                    compileProcess.Start();
                    await compileProcess.WaitForExitAsync();

                    // IF COMPILE ERROR
                    var compileErrors = await compileErrorsTask;
                    if (!string.IsNullOrEmpty(compileErrors))
                    {
                        model.Error = compileErrors;
                        model.IsError = true; // Đánh dấu là có lỗi
                        return model;
                    }

                    // RUN PROCESS
                    var exeFilePath = Path.Combine(userDirectory, Path.GetFileNameWithoutExtension(model.FileName) + ".exe");
                    var runProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = exeFilePath,
                            RedirectStandardInput = true,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            WorkingDirectory = userDirectory
                        }
                    };

                    // create run time watching
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    runProcess.Start();

                    // INPUT FILE EXIST
                    if (!string.IsNullOrEmpty(model.InputFile))
                    {
                        var inputFilePath = Path.Combine(userDirectory, model.InputFile);
                        await File.WriteAllTextAsync(inputFilePath, model.Input);

                        if (runProcess.StartInfo.RedirectStandardInput)
                        {
                            runProcess.StartInfo.Arguments = $" < {inputFilePath}";
                        }
                    }

                    // STDIN
                    if (string.IsNullOrEmpty(model.InputFile) && !string.IsNullOrEmpty(model.Input))
                    {
                        await runProcess.StandardInput.WriteLineAsync(model.Input);
                        await runProcess.StandardInput.FlushAsync();
                    }

                    runProcess.StandardInput.Close();

                    // Create a task to monitor the process exit
                    var processTask = Task.Run(() =>
                    {
                        runProcess.WaitForExit(model.TimeLimit * 1000 ?? 1000);
                    });

                    // Waiting run time exit
                    if (await Task.WhenAny(processTask, Task.Delay(model.TimeLimit * 1000 ?? 1000)) == processTask)
                    {
                        stopwatch.Stop();
                        model.RunTime = (float)stopwatch.Elapsed.TotalSeconds;
                        // Process completed within time limit
                        if (!string.IsNullOrEmpty(model.OutputFile))
                        {
                            var outputFilePath = Path.Combine(userDirectory, model.OutputFile);

                            runProcess.StartInfo.RedirectStandardOutput = false;
                            runProcess.StartInfo.RedirectStandardError = false;
                            runProcess.StartInfo.Arguments += $" > {outputFilePath}";

                            await runProcess.WaitForExitAsync();

                            if (File.Exists(outputFilePath))
                            {
                                model.Output = await File.ReadAllTextAsync(outputFilePath);
                            }
                        }
                        else
                        {
                            // Read the output if output file is not used
                            model.Output = await runProcess.StandardOutput.ReadToEndAsync();
                        }

                        var runErrors = await runProcess.StandardError.ReadToEndAsync();

                        // Check for runtime errors
                        if (!string.IsNullOrEmpty(runErrors))
                        {
                            model.Error = runErrors;
                            model.IsError = true; // Đánh dấu là có lỗi
                        }
                        else
                        {
                            model.IsError = false; // Không có lỗi
                        }
                    }
                    else
                    {
                        try
                        {
                            runProcess.Kill();
                            runProcess.WaitForExit(); // Ensure the process has exited
                        }
                        catch (Exception ex)
                        {
                            model.Error = $"Error terminating process: {ex.Message}";
                            model.IsError = true;
                            return model;
                        }

                        stopwatch.Stop();
                        model.RunTime = (float)stopwatch.Elapsed.TotalSeconds;
                        model.Error = "Time limit exceed";
                        model.IsError = true;
                    }
                    
                }
                /**************************************************************
                 * 
                 *                 RUN JAVASCRIPT (Node.js) CODE
                 * 
                 **************************************************************/
                else if (model.ProgramLanguageId == 4)
                {
                    // JavaScript (Node.js) execution logic
                    model.FileName = model.FileName + ".js";
                    var sourceFilePath = Path.Combine(userDirectory, model.FileName);
                    await File.WriteAllTextAsync(sourceFilePath, model.SourceCode);

                    // RUN PROCESS
                    var runProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "sudo",
                            Arguments = $"-u sandbox {GetCompilerPath("node")} \"{model.FileName}\"",
                            RedirectStandardInput = true,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            WorkingDirectory = userDirectory
                        }
                    };


                    // create run time watching
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    runProcess.Start();

                    // INPUT FILE EXIST
                    if (!string.IsNullOrEmpty(model.InputFile))
                    {
                        var inputFilePath = Path.Combine(userDirectory, model.InputFile);
                        await File.WriteAllTextAsync(inputFilePath, model.Input);

                        if (runProcess.StartInfo.RedirectStandardInput)
                        {
                            runProcess.StartInfo.Arguments += $" < {inputFilePath}";
                        }
                    }

                    // STDIN
                    if (string.IsNullOrEmpty(model.InputFile) && !string.IsNullOrEmpty(model.Input))
                    {
                        await runProcess.StandardInput.WriteLineAsync(model.Input);
                        await runProcess.StandardInput.FlushAsync();
                    }

                    runProcess.StandardInput.Close();

                    // Create a task to monitor the process exit
                    var processTask = Task.Run(() =>
                    {
                        runProcess.WaitForExit();
                    });

                    // Waiting run time exit
                    if (await Task.WhenAny(processTask, Task.Delay(model.TimeLimit * 1000 ?? 1000)) == processTask)
                    {
                        stopwatch.Stop();
                        model.RunTime = (float)stopwatch.Elapsed.TotalSeconds;
                        // Process completed within time limit
                        if (!string.IsNullOrEmpty(model.OutputFile))
                        {
                            var outputFilePath = Path.Combine(userDirectory, model.OutputFile);

                            runProcess.StartInfo.RedirectStandardOutput = false;
                            runProcess.StartInfo.RedirectStandardError = false;
                            runProcess.StartInfo.Arguments += $" > {outputFilePath}";

                            await runProcess.WaitForExitAsync();

                            if (File.Exists(outputFilePath))
                            {
                                model.Output = await File.ReadAllTextAsync(outputFilePath);
                            }
                        }
                        else
                        {
                            // Read the output if output file is not used
                            model.Output = await runProcess.StandardOutput.ReadToEndAsync();
                        }

                        var runErrors = await runProcess.StandardError.ReadToEndAsync();

                        // Check for runtime errors
                        if (!string.IsNullOrEmpty(runErrors))
                        {
                            model.Error = runErrors;
                            model.IsError = true; // Đánh dấu là có lỗi
                        }
                        else
                        {
                            model.IsError = false; // Không có lỗi
                        }
                    }
                    else
                    {
                        try
                        {
                            runProcess.Kill();
                            runProcess.WaitForExit(); // Ensure the process has exited
                        }
                        catch (Exception ex)
                        {
                            model.Error = $"Error terminating process: {ex.Message}";
                            model.IsError = true;
                            return model;
                        }

                        stopwatch.Stop();
                        model.RunTime = (float)stopwatch.Elapsed.TotalSeconds;
                        model.Error = "Time limit exceed";
                        model.IsError = true;
                    }
                    if (File.Exists(sourceFilePath))
                    {
                        // Xóa tệp
                        File.Delete(sourceFilePath);
                    }
                }
                
            }
            catch (Exception ex)
            {
                model.Error = ex.Message;
                model.IsError = true;
            }


            return model;
        }

        public async Task<RunProcessViewModel> CompileCppFile(RunProcessViewModel model, string userDirectory)
        {
            //COMPILE CODE
            var compileProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "sudo",
                    Arguments = $"-u sandbox {GetCompilerPath("g++")} \"{model.FileName}\" -o \"{Path.ChangeExtension(model.FileName, "exe")}\" -std=c++14",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = userDirectory
                }
            };


            compileProcess.Start();
            var compileErrorsTask = compileProcess.StandardError.ReadToEndAsync();
            await compileProcess.WaitForExitAsync();

            var compileErrors = await compileErrorsTask;
            //IF COMPILE ERRROR
            if (!string.IsNullOrEmpty(compileErrors))
            {
                model.Error = compileErrors;
                model.IsError = true;
                return model;
            }
            // If compilation is successful, return the model with no errors
            model.IsError = false; // Không có lỗi
            model.Error = null; // Không có lỗi
            return model;
        }


    }
}
