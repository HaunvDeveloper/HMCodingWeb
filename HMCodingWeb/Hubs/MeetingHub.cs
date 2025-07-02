using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace HMCodingWeb.Hubs
{
    public class MeetingHub : Hub
    {
        public async Task JoinRoom(string meetingId, string userId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, meetingId);
            await Clients.Group(meetingId).SendAsync("UserJoined", userId, Context.ConnectionId);
        }

        public async Task SendOffer(string meetingId, string targetConnectionId, string offer)
        {
            await Clients.Client(targetConnectionId).SendAsync("ReceiveOffer", offer, Context.ConnectionId);
        }

        public async Task SendAnswer(string meetingId, string targetConnectionId, string answer)
        {
            await Clients.Client(targetConnectionId).SendAsync("ReceiveAnswer", answer);
        }

        public async Task SendIceCandidate(string meetingId, string targetConnectionId, string candidate)
        {
            await Clients.Client(targetConnectionId).SendAsync("ReceiveIceCandidate", candidate);
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            // Remove user from group on disconnect
            await base.OnDisconnectedAsync(exception);
        }
    }
}