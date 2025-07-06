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
        private readonly GenerateSampleOutputService _generateSampleOutputService;

        public ExerciseController(ILogger<ExerciseController> logger, OnlineCodingWebContext context, RunProcessService runProcessService, MarkingService markingService, UserPointService userPointService, RankingService rankingService, GenerateSampleOutputService generateSampleOutputService)
        {
            _logger = logger;
            _context = context;
            _runProcessService = runProcessService;
            _markingService = markingService;
            _userPointService = userPointService;
            _rankingService = rankingService;
            _generateSampleOutputService = generateSampleOutputService;
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
        public async Task<IActionResult> _GetList(int p = 1, int s = 10, string key = "", int? dId = null, int? cId = null, int? etId = null, string? tM = null, string? kM = null, string? sortBy = "difficulty", string? sortOrder = "asc")
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
                .AsNoTracking();
            
            if(sortBy == "createdDate")
            {
                if (sortOrder == "asc")
                {
                    rawDataQuery = rawDataQuery.OrderBy(ex => ex.Exercise.CreatedDate);
                }
                else
                {
                    rawDataQuery = rawDataQuery.OrderByDescending(ex => ex.Exercise.CreatedDate);
                }
            }

            if (sortBy == "success")
            {
                if (sortOrder == "asc")
                {
                    rawDataQuery = rawDataQuery
                        .OrderBy(ex => ex.SuccessfulUsers);
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
                    rawDataQuery = rawDataQuery
                        .OrderBy(ex => ex.DifficultyLevel)
                            .ThenByDescending(ex => ex.SuccessfulUsers);
                }
                else
                {
                    rawDataQuery = rawDataQuery
                        .OrderByDescending(ex => ex.DifficultyLevel)
                            .ThenBy(ex => ex.SuccessfulUsers);
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
            ViewBag.TotalRecords = totalRecords;
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
        public async Task<IActionResult> _GetCompletedExByUser(long? userId = null, int start = 0, int length = 5, string keyword = "", [FromForm] List<OrderParameter> order = null )
        {
            var draw = Request.Form["draw"].ToString();
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
                .Where(e => completedExerciseIdsQuery.Contains(e.Id) && e.IsAccept == true && e.AccessId == 3)
                .Join(_context.DifficultyLevels,
                    e => e.DifficultyId,
                    d => d.Id,
                    (e, d) => new 
                    { 
                        Exercise = e, 
                        DifficultyId = d.Id,
                        Difficulty = d.DifficultyName,
                        FirstMarkingTime = _context.Markings
                            .Where(m => m.UserId == userId && m.ExerciseId == e.Id)
                            .OrderBy(m => m.MarkingDate)
                            .Select(m => m.MarkingDate)
                            .FirstOrDefault(),
                        FirstCorrectMarkingTime = _context.Markings
                            .Where(m => m.UserId == userId && m.ExerciseId == e.Id && m.IsAllCorrect == true)
                            .OrderBy(m => m.MarkingDate)
                            .Select(m => m.MarkingDate)
                            .FirstOrDefault()
                    });

            if (!string.IsNullOrEmpty(keyword))
            {
                keyword = keyword.ToLower();
                query = query.Where(x => x.Exercise.ExerciseCode.ToLower().Contains(keyword) ||
                                        x.Exercise.ExerciseName.ToLower().Contains(keyword));
            }

            // Lấy tổng số bản ghi
            var totalRecords = await query.CountAsync();

            // Xử lý sắp xếp
            if (order != null && order.Count > 0)
            {
                foreach (var ord in order)
                {
                    var column = ord.column;
                    var dir = ord.dir;
                    if (column == "0") // ExerciseCode
                    {
                        query = dir == "asc" ? query.OrderBy(x => x.Exercise.ExerciseCode) : query.OrderByDescending(x => x.Exercise.ExerciseCode);
                    }
                    else if (column == "1") // ExerciseName
                    {
                        query = dir == "asc" ? query.OrderBy(x => x.Exercise.ExerciseName) : query.OrderByDescending(x => x.Exercise.ExerciseName);
                    }
                    else if (column == "2") // Difficulty
                    {
                        query = dir == "asc" ? query.OrderBy(x => x.DifficultyId) : query.OrderByDescending(x => x.DifficultyId);
                    }
                    else if (column == "3") // FirstMarkingTime
                    {
                        query = dir == "asc" ? query.OrderBy(x => x.FirstMarkingTime) : query.OrderByDescending(x => x.FirstMarkingTime);
                    }
                    else if (column == "4") // FirstCorrectMarkingTime
                    {
                        query = dir == "asc" ? query.OrderBy(x => x.FirstCorrectMarkingTime) : query.OrderByDescending(x => x.FirstCorrectMarkingTime);
                    }
                }
            }

            // Lấy dữ liệu phân trang
            var completedExercises = await query
                .Skip(start)
                .Take(length)
                .Select(x => new
                {
                    x.Exercise.Id,
                    x.Exercise.ExerciseCode,
                    x.Exercise.ExerciseName,
                    Difficulty = x.Difficulty,
                    FirstMarkingTime = x.FirstMarkingTime,
                    FirstCorrectMarkingTime = x.FirstCorrectMarkingTime
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
            ViewBag.ProgramLanguageList = _context.ProgramLanguages.ToList();
            return View();
        }

        [HttpPost]
        [RequestSizeLimit(2L * 1024 * 1024 * 1024)]
        public async Task<IActionResult> Create([FromBody] Exercise model)
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
                return Json(new { status = true, redirect = Url.Action("Index"), isAccept = model.IsAccept, message = model.IsAccept ? "Đăng bài thành công" : "Bài của bạn đang chờ kiểm duyệt..." });
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
            ViewBag.ProgramLanguageList = _context.ProgramLanguages.ToList();
            return View(exercise);
        }

        [HttpPost]
        public async Task<IActionResult> Edit([FromBody]Exercise model)
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
                if (!(userRole == "admin" || userRole == "teacher" || exercise.UserCreatedId == userId))
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


        public IActionResult Code(long? id, bool viewCode = false)
        {
            if (id == null)
            {
                return View("Error");
            }
            var exercise = _context.Exercises
                .SingleOrDefault(ex => ex.Id == id);
            if (exercise == null)
            {
                return View("Error");
            }

            long userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var user = _context.Users.Find(userId);

            if (user == null)
            {
                return View("Error");
            }

            if (exercise.IsAccept == false && userId != exercise.UserCreatedId && user.AuthId != 1 && user.AuthId != 2)
            {
                return View("NotAccess");
            }

            if (exercise.AccessId == 1 && userId != exercise.UserCreatedId && user.AuthId != 1 && user.AuthId != 2)
            {
                return View("NotAccess");
            }

            if(viewCode)
            {
                var sourceCode = _context.Markings
                    .AsNoTracking()
                    .Where(m => m.ExerciseId == id && m.UserId == userId && m.IsAllCorrect == true)
                    .OrderByDescending(m => m.MarkingDate)
                    .FirstOrDefault()?.SourceCode;
                ViewBag.SourceCode = sourceCode ?? string.Empty;
            }

            ViewBag.ListTheme = _context.Themes.ToList();
            ViewBag.UserInfo = user;
            ViewBag.ProgramLanguageList = _context.ProgramLanguages.ToList();
            ViewBag.Difficulty = _context.DifficultyLevels.ToList();

            ViewBag.CreatedUser = _context.Users.SingleOrDefault(user => user.Id == exercise.UserCreatedId);
            return View(exercise);
        }


        [HttpPost]
        public async Task<IActionResult> GenerateOutput([FromBody] GenerateOutputViewModel model)
        {
            if (model == null || model.SampleOutputs.Count == 0)
            {
                return Json(new { status = false, message = "Không có dữ liệu để tạo đầu ra!" });
            }
            try
            {
                long userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                model.UserId = userId;
                var result = await _generateSampleOutputService.GenerateSampleOutputAsync(model);
                return Json(new { status = true, data = result });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Lỗi khi tạo đầu ra: " + ex.Message });
            }
        }










        [HttpGet]
        public async Task<IActionResult> GetComments(long exerciseId)
        {
            var userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "student";
            var comments = await _context.CommentToExercises
                .Include(c => c.User)
                .Include(c => c.InverseAnswerToCmt)
                    .ThenInclude(r => r.User)
                .Include(c => c.UserContactToCommentExes)
                .Where(c => c.ExerciseId == exerciseId && c.IsApproved)
                .OrderBy(c => c.CreatedDate)
                .ToListAsync();

            var commentDtos = comments
                .Select(c => new
                {
                    c.Id,
                    c.Content,
                    Username = c.User.Username,
                    CreatedDate = c.CreatedDate.ToString("o"),
                    LikeCount = c.UserContactToCommentExes.Count(uc => uc.IsLike),
                    DislikeCount = c.UserContactToCommentExes.Count(uc => !uc.IsLike),
                    UserLiked = c.UserContactToCommentExes.Any(uc => uc.UserId == userId && uc.IsLike),
                    UserDisliked = c.UserContactToCommentExes.Any(uc => uc.UserId == userId && !uc.IsLike),
                    CanDelete = userRole == "admin" || userRole == "teacher" || c.UserId == userId,
                    UserId = c.UserId,
                    AnswerToUser = c.AnswerToCmtId.HasValue ? c.AnswerToCmt?.User.Username : null,
                    AnswerToUserId = c.AnswerToCmtId.HasValue ? c.AnswerToCmt?.UserId : null,
                }).ToList();

            return Json(new { status = true, comments = commentDtos });
        }

        [HttpPost]
        public async Task<IActionResult> AddComment(long ExerciseId, string Content, long? AnswerToCmtId)
        {
            if (string.IsNullOrEmpty(Content))
            {
                return Json(new { status = false, message = "Nội dung bình luận không được để trống!" });
            }

            var userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var comment = new CommentToExercise
            {
                ExerciseId = ExerciseId,
                UserId = userId,
                Content = Content,
                CreatedDate = DateTime.UtcNow,
                IsApproved = true,
                AnswerToCmtId = AnswerToCmtId
            };

            _context.CommentToExercises.Add(comment);
            await _context.SaveChangesAsync();

            return Json(new { status = true, message = "Bình luận đã được gửi!" });
        }

        [HttpPost]
        public async Task<IActionResult> LikeComment(long commentId)
        {
            var userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var comment = await _context.CommentToExercises.FindAsync(commentId);
            if (comment == null)
            {
                return Json(new { status = false, message = "Bình luận không tồn tại!" });
            }

            var contact = await _context.UserContactToCommentExes
                .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.CommentExId == commentId);

            if (contact == null)
            {
                // Chưa tương tác: Thêm Like
                _context.UserContactToCommentExes.Add(new UserContactToCommentEx
                {
                    UserId = userId,
                    CommentExId = commentId,
                    IsLike = true,
                    ContactDate = DateTime.UtcNow
                });
            }
            else if (contact.IsLike)
            {
                // Đã Like: Xóa tương tác
                _context.UserContactToCommentExes.Remove(contact);
            }
            else
            {
                // Đã Dislike: Chuyển sang Like
                contact.IsLike = true;
                contact.ContactDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return Json(new { status = true });
        }

        [HttpPost]
        public async Task<IActionResult> DislikeComment(long commentId)
        {
            var userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var comment = await _context.CommentToExercises.FindAsync(commentId);
            if (comment == null)
            {
                return Json(new { status = false, message = "Bình luận không tồn tại!" });
            }

            var contact = await _context.UserContactToCommentExes
                .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.CommentExId == commentId);

            if (contact == null)
            {
                // Chưa tương tác: Thêm Dislike
                _context.UserContactToCommentExes.Add(new UserContactToCommentEx
                {
                    UserId = userId,
                    CommentExId = commentId,
                    IsLike = false,
                    ContactDate = DateTime.UtcNow
                });
            }
            else if (!contact.IsLike)
            {
                // Đã Dislike: Xóa tương tác
                _context.UserContactToCommentExes.Remove(contact);
            }
            else
            {
                // Đã Like: Chuyển sang Dislike
                contact.IsLike = false;
                contact.ContactDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return Json(new { status = true });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteComment(long commentId)
        {
            var userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "");
            var comment = await _context.CommentToExercises
                .Include(c => c.InverseAnswerToCmt)
                .FirstOrDefaultAsync(c => c.Id == commentId);

            if (comment == null)
            {
                return Json(new { status = false, message = "Bình luận không tồn tại!" });
            }
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "student";
            if (!(userRole == "admin" || userRole == "teacher" || comment.UserId == userId))
            {
                return Json(new { status = false, message = "Bạn không có quyền xóa bình luận này!" });
            }

            // Thu thập tất cả bình luận cần xóa (đệ quy)
            var commentIdsToDelete = new List<long>();
            CollectCommentIds(comment, commentIdsToDelete);

            // Xóa tương tác Like/Dislike
            var contacts = await _context.UserContactToCommentExes
                .Where(uc => commentIdsToDelete.Contains(uc.CommentExId))
                .ToListAsync();
            _context.UserContactToCommentExes.RemoveRange(contacts);

            // Xóa bình luận
            var commentsToDelete = await _context.CommentToExercises
                .Where(c => commentIdsToDelete.Contains(c.Id))
                .ToListAsync();
            _context.CommentToExercises.RemoveRange(commentsToDelete);

            await _context.SaveChangesAsync();
            return Json(new { status = true, message = "Bình luận đã được xóa!" });
        }

        private void CollectCommentIds(CommentToExercise comment, List<long> commentIds)
        {
            commentIds.Add(comment.Id);
            foreach (var reply in comment.InverseAnswerToCmt)
            {
                CollectCommentIds(reply, commentIds);
            }
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

        
        
    
    }
}