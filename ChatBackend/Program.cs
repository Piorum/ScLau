using ChatBackend;
using ChatBackend.Data;
using ChatBackend.Factories;
using ChatBackend.Interfaces;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();

builder.Services.AddControllers();

var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING") ?? throw new("CONNECTION_STRING is null");
var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
dataSourceBuilder.EnableDynamicJson();
await using var dataSource = dataSourceBuilder.Build();
builder.Services.AddDbContextFactory<ChatContext>(options => options.UseNpgsql(dataSource));

builder.Services.AddSingleton<IChatCache, ChatCache>();

builder.Services.AddSingleton<IToolFactory, ToolFactory>();

builder.Services.AddSingleton<IChatProvider, HarmonyFormatProvider>();
builder.Services.AddSingleton<IChatProviderFactory, ChatProviderFactory>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ChatContext>();
    context.Database.Migrate();
}

app.MapControllers();

app.Run();
