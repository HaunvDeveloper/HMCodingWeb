using HMCodingWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HMCodingWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "admin,teacher")]
    public class ChapterController : Controller
    {
        private readonly OnlineCodingWebContext _context;
        public ChapterController(OnlineCodingWebContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Chapter model)
        {
            if (ModelState.IsValid)
            {
                // Check if chapter code already exists
                if (_context.Chapters.Any(c => c.ChapterCode == model.ChapterCode))
                {
                    return Json(new { status = false, message = "Chapter code already exists. Please choose a different code." });
                }
                _context.Chapters.Add(model);
                await _context.SaveChangesAsync();
                return Json(new {status=true, message = "Chapter created successfully!", chapterId = model.Id, chapterName = model.ChapterName });
            }
            return Json(new { status = false, message = "Failed to create chapter. Please check the input." });
        }
    }
}
