using HandBrakeRenderer.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace HandBrakeRenderer;

class Program
{
    static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        // Configure settings
        ConfigureSettings(builder.Configuration);

        // Add services
        builder.Services.Configure<AppSettings>(
            builder.Configuration.GetSection(AppSettings.SectionName));

        builder.Services.AddSingleton<IFileManagementService, FileManagementService>();
        builder.Services.AddSingleton<IHandBrakeService, HandBrakeService>();
        builder.Services.AddSingleton<IRenderQueueService, RenderQueueService>();
        builder.Services.AddHostedService<RenderWorker>();

        // Configure logging
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.AddFile();

        var host = builder.Build();
        await host.RunAsync();
    }

    private static void ConfigureSettings(IConfiguration configuration)
    {
        var settingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");

        if (!File.Exists(settingsPath))
        {
            Console.WriteLine("Creating default appsettings.json...");
            CreateDefaultSettings(settingsPath);
        }
    }

    private static void CreateDefaultSettings(string path)
    {
        var settings = new
        {
            AppSettings = new AppSettings()
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(settings, options);
        File.WriteAllText(path, json);
        Console.WriteLine($"Created default settings at: {path}");
    }
}

// Extension method for file logging
public static class FileLoggerExtensions
{
    public static ILoggingBuilder AddFile(this ILoggingBuilder builder)
    {
        builder.Services.AddSingleton<ILoggerProvider, FileLoggerProvider>();
        return builder;
    }
}
