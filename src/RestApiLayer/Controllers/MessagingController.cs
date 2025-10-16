using Dapr;
using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using RestApiLayer.Models;
using System.Collections.Concurrent;

namespace RestApiLayer.Controllers;

[ApiController]
[Route("[controller]")]
public class MessagingController : ControllerBase
{
    private readonly ILogger<MessagingController> _logger;
    private readonly DaprClient _daprClient;
    private const string PubSubName = "mqtt-pubsub";

    // In-memory storage for latest telemetry (use Redis/DB in production)
    private static readonly ConcurrentDictionary<string, PrinterTelemetry> LatestTelemetry = new();
    private static readonly ConcurrentDictionary<string, List<PrinterEvent>> PrinterEvents = new();

    public MessagingController(ILogger<MessagingController> logger, DaprClient daprClient)
    {
        _logger = logger;
        _daprClient = daprClient;
    }

    /// <summary>
    /// Publish a message to a specific topic
    /// POST: /messaging/publish
    /// </summary>
    [HttpPost("publish")]
    public async Task<IActionResult> PublishMessage([FromBody] PublishRequest request)
    {
        _logger.LogInformation("Publishing to topic '{Topic}': {Content}", 
            request.Topic, request.Message.Content);

        await _daprClient.PublishEventAsync(PubSubName, request.Topic, request.Message);
        
        return Ok(new 
        { 
            status = "Message published successfully!",
            topic = request.Topic,
            messageId = request.Message.Id 
        });
    }

    /// <summary>
    /// Subscribe to 3D printer telemetry data
    /// Topic: university/lab/printer/+/telemetry
    /// This receives real-time operational data from all printers
    /// </summary>
    [Topic(PubSubName, "university/lab/printer/+/telemetry")]
    [HttpPost("subscribe/printer-telemetry")]
    public IActionResult SubscribeToPrinterTelemetry(PrinterTelemetry telemetry)
    {
        _logger.LogInformation(
            "🖨️ PRINTER TELEMETRY [{PrinterId}]: Status={Status}, Nozzle={Nozzle}°C, Bed={Bed}°C, Progress={Progress}%, Filament={Filament}g",
            telemetry.PrinterId, 
            telemetry.Status,
            telemetry.NozzleTemperature, 
            telemetry.BedTemperature,
            telemetry.PrintProgress,
            telemetry.FilamentRemaining
        );

        // Store latest telemetry for each printer
        LatestTelemetry[telemetry.PrinterId] = telemetry;

        // TODO: Store in database for historical analysis
        // TODO: Trigger alerts if temperature too high
        // TODO: Notify frontend via SignalR for real-time dashboard updates
        
        return Ok();
    }

    /// <summary>
    /// Subscribe to 3D printer events (errors, warnings, completions)
    /// Topic: university/lab/printer/+/events
    /// </summary>
    [Topic(PubSubName, "university/lab/printer/+/events")]
    [HttpPost("subscribe/printer-events")]
    public IActionResult SubscribeToPrinterEvents(PrinterEvent printerEvent)
    {
        var emoji = printerEvent.EventType switch
        {
            "error" => "❌",
            "warning" => "⚠️",
            "completed" => "✅",
            _ => "ℹ️"
        };

        _logger.LogInformation(
            "{Emoji} PRINTER EVENT [{PrinterId}]: {EventType} - {Message}",
            emoji,
            printerEvent.PrinterId,
            printerEvent.EventType,
            printerEvent.Message
        );

        // Store event history
        if (!PrinterEvents.ContainsKey(printerEvent.PrinterId))
        {
            PrinterEvents[printerEvent.PrinterId] = new List<PrinterEvent>();
        }
        PrinterEvents[printerEvent.PrinterId].Add(printerEvent);

        // TODO: Send email/SMS alerts for critical errors
        // TODO: Log to monitoring system (Grafana, Prometheus)
        // TODO: Notify maintenance team via webhook
        
        return Ok();
    }

