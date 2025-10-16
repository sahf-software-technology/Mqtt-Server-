using Microsoft.AspNetCore.SignalR;

namespace RestApiLayer.Hubs
{
    // The PrinterHub manages the connections between the server and the browser clients.
    // The C# Controller will use the IHubContext<PrinterHub> to send messages 
    // to all connected clients (the real-time dashboard).
    public class PrinterHub : Hub
    {
        // No methods are explicitly needed here for server-to-client push; 
        // the hub primarily serves as the connection endpoint (/printerhub).
    }
}
