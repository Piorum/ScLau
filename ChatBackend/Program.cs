using System.Reflection;
using ChatBackend;
using ChatBackend.Factories;
using ChatBackend.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var toolInterfaceType = typeof(ITool<>);
var toolImplementations = Assembly.GetExecutingAssembly().GetTypes()
    .Where(t => t.IsClass && !t.IsAbstract && t.GetInterfaces()
        .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == toolInterfaceType));

foreach (var toolType in toolImplementations)
{
    builder.Services.AddScoped(toolType);
}

builder.Services.AddSingleton<IToolDiscoveryService, ToolDiscoveryService>();

builder.Services.AddSingleton<IChatProvider, HarmonyFormatProvider>();

builder.Services.AddSingleton<IChatProviderFactory, ChatProviderFactory>();

var app = builder.Build();

app.MapControllers();

app.Run();