    /// <summary>
    /// Subscribe to print job updates
    /// Topic: university/lab/printer/+/jobs
    /// </summary>
    [Topic(PubSubName, "university/lab/printer/+/jobs")]
    [HttpPost("subscribe/print-jobs")]
    public IActionResult SubscribeToPrintJobs(PrintJob job)
    {
        _logger.LogInformation(
            "📋 PRINT JOB [{JobId}] on {PrinterId}: {FileName} - Status: {Status}",
            job.JobId,
            job.PrinterId,
            job.FileName,
            job.Status
        );

        // TODO: Store job in database
        // TODO: Update job queue dashboard
        
        return Ok();
    }

    /// <summary>
    /// Subscribe to lab equipment telemetry (CNC, Laser Cutters, etc.)
    /// Topic: university/lab/equipment/+/telemetry
    /// </summary>
    [Topic(PubSubName, "university/lab/equipment/+/telemetry")]
    [HttpPost("subscribe/equipment-telemetry")]
    public IActionResult SubscribeToEquipmentTelemetry(LabEquipmentTelemetry equipment)
    {
        _logger.LogInformation(
            "🔧 EQUIPMENT TELEMETRY [{EquipmentId}] {EquipmentType}: Status={Status}",
            equipment.EquipmentId,
            equipment.EquipmentType,
            equipment.Status
        );
        
        return Ok();
    }

    /// <summary>
    /// Send command to a specific 3D printer
    /// POST: /messaging/printer/{printerId}/command
    /// </summary>
    [HttpPost("printer/{printerId}/command")]
    public async Task<IActionResult> SendPrinterCommand(string printerId, [FromBody] PrinterCommand command)
    {
        var topic = $"university/lab/printer/{printerId}/commands";
        
        _logger.LogInformation(
            "🎮 Sending command '{Action}' to printer {PrinterId}",
            command.Action, 
            printerId
        );

        await _daprClient.PublishEventAsync(PubSubName, topic, command);
        
        return Ok(new 
        { 
            status = "Command sent successfully!",
            printerId,
            topic,
            command = command.Action
        });
    }

    /// <summary>
    /// Get latest telemetry for a specific printer
    /// GET: /messaging/printer/{printerId}/telemetry
    /// </summary>
    [HttpGet("printer/{printerId}/telemetry")]
    public IActionResult GetPrinterTelemetry(string printerId)
    {
        if (LatestTelemetry.TryGetValue(printerId, out var telemetry))
        {
            return Ok(telemetry);
        }
        
        return NotFound(new { message = $"No telemetry data found for printer {printerId}" });
    }

    /// <summary>
    /// Get all printers with their latest status
    /// GET: /messaging/printers
    /// </summary>
    [HttpGet("printers")]
    public IActionResult GetAllPrinters()
    {
        var printers = LatestTelemetry.Values.Select(t => new
        {
            t.PrinterId,
            t.Status,
            t.PrintProgress,
            t.NozzleTemperature,
            t.BedTemperature,
            t.FilamentRemaining,
            t.Timestamp
        });

        return Ok(new
        {
            totalPrinters = printers.Count(),
            activePrinters = printers.Count(p => p.Status == "printing"),
            printers
        });
    }

    /// <summary>
    /// Get event history for a specific printer
    /// GET: /messaging/printer/{printerId}/events
    /// </summary>
    [HttpGet("printer/{printerId}/events")]
    public IActionResult GetPrinterEvents(string printerId, [FromQuery] int limit = 50)
    {
        if (PrinterEvents.TryGetValue(printerId, out var events))
        {
            var recentEvents = events.OrderByDescending(e => e.Timestamp).Take(limit);
            return Ok(recentEvents);
        }
        
        return Ok(new List<PrinterEvent>());
    }

    /// <summary>
    /// Get subscription health check
    /// GET: /messaging/health
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new 
        { 
            status = "healthy",
            pubSubName = PubSubName,
            subscribedTopics = new[] 
            { 
                "university/lab/printer/+/telemetry",
                "university/lab/printer/+/events",
                "university/lab/printer/+/jobs",
                "university/lab/equipment/+/telemetry"
            },
            connectedPrinters = LatestTelemetry.Count,
            totalEvents = PrinterEvents.Sum(kvp => kvp.Value.Count)
        });
    }
}