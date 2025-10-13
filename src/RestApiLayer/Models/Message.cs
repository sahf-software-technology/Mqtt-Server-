namespace RestApiLayer.Models;

public class Message
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? Content { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}