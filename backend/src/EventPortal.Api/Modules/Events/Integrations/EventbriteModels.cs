using System.Text.Json.Serialization;

namespace EventPortal.Api.Modules.Events.Integrations;

public class EventbriteEvent
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public EventbriteText? Name { get; set; }

    [JsonPropertyName("start")]
    public EventbriteDateTime? Start { get; set; }

    [JsonPropertyName("end")]
    public EventbriteDateTime? End { get; set; }

    [JsonPropertyName("venue")]
    public EventbriteVenue? Venue { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("logo")]
    public EventbriteLogo? Logo { get; set; }

    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;
}

public class EventbriteText
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}

public class EventbriteDateTime
{
    [JsonPropertyName("utc")]
    public DateTime Utc { get; set; }
}

public class EventbriteVenue
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class EventbriteLogo
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}

public class EventbriteTicketClass
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("cost")]
    public EventbriteCost? Cost { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;

    [JsonPropertyName("capacity")]
    public int? Capacity { get; set; }  // null means unlimited capacity on Eventbrite

    [JsonPropertyName("quantity_sold")]
    public int? QuantitySold { get; set; }
}

public class EventbriteCost
{
    [JsonPropertyName("value")]
    public int Value { get; set; }  // Eventbrite returns minor units (cents)
}

public class EventbriteOrder
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("attendees")]
    public List<EventbriteAttendee> Attendees { get; set; } = new();
}

public class EventbriteAttendee
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("profile")]
    public EventbriteProfile? Profile { get; set; }

    [JsonPropertyName("checked_in")]
    public bool CheckedIn { get; set; }

    [JsonPropertyName("created")]
    public DateTime Created { get; set; }

    [JsonPropertyName("ticket_class_id")]
    public string TicketClassId { get; set; } = string.Empty;
}

public class EventbriteProfile
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
}

public class EventbritePagedResponse<T>
{
    [JsonPropertyName("pagination")]
    public EventbritePagination? Pagination { get; set; }

    [JsonPropertyName("events")]
    public List<T>? Events { get; set; }

    [JsonPropertyName("ticket_classes")]
    public List<T>? TicketClasses { get; set; }

    [JsonPropertyName("orders")]
    public List<T>? Orders { get; set; }

    [JsonPropertyName("attendees")]
    public List<T>? Attendees { get; set; }
}

public class EventbritePagination
{
    [JsonPropertyName("page_count")]
    public int PageCount { get; set; }

    [JsonPropertyName("page_number")]
    public int PageNumber { get; set; }
}
