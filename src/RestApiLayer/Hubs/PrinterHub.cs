using Microsoft.AspNetCore.SignalR;

namespace RestApiLayer.Hubs;

// This hub handles the persistent connection and broadcasting of real-time data to clients.
// It is strongly typed with IHubContext<PrinterHub> in the MessagingController.
public class PrinterHub : Hub
{
    private readonly ILogger<PrinterHub> _logger;

    public PrinterHub(ILogger<PrinterHub> logger)
    {
        _logger = logger;
    }
    
    // Clients can call this method if needed (e.g., to subscribe to a specific printer)
    public async Task JoinPrinterGroup(string printerId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Printer-{printerId}");
        _logger.LogInformation("Client {ConnectionId} joined group for printer {PrinterId}", Context.ConnectionId, printerId);
    }
    
    // The main data broadcasting will be done from the controller using IHubContext.
    // No additional methods are strictly required here for that functionality.
}
