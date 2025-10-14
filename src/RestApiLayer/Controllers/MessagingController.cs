using Dapr;
using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using RestApiLayer.Models;

namespace RestApiLayer.Controllers;

[ApiController]
[Route("[controller]")]
public class MessagingController : ControllerBase
{
    private readonly ILogger<MessagingController> _logger;
    private readonly DaprClient _daprClient;
    private const string PubSubName = "mqtt-pubsub";

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
    /// Subscribe to new-orders topic
    /// This is called automatically by Dapr when messages arrive
    /// </summary>
    [Topic(PubSubName, "new-orders")]
    [HttpPost("subscribe/orders")]
    public IActionResult SubscribeToOrders(Message message)
    {
        _logger.LogInformation("✅ ORDER Received: '{Content}' (ID: {MessageId})",
            message.Content, message.Id);
        
        // Process order here
        
        return Ok();
    }

    /// <summary>
    /// Subscribe to device telemetry
    /// Topic: devices/+/telemetry (wildcard subscription)
    /// </summary>
    [Topic(PubSubName, "devices/+/telemetry")]
    [HttpPost("subscribe/telemetry")]
    public IActionResult SubscribeToTelemetry(DeviceTelemetryMessage telemetry)
    {
        _logger.LogInformation("📊 TELEMETRY from {DeviceId}: Temp={Temp}°C, Humidity={Humidity}%",
            telemetry.DeviceId, telemetry.Temperature, telemetry.Humidity);
        
        // Store telemetry data or trigger alerts
        
        return Ok();
    }

    /// <summary>
    /// Subscribe to device events
    /// Topic: devices/+/events
    /// </summary>
    [Topic(PubSubName, "devices/+/events")]
    [HttpPost("subscribe/events")]
    public IActionResult SubscribeToEvents(DeviceEventMessage deviceEvent)
    {
        _logger.LogInformation("⚡ EVENT from {DeviceId}: {EventType} - {Message}",
            deviceEvent.DeviceId, deviceEvent.EventType, deviceEvent.Message);
        
        // Handle device events (errors, warnings, etc.)
        
        return Ok();
    }

    /// <summary>
    /// Send command to a specific device
    /// POST: /messaging/command/{deviceId}
    /// </summary>
    [HttpPost("command/{deviceId}")]
    public async Task<IActionResult> SendCommand(string deviceId, [FromBody] DeviceCommand command)
    {
        var topic = $"devices/{deviceId}/commands";
        
        _logger.LogInformation("🎮 Sending command '{Action}' to device {DeviceId}",
            command.Action, deviceId);

        await _daprClient.PublishEventAsync(PubSubName, topic, command);
        
        return Ok(new 
        { 
            status = "Command sent successfully!",
            deviceId,
            topic,
            command = command.Action
        });
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
                "new-orders", 
                "devices/+/telemetry", 
                "devices/+/events" 
            }
        });
    }
}