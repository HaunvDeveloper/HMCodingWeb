using HMCodingWeb.Hubs;
using HMCodingWeb.Models;
using HMCodingWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HMCodingWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "admin")]
    public class NotificationController : Controller
    {
        private readonly OnlineCodingWebContext _context;
        private readonly EmailSendService _emailSendService;
        private readonly IHubContext<OnlineUsersHub> _hubContext;

        public NotificationController(OnlineCodingWebContext context, EmailSendService emailSendService, IHubContext<OnlineUsersHub> hubContext)
        {
            _context = context;
            _emailSendService = emailSendService;
            _hubContext = hubContext;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Create()
        {
            ViewBag.Types = new List<SelectListItem>
            {
                new SelectListItem { Text = "Hệ thống", Value = "system" },
                new SelectListItem { Text = "Người dùng", Value = "user" },
            };
            ViewBag.Users = _context.Users
                .Select(u => new User() {Id = u.Id, Username = u.Username })
                .OrderBy(u => u.Username)
                .ToList();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Notification model, long[] selectedUsers)
        {

            var userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0)
            {
                return Json(new { status = false, message = "Người dùng không hợp lệ." });
            }

            var notification = new Notification
            {
                Title = model.Title,
                Message = model.Message,
                Type = model.Type,
                IsGlobal = model.IsGlobal,
                IsImportant = model.IsImportant,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId,
                CreatedByUsername = User.FindFirst(ClaimTypes.Name)?.Value,
                IsSendEmail = model.IsSendEmail
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            if (!model.IsGlobal && selectedUsers != null && selectedUsers.Length > 0)
            {
                foreach (var uid in selectedUsers)
                {
                    _context.NotificationSeenStatuses.Add(new NotificationSeenStatus
                    {
                        NotificationId = notification.Id,
                        UserId = uid,
                        IsSeen = false
                    });
                    await _hubContext.Clients.User(uid.ToString()).SendAsync("ReceiveNotification", true);
                }
                await _context.SaveChangesAsync();
                
            }
            else
            {
                var allUsers = await _context.Users.Select(u => u.Id.ToString()).ToListAsync();
                foreach (var uid in allUsers)
                {
                    await _hubContext.Clients.User(uid).SendAsync("ReceiveNotification", true);
                }
            }

            // TODO: Xử lý gửi email nếu IsSendEmail = true
            if (model.IsSendEmail)
            {
                _emailSendService.SendNotificationToUser(
                    notification.Title,
                    notification.Message,
                    selectedUsers ?? new long[] { }
                ).Wait();
            }

            return Json(new { status = true, message = "Tạo thông báo thành công!" });
        }

        public async Task<IActionResult> Edit(long id)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id);

            if (notification == null)
            {
                return NotFound();
            }

            ViewBag.Types = new List<SelectListItem>
            {
                new SelectListItem { Text = "Hệ thống", Value = "system" },
                new SelectListItem { Text = "Người dùng", Value = "user" },
            };

            ViewBag.Users = await _context.Users
                .Select(u => new User(){Id= u.Id,Username= u.Username })
                .OrderBy(u => u.Username)
                .ToListAsync();

            ViewBag.SelectedUsers = await _context.NotificationSeenStatuses
                .Where(s => s.NotificationId == id)
                .Select(s => s.UserId)
                .ToListAsync();

            return View(notification);
        }

        // Action Edit (POST): Cập nhật thông báo
        [HttpPost]
        public async Task<IActionResult> Edit(Notification model, long[] selectedUsers)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { status = false, message = "Dữ liệu không hợp lệ." });
            }

            var userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0)
            {
                return Json(new { status = false, message = "Người dùng không hợp lệ." });
            }

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == model.Id);

            if (notification == null)
            {
                return Json(new { status = false, message = "Không tìm thấy thông báo." });
            }

            // Cập nhật thông báo
            notification.Title = model.Title;
            notification.Message = model.Message;
            notification.Type = model.Type;
            notification.IsGlobal = model.IsGlobal;
            notification.IsImportant = model.IsImportant;

            // Xử lý danh sách người dùng nhận thông báo
            if (!model.IsGlobal)
            {
                // Xóa tất cả NotificationSeenStatus hiện tại
                var existingStatuses = await _context.NotificationSeenStatuses
                    .Where(s => s.NotificationId == model.Id)
                    .ToListAsync();
                _context.NotificationSeenStatuses.RemoveRange(existingStatuses);

                // Thêm mới NotificationSeenStatus cho selectedUsers
                if (selectedUsers != null && selectedUsers.Length > 0)
                {
                    foreach (var selectedUserId in selectedUsers)
                    {
                        _context.NotificationSeenStatuses.Add(new NotificationSeenStatus
                        {
                            NotificationId = model.Id,
                            UserId = selectedUserId,
                            IsSeen = false
                        });
                        await _hubContext.Clients.User(selectedUserId.ToString()).SendAsync("ReceiveNotification", true);
                    }
                }
            }
            else
            {
                // Nếu IsGlobal = true, xóa tất cả NotificationSeenStatus
                var existingStatuses = await _context.NotificationSeenStatuses
                    .Where(s => s.NotificationId == model.Id)
                    .ToListAsync();
                _context.NotificationSeenStatuses.RemoveRange(existingStatuses);
                var allUsers = await _context.Users.Select(u => u.Id.ToString()).ToListAsync();
                foreach (var uid in allUsers)
                {
                    await _hubContext.Clients.User(uid).SendAsync("ReceiveNotification", true);
                }
            }

            await _context.SaveChangesAsync();

            // TODO: Xử lý gửi email nếu IsSendEmail = true
            if (model.IsSendEmail)
            {
                _emailSendService.SendNotificationToUser(
                    notification.Title,
                    notification.Message,
                    selectedUsers ?? new long[] { }
                ).Wait();
            }

            return Json(new { status = true, message = "Cập nhật thông báo thành công!" });
        }



        public async Task<IActionResult> Delete(long id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null)
            {
                return Json(new { status = false, message = "Thông báo không tồn tại." });
            }
            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();



            return Json(new { status = true, message = "Xóa thông báo thành công!" });
        }

        [HttpPost]
        public async Task<IActionResult> _GetList(int draw, int start, int length, string keyword = "")
        {
            var query = _context.Notifications
                .Include(n => n.NotificationSeenStatuses)
                .AsQueryable();

            // Apply search filter
            if (!string.IsNullOrEmpty(keyword))
            {
                keyword = keyword.ToLower();
                query = query.Where(u => u.Title.ToLower().Contains(keyword) ||
                                        (u.Message != null && u.Message.ToLower().Contains(keyword)));
            }

            // Get total records
            var totalRecords = await query.CountAsync();

            var users = await query
                .Skip(start)
                .Take(length)
                .Select(u => new
                {
                    id = u.Id,
                    title = u.Title,
                    createdAt = u.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                    createdByUsername = u.CreatedByUsername,
                    seenCount = u.NotificationSeenStatuses.Count(ns => ns.IsSeen),
                    isGlobal = u.IsGlobal ? "Yes" : "No",
                    isImportant = u.IsImportant ? "Yes" : "No",
                    isSendEmail = u.IsSendEmail ? "Yes" : "No",
                    type = u.Type,

                })
                .AsNoTracking()
                .ToListAsync();

            return Json(new
            {
                draw = draw,
                recordsTotal = totalRecords,
                recordsFiltered = totalRecords,
                data = users
            });
        }
    }
}
