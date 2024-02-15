using System.ComponentModel.DataAnnotations;

namespace UserService.Models;

public class OutboxMessage
{
    [Key]
    public Guid MessageId { get; set; }
    public string EventData { get; set; }
    public string EventType { get; set; }
    public bool IsSent { get; set; }
    public DateTime TimeCreated { get; set; }

    public OutboxMessage()
    {
        EventData = string.Empty;
        EventType = string.Empty;
        IsSent = false;
        TimeCreated = DateTime.Now;
    }

    public OutboxMessage(string eventData, string eventType, bool isSent, DateTime timeCreated)
    {
        EventData = eventData;
        EventType = eventType;
        IsSent = isSent;
        TimeCreated = timeCreated;
    }
}