namespace HMCodingWeb.Services
{
    public class UserCleanupService : BackgroundService
    {
        private readonly OnlineUsersService _onlineUsersService;
        private readonly TimeSpan _inactivityTimeout = TimeSpan.FromMinutes(10);

        public UserCleanupService(OnlineUsersService onlineUsersService)
        {
            _onlineUsersService = onlineUsersService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var users = _onlineUsersService.GetOnlineUsers();
                var now = DateTime.UtcNow;
                var inactiveUsers = users.Where(u => (now - u.LastActive) > _inactivityTimeout).ToList();

                foreach (var user in inactiveUsers)
                {
                    _onlineUsersService.RemoveUserByUserId(user.UserId);
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Kiểm tra mỗi phút
            }
        }
    }
}
