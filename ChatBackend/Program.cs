using ChatBackend;
using ChatBackend.Data;
using ChatBackend.Factories;
using ChatBackend.Interfaces;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();


builder.Services.AddDbContextFactory<ChatContext>(options =>
    options.UseNpgsql( new NpgsqlDataSourceBuilder(builder.Configuration["POSTGRES_CONNECTION_STRING"] ?? throw new("POSTGRES_CONNECTION_STRING is null."))
        .EnableDynamicJson()
        .Build()
    ));

builder.Services.AddSingleton<IChatCache, ChatCache>();

builder.Services.AddSingleton<IToolFactory, ToolFactory>();

builder.Services.AddHttpClient<ILLMProvider, OllamaProvider>(client =>
    client.BaseAddress = new Uri(builder.Configuration["OLLAMA_BASE_ADDRESS"] ?? throw new("OLLAMA_BASE_ADDRESS is null.")));

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
