using Microsoft.AspNetCore.SignalR;

namespace FileProcessorApi.Hubs
{
    public class ProcessingHub : Hub
    {
        public async Task JoinTaskGroup(string taskId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, taskId);
        }
    }
}
