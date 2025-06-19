namespace HMCodingWeb.Hubs
{
    using HMCodingWeb.Services;
    using Microsoft.AspNetCore.SignalR;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.SignalR;
    using System.Security.Claims;

    public class OnlineUsersHub : Hub
    {
        private readonly OnlineUsersService _onlineUsersService;

        public OnlineUsersHub(OnlineUsersService onlineUsersService)
        {
            _onlineUsersService = onlineUsersService;
        }

        public override async Task OnConnectedAsync()
        {
            // Lấy UserId từ Claims (nếu người dùng đã đăng nhập)
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var username = Context.User?.FindFirst(ClaimTypes.Name)?.Value;
            var fullname = Context.User?.FindFirst(ClaimTypes.GivenName)?.Value;
            var auth = Context.User?.FindFirst(ClaimTypes.Authentication)?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                _onlineUsersService.AddUser(userId, username, fullname, auth, Context.ConnectionId);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            // Xóa người dùng khỏi danh sách online dựa trên ConnectionId
            _onlineUsersService.RemoveUserByConnectionId(Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }

        public async Task GetOnlineUsers()
        {
            var users = _onlineUsersService.GetOnlineUsers();
            await Clients.Caller.SendAsync("ReceiveOnlineUsers", users.Select(u => new { u.UserId, u.Username, u.Fullname, u.Auth }));
        }
    }
}
