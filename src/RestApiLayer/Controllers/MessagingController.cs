using Dapr;
using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using RestApiLayer.Models;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using RestApiLayer.Hubs;

namespace RestApiLayer.Controllers;

[ApiController]
[Route("[controller]")]
public class MessagingController : ControllerBase
{
    private readonly ILogger<MessagingController> _logger;
    private readonly DaprClient _daprClient;
    private readonly IHubContext<PrinterHub> _hubContext;
    private const string PubSubName = "mqtt-pubsub";

    private static readonly ConcurrentDictionary<string, PrinterTelemetry> LatestTelemetry = new();
    private static readonly ConcurrentDictionary<string, List<PrinterEvent>> PrinterEvents = new();
    private static readonly ConcurrentDictionary<string, object> PrintJobs = new();
    private static readonly ConcurrentDictionary<string, object> EquipmentTelemetry = new();

    public MessagingController(ILogger<MessagingController> logger, DaprClient daprClient, IHubContext<PrinterHub> hubContext)
    {
        _logger = logger;
        _daprClient = daprClient;
        _hubContext = hubContext;
    }

    // --- PUBLISH ---

    [HttpPost("publish")]
    public async Task<IActionResult> PublishMessage([FromBody] PublishRequest request)
    {
        _logger.LogInformation("Publishing to topic '{Topic}': {Content}", request.Topic, request.Message.Content);
        await _daprClient.PublishEventAsync(PubSubName, request.Topic, request.Message);
        return Ok(new
        {
            status = "Message published successfully!",
            topic = request.Topic,
            messageId = request.Message.Id
        });
    }

    // --- SUBSCRIPTIONS (rawPayload already handled in mqtt-pubsub.yaml) ---

    [Topic(PubSubName, "university/lab/printer/+/telemetry")]
    [HttpPost("subscribe/printer-telemetry")]
    public async Task<IActionResult> SubscribeToPrinterTelemetry([FromBody] PrinterTelemetry telemetry)
    {
        try
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

            LatestTelemetry[telemetry.PrinterId] = telemetry;
            await _hubContext.Clients.All.SendAsync("ReceiveTelemetry", telemetry);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🔴 DESERIALIZATION/PROCESSING FAILED for printer telemetry.");
            return StatusCode(500, "Internal server error during telemetry processing.");
        }
    }

    [Topic(PubSubName, "university/lab/printer/+/events")]
    [HttpPost("subscribe/printer-events")]
    public async Task<IActionResult> SubscribeToPrinterEvents([FromBody] PrinterEvent printerEvent)
    {
        try
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

            if (!PrinterEvents.ContainsKey(printerEvent.PrinterId))
                PrinterEvents[printerEvent.PrinterId] = new List<PrinterEvent>();

            PrinterEvents[printerEvent.PrinterId].Add(printerEvent);
            await _hubContext.Clients.All.SendAsync("ReceiveEvent", printerEvent);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🔴 DESERIALIZATION/PROCESSING FAILED for printer event.");
            return StatusCode(500, "Internal server error during event processing.");
        }
    }

    [Topic(PubSubName, "university/lab/printer/+/jobs")]
    [HttpPost("subscribe/print-jobs")]
    public IActionResult SubscribeToPrintJobs([FromBody] PrintJob job)
    {
        _logger.LogInformation(
            "📋 PRINT JOB [{JobId}] on {PrinterId}: {FileName} - Status: {Status}",
            job.JobId,
            job.PrinterId,
            job.FileName,
            job.Status
        );
        PrintJobs[job.JobId] = job;
        return Ok();
    }

    [Topic(PubSubName, "university/lab/equipment/+/telemetry")]
    [HttpPost("subscribe/equipment-telemetry")]
    public IActionResult SubscribeToEquipmentTelemetry([FromBody] LabEquipmentTelemetry equipment)
    {
        _logger.LogInformation(
            "🔧 EQUIPMENT TELEMETRY [{EquipmentId}] {EquipmentType}: Status={Status}",
            equipment.EquipmentId,
            equipment.EquipmentType,
            equipment.Status
        );
        EquipmentTelemetry[equipment.EquipmentId] = equipment;
        return Ok();
    }

    // --- COMMANDS / REST ACCESS ---

    [HttpPost("printer/{printerId}/command")]
    public async Task<IActionResult> SendPrinterCommand(string printerId, [FromBody] PrinterCommand command)
    {
        var topic = $"university/lab/printer/{printerId}/commands";
        _logger.LogInformation("🎮 Sending command '{Action}' to printer {PrinterId}", command.Action, printerId);
        await _daprClient.PublishEventAsync(PubSubName, topic, command);

        return Ok(new
        {
            status = "Command sent successfully!",
            printerId,
            topic,
            command = command.Action
        });
    }

    [HttpGet("printer/{printerId}/telemetry")]
    public IActionResult GetPrinterTelemetry(string printerId)
    {
        if (LatestTelemetry.TryGetValue(printerId, out var telemetry))
            return Ok(telemetry);
        return NotFound(new { message = $"No telemetry data found for printer {printerId}" });
    }

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

// --- Placeholder Models (define properly in your Models folder) ---
public record PrintJob(string JobId, string PrinterId, string FileName, string Status);
public record LabEquipmentTelemetry(string EquipmentId, string EquipmentType, string Status);
public record PrinterCommand(string Action, string Value);
public record PublishRequest(string Topic, Message Message);
public record Message(string Id, object Content);
