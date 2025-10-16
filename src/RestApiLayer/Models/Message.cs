namespace RestApiLayer.Models;

// Base message structure
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

// 3D Printer Telemetry (Real-time operational data)
public class PrinterTelemetry
{
    public string PrinterId { get; set; } = string.Empty;
    public double NozzleTemperature { get; set; }      // °C
    public double BedTemperature { get; set; }         // °C
    public double PrintProgress { get; set; }          // 0-100%
    public double FilamentRemaining { get; set; }      // grams
    public string CurrentLayer { get; set; } = string.Empty; // e.g., "45/200"
    public double PrintSpeed { get; set; }             // mm/s
    public string Status { get; set; } = string.Empty; // "printing", "idle", "paused", "error"
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

// 3D Printer Events (Alerts, errors, state changes)
public class PrinterEvent
{
    public string PrinterId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty; // "error", "warning", "info", "completed"
    public string Message { get; set; } = string.Empty;
    public string? JobId { get; set; }                     // Current print job ID
    public Dictionary<string, object>? Data { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

// 3D Printer Commands (Control printer remotely)
public class PrinterCommand
{
    public string Action { get; set; } = string.Empty; 
    // Actions: "start_print", "pause_print", "resume_print", "cancel_print", 
    //          "set_temperature", "home_axes", "emergency_stop"
    public Dictionary<string, object>? Parameters { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

// Print Job Information
public class PrintJob
{
    public string JobId { get; set; } = Guid.NewGuid().ToString();
    public string PrinterId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public double EstimatedTime { get; set; }          // minutes
    public double FilamentRequired { get; set; }       // grams
    public string Status { get; set; } = "queued";     // "queued", "printing", "completed", "failed", "cancelled"
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

// Laboratory Equipment Telemetry (Bonus - other IoT devices)
public class LabEquipmentTelemetry
{
    public string EquipmentId { get; set; } = string.Empty;
    public string EquipmentType { get; set; } = string.Empty; // "cnc_mill", "laser_cutter", "oscilloscope"
    public string Status { get; set; } = string.Empty;
    public Dictionary<string, object>? Metrics { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}