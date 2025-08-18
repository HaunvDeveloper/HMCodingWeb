using HMCodingWeb.Models;
using HMCodingWeb.Services;
using HMCodingWeb.ViewModels;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace HMCodingWeb.Hubs
{
    public class ChatHub : Hub
    {

        private readonly OnlineCodingWebContext _context;
        private readonly IMemoryCache _cache;
        public ChatHub(OnlineCodingWebContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }
        public async Task SendMessage(string ToUserId, string BoxChatId, string SenderId, string SenderName, string Content, string AvatarUrl, bool IsCurrentUser, string CreatedAt, string MessageId)
        {
            await Clients.User(ToUserId).SendAsync("ReceiveMessage", new
            {
                ToUserId,
                BoxChatId,
                SenderId,
                SenderName,
                Content,
                AvatarUrl,
                IsCurrentUser,
                CreatedAt,
                MessageId
            });
        }
        public async Task Typing(TypingDto data)
        {
            var cacheKey = $"BoxMembers_{data.BoxChatId}";

            if (!_cache.TryGetValue(cacheKey, out List<string> members))
            {
                members = _context.BoxChatMembers
                    .Where(m => m.BoxChatId == data.BoxChatId)
                    .Select(m => m.UserId.ToString())
                    .ToList();

                _cache.Set(cacheKey, members, TimeSpan.FromMinutes(5)); //  5 phút
            }

            foreach (var memberId in members.Where(id => id != data.UserId.ToString()))
            {
                await Clients.User(memberId).SendAsync("ReceiveTyping", new
                {
                    boxChatId = data.BoxChatId,
                    userId = data.UserId,
                    userName = data.UserName
                });
            }
        }


    }
}