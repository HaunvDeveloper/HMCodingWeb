using HMCodingWeb.Hubs;
using HMCodingWeb.Models;
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
        private readonly IServiceScopeFactory _scopeFactory;

        public OnlineUsersService(IMemoryCache cache, IHubContext<OnlineUsersHub> hubContext, IServiceScopeFactory scopeFactory)
        {
            _cache = cache;
            _hubContext = hubContext;
            _scopeFactory = scopeFactory;
        }

        public void AddUser(string userId, string username, string fullname, string auth, string connectionId)
        {
            var users = GetOnlineUsers();
            var user = users.FirstOrDefault(u => u.UserId == userId);
            var now = DateTime.Now;

            if (user == null)
            {
                users.Add(new OnlineUser
                {
                    UserId = userId,
                    Username = username,
                    Fullname = fullname,
                    Auth = auth,
                    ConnectionId = connectionId,
                    LastActive = now
                });
            }
            else
            {
                user.ConnectionId = connectionId;
                user.LastActive = now;
            }

            _cache.Set(OnlineUsersKey, users, TimeSpan.FromMinutes(10));
            _hubContext.Clients.All.SendAsync("ReceiveOnlineUsers", users.Select(u => new { u.UserId, u.Username, u.Fullname, u.Auth }));

            // Cập nhật LastOnline vào database
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<OnlineCodingWebContext>();
                var dbUser = dbContext.Users.FirstOrDefault(u => u.Id == Convert.ToInt64(userId));
                if (dbUser != null)
                {
                    if (dbUser.LastOnline < now.AddMinutes(-1))
                    {
                        dbUser.LastOnline = now;
                        dbContext.SaveChanges();
                    }
                }
            }
        }


        public void RemoveUserByConnectionId(string connectionId)
        {
            var users = GetOnlineUsers();
            var user = users.FirstOrDefault(u => u.ConnectionId == connectionId);
            if (user != null)
            {
                users.Remove(user);
                _cache.Set(OnlineUsersKey, users, TimeSpan.FromMinutes(10));
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
                _cache.Set(OnlineUsersKey, users, TimeSpan.FromMinutes(10));
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
