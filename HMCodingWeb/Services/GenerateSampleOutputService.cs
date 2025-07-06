using HMCodingWeb.Models;
using HMCodingWeb.ViewModels;

namespace HMCodingWeb.Services
{
    public class GenerateSampleOutputService
    {
        private readonly OnlineCodingWebContext _context;
        private readonly RunProcessService _runProcessService;
        public GenerateSampleOutputService(OnlineCodingWebContext context, RunProcessService runProcessService)
        {
            _context = context;
            _runProcessService = runProcessService;
        }

        public async Task<GenerateOutputViewModel> GenerateSampleOutputAsync(GenerateOutputViewModel model)
        {
            try
            {
                if(model == null || !model.SampleOutputs.Any())
                {
                    return new GenerateOutputViewModel();
                }

                var results = new GenerateOutputViewModel();
                var sampleInputs = model.SampleOutputs;
                for (var i = 0; i < sampleInputs.Count; ++i)
                {
                    var sampleInput = sampleInputs[i];
                    var output = await _runProcessService.RunProcessWithInput(new RunProcessViewModel()
                    {
                        UserId = model.UserId,
                        ProgramLanguageId = model.ProgramLanguageId,
                        FileName = "GenerateSampleOutput",
                        SourceCode = model.SourceCode,
                        Input = sampleInput.Input,
                        TimeLimit = model.TimeLimit,
                    }, i == 0);
                    if (output != null)
                    {
                        sampleInput.Output = output.Output;
                        sampleInput.Error = output.Error;
                        sampleInput.IsError = output.IsError ?? false;
                        sampleInput.FileName = output.FileName;
                        sampleInput.SampleIndex = i;
                        sampleInput.RunTime = output.RunTime;
                        results.SampleOutputs.Add(sampleInput);
                        if (output.IsError == true && sampleInput.RunTime < model.TimeLimit)
                        {
                            results.IsError = true;
                            results.Error = output.Error ?? "An error occurred while generating output.";
                            break;
                        }
                    }
                    else
                    {
                        // Handle the case where output is null
                        sampleInput.IsError = true;
                        sampleInput.Error = "Failed to generate output.";
                        results.SampleOutputs.Add(sampleInput);
                        break;
                    }
                }
                results.IsError = results.SampleOutputs.Any(x => x.IsError);
                _runProcessService.ClearTempFolder(model.UserId);
                return results;
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                Console.WriteLine($"An error occurred: {ex.Message}");
                return new GenerateOutputViewModel();
            }
        }

    }
}
