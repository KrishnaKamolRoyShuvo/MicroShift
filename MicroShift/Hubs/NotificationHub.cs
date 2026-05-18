using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace MicroShift.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        // This hub is deliberately empty! 
        // We will inject IHubContext<NotificationHub> directly into our controllers 
        // to send messages from the server to the client.
    }
}