using HMCodingWeb.Services;
using System.Security.Claims;

namespace HMCodingWeb.Middlewares
{
    public class UpdateLastActiveMiddleware
    {
        private readonly RequestDelegate _next;

        public UpdateLastActiveMiddleware(RequestDelegate next)
        {
            _next = next;
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
            }

            await _next(context);
        }
    }
}
