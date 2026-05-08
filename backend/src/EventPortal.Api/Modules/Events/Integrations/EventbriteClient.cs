using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EventPortal.Api.Modules.Events.Integrations;

public class EventbriteClient : IEventbriteClient
{
    private readonly HttpClient _http;
    private readonly ILogger<EventbriteClient> _logger;
    private readonly string _orgId;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public EventbriteClient(HttpClient http, IConfiguration config, ILogger<EventbriteClient> logger)
    {
        _http = http;
        _logger = logger;

        var apiToken = config["Eventbrite:ApiToken"] ?? string.Empty;
        var baseUrl = config["Eventbrite:BaseUrl"] ?? "https://www.eventbriteapi.com/v3";
        _orgId = config["Eventbrite:OrganizationId"] ?? string.Empty;

        _http.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);
    }

    public async Task<List<EventbriteEvent>> GetEventsAsync(string organizationId)
    {
        var results = new List<EventbriteEvent>();
        var page = 1;
        int pageCount;

        do
        {
            var url = $"organizations/{organizationId}/events/?expand=venue&status=all&order_by=start_desc&page_size=50&page={page}";
            var response = await GetWithRetryAsync<EventbritePagedResponse<EventbriteEvent>>(url);
            if (response?.Events != null)
                results.AddRange(response.Events);

            pageCount = response?.Pagination?.PageCount ?? 1;
            page++;
        }
        while (page <= pageCount);

        return results;
    }

    public async Task<List<EventbriteTicketClass>> GetTicketClassesAsync(string eventId)
    {
        var results = new List<EventbriteTicketClass>();
        var page = 1;
        int pageCount;

        do
        {
            var url = $"events/{eventId}/ticket_classes/?page={page}";
            var response = await GetWithRetryAsync<EventbritePagedResponse<EventbriteTicketClass>>(url);
            if (response?.TicketClasses != null)
                results.AddRange(response.TicketClasses);

            pageCount = response?.Pagination?.PageCount ?? 1;
            page++;
        }
        while (page <= pageCount);

        return results;
    }

    public async Task<List<EventbriteOrder>> GetOrdersAsync(string eventId)
    {
        var results = new List<EventbriteOrder>();
        var page = 1;
        int pageCount;

        do
        {
            var url = $"events/{eventId}/orders/?expand=attendees&page_size=100&page={page}";
            var response = await GetWithRetryAsync<EventbritePagedResponse<EventbriteOrder>>(url);
            if (response?.Orders != null)
                results.AddRange(response.Orders);

            pageCount = response?.Pagination?.PageCount ?? 1;
            page++;
        }
        while (page <= pageCount);

        return results;
    }

    public async Task<List<EventbriteAttendee>> GetAttendeesAsync(string eventId)
    {
        var results = new List<EventbriteAttendee>();
        var page = 1;
        int pageCount;

        do
        {
            var url = $"events/{eventId}/attendees/?page={page}";
            var response = await GetWithRetryAsync<EventbritePagedResponse<EventbriteAttendee>>(url);
            if (response?.Attendees != null)
                results.AddRange(response.Attendees);

            pageCount = response?.Pagination?.PageCount ?? 1;
            page++;
        }
        while (page <= pageCount);

        return results;
    }

    private async Task<T?> GetWithRetryAsync<T>(string url)
    {
        var response = await _http.GetAsync(url);

        if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            _logger.LogWarning("Eventbrite rate limit hit for {Url}. Retrying after 1s.", url);
            await Task.Delay(TimeSpan.FromSeconds(1));
            response = await _http.GetAsync(url);
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Eventbrite request failed: {StatusCode} for {Url}", response.StatusCode, url);
            response.EnsureSuccessStatusCode();
        }

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }
}
