using HMCodingWeb.Models;
using HMCodingWeb.Services;
using System.Security.Claims;

namespace HMCodingWeb.Middlewares
{
    public class UpdateLastActiveMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly OnlineUsersService _onlineUsersService;

        public UpdateLastActiveMiddleware(RequestDelegate next, OnlineUsersService onlineUsersService)
        {
            _next = next;
            _onlineUsersService = onlineUsersService;
        }

        public async Task InvokeAsync(HttpContext context, OnlineUsersService onlineUsersService)
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                var users = onlineUsersService.GetOnlineUsers();
                var user = users.FirstOrDefault(u => u.UserId == userId);
                if (user != null)
                {
                    user.LastActive = DateTime.UtcNow;
                    onlineUsersService.GetOnlineUsers(); // Cập nhật cache
                }
                else
                {
                    // Nếu người dùng không có trong danh sách, có thể thêm mới
                    var username = context.User.Identity?.Name;
                    var fullname = context.User.FindFirst(ClaimTypes.Name)?.Value;
                    var auth = context.User.FindFirst(ClaimTypes.Role)?.Value;
                    var connectionId = context.Connection.Id; // Lấy ConnectionId từ HttpContext
                    onlineUsersService.AddUser(userId, username ?? "", fullname ?? "", auth ?? "", connectionId);
                }
            }
            

            await _next(context);
        }
    }
}
