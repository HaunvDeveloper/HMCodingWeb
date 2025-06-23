using HMCodingWeb.Models;
using Microsoft.AspNetCore.SignalR;

namespace HMCodingWeb.Hubs
{
    public class MarkingHub : Hub
    {
        public async Task SendTestCaseResult(string userId, MarkingDetail result)
        {
            await Clients.User(userId).SendAsync("ReceiveTestCaseResult", result);
        }

        //Send announcement to reload the list marking
        public async Task SendReloadMarkingList(bool reload)
        {
            await Clients.Caller.SendAsync("AnnouncementToReload", reload);
        }


    }
}
