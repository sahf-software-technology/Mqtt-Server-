namespace RestApiLayer.Models;

public class Message
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? Content { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class PublishRequest
{
    public string Topic { get; set; } = string.Empty;
    public Message Message { get; set; } = new();
}

public class DeviceTelemetryMessage
{
    public string DeviceId { get; set; } = string.Empty;
    public double? Temperature { get; set; }
    public double? Humidity { get; set; }
    public Dictionary<string, object>? AdditionalData { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class DeviceEventMessage
{
    public string DeviceId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty; // "error", "warning", "info"
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, object>? Data { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class DeviceCommand
{
    public string Action { get; set; } = string.Empty; // "turn_on", "turn_off", "read", "configure"
    public Dictionary<string, object>? Parameters { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}