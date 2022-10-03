using System;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Polly.Retry;

namespace Tvmaze.Scraper;

public interface ITvMazeClient
{
    Task<IList<Show>> GetShowsAsync();
    Task<IList<Cast>> GetCastByShowIdAsync(string id);
}

public class TvMazeClient : ITvMazeClient
{
    private readonly HttpClient _client;
    private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;
    private readonly ILogger<TvMazeClient> _logger;
    public TvMazeClient(HttpClient httpClient, AsyncRetryPolicy<HttpResponseMessage> retryPolicy, ILogger<TvMazeClient> logger)
    {
        httpClient.BaseAddress = new Uri("https://api.tvmaze.com");
        httpClient.Timeout = TimeSpan.FromSeconds(5);
        _client = httpClient;
        _retryPolicy = retryPolicy;
        _logger = logger;
    }

    public async Task<IList<Show>> GetShowsAsync()
    {
        var httpResponse = await _retryPolicy.ExecuteAsync(async () => await _client.GetAsync("/shows").ConfigureAwait(false)).ConfigureAwait(false);
        httpResponse.EnsureSuccessStatusCode();
        var response = await httpResponse.Content.ReadAsStringAsync();
        var showList = JsonConvert.DeserializeObject<List<Show>>(response);
        return showList ?? new List<Show>();
    }

    public async Task<IList<Cast>> GetCastByShowIdAsync(string id)
    {
        var httpResponse = await _retryPolicy.ExecuteAsync(async () => await _client.GetAsync($"/shows/{id}/cast").ConfigureAwait(false)).ConfigureAwait(false);
        var response = await httpResponse.Content.ReadAsStringAsync();
        var castList = JsonConvert.DeserializeObject<List<Cast>>(response);
        return castList ?? new List<Cast>();
    }
}

public class Show
{
    public int Id { get; set; }
    public string? Name { get; set; }
}

public class Cast
{
    public Person? Person { get; set; }
}

public class Person
{
    public int Id { get; set; }
    [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd")]
    public DateTime? Birthday { get; set; }
    public string? Name { get; set; }
}

public class DateFormatConverter : IsoDateTimeConverter
{
    public DateFormatConverter(string format)
    {
        DateTimeFormat = format;
    }
}

