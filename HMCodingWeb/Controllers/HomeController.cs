using HMCodingWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Diagnostics;
using System.Security.Claims;

namespace HMCodingWeb.Controllers
{
    [AllowAnonymous]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly OnlineCodingWebContext _context;
        public HomeController(ILogger<HomeController> logger, OnlineCodingWebContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            ViewBag.NewExercises = _context.Exercises
                .OrderByDescending(e => e.CreatedDate)
                .Take(10)
                .Select(e => new Exercise
                {
                    Id = e.Id,
                    ExerciseCode = e.ExerciseCode,
                    ExerciseName = e.ExerciseName,
                })
                .ToList();
            return View();
        }



        [HttpGet]
        public IActionResult GetLatestAnnouncements(int start = 0, int length = 5)
        {
            var announcements = _context.Announcements
                .Include(x => x.CreatedByUser)
                .Where(a => a.IsVisible)
                .OrderByDescending(a => a.CreatedAt)
                .Skip(start)
                .Take(length)
                .Select(a => new
                {
                    a.Id,
                    a.Title,
                    a.Content,
                    a.CreatedByUserId,
                    CreatedByUserName = a.CreatedByUser.Username,
                    CreatedAt = a.CreatedAt.ToString("dd-MM-yyyy HH:mm"),
                    CanEdit = User.IsInRole("admin") || User.IsInRole("teacher")
                })
                .ToList();

            return Json(announcements);
        }


        [HttpPost]
        public IActionResult CreateAnnouncement(Announcement model)
        {
            if (string.IsNullOrEmpty(model.Title) || string.IsNullOrEmpty(model.Content))
            {
                return Json(new { status = false, message = "Title and Content cannot be empty." });
            }

            model.CreatedByUserId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            model.CreatedAt = DateTime.Now;
            model.IsVisible = true;

            _context.Announcements.Add(model);
            _context.SaveChanges();
            return Json(new
            {
                status = true,
                message = "Announcement created successfully."
            });
        }

        [HttpPost]
        public IActionResult EditAnnouncement(int id, string title, string content)
        {
            var announcement = _context.Announcements.FirstOrDefault(a => a.Id == id);
            if (announcement == null)
                return Json(new { status = false, message = "Không tìm thấy thông báo" });

            // Check quyền sửa
            var currentUserId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var isAdminOrTeacher = User.IsInRole("admin") || User.IsInRole("teacher");

            if (announcement.CreatedByUserId != currentUserId && !isAdminOrTeacher)
                return Json(new { status = false, message = "Bạn không có quyền sửa thông báo này" });

            announcement.Content = content;
            announcement.Title = title;
            _context.SaveChanges();

            return Json(new { status = true });
        }

        [HttpPost]
        public IActionResult DeleteAnnouncement(int id)
        {
            var announcement = _context.Announcements.FirstOrDefault(a => a.Id == id);
            if (announcement == null)
                return Json(new { status = false, message = "Không tìm thấy thông báo" });
            // Check quyền xóa
            var currentUserId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var isAdminOrTeacher = User.IsInRole("admin") || User.IsInRole("teacher");
            if (announcement.CreatedByUserId != currentUserId && !isAdminOrTeacher)
                return Json(new { status = false, message = "Bạn không có quyền xóa thông báo này" });
            _context.Announcements.Remove(announcement);
            _context.SaveChanges();
            return Json(new { status = true });
        }



        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}