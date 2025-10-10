using ChatBackend;
using ChatBackend.Factories;
using ChatBackend.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddSingleton<IChatProvider, HarmonyFormatProvider>();

builder.Services.AddSingleton<IChatProviderFactory, ChatProviderFactory>();

var app = builder.Build();

app.MapControllers();

app.Run();
