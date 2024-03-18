using System.ComponentModel.DataAnnotations;

namespace AuthenticationService.Models;

public class InboxMessage
{
    [Key]
    public Guid MessageId { get; set; }
    public string EventData { get; set; }
    public string EventType { get; set; }
    public bool IsProcessedSuccessfully { get; set; }
    public DateTime TimeCreated { get; set; }

    public InboxMessage()
    {
        EventData = string.Empty;
        EventType = string.Empty;
        IsProcessedSuccessfully = false;
        TimeCreated = DateTime.Now;
    }

    public InboxMessage(string eventData, string eventType, bool isProcessedSuccessfully, DateTime timeCreated)
    {
        EventData = eventData;
        EventType = eventType;
        IsProcessedSuccessfully = isProcessedSuccessfully;
        TimeCreated = timeCreated;
    }
}