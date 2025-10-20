using ChatBackend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ChatBackend.Factories;

public class ChatContextDesignTimeFactory : IDesignTimeDbContextFactory<ChatContext>
{
    public ChatContext CreateDbContext(string[] args)
    {
        // This code builds a configuration object just like the main application would,
        // reading from appsettings.json.
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<ChatContext>();

        var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING") ?? throw new("CONNECTION_STRING is null");
        optionsBuilder.UseNpgsql(connectionString);

        // Create and return a new instance of the DbContext using the options.
        return new ChatContext(optionsBuilder.Options);
    }
}