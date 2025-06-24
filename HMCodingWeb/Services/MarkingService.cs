using HMCodingWeb.Hubs;
using HMCodingWeb.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using HMCodingWeb.ViewModels;

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

        public string GetColumnName(int columnIndex)
        {
            // Map DataTable column index to model property
            switch (columnIndex)
            {
                case 0: return "markingDate";
                case 2: return "userName";
                case 3: return "exerciseCode";
                case 4: return "exerciseName";
                case 5: return "kindMarking";
                case 6: return "score";
                case 7: return "status";
                case 8: return "programLanguageName";
                default: return "markingDate"; // Default to rank
            }
        }

        public IOrderedQueryable<Marking> ApplyOrder(IQueryable<Marking> query, string columnName, string direction)
        {
            switch (columnName)
            {
                case "markingDate":
                    return direction == "asc"
                        ? query.OrderBy(u => u.MarkingDate)
                        : query.OrderByDescending(u => u.MarkingDate);
                case "userName":
                    return direction == "asc"
                        ? query.OrderBy(u => u.User != null ? u.User.Username : "")
                        : query.OrderByDescending(u => u.User != null ? u.User.Username : "");
                case "exerciseCode":
                    return direction == "asc"
                        ? query.OrderBy(u => u.Exercise != null ? u.Exercise.ExerciseCode : "")
                        : query.OrderByDescending(u => u.Exercise != null ? u.Exercise.ExerciseCode : "");
                case "exerciseName":
                    return direction == "asc"
                        ? query.OrderBy(u => u.Exercise != null ? u.Exercise.ExerciseName : "")
                        : query.OrderByDescending(u => u.Exercise != null ? u.Exercise.ExerciseName : "");
                case "kindMarking":
                    return direction == "asc"
                        ? query.OrderBy(u => u.KindMarking)
                        : query.OrderByDescending(u => u.KindMarking);
                case "score":
                    return direction == "asc"
                        ? query.OrderBy(u => u.Score)
                        : query.OrderByDescending(u => u.Score);
                case "status":
                    return direction == "asc"
                        ? query.OrderBy(u => u.Status)
                        : query.OrderByDescending(u => u.Status);
                case "programLanguageName":
                    return direction == "asc"
                        ? query.OrderBy(u => u.Status)
                        : query.OrderByDescending(u => u.Status);
                default:
                    return query.OrderByDescending(u => u.MarkingDate);
            }
        }

        public IOrderedQueryable<Marking> ApplyThenOrder(IOrderedQueryable<Marking> query, string columnName, string direction)
        {
            switch (columnName)
            {
                case "markingDate":
                    return direction == "asc"
                        ? query.ThenBy(u => u.MarkingDate)
                        : query.ThenByDescending(u => u.MarkingDate);
                case "userName":
                    return direction == "asc"
                        ? query.ThenBy(u => u.User != null ? u.User.Username : "")
                        : query.ThenByDescending(u => u.User != null ? u.User.Username : "");
                case "exerciseCode":
                    return direction == "asc"
                        ? query.ThenBy(u => u.Exercise != null ? u.Exercise.ExerciseCode : "")
                        : query.ThenByDescending(u => u.Exercise != null ? u.Exercise.ExerciseCode : "");
                case "exerciseName":
                    return direction == "asc"
                        ? query.ThenBy(u => u.Exercise != null ? u.Exercise.ExerciseName : "")
                        : query.ThenByDescending(u => u.Exercise != null ? u.Exercise.ExerciseName : "");
                case "kindMarking":
                    return direction == "asc"
                        ? query.ThenBy(u => u.KindMarking)
                        : query.ThenByDescending(u => u.KindMarking);
                case "score":
                    return direction == "asc"
                        ? query.ThenBy(u => u.Score)
                        : query.ThenByDescending(u => u.Score);
                case "status":
                    return direction == "asc"
                        ? query.ThenBy(u => u.Status)
                        : query.ThenByDescending(u => u.Status);
                case "programLanguageName":
                    return direction == "asc"
                        ? query.ThenBy(u => u.Status)
                        : query.ThenByDescending(u => u.Status);
                default:
                    return query.ThenByDescending(u => u.MarkingDate);
            }
        }


        public async Task<Marking> Marking(long ExerciseId, int ProgramLanguageId, string SourceCode, long userId)
        {
            var exercise = await _context.Exercises
                .FindAsync(ExerciseId);
            if (exercise == null)
            {
                throw new Exception("Exercise not found");
            }


            var testCases = _context.TestCases
                .Where(tc => tc.ExerciseId == ExerciseId)
                .OrderBy(tc => tc.Position)
                .ToList();

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
                ResultContent = string.Empty,
                TotalTestNumber = testCases.Count,
            };

            

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
                    IsTimeLimitExceed = result.RunTime > model.TimeLimit || result.Error == "Time limit exceed",
                    IsCorrect = result.IsError == true ? false : MatchingResult(model.TypeMarking, result.Output, testCase.Output),
                    TestCaseIndex = testCase.Position,
                };

                
                model.MarkingDetails.Add(markingDetail);

                // Send test case result to client via SignalR
                await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveTestCaseResult", new MarkingDetailViewModel(markingDetail));

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
            if(model.IsAllCorrect)
            {
                model.Status = "Passed";
            }
            else if(model.IsError)
            {
                model.Status = "Error";
                model.ResultContent += "There was an error during the marking process.\n";
            }
            else
            {
                model.Status = "Wrong";
            }

            model.ResultContent += $"Final score: {model.Score}\n";

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
                return a.Trim().Equals(b?.Trim(), StringComparison.OrdinalIgnoreCase); // Remove all whitespace and compare 
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
