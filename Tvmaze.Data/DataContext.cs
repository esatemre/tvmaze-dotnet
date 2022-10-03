namespace Tvmaze.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

public class DataContext : DbContext
{
    protected readonly IConfiguration Configuration;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public DataContext(IConfiguration configuration)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    {
        Configuration = configuration;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseNpgsql(Configuration.GetConnectionString("Database"), b => b.MigrationsAssembly("Tvmaze.Api"));
    }

    public DbSet<Show> Shows { get; set; }
    public DbSet<Person> Persons { get; set; }
    public DbSet<Cast> Casts { get; set; }
}

public class Cast
{
    public int Id { get; set; }
    public int ShowId { get; set; }
    public int PersonId { get; set; }

    public Show? Show { get; set; }
    public Person? Person { get; set; }
}

public class Show
{
    public int Id { get; set; }
    public string Name { get; set; } = "";

    public List<Cast> Casts { get; set; } = new List<Cast>();
}

public class Person
{
    public int Id { get; set; }
    public DateTime Birthday { get; set; }
    public string Name { get; set; } = "";
}