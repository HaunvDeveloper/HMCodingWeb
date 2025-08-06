using HMCodingWeb.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;

namespace HMCodingWeb.Services
{
    public class OnlineUser
    {
        public string UserId { get; set; }
        public string Username { get; set; }
        public string Fullname { get; set; }
        public string Auth { get; set; }
        public string ConnectionId { get; set; } // Thêm ConnectionId
        public DateTime LastActive { get; set; }
    }

    public class OnlineUsersService
    {
        private readonly IMemoryCache _cache;
        private readonly IHubContext<OnlineUsersHub> _hubContext;
        private const string OnlineUsersKey = "OnlineUsers";

        public OnlineUsersService(IMemoryCache cache, IHubContext<OnlineUsersHub> hubContext)
        {
            _cache = cache;
            _hubContext = hubContext;
        }

        public void AddUser(string userId, string username, string fullname, string auth, string connectionId)
        {
            var users = GetOnlineUsers();
            var user = users.FirstOrDefault(u => u.UserId == userId);
            if (user == null)
            {
                users.Add(new OnlineUser
                {
                    UserId = userId,
                    Username = username,
                    Fullname = fullname,
                    Auth = auth,
                    ConnectionId = connectionId,
                    LastActive = DateTime.Now
                });
                _cache.Set(OnlineUsersKey, users, TimeSpan.FromHours(3));
                _hubContext.Clients.All.SendAsync("ReceiveOnlineUsers", users.Select(u => new { u.UserId, u.Username, u.Fullname, u.Auth }));
            }
            else
            {
                // Cập nhật ConnectionId nếu người dùng đã tồn tại
                user.ConnectionId = connectionId;
                user.LastActive = DateTime.Now;
                _cache.Set(OnlineUsersKey, users, TimeSpan.FromHours(3));
                _hubContext.Clients.All.SendAsync("ReceiveOnlineUsers", users.Select(u => new { u.UserId, u.Username, u.Fullname, u.Auth }));
            }
        }

        public void RemoveUserByConnectionId(string connectionId)
        {
            var users = GetOnlineUsers();
            var user = users.FirstOrDefault(u => u.ConnectionId == connectionId);
            if (user != null)
            {
                users.Remove(user);
                _cache.Set(OnlineUsersKey, users, TimeSpan.FromHours(3));
                _hubContext.Clients.All.SendAsync("ReceiveOnlineUsers", users.Select(u => new { u.UserId, u.Username, u.Fullname, u.Auth }));
            }
        }

        public void RemoveUserByUserId(string userId)
        {
            var users = GetOnlineUsers();
            var user = users.FirstOrDefault(u => u.UserId == userId);
            if (user != null)
            {
                users.Remove(user);
                _cache.Set(OnlineUsersKey, users, TimeSpan.FromHours(3));
                _hubContext.Clients.All.SendAsync("ReceiveOnlineUsers", users.Select(u => new { u.UserId, u.Username, u.Fullname, u.Auth }));
            }
        }

        public List<OnlineUser> GetOnlineUsers()
        {
            return _cache.TryGetValue(OnlineUsersKey, out List<OnlineUser> users)
                ? users
                : new List<OnlineUser>();
        }
    }
}
