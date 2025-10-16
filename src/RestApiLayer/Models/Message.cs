using System.Text.Json.Serialization;

namespace RestApiLayer.Models;

// Base message structure
public class Message
{
    [JsonPropertyName("Id")]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [JsonPropertyName("Content")]
    public string? Content { get; set; }
    
    [JsonPropertyName("Timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class PublishRequest
{
    [JsonPropertyName("Topic")]
    public required string Topic { get; set; }
    
    [JsonPropertyName("Message")]
    public required Message Message { get; set; }
}

// 3D Printer Telemetry (Real-time operational data)
public class PrinterTelemetry
{
    [JsonPropertyName("PrinterId")]
    public required string PrinterId { get; set; }
    
    [JsonPropertyName("NozzleTemperature")]
    public required double NozzleTemperature { get; set; }      // °C
    
    [JsonPropertyName("BedTemperature")]
    public required double BedTemperature { get; set; }         // °C
    
    [JsonPropertyName("PrintProgress")]
    public required double PrintProgress { get; set; }          // 0-100%
    
    [JsonPropertyName("FilamentRemaining")]
    public required double FilamentRemaining { get; set; }      // grams
    
    [JsonPropertyName("CurrentLayer")]
    public required string CurrentLayer { get; set; } // e.g., "45/200"
    
    [JsonPropertyName("PrintSpeed")]
    public required double PrintSpeed { get; set; }             // mm/s
    
    [JsonPropertyName("Status")]
    public required string Status { get; set; } // "printing", "idle", "paused", "error"
    
    [JsonPropertyName("Timestamp")]
    public required DateTime Timestamp { get; set; }
}

// 3D Printer Events (Alerts, errors, state changes)
public class PrinterEvent
{
    [JsonPropertyName("PrinterId")]
    public required string PrinterId { get; set; }
    
    [JsonPropertyName("EventType")]
    public required string EventType { get; set; } // "error", "warning", "info", "completed"
    
    [JsonPropertyName("Message")]
    public required string Message { get; set; }
    
    [JsonPropertyName("Source")]
    public required string Source { get; set; } // "firmware", "host", "user"
    
    [JsonPropertyName("Severity")]
    public required int Severity { get; set; } // 1 (low) to 5 (critical)
    
    [JsonPropertyName("Timestamp")]
    public required DateTime Timestamp { get; set; }
}

// 3D Printer Commands (Control printer remotely)
public class PrinterCommand
{
    [JsonPropertyName("Action")]
    public required string Action { get; set; }
    
    // Actions: "start_print", "pause_print", "resume_print", "cancel_print", 
    //          "set_temperature", "home_axes", "emergency_stop"
    [JsonPropertyName("Parameters")]
    public Dictionary<string, object>? Parameters { get; set; }
    
    [JsonPropertyName("Timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

// Print Job Information
public class PrintJob
{
    [JsonPropertyName("JobId")]
    public required string JobId { get; set; } = Guid.NewGuid().ToString();
    
    [JsonPropertyName("PrinterId")]
    public required string PrinterId { get; set; }
    
    [JsonPropertyName("FileName")]
    public required string FileName { get; set; }
    
    [JsonPropertyName("EstimatedTime")]
    public required double EstimatedTime { get; set; }          // minutes
    
    [JsonPropertyName("FilamentRequired")]
    public required double FilamentRequired { get; set; }       // grams
    
    [JsonPropertyName("Status")]
    public required string Status { get; set; } = "queued";     // "queued", "printing", "completed", "failed", "cancelled"
    
    [JsonPropertyName("StartedAt")]
    public DateTime StartedAt { get; set; }
    
    [JsonPropertyName("CompletedAt")]
    public DateTime? CompletedAt { get; set; }
    
    [JsonPropertyName("Timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

// Laboratory Equipment Telemetry (Bonus - other IoT devices)
public class LabEquipmentTelemetry
{
    [JsonPropertyName("EquipmentId")]
    public required string EquipmentId { get; set; }
    
    [JsonPropertyName("EquipmentType")]
    public required string EquipmentType { get; set; } // "cnc_mill", "laser_cutter", "oscilloscope"
    
    [JsonPropertyName("Status")]
    public required string Status { get; set; }
    
    [JsonPropertyName("Temperature")]
    public required double Temperature { get; set; }
    
    [JsonPropertyName("UsageHours")]
    public required double UsageHours { get; set; }
    
    [JsonPropertyName("Timestamp")]
    public required DateTime Timestamp { get; set; }
}
