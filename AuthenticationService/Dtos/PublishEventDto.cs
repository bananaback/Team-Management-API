namespace AuthenticationService.Dtos;

public class PublishEventDto
{
    public string EventData { get; set; }
    public string EventType { get; set; }
    public DateTime TimeCreated { get; set; }

    public PublishEventDto()
    {
        EventData = string.Empty;
        EventType = string.Empty;
        TimeCreated = DateTime.Now;
    }
    public PublishEventDto(string eventData, string eventType, DateTime timeCreated)
    {
        EventData = eventData;
        EventType = eventType;
        TimeCreated = timeCreated;
    }
}