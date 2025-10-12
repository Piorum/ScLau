using System.Reflection;
using ChatBackend;
using ChatBackend.Attributes;
using ChatBackend.Factories;
using ChatBackend.Interfaces;
using ChatBackend.Tools;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var tools = Assembly.GetExecutingAssembly().GetTypes().Where(t =>
    t.GetCustomAttribute<ToolAttribute>() != null &&
    t.GetInterfaces().Any(i =>
        i.IsGenericType &&
        i.GetGenericTypeDefinition() == typeof(ITool<>)));
        
foreach (var tool in tools)
    builder.Services.AddSingleton(typeof(ITool), tool);

builder.Services.AddSingleton<IToolFactory, ToolFactory>();
builder.Services.AddSingleton<IToolExecutor, ToolExecutor>();

builder.Services.AddSingleton<IChatProvider, HarmonyFormatProvider>();
builder.Services.AddSingleton<IChatProviderFactory, ChatProviderFactory>();

var app = builder.Build();

app.MapControllers();

app.Run();
