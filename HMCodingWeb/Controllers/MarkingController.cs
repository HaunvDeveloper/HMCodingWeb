using HMCodingWeb.Hubs;
using HMCodingWeb.Models;
using HMCodingWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Drawing.Printing;
using System.Security.Claims;

namespace HMCodingWeb.Controllers
{
    [Authorize]
    public class MarkingController : Controller
    {
        private readonly ILogger<MarkingController> _logger;
        private readonly OnlineCodingWebContext _context;
        private readonly MarkingService _markingService;
        private readonly UserPointService _userPointService;
        private readonly RankingService _rankingService;
        private readonly IHubContext<MarkingHub> _hubContext;

        public MarkingController(ILogger<MarkingController> logger, OnlineCodingWebContext context, MarkingService markingService, UserPointService userPointService, RankingService rankingService, IHubContext<MarkingHub> hubContext)
        {
            _logger = logger;
            _context = context;
            _markingService = markingService;
            _userPointService = userPointService;
            _rankingService = rankingService;
            _hubContext = hubContext;
        }

        [AllowAnonymous]
        public IActionResult Index(long exId, bool myPost)
        {
            ViewBag.ExerciseId = exId;
            if(exId > 0)
            {
                ViewBag.ExerciseCode = _context.Exercises
                    .Where(e => e.Id == exId)
                    .Select(e => e.ExerciseCode)
                    .FirstOrDefault() ?? "Bài tập không tồn tại";
            }
            ViewBag.IsMyPost = myPost ? "checked" : "";
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> _GetList(long userId = 0, long exerciseId = 0, int start = 0, int length = 25, string keyword = "", bool isMyPost = false, bool isCorrectOnly = false, [FromForm] Dictionary<string, string>[] order = null)
        {
            var draw = Request.Form["draw"].ToString();
            var query = _context.Markings
                .Include(mk => mk.Exercise)
                .Include(mk => mk.User)
                .Include(mk => mk.ProgramLanguage)
                .AsQueryable();

            if(isMyPost)
            {
                long currentUserId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                query = query.Where(mk => mk.UserId == currentUserId);
            }
            else if (userId > 0)
            {
                query = query.Where(mk => mk.UserId == userId);
            }

            if (isCorrectOnly)
            {
                query = query.Where(mk => mk.IsAllCorrect == true);
            }

            if (exerciseId > 0)
            {
                query = query.Where(mk => mk.ExerciseId == exerciseId);
            }

            if (!string.IsNullOrEmpty(keyword))
            {
                keyword = keyword.Trim().ToLower();
                query = query.Where(mk => mk.Exercise.ExerciseName.ToLower().Contains(keyword) ||
                                          mk.User.Username.ToLower().Contains(keyword) ||
                                          mk.ProgramLanguage.ProgramLanguageName.ToLower().Contains(keyword));
            }    

            var totalRecords = await query.CountAsync();

            // Apply sorting
            if (order != null && order.Length > 0)
            {
                IOrderedQueryable<Marking> orderedQuery = null;
                for (int i = 0; i < order.Length; i++)
                {
                    var columnIndex = int.Parse(order[i]["column"]);
                    var direction = order[i]["dir"].ToLower();
                    var columnName = _markingService.GetColumnName(columnIndex);
                    orderedQuery = _markingService.ApplyOrder(query, columnName, direction);
                }
                query = orderedQuery ?? query;
            }
            else
            {
                // Default sorting: MarkingDate descending
                query = query.OrderByDescending(x => x.MarkingDate);
            }




                var list = await query
                    .Skip(start)
                    .Take(length)
                    .Select(x => new
                    {
                        MarkingDate = x.MarkingDate.ToString("HH:mm:ss dd-MM-yyyy"),
                        UserName = x.User.Username,
                        UserFullname = x.User.Fullname,
                        Avatar = x.User.AvartarImage != null ? $"/api/avatar/{x.UserId}" : null,
                        ExerciseName = x.Exercise.ExerciseName,
                        ExerciseCode = x.Exercise.ExerciseCode,
                        ProgramLanguageName = x.ProgramLanguage.ProgramLanguageName,
                        Status = x.Status,
                        KindMarking = x.KindMarking,
                        MarkingId = x.Id,
                        CorrectTestNumber = x.CorrectTestNumber.ToString() + "/" + x.TotalTestNumber,
                        UserId = x.UserId,
                        ResultContent = x.ResultContent,
                        TimeSpent = x.TimeSpent.Value.ToString("hh\\:mm\\:ss"),
                        ExerciseId = x.ExerciseId,
                        Score = x.Score,
                    })
                    .ToListAsync();

            return Json(new
            {
                draw = draw,
                recordsTotal = totalRecords,
                recordsFiltered = totalRecords,
                data = list
            });
        }


        [HttpGet]
        public async Task<IActionResult> Detail(long id)
        {
            long userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            string userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

            var userTheme = _context.Users
                .Include(u => u.ThemeCode)
                .Where(u => u.Id == userId)
                .Select(u => u.ThemeCode.ThemeCode)
                .FirstOrDefault();

            var marking = await _context.Markings
                .Include(x => x.ProgramLanguage)
                .Select(m => new
                {
                    Id = m.Id,
                    SourceCode = m.SourceCode,
                    ResultContent = m.ResultContent,
                    Theme = "ace/theme/" + userTheme,
                    ProgramLanguageName = m.ProgramLanguage.ProgramLanguageCode,
                    UserId = m.UserId,
                    ExerciseId = m.ExerciseId,
                })
                .FirstOrDefaultAsync(m => m.Id == id);


            if (marking == null)
            {
                return Json(new { status = false, error = "Không tìm thấy bài nộp" });
            }
            var isUserDone = _context.Markings
                .Any(x => x.IsAllCorrect == true && x.ExerciseId == marking.ExerciseId && x.UserId == userId);
            if (!(marking.UserId == userId || userRole == "admin" || userRole == "teacher" || isUserDone))
            {
                return Json(new { status = false, error = "Bạn không có quyền xem bài nộp này" });
            }
            return Json(new { status = true, data = marking });
        }



        [HttpGet]
        public IActionResult ViewDetailMarking(long id)
        {
            var marking = _context.Markings
                .Include(m => m.Exercise)
                .Include(p => p.ProgramLanguage)
                .Include(m => m.User)
                .FirstOrDefault(m => m.Id == id);

            long userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            string userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
            var isUserDone = _context.Markings
                .Any(x => x.IsAllCorrect == true && x.ExerciseId == marking.ExerciseId && x.UserId == userId);
            if (!(marking?.UserId == userId || userRole == "admin" || userRole == "teacher" || isUserDone))
            {
                return Unauthorized();
            }
            var userTheme = _context.Users
                .Include(u => u.ThemeCode)
                .Where(u => u.Id == userId)
                .Select(u => u.ThemeCode.ThemeCode)
                .FirstOrDefault();
            ViewBag.Theme = "ace/theme/" + userTheme;
            return View(marking);
        }

        [HttpGet]
        public async Task<IActionResult> _GetViewDetailMarking(long id)
        {
            long userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            string userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
            var marking = await _context.Markings
                .FirstOrDefaultAsync(m => m.Id == id);

            if (marking == null)
            {
                return Json(new { status = false, error = "Marking not found" });
            }

            var isUserDone = _context.Markings
                .Any(x => x.IsAllCorrect == true && x.ExerciseId == marking.ExerciseId && x.UserId == userId);
            var canViewAll = userRole == "admin" || userRole == "teacher" || isUserDone;

            var markingDetails = await _context.MarkingDetails
                .Where(md => md.MarkingId == id)
                .Select(md => new
                {
                    Id = md.Id,
                    TestCaseId = md.TestCaseId,
                    TestCaseIndex = md.TestCaseIndex,
                    Input = (!canViewAll && !md.IsCorrect)
                        ? "Không được xem"
                        : ((md.Input ?? "").Length > 50
                            ? (md.Input ?? "").Substring(0, 50) + "..."
                            : (md.Input ?? "")),
                                    Output = (!canViewAll && !md.IsCorrect)
                        ? "Không được xem"
                        : ((md.Output ?? "").Length > 50
                            ? (md.Output ?? "").Substring(0, 50) + "..."
                            : (md.Output ?? "")),
                                    CorrectOutput = (!canViewAll && !md.IsCorrect)
                        ? "Không được xem"
                        : ((md.CorrectOutput ?? "").Length > 50
                            ? (md.CorrectOutput ?? "").Substring(0, 50) + "..."
                            : (md.CorrectOutput ?? "")),
                    IsCorrect = md.IsCorrect,
                    RunTime = md.RunTime.ToString("0.#######") + " s",
                    IsTimeLimitExceed = md.IsTimeLimitExceed,
                    ErrorContent = md.ErrorContent,
                    IsError = md.IsError,
                })
                .ToListAsync();


            return Json(new {status=true, data=markingDetails});
        }

        [HttpPost]
        public async Task<IActionResult> GetFullTestCase(long markingDetailId)
        {
            
            var markingDetail = await _context.MarkingDetails
                .Include(md => md.Marking)
                .Where(md => md.Id == markingDetailId)
                .Select(md => new
                {
                    md.Input,
                    md.Output,
                    md.CorrectOutput,
                    md.IsCorrect,
                    md.RunTime,
                    md.IsTimeLimitExceed,
                    md.ErrorContent,
                    md.IsError,
                    ExerciseId = md.Marking.ExerciseId,
                })
                .FirstOrDefaultAsync();
            if (markingDetail == null)
            {
                return Json(new { status = false, error = "Không tìm thấy chi tiết bài nộp" });
            }


            long userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            string userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
            var isUserDone = _context.Markings
                .Any(x => x.IsAllCorrect == true && x.ExerciseId == markingDetail.ExerciseId && x.UserId == userId);
            var canViewAll = userRole == "admin" || userRole == "teacher" || isUserDone;
            if(!canViewAll && !markingDetail.IsCorrect)
            {
                return Json(new { status = false, error = "Bạn không có quyền xem chi tiết bài nộp này" });
            }

            return Json(new { status = true, data = markingDetail });
        }

        [HttpPost]
        public async Task<IActionResult> Marking(long ExerciseId, int ProgramLanguageId, string SourceCode)
        {
            try
            {
                if (HttpContext.Session.GetString("IsRunning") == "true")
                {
                    return Json(new { status = false, message = "Đang có một tiến trình chạy, vui lòng đợi!" });
                }
                HttpContext.Session.SetString("IsRunning", "true");
                long userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var marking = await _markingService.Marking(ExerciseId, ProgramLanguageId, SourceCode, userId);
                HttpContext.Session.SetString("IsRunning", "false");


                int pointGain = 0;
                if (marking.IsAllCorrect)
                {
                    // If the marking is successful, add points to the user
                    pointGain = await _userPointService.AddPointPassedExercise(userId, ExerciseId);
                }
                _context.Markings.Add(marking);
                await _context.SaveChangesAsync();

                //Send MarkingHub SendReloadMarkingList
                await _hubContext.Clients.All.SendAsync("AnnouncementToReload", true);

                var isGainRank = false;
                var newRank = "";
                if (pointGain > 0)
                {
                    var obj = await _rankingService.UpdateRankUser(userId);
                    isGainRank = obj.isGain;
                    newRank = obj.rankName;
                }
                return Json(new { status = true, data = new { marking.IsAllCorrect, marking.ResultContent, marking.Score, marking.IsError, pointGain, isGainRank, newRank, newId = marking.Id } });
            }
            catch (Exception ex)
            {
                HttpContext.Session.SetString("IsRunning", "false");
                _logger.LogError(ex, "Error during marking");
                return Json(new { status = false, error = ex.Message });
            }
        }


    }
}
