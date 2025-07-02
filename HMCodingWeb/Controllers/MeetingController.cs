using HMCodingWeb.Models;
using HMCodingWeb.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HMCodingWeb.Controllers
{
    [Authorize]
    public class MeetingController : Controller
    {
        private readonly OnlineCodingWebContext _context;

        public MeetingController(OnlineCodingWebContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> _GetList(int draw, int start = 0, int length = 10, string keyword="")
        {
            long userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            // Query meeting
            var query = _context.Meetings
                .Where(m =>
                    !m.IsPrivate
                    || m.MeetingParticipants.Any(p => p.UserId == userId)
                );

            // Filter search keyword
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(m => m.Title.Contains(keyword));
            }

            var totalCount = await query.CountAsync();

            // Sorting
            query = query.OrderByDescending(m => m.CreatedAt);

            // Paging
            var meetings = await query
                .Skip(start)
                .Take(length)
                .Select(m => new
                {
                    m.Id,
                    m.Title,
                    m.Description,
                    m.IsPrivate,
                    CreatedAt = m.CreatedAt.ToString("dd/MM/yyyy HH:mm:ss"),
                })
                .ToListAsync();

            return Json(new
            {
                draw = draw,
                data = meetings,
                recordsTotal = totalCount,
                recordsFiltered = totalCount
            });
        }

        public async Task<IActionResult> Join(Guid id)
        {
            long userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var meeting = await _context.Meetings
                .Include(m => m.MeetingParticipants)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (meeting == null)
                return NotFound();

            if (meeting.IsPrivate)
            {
                var isAllowed = meeting.MeetingParticipants.Any(p => p.UserId == userId);
                if (!isAllowed)
                    return Forbid(); // Hoặc return View("AccessDenied");
            }

            var model = new JoinMeetingViewModel
            {
                MeetingId = meeting.Id,
                Title = meeting.Title,
                Description = meeting.Description,
                IsPrivate = meeting.IsPrivate,
                HostUserId = meeting.HostUserId,
                StartTime = meeting.StartTime,
                EndTime = meeting.EndTime
            };

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> JoinConfirm(Guid meetingId, string MuteMic, string DisableCamera)
        {
            long userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            bool muteMic = MuteMic == "on";
            bool disableCamera = DisableCamera == "on";

            var meeting = await _context.Meetings
                .Include(m => m.MeetingParticipants)
                .FirstOrDefaultAsync(m => m.Id == meetingId);

            if (meeting == null)
                return NotFound();

            if (meeting.IsPrivate)
            {
                var isAllowed = meeting.MeetingParticipants.Any(p => p.UserId == userId);
                if (!isAllowed)
                    return Forbid();
            }

            // Ghi nhận tham gia
            var participant = meeting.MeetingParticipants.FirstOrDefault(p => p.UserId == userId);
            if (participant != null && participant.JoinedAt == null)
            {
                participant.JoinedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            // Chuyển đến phòng họp, truyền trạng thái mic/camera (nếu cần)
            return RedirectToAction("Room", new { id = meetingId, muteMic = muteMic, disableCamera = disableCamera });
        }

        [HttpGet]
        public async Task<IActionResult> Room(Guid id, bool muteMic, bool disableCamera)
        {
            long userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var meeting = await _context.Meetings
                .Include(m => m.MeetingParticipants)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (meeting == null)
                return NotFound();

            if (meeting.IsPrivate)
            {
                var isAllowed = meeting.MeetingParticipants.Any(p => p.UserId == userId);
                if (!isAllowed && meeting.HostUserId != userId)
                    return Forbid();
            }

            // Create view model
            var viewModel = new RoomViewModel
            {
                MeetingId = id,
                UserId = userId,
                MuteMic = muteMic,
                DisableCamera = disableCamera,
                MeetingTitle = meeting.Title
            };

            return View(viewModel);
        }
    

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateMeetingViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            long userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var meeting = new Meeting
            {
                Id = Guid.NewGuid(),
                Title = model.Title,
                Description = model.Description,
                StartTime = model.StartTime,
                EndTime = model.EndTime,
                IsPrivate = model.IsPrivate,
                IsRequireToJoin = model.IsRequireToJoin,
                HostUserId = userId, // tự viết hàm lấy user đăng nhập
                CreatedAt = DateTime.Now
            };

            _context.Meetings.Add(meeting);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

    }
}
