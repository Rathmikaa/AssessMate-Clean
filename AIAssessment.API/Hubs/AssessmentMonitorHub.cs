using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace AIAssessment.API.Hubs
{
    [Authorize]
    public class AssessmentMonitorHub : Hub
    {
        // Admin dashboards call this right after connecting.
        public async Task JoinAdminGroup()
        {
            if (Context.User?.IsInRole("Admin") == true)
                await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Admins");
            await base.OnDisconnectedAsync(exception);
        }
    }
}