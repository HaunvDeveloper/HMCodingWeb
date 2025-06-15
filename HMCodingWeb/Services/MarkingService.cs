using HMCodingWeb.Hubs;
using HMCodingWeb.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace HMCodingWeb.Services
{
    public class MarkingService
    {
        private readonly OnlineCodingWebContext _context;
        private readonly RunProcessService _runProcessService;
        private readonly IHubContext<MarkingHub> _hubContext;
        private readonly UserPointService _userPointService;

        public MarkingService(OnlineCodingWebContext context, RunProcessService runProcessService, IHubContext<MarkingHub> hubContext, UserPointService userPointService)
        {
            _context = context;
            _runProcessService = runProcessService;
            _hubContext = hubContext;
            _userPointService = userPointService;
        }

        public async Task<Marking> Marking(long ExerciseId, int ProgramLanguageId, string SourceCode, long userId)
        {
            var exercise = await _context.Exercises
                .FindAsync(ExerciseId);
            if (exercise == null)
            {
                throw new Exception("Exercise not found");
            }

            var model = new Marking
            {
                ExerciseId = ExerciseId,
                ProgramLanguageId = ProgramLanguageId,
                SourceCode = SourceCode,
                UserId = userId,
                InputFile = exercise.InputFile,
                OutputFile = exercise.OutputFile,
                TimeLimit = exercise.RuntimeLimit,
                IsExam = exercise.IsExam,
                KindMarking = exercise.KindMarking,
                TypeMarking = exercise.TypeMarking,
                MarkingDate = DateTime.Now,
                IsError = false,
                IsAllCorrect = false,
                Score = 0,
                CorrectTestNumber = 0,
                ResultContent = string.Empty
            };

            var testCases = _context.TestCases
                .Where(tc => tc.ExerciseId == ExerciseId)
                .OrderBy(tc => tc.Position)
                .ToList();

            for (int i = 0; i < testCases.Count; i++)
            {
                var testCase = testCases[i];
                var result = await _runProcessService.RunProcessWithInput(new ViewModels.RunProcessViewModel
                {
                    ProgramLanguageId = ProgramLanguageId,
                    SourceCode = SourceCode,
                    InputFile = model.InputFile,
                    OutputFile = model.OutputFile,
                    TimeLimit = (int)model.TimeLimit,
                    Input = testCase.Input,
                    UserId = userId,
                    FileName = $"test_{testCase.Id}_{userId}"
                });



                var markingDetail = new MarkingDetail
                {
                    TestCaseId = testCase.Id,
                    Output = result.Output,
                    CorrectOutput = testCase.Output,
                    ErrorContent = result.Error,
                    IsError = result.IsError ?? false,
                    Input = testCase.Input,
                    RunTime = result.RunTime ?? 0,
                    IsTimeLimitExceed = result.RunTime > model.TimeLimit,
                    IsCorrect = result.IsError == true ? false : MatchingResult(model.TypeMarking, result.Output, testCase.Output),
                    TestCaseIndex = testCase.Position,
                };

                
                model.MarkingDetails.Add(markingDetail);

                // Send test case result to client via SignalR
                await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveTestCaseResult", markingDetail);

                if (markingDetail.IsCorrect)
                {
                    model.ResultContent += $"Test case {testCase.Position} passed.\n";
                    model.CorrectTestNumber++;
                }
                else if (markingDetail.IsTimeLimitExceed)
                {
                    model.ResultContent += $"Test case {testCase.Position} failed due to time limit exceeded.\n";
                    model.IsError = true;
                }
                else if (markingDetail.IsError)
                {
                    model.ResultContent += $"Test case {testCase.Position} failed with error: {markingDetail.ErrorContent}\n";
                    model.IsError = true;
                    break;
                }
                else
                {
                    model.ResultContent += $"Test case {testCase.Position} wrong answer.\n";
                }
                
            }
            model.IsAllCorrect = model.CorrectTestNumber == testCases.Count;
            model.Score = MarkingScore(model.KindMarking, model.CorrectTestNumber, testCases.Count);
            model.TimeSpent = TimeOnly.FromTimeSpan(DateTime.Now - model.MarkingDate);
            model.ResultContent += $"Total correct test cases: {model.CorrectTestNumber}/{testCases.Count}\n";
            if (model.IsError)
            {
                model.ResultContent += "There was an error during the marking process.\n";
            }
            else
            {
                model.ResultContent += $"Final score: {model.Score}\n";
            }
            return model;
        }

        private bool MatchingResult(string TypeMarking, string? a, string? b)
        {
            if (TypeMarking == "Chính xác")
            {
                return a == b;
            }
            else if (TypeMarking == "Tương đối")
            {
                // Extract numerical values using regex, ignoring whitespace, tabs, and newlines
                var regex = new Regex(@"[-]?\d*\.?\d+");
                var numbersA = regex.Matches(a ?? "").Select(m => m.Value).ToList();
                var numbersB = regex.Matches(b ?? "").Select(m => m.Value).ToList();

                // Compare the lists of numbers
                return numbersA.SequenceEqual(numbersB);
            }
            throw new ArgumentException("Invalid TypeMarking value");
        }

        private double MarkingScore(string KindMarking, int correctTestNumber, int totalTestCases)
        {
            if (totalTestCases == 0) return 0;
            if (KindMarking == "io")
            {
                return (double)Math.Round((decimal)(correctTestNumber / (double)totalTestCases) * 100, 2);
            }
            else if (KindMarking == "acm")
            {
                return correctTestNumber == totalTestCases ? 100 : 0;
            }
            throw new ArgumentException("Invalid KindMarking value");
        }

        
    }
}
