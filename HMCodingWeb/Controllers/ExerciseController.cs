using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HMCodingWeb.Models;
using HMCodingWeb.Services;
using HMCodingWeb.ViewModels;
using System.Security.Claims;
using System.IO;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace HMCodingWeb.Controllers
{
    [Authorize]
    public class ExerciseController : Controller
    {
        private readonly ILogger<ExerciseController> _logger;
        private readonly OnlineCodingWebContext _context;
        private readonly RunProcessService _runProcessService;
        private readonly MarkingService _markingService;
        private readonly UserPointService _userPointService;
        private readonly RankingService _rankingService;

        public ExerciseController(ILogger<ExerciseController> logger, OnlineCodingWebContext context, RunProcessService runProcessService, MarkingService markingService, UserPointService userPointService, RankingService rankingService)
        {
            _logger = logger;
            _context = context;
            _runProcessService = runProcessService;
            _markingService = markingService;
            _userPointService = userPointService;
            _rankingService = rankingService;
        }
        [AllowAnonymous]
        public IActionResult Index()
        {
            var kingMarking = new List<string> { "io", "acm" };
            var typeMarking = new List<string> { "Tương đối", "Chính xác" };
            ViewBag.KindMarking = kingMarking.Select(x => new SelectListItem()
            {
                Value = x,
                Text = x
            });
            ViewBag.TypeMarking = typeMarking.Select(x => new SelectListItem()
            {
                Value = x,
                Text = x
            });
            ViewBag.Difficults = _context.DifficultyLevels.Select(x => new SelectListItem()
            {
                Value = x.Id.ToString(),
                Text = x.DifficultyName
            }).ToList();
            ViewBag.Chapters = _context.Chapters.Select(x => new SelectListItem()
            {
                Value = x.Id.ToString(),
                Text = x.ChapterName
            }).ToList();
            ViewBag.ExerciseType = _context.ExerciseTypes.Select(x => new SelectListItem()
            {
                Value = x.Id.ToString(),
                Text = x.TypeName
            }).ToList();
            return View();
        }

        [AllowAnonymous]
        public async Task<IActionResult> _GetList(int p = 1, int s = 10, string key = "", int? dId = null, int? cId = null, int? etId = null, string? tM = null, string? kM = null, string? sortBy = null, string? sortOrder = null)
        {
            // Validate pagination parameters
            p = Math.Max(1, p);
            s = Math.Clamp(s, 1, 100);

            // Get current user ID (default to 0 if not authenticated)
            long userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            // Base query with initial filters and includes
            var query = _context.Exercises
                .Where(ex => ex.IsAccept == true && (ex.AccessId == 3 || ex.UserCreatedId == userId))
                .Include(ex => ex.Difficulty)
                .Include(ex => ex.Chapter)
                .Include(ex => ex.ExerciseBelongTypes)
                    .ThenInclude(ebt => ebt.ExerciseType)
                 .AsQueryable();

            // Apply search filters
            if (!string.IsNullOrEmpty(key))
            {
                key = key.ToLower();
                query = query.Where(ex => ex.ExerciseName.ToLower().Contains(key) ||
                                         ex.ExerciseCode.ToLower().Contains(key));
            }

            if (dId.HasValue)
            {
                query = query.Where(ex => ex.DifficultyId == dId.Value);
            }

            if (cId.HasValue)
            {
                query = query.Where(ex => ex.ChapterId == cId.Value);
            }

            if (etId.HasValue)
            {
                query = query.Where(ex => ex.ExerciseBelongTypes.Any(ebt => ebt.ExerciseTypeId == etId.Value));
            }

            if (!string.IsNullOrEmpty(kM))
            {
                query = query.Where(ex => ex.KindMarking == kM);
            }

            if (!string.IsNullOrEmpty(tM))
            {
                query = query.Where(ex => ex.TypeMarking == tM);
            }
            

            // Calculate total records
            var totalRecords = await query.CountAsync();

            var userMarkingsQuery = _context.Markings
                .Where(m => m.UserId == userId);

            var successCountsQuery = _context.Markings
                .Where(m => m.IsAllCorrect)
                .GroupBy(m => m.ExerciseId)
                .Select(g => new
                {
                    ExerciseId = g.Key,
                    SuccessfulUsers = g.Select(m => m.UserId).Distinct().Count()
                });

            var rawDataQuery = query
                .GroupJoin(
                    successCountsQuery,
                    ex => ex.Id,
                    sc => sc.ExerciseId,
                    (ex, scGroup) => new { ex, scGroup }
                )
                .Select(joined => new
                {
                    Exercise = joined.ex,
                    SuccessfulUsers = joined.scGroup.Select(g => g.SuccessfulUsers).FirstOrDefault(),
                    DifficultyLevel = joined.ex.DifficultyId,
                    IsCorrect = userId == 0
                        ? ""
                        : userMarkingsQuery
                            .Where(m => m.ExerciseId == joined.ex.Id)
                            .OrderByDescending(m => m.IsAllCorrect) // ưu tiên Passed nếu có
                            .Select(m => m.IsAllCorrect ? "Passed" : "Failed")
                            .FirstOrDefault() ?? ""
                })
                .AsNoTracking()
                .OrderByDescending(x => x.Exercise.CreatedDate);
                
            if (sortBy == "success")
            {
                if (sortOrder == "asc")
                {
                    rawDataQuery = rawDataQuery.OrderBy(ex => ex.SuccessfulUsers);
                }
                else
                {
                    rawDataQuery = rawDataQuery.OrderByDescending(ex => ex.SuccessfulUsers);
                }
            }
            if (sortBy == "difficulty")
            {
                if (sortOrder == "asc")
                {
                    rawDataQuery = rawDataQuery.OrderBy(ex => ex.DifficultyLevel);
                }
                else
                {
                    rawDataQuery = rawDataQuery.OrderByDescending(ex => ex.DifficultyLevel);
                }
            }
            var rawData = await rawDataQuery
                .Skip((p - 1) * s)
                .Take(s)
                .ToListAsync();

            // Transform to ExerciseViewModel client-side
            var listData = rawData.Select(x => new ExerciseViewModel(x.Exercise, x.SuccessfulUsers, x.IsCorrect)).ToList();

            // Set ViewBag properties
            ViewBag.CurrentPage = p;
            ViewBag.PageSize = s;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalRecords / s);
            ViewBag.SearchKey = key;
            ViewBag.DifficultId = dId;
            ViewBag.ChapterId = cId;
            ViewBag.ExerciseTypeId = etId;
            ViewBag.TypeMarkingSearch = tM;
            ViewBag.KindMarkingSearch = kM;
            ViewBag.AvailablePageSizes = new int[] { 5, 10, 20, 100 };
            ViewBag.UserId = userId;
            ViewBag.SortOrder = sortOrder;
            ViewBag.SortBy = sortBy;
            return PartialView(listData);
        }

        [HttpPost]
        public async Task<IActionResult> _GetCompletedExByUser(int draw, int start, int length, string keyword = "", long? userId = null)
        {
            // Nếu không có userId, lấy từ Claims (nếu có)
            if (!userId.HasValue)
            {
                userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            }

            // Base query: Lấy các ExerciseId mà user đã hoàn thành (IsAllCorrect == true)
            var completedExerciseIdsQuery = _context.Markings
                .Where(m => m.UserId == userId && m.IsAllCorrect == true)
                .Select(m => m.ExerciseId)
                .Distinct();

            // Áp dụng tìm kiếm theo keyword
            var query = _context.Exercises
                .Where(e => completedExerciseIdsQuery.Contains(e.Id))
                .Join(_context.DifficultyLevels,
                    e => e.DifficultyId,
                    d => d.Id,
                    (e, d) => new { Exercise = e, Difficulty = d.DifficultyName });

            if (!string.IsNullOrEmpty(keyword))
            {
                keyword = keyword.ToLower();
                query = query.Where(x => x.Exercise.ExerciseCode.ToLower().Contains(keyword) ||
                                        x.Exercise.ExerciseName.ToLower().Contains(keyword));
            }

            // Lấy tổng số bản ghi
            var totalRecords = await query.CountAsync();

            // Lấy dữ liệu phân trang
            var completedExercises = await query
                .OrderByDescending(x => x.Exercise.CreatedDate) // Sắp xếp theo CreatedDate của Exercise
                .Skip(start)
                .Take(length)
                .Select(x => new
                {
                    x.Exercise.Id,
                    x.Exercise.ExerciseCode,
                    x.Exercise.ExerciseName,
                    Difficulty = x.Difficulty,
                    FirstMarkingTime = _context.Markings
                        .Where(m => m.UserId == userId && m.ExerciseId == x.Exercise.Id)
                        .OrderBy(m => m.MarkingDate)
                        .Select(m => m.MarkingDate)
                        .FirstOrDefault(),
                    FirstCorrectMarkingTime = _context.Markings
                        .Where(m => m.UserId == userId && m.ExerciseId == x.Exercise.Id && m.IsAllCorrect == true)
                        .OrderBy(m => m.MarkingDate)
                        .Select(m => m.MarkingDate)
                        .FirstOrDefault()
                })
                .AsNoTracking()
                .ToListAsync();

            // Transform dữ liệu cho DataTable
            var data = completedExercises.Select(x => new
            {
                x.Id,
                x.ExerciseCode,
                x.ExerciseName,
                x.Difficulty,
                FirstMarkingTime = x.FirstMarkingTime != default(DateTime) ? x.FirstMarkingTime.ToString("dd/MM/yyyy HH:mm") : "N/A",
                FirstCorrectMarkingTime = x.FirstCorrectMarkingTime != default(DateTime) ? x.FirstCorrectMarkingTime.ToString("dd/MM/yyyy HH:mm") : "N/A"
            }).ToList();

            // Trả về JSON cho DataTable
            return Json(new
            {
                draw = draw,
                recordsTotal = totalRecords,
                recordsFiltered = totalRecords, // Nếu có thêm bộ lọc phức tạp, cần tính lại recordsFiltered
                data = data
            });
        }
        public IActionResult Create()
        {
            ViewBag.AccessRole = _context.AccessRoles.ToList();
            ViewBag.Difficulty = _context.DifficultyLevels.ToList();
            ViewBag.ExerciseType = _context.ExerciseTypes.ToList();
            ViewBag.Chapter = _context.Chapters.ToList();
            ViewBag.KindMarking = new List<string> { "io", "acm" };
            ViewBag.TypeMarking = new List<string> { "Tương đối", "Chính xác" };


            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Exercise model)
        {
            try
            {
                var existEx = await _context.Exercises.FirstOrDefaultAsync(x => x.ExerciseCode == model.ExerciseCode);
                if (existEx != null)
                {
                    return Json(new { status = false, error = "Đã tồn tại Mã bài!!!" });
                }

                long userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var user = await _context.Users.FindAsync(userId);
                model.CreatedDate = DateTime.Now;
                if (model.IsExam)
                {
                    model.AccessId = 1;
                }

                if (user != null && (user.AuthId == 1 || user.AuthId == 2))
                    model.IsAccept = true;
                else
                    model.IsAccept = false;
                model.UserCreatedId = userId;

                _context.Exercises.Add(model);
                await _context.SaveChangesAsync();
                return Json(new { status = true, redirect = Url.Action("Index") });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, error = ex.ToString() });
            }
        }


        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null)
            {
                return View("Error");
            }
            var exercise = await _context.Exercises
                .Include(ex => ex.TestCases)
                .Include(ex => ex.Difficulty)
                .Include(ex => ex.Chapter)
                .Include(ex => ex.ExerciseBelongTypes)
                    .ThenInclude(ebt => ebt.ExerciseType)
                .SingleOrDefaultAsync(ex => ex.Id == id);

            if (exercise == null)
            {
                return View("Error");
            }

            long userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            string userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "User";
            if (!(userRole == "admin" || userRole == "teacher" || exercise.UserCreatedId == userId))
            {
                return View("NotAccess");
            }

            ViewBag.AccessRole = _context.AccessRoles.ToList();
            ViewBag.Difficulty = _context.DifficultyLevels.ToList();
            ViewBag.ExerciseType = _context.ExerciseTypes.ToList();
            ViewBag.Chapter = _context.Chapters.ToList();
            ViewBag.KindMarking = new List<string> { "io", "acm" };
            ViewBag.TypeMarking = new List<string> { "Tương đối", "Chính xác" };
            return View(exercise);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Exercise model)
        {
            try
            {
                var exercise = await _context.Exercises
                .Include(e => e.ExerciseBelongTypes)
                .Include(e => e.TestCases)
                .FirstOrDefaultAsync(e => e.Id == model.Id);
                
                if (exercise == null)
                {
                    return Json(new { status = false, error = "Bài tập không tồn tại" });
                }
                long userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var user = await _context.Users.FindAsync(userId);
                string userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "User";
                if (userRole != "Admin" && userRole != "Teacher" && exercise.UserCreatedId != userId)
                {
                    return Json(new { status = false, error = "Bạn không có quyền sửa bài tập này" });
                }


                // Update exercise properties
                exercise.ExerciseCode = model.ExerciseCode;
                exercise.ExerciseName = model.ExerciseName;
                exercise.ExerciseContent = model.ExerciseContent;
                exercise.InputFile = model.InputFile;
                exercise.OutputFile = model.OutputFile;
                exercise.NumberTestcase = model.NumberTestcase;
                exercise.KindMarking = model.KindMarking;
                exercise.TypeMarking = model.TypeMarking;
                exercise.DifficultyId = model.DifficultyId;
                exercise.ChapterId = model.ChapterId;
                exercise.AccessId = model.AccessId;
                exercise.IsExam = model.IsExam;
                exercise.RuntimeLimit = model.RuntimeLimit;
                if (model.IsExam)
                {
                    exercise.AccessId = 1;
                }



                // Update ExerciseBelongTypes
                _context.ExerciseBelongTypes.RemoveRange(exercise.ExerciseBelongTypes);
                exercise.ExerciseBelongTypes = model.ExerciseBelongTypes;

                // Update TestCases
                _context.TestCases.RemoveRange(exercise.TestCases);
                exercise.TestCases = model.TestCases;

                _context.Exercises.Update(exercise);
                await _context.SaveChangesAsync();

                return Json(new { status = true, redirect = Url.Action("Index", "Exercise") });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating exercise");
                return Json(new { status = false, error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var exercise = await _context.Exercises.FindAsync(id);
                if (exercise == null)
                {
                    return Json(new { status = false, error = "Bài tập không tồn tại" });
                }
                long userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                string userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "User";
                if (!(userRole == "admin" || userRole == "teacher" || exercise.UserCreatedId == userId))
                {
                    return Json(new { status = false, error = "Bạn không có quyền xóa bài tập này" });
                }
                _context.Exercises.Remove(exercise);
                await _context.SaveChangesAsync();
                return Json(new { status = true, redirect = Url.Action("Index", "Exercise") });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting exercise");
                return Json(new { status = false, error = ex.Message });
            }
        }




        public IActionResult Code(long? id)
        {
            if (id == null)
            {
                return View("Error");
            }
            var exercise = _context.Exercises.SingleOrDefault(ex => ex.Id == id);
            if (exercise == null)
            {
                return View("Error");
            }

            long userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            ViewBag.ListTheme = _context.Themes.ToList();
            var user = _context.Users.Where(user => user.Id == userId).SingleOrDefault();
            if (user != null)
            {
                user.Password = "";
            }
            ViewBag.UserInfo = user;
            ViewBag.ProgramLanguageList = _context.ProgramLanguages.ToList();
            ViewBag.Difficulty = _context.DifficultyLevels.ToList();

            exercise.UserCreated = _context.Users.Single(user => user.Id == exercise.UserCreatedId);
            return View(exercise);
        }




        [HttpPost]
        public JsonResult CheckCopy(CopyPasteHistory model)
        {
            long userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            model.UserId = userId;
            model.LaunchTime = DateTime.Now;

            _context.CopyPasteHistories.Add(model);
            _context.SaveChanges();
            return Json(new { status = true, message = "success" });
        }

        [HttpPost]
        public async Task<IActionResult> Marking(long ExerciseId, int ProgramLanguageId, string SourceCode)
        {
            try
            {
                long userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var marking = await _markingService.Marking(ExerciseId, ProgramLanguageId, SourceCode, userId);
                int pointGain = 0;
                if (marking.IsAllCorrect)
                {
                    // If the marking is successful, add points to the user
                    pointGain = await _userPointService.AddPointPassedExercise(userId, ExerciseId);
                    if (pointGain > 0)
                    {
                        await _rankingService.UpdateRankUser(userId);
                    }
                }
                _context.Markings.Add(marking);
                await _context.SaveChangesAsync();
                return Json(new { status = true, data = new { marking.IsAllCorrect, marking.ResultContent, marking.Score, marking.IsError, pointGain} });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during marking");
                return Json(new { status = false, error = ex.Message });
            }
        }

    
    }
}