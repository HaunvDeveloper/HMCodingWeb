using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using HMCodingWeb.ViewModels;
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



        public async Task<RunProcessViewModel> RunProcessWithInput(RunProcessViewModel model)
        {
            try
            {
                var userDirectory = Path.Combine("Temp", model.UserId.ToString());
                if (!Directory.Exists(userDirectory))
                {
                    Directory.CreateDirectory(userDirectory);
                }

                if (model.ProgramLanguageId == 1)
                {
                    model.FileName = model.FileName + ".cpp";
                    var sourceFilePath = Path.Combine(userDirectory, model.FileName);
                    await File.WriteAllTextAsync(sourceFilePath, model.SourceCode);


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
                    await compileProcess.WaitForExitAsync();

                    //IF COMPILE ERRROR
                    var compileErrors = await compileProcess.StandardError.ReadToEndAsync();
                    if (!string.IsNullOrEmpty(compileErrors))
                    {
                        model.Error = compileErrors;
                        model.IsError = true;
                        return model;
                    }

                    var runProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = Path.Combine(userDirectory, Path.ChangeExtension(model.FileName, "exe")),
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
                    var processTask = Task.CompletedTask;

                    if (!string.IsNullOrEmpty(model.InputFile))
                    {
                        var inputFilePath = Path.Combine(userDirectory, model.InputFile);
                        await File.WriteAllTextAsync(inputFilePath, model.Input);

                        if (runProcess.StartInfo.RedirectStandardInput)
                        {
                            runProcess.StartInfo.Arguments = $" < {inputFilePath}";
                        }
                        processTask = Task.Run(() =>
                        {
                            runProcess.WaitForExit();
                        });
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
                        // Create a task to monitor the process exit
                        processTask = Task.Run(() =>
                        {
                            runProcess.StandardInput.Close();
                            runProcess.WaitForExit();
                        });
                    }
                        

                    //wating run time exit
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
                            model.Output = await stdoutTask;
                        }

                        var runErrors = await stderrTask;

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
                    if (File.Exists(Path.ChangeExtension(sourceFilePath, "exe")))
                    {
                        // Xóa tệp
                        File.Delete(Path.ChangeExtension(sourceFilePath, "exe"));
                    }

                }
                else if (model.ProgramLanguageId == 2)
                {
                    // Python code execution logic
                    model.FileName = model.FileName + ".py";
                    var sourceFilePath = Path.Combine(userDirectory, model.FileName);
                    await File.WriteAllTextAsync(sourceFilePath, model.SourceCode);

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
                    var processTask = Task.CompletedTask;

                    if (!string.IsNullOrEmpty(model.InputFile))
                    {
                        var inputFilePath = Path.Combine(userDirectory, model.InputFile);
                        await File.WriteAllTextAsync(inputFilePath, model.Input, Encoding.UTF8);

                        if (runProcess.StartInfo.RedirectStandardInput)
                        {
                            runProcess.StartInfo.Arguments += $" < {inputFilePath}";
                        }
                        processTask = Task.Run(() =>
                        {
                            runProcess.WaitForExit();
                        });
                    }else if (!string.IsNullOrEmpty(model.Input))
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
                        else
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
                        model.Error = "Time limit exceed";
                        model.IsError = true;
                    }
                    if (File.Exists(sourceFilePath))
                    {
                        // Xóa tệp
                        File.Delete(sourceFilePath);
                    }
                }
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


                    compileProcess.Start();
                    await compileProcess.WaitForExitAsync();

                    // IF COMPILE ERROR
                    var compileErrors = await compileProcess.StandardError.ReadToEndAsync();
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
                    if (File.Exists(sourceFilePath))
                    {
                        // Xóa tệp
                        File.Delete(sourceFilePath);
                    }
                }
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
                if (System.IO.File.Exists(userDirectory))
                {
                    System.IO.File.Delete(userDirectory);
                }
            }
            catch (Exception ex)
            {
                model.Error = ex.Message;
                model.IsError = true;
            }


            return model;
        }
    }
}
