using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HMCodingWeb.Models;
using HMCodingWeb.Services;
using HMCodingWeb.ViewModels;
using System.Security.Claims;
using System.IO;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HMCodingWeb.Controllers
{
    [Authorize]
    public class ExerciseController : Controller
    {
        private readonly ILogger<ExerciseController> _logger;
        private readonly OnlineCodingWebContext _context;
        private readonly RunProcessService _runProcessService;

        public ExerciseController(ILogger<ExerciseController> logger, OnlineCodingWebContext context, RunProcessService runProcessService)
        {
            _logger = logger;
            _context = context;
            _runProcessService = runProcessService;
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
        public IActionResult _GetList(int p = 1, int s = 10, string key = "", int? dId = null, int? cId = null, int? etId = null, string? tM = null, string? kM = null)
        {


            var query = _context.Exercises
                .Where(ex => ex.IsAccept == true && ex.AccessId == 3);

            if (!string.IsNullOrEmpty(key))
            {
                key = key.ToLower();
                query = query.Where(ex => ex.ExerciseName.ToLower().Contains(key) ||
                    ex.ExerciseCode.ToLower().Contains(key)
                );
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
                query = query.Where(ex => ex.KindMarking == tM);
            }

            var totalRecords = query.Count();

            var listData = query
                .Include(ex => ex.Difficulty)
                .Include(ex => ex.Chapter)
                .Include(ex => ex.ExerciseBelongTypes)
                    .ThenInclude(ebt => ebt.ExerciseType)
                .AsNoTracking()
                .OrderByDescending(ex => ex.CreatedDate)
                .Skip((p - 1) * s)
                .Take(s)
                .Select(ex => new Exercise
                {
                    Id = ex.Id,
                    ExerciseName = ex.ExerciseName,
                    ExerciseCode = ex.ExerciseCode,
                    Difficulty = ex.Difficulty,
                    Chapter = ex.Chapter,
                    ExerciseBelongTypes = ex.ExerciseBelongTypes
                        .Select(ebt => new ExerciseBelongType
                        {
                            ExerciseType = ebt.ExerciseType
                        }).ToList()
                })
                .ToList();


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
            return PartialView(listData);
        }


        [Authorize]
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

    }
}