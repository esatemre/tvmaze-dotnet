using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using Microsoft.EntityFrameworkCore;
using Tvmaze.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<DataContext>();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// run ef migrations
await using var scope = app.Services.CreateAsyncScope();
using var db = scope.ServiceProvider.GetRequiredService<DataContext>();
await db.Database.MigrateAsync();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


app.MapGet("/pagedshows", async (int pageNumber, int pageSize, DataContext db) =>
{
    var validationResult = Validate(pageNumber, pageSize);
    if (validationResult != null)
    {
        return Results.BadRequest(validationResult);
    }

    var data = await db.Shows.Skip((pageNumber - 1) * pageSize).Take(pageSize)
    .Include(p => p.Casts).ThenInclude(p => p.Person).ToListAsync();


    var response = new List<PagedShow>();

    foreach (var d in data)
    {
        response.Add(new PagedShow()
        {
            Id = d.Id,
            Name = d.Name,
            Cast = d.Casts.Select(p => new PagedShowCast()
            {
                Id = p.PersonId,
                Name = p.Person!.Name,
                Birthday = p.Person!.Birthday
            }).OrderByDescending(p => p.Birthday).ToList()
        });
    }

    return Results.Ok(response);
})
.Produces<List<PagedShow>>(StatusCodes.Status200OK)
.ProducesValidationProblem(StatusCodes.Status400BadRequest)
.WithName("GetShowsByPage");

app.Run();


static ValidationResult? Validate(int pageNumber, int pageSize)
{
    if (pageNumber < 1) { return new ValidationResult("pageNumber should be greater than 0", new[] { nameof(pageNumber) }); }

    if (pageSize > 50) { return new ValidationResult("pageSize should be lesser than 50", new[] { nameof(pageSize) }); }

    if (pageSize < 1) { return new ValidationResult("pageSize should be greater than 0", new[] { nameof(pageSize) }); }
    return null;
}

class PagedShow
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public List<PagedShowCast>? Cast { get; set; }
}

class PagedShowCast
{
    public int Id { get; set; }
    public DateTime Birthday { get; set; }
    public string? Name { get; set; }
}