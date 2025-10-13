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

    // The name of our Dapr pub/sub component from mqtt-pubsub.yaml
    private const string PubSubName = "mqtt-pubsub";

    // The topic we'll publish to and subscribe to
    private const string TopicName = "new-orders";

    public MessagingController(ILogger<MessagingController> logger, DaprClient daprClient)
    {
        _logger = logger;
        _daprClient = daprClient;
    }

    /// <summary>
    /// Publishes (writes) a message to the MQTT topic.
    /// You would call this endpoint to send a message.
    /// POST: /messaging/publish
    /// </summary>
    [HttpPost("publish")]
    public async Task<IActionResult> PublishMessage([FromBody] Message message)
    {
        _logger.LogInformation("Publishing message: {MessageContent}", message.Content);

        // Use the Dapr client to publish an event to the "new-orders" topic
        await _daprClient.PublishEventAsync(PubSubName, TopicName, message);
        
        return Ok(new { status = "Message published successfully!", messageId = message.Id });
    }

    /// <summary>
    /// Subscribes to (reads) messages from the MQTT topic.
    /// Dapr will automatically call this endpoint when a message arrives.
    /// This endpoint is NOT meant to be called by users directly.
    /// </summary>
    [Topic(PubSubName, TopicName)] // <-- This attribute subscribes the endpoint to the topic
    [HttpPost("subscribe")]
    public IActionResult SubscribeToMessage(Message message)
    {
        _logger.LogInformation("✅ Message Received from topic '{TopicName}': '{MessageContent}' (ID: {MessageId})",
            TopicName, message.Content, message.Id);
            
        // Your business logic to process the message would go here.
        
        return Ok(); // A 200 OK response acknowledges the message.
    }
}