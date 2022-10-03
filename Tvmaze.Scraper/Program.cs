using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Polly;
using Polly.RateLimit;
using Polly.Retry;
using System.Globalization;
using System.Net;
using System.Threading;
using Tvmaze.Data;
using Tvmaze.Scraper;

var builder = WebApplication.CreateBuilder(args);
// to fix postgres DataTime issue on dotnet6
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
// logging only to console for now
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
// this api probably will be for internal use
builder.WebHost.UseKestrel(options =>
{
    options.Limits.MaxConcurrentConnections = 10;
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(60);
});
// Tvmaze.Data
builder.Services.AddDbContext<DataContext>();
// swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// polly retry policy
var httpRetryPolicy = Policy.HandleResult<HttpResponseMessage>(mes => mes.StatusCode == HttpStatusCode.TooManyRequests)
    .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromMilliseconds(10500 * (retryAttempt - 1)));
builder.Services.AddSingleton<AsyncRetryPolicy<HttpResponseMessage>>(httpRetryPolicy);
// http client
builder.Services.AddHttpClient<ITvMazeClient, TvMazeClient>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPost("/start_scrape", async (ITvMazeClient tvMazeClient, DataContext db) =>
{
    var tvShows = await tvMazeClient.GetShowsAsync();
    var semaphoreSlim = new SemaphoreSlim(1, 1);
    await semaphoreSlim.WaitAsync();
    try
    {
        foreach (var show in tvShows)
        {
            if (db.Shows.All(p => p.Id != show.Id))
            {
                db.Shows.Add(new Tvmaze.Data.Show() { Id = show.Id, Name = show.Name ?? "" });
            }

            var cast = await tvMazeClient.GetCastByShowIdAsync(show.Id.ToString());

            var personList = cast.Where(p => p.Person != null && p.Person.Birthday != null && !string.IsNullOrEmpty(p.Person.Name))
            .Select(c => new Tvmaze.Data.Person()
            {
                Id = c.Person!.Id,
                Birthday = c.Person!.Birthday!.Value,
                Name = c.Person!.Name ?? ""
            })
            .DistinctBy(p => p.Id);

            db.Persons.AddRange(personList.Where(p => db.Persons.All(q => q.Id != p.Id)));


            var showPersons = personList.Select(c => new Tvmaze.Data.Cast() { PersonId = c.Id, ShowId = show.Id });

            db.Casts.AddRange(showPersons.Where(p => db.Casts.All(q => q.ShowId != p.ShowId && q.PersonId != p.PersonId)));

            db.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Scraping issue!");
    }
    finally
    {
        semaphoreSlim.Release();
    }
})
.WithName("Scrape");

app.Run();