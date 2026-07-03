using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace QACopilot.Infrastructure.Data.Context;

public class QACopilotDbContextFactory : IDesignTimeDbContextFactory<QACopilotDbContext>
{
    public QACopilotDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../QACopilot.API"))
            .AddJsonFile("appsettings.json")
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<QACopilotDbContext>();
        optionsBuilder.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));

        return new QACopilotDbContext(optionsBuilder.Options);
    }
}