using HMCodingWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HMCodingWeb.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly OnlineCodingWebContext _context;
        public NotificationController(OnlineCodingWebContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        // Action hiện có: Lấy danh sách thông báo
        public async Task<IActionResult> GetUserNotifications(int start = 0)
        {
            var userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0)
            {
                return Json(new { notifications = new List<object>(), unseenCount = 0 });
            }
            
            var unseenCount = await _context.Notifications
                .Include(x => x.NotificationSeenStatuses)
                .Where(n => 
                    (
                        n.IsGlobal 
                        || n.NotificationSeenStatuses.Any(s => s.UserId == userId)
                    ) 
                    && !n.NotificationSeenStatuses.Any(s => s.UserId == userId && s.IsSeen))
                .CountAsync();

            var notifications = await _context.Notifications
                .Include(x => x.NotificationSeenStatuses)
                .Where(n => n.IsGlobal || n.NotificationSeenStatuses.Any(s => s.UserId == userId))
                .Select(n => new
                {
                    n.Id,
                    n.Title,
                    Message = n.Message.Length > 20 ? n.Message.Substring(0, 20) + "..." : n.Message, // Cắt ngắn trong dropdown
                    n.CreatedAt,
                    n.CreatedByUsername,
                    n.Type,
                    n.IsImportant,
                    IsSeen = n.NotificationSeenStatuses.Any(s => s.UserId == userId && s.IsSeen)
                })
                .OrderByDescending(n => n.CreatedAt)
                .Skip(start)
                .Take(5)
                .ToListAsync();

            return Json(new { notifications, length = notifications.Count, unseenCount });
        }

        // Action mới: Lấy chi tiết thông báo theo Id
        public async Task<IActionResult> GetNotificationById(long notificationId)
        {
            var userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0)
            {
                return Json(new { success = false, message = "User không hợp lệ." });
            }

            var notification = await _context.Notifications
                .Where(n => n.Id == notificationId && (n.IsGlobal || n.NotificationSeenStatuses.Any(s => s.UserId == userId)))
                .Select(n => new
                {
                    n.Id,
                    n.Title,
                    n.Message, // Lấy toàn bộ nội dung
                    n.CreatedAt,
                    n.CreatedByUsername,
                    n.Type,
                    n.IsImportant,
                    IsSeen = n.NotificationSeenStatuses.Any(s => s.UserId == userId && s.IsSeen)
                })
                .FirstOrDefaultAsync();

            if (notification == null)
            {
                return Json(new { success = false, message = "Không tìm thấy thông báo." });
            }

            return Json(new { success = true, notification });
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsSeen(long notificationId)
        {
            var userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0)
            {
                return Json(new { success = false });
            }

            var seenStatus = await _context.NotificationSeenStatuses
                .FirstOrDefaultAsync(s => s.UserId == userId && s.NotificationId == notificationId);

            if (seenStatus != null && seenStatus.IsSeen == false)
            {
                seenStatus.IsSeen = true;
                seenStatus.SeenAt = DateTime.Now;
            }
            else if (await _context.Notifications.AnyAsync(n => n.Id == notificationId && n.IsGlobal))
            {
                _context.NotificationSeenStatuses.Add(new NotificationSeenStatus
                {
                    UserId = userId,
                    NotificationId = notificationId,
                    IsSeen = true,
                    SeenAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
    }
}
