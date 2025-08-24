using HMCodingWeb.Hubs;
using HMCodingWeb.Models;
using HMCodingWeb.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace HMCodingWeb.Controllers
{
    [Authorize]
    public class MessageController : Controller
    {
        private readonly OnlineCodingWebContext _context;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly IMemoryCache _cache;
        public MessageController(OnlineCodingWebContext context, IHubContext<ChatHub> hubContext, IMemoryCache cache)
        {
            _context = context;
            _hubContext = hubContext;
            _cache = cache;
        }


        public IActionResult Index()
        {
            return View();
        }

        // API: Lấy danh sách box chat
        [HttpGet]
        public IActionResult GetBoxChats()
        {
            var currentUserId = GetCurrentUser(); 

            var boxChats = _context.BoxChatMembers
                .Where(m => m.UserId == currentUserId)
                .Select(m => new
                {
                    BoxChatId = m.BoxChatId,
                    IsGroup = m.BoxChat.IsGroup,
                    Participants = m.BoxChat.BoxChatMembers
                        .Where(x => x.UserId != currentUserId)
                        .Select(x => x.UserId).ToList(),

                    BoxName = m.BoxChat.IsGroup
                        ? (m.DisplayName ?? m.BoxChat.Name ?? "")
                        : _context.BoxChatMembers
                            .Where(x => x.BoxChatId == m.BoxChatId && x.UserId != currentUserId)
                            .Select(x => x.User.Fullname)
                            .FirstOrDefault() ?? "",
                    
                    AvatarUrl = m.BoxChat.IsGroup
                        ? "/api/avatar/group/" + m.BoxChatId
                        : "/api/avatar/" + (
                            _context.BoxChatMembers
                                .Where(x => x.BoxChatId == m.BoxChatId && x.UserId != currentUserId)
                                .Select(x => x.UserId)
                                .FirstOrDefault()
                          ),

                    LastMessageInfo = m.BoxChat.Messages
                        .OrderByDescending(msg => msg.CreatedAt)
                        .Select(msg => new
                        {
                            Content = msg.Content,
                            CreatedAt = msg.CreatedAt,
                            IsSeen = msg.MessageReadStatuses.Any(mrs => mrs.UserId == currentUserId)
                        })
                        .FirstOrDefault(),

                    UnreadCount = m.BoxChat.Messages
                        .Count(msg => !msg.MessageReadStatuses.Any(mrs => mrs.UserId == currentUserId)
                                      && msg.SenderId != currentUserId) // không tính tin mình gửi
                })
                .AsEnumerable()
                .Select(m => new BoxChatListItemVM
                {
                    BoxChatId = m.BoxChatId,
                    IsGroup = m.IsGroup,
                    Participants = m.Participants,
                    BoxName = m.BoxName,
                    AvatarUrl = m.AvatarUrl,
                    LastMessage = m.LastMessageInfo?.Content ?? "",
                    LastMessageTime = m.LastMessageInfo?.CreatedAt ?? DateTime.MinValue,
                    UnreadCount = m.UnreadCount
                })
                .OrderByDescending(bc => bc.LastMessageTime)
                .ToList();

            return Json(boxChats);
        }




        // API: Lấy tin nhắn của 1 box
        [HttpGet]
        public IActionResult GetMessages(long boxChatId)
        {
            var currentUserId = GetCurrentUser();

            // Kiểm tra user có thuộc box này không
            bool isMember = _context.BoxChatMembers
                .Any(m => m.BoxChatId == boxChatId && m.UserId == currentUserId);

            if (!isMember)
            {
                return Forbid();
            }

            // ✅ Lấy danh sách các messageId đã seen bởi user
            var seenMessageIds = _context.MessageReadStatuses
                .Where(mrs => mrs.UserId == currentUserId && mrs.Message.BoxChatId == boxChatId)
                .Select(mrs => mrs.MessageId)
                .ToHashSet();

            // Lấy message + đánh dấu IsSeen
            var messages = _context.Messages
                .Where(msg => msg.BoxChatId == boxChatId)
                .OrderBy(msg => msg.CreatedAt)
                .Select(msg => new ChatMessageVM
                {
                    MessageId = msg.Id,
                    BoxChatId = msg.BoxChatId,
                    SenderName = msg.Sender.Fullname,
                    Content = msg.Content ?? "",
                    CreatedAt = msg.CreatedAt,
                    AvatarUrl = "/api/avatar/" + msg.SenderId,
                    SenderId = msg.SenderId,
                    // ✅ Check trong HashSet (O(1)) thay vì query lồng nhau
                    IsSeen = seenMessageIds.Contains(msg.Id)
                })
                .ToList();

            return Json(messages);
        }


        [HttpPost]
        public async Task<IActionResult> Send([FromBody] SendMessageRequest request)
        {
            var currentUserId = GetCurrentUser();
            var boxChatId = request.BoxChatId;
            var content = request.Content;

            // Giới hạn độ dài tin nhắn
            if (content.Length > 50000)
            {
                return BadRequest(new { error = "Message content exceeds maximum length of 1000 characters." });
            }

            // Kiểm tra user có tồn tại và có bị block không
            var user = _context.Users.FirstOrDefault(u => u.Id == currentUserId);
            if (user == null)
            {
                return Forbid();
            }
            if (user.IsBlock)
            {
                return Forbid(); // User đã bị block
            }

            // Kiểm tra user có thuộc box này không
            bool isMember = _context.BoxChatMembers
                .Any(m => m.BoxChatId == boxChatId && m.UserId == currentUserId);

            if (!isMember)
            {
                return Forbid();
            }

            // Kiểm tra nội dung tin nhắn
            if (string.IsNullOrWhiteSpace(content))
            {
                return BadRequest(new { error = "Message content cannot be empty." });
            }

            // ======================= CHỐNG SPAM =======================
            var cacheKey = $"messages_{currentUserId}";
            var now = DateTime.UtcNow;

            // Lấy lịch sử tin nhắn gần đây (trong 10s)
            var history = _cache.GetOrCreate(cacheKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10);
                return new List<DateTime>();
            });

            // Xóa những tin quá 10s
            history.RemoveAll(t => (now - t).TotalSeconds > 10);

            // Thêm tin nhắn mới vào lịch sử
            history.Add(now);

            // Nếu quá 25 tin trong 10s → block user
            if (history.Count > 25)
            {
                user.IsBlock = true;
                _context.SaveChanges();
                return Forbid(); // chặn luôn
            }

            // Lưu lại vào cache
            _cache.Set(cacheKey, history, TimeSpan.FromSeconds(10));
            // ==========================================================

            // Tạo tin nhắn mới
            var message = new Message
            {
                BoxChatId = boxChatId,
                SenderId = currentUserId,
                Content = content,
                MessageType = "text",
                CreatedAt = DateTime.Now,
                MessageReadStatuses = new List<MessageReadStatus>
        {
            new MessageReadStatus
            {
                UserId = currentUserId,
                ReadAt = DateTime.Now
            }
        }
            };

            _context.Messages.Add(message);
            _context.SaveChanges();

            // Trả lại tin nhắn vừa gửi
            var messageVm = new ChatMessageVM
            {
                MessageId = message.Id,
                SenderId = currentUserId,
                BoxChatId = boxChatId,
                SenderName = _context.Users
                    .Where(u => u.Id == currentUserId)
                    .Select(u => u.Fullname)
                    .FirstOrDefault() ?? "",
                AvatarUrl = "/api/avatar/" + currentUserId,
                Content = message.Content,
                CreatedAt = message.CreatedAt
            };

            // Gửi tin nhắn đến tất cả người dùng trong box chat
            var boxChatMembers = _context.BoxChatMembers
                .Where(m => m.BoxChatId == boxChatId)
                .Select(m => m.UserId)
                .ToList();

            foreach (var memberId in boxChatMembers)
            {
                await _hubContext.Clients.User(memberId.ToString())
                    .SendAsync("ReceiveMessage", messageVm);
            }

            return Json(messageVm);
        }


        public IActionResult ChatTo(long userId)
        {
            var currentUserId = GetCurrentUser();
            // Kiểm tra xem đã có box chat với user này chưa
            var boxChat = _context.BoxChats
                .FirstOrDefault(bc => !bc.IsGroup &&
                                      bc.BoxChatMembers.Any(m => m.UserId == currentUserId) &&
                                      bc.BoxChatMembers.Any(m => m.UserId == userId));
            if (boxChat == null)
            {
                // Tạo box chat mới
                boxChat = new BoxChat
                {
                    IsGroup = false,
                    Name = null, // Không cần tên cho chat 1-1
                    CreatedAt = DateTime.Now,
                    CreatedBy = currentUserId,
                    BoxChatMembers = new List<BoxChatMember>
                    {
                        new BoxChatMember { UserId = currentUserId },
                        new BoxChatMember { UserId = userId }
                    }
                };
                _context.BoxChats.Add(boxChat);
                _context.SaveChanges();
            }
            return RedirectToAction("Index", "Message", new { boxChatId = boxChat.Id });
        }


        [HttpPost]
        public IActionResult MarkSeen([FromBody] MarkSeenRequest request)
        {
            var currentUserId = GetCurrentUser();
            if (request?.MessageIds == null || !request.MessageIds.Any())
                return BadRequest("No message ids provided.");


            var messages = _context.Messages
                .Where(m => request.MessageIds.Contains(m.Id))
                .Select(m => m.Id)
                .ToList();

            if (!messages.Any())
                return Ok(new { status = false, message = "No valid messages to mark as seen." });

            // Lấy những status đã tồn tại để tránh insert trùng
            var existingStatuses = _context.MessageReadStatuses
                .Where(mrs => messages.Contains(mrs.MessageId) && mrs.UserId == currentUserId)
                .Select(mrs => mrs.MessageId)
                .ToHashSet();

            var newStatuses = messages
                .Where(id => !existingStatuses.Contains(id))
                .Select(id => new MessageReadStatus
                {
                    MessageId = id,
                    UserId = currentUserId,
                    ReadAt = DateTime.Now
                })
                .ToList();

            if (newStatuses.Any())
            {
                _context.MessageReadStatuses.AddRange(newStatuses);
                _context.SaveChanges();
            }

            return Ok(new { status = true, count = newStatuses.Count });
        }

        [HttpGet]
        public IActionResult GetUnreadBoxCount()
        {
            var currentUserId = GetCurrentUser();

            // Tất cả các box mà user đang tham gia
            var userBoxIds = _context.BoxChatMembers
                .Where(m => m.UserId == currentUserId)
                .Select(m => m.BoxChatId);

            // Đếm số box có ít nhất 1 tin nhắn chưa đọc
            var unreadBoxCount = _context.Messages
                .Where(m => userBoxIds.Contains(m.BoxChatId) && m.SenderId != currentUserId)
                .Where(m => !_context.MessageReadStatuses
                    .Any(r => r.MessageId == m.Id && r.UserId == currentUserId))
                .Select(m => m.BoxChatId)
                .Distinct()
                .Count();

            return Ok(unreadBoxCount);
        }



        private long GetCurrentUser()
        {
            return long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        }

    }
}
