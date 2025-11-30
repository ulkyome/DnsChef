using DnsChef.Services;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure DNS Server Service
builder.Services.AddSingleton<IDnsServerService, DnsServerService>();

// Configure logging
builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.AddConsole();
    loggingBuilder.AddDebug();
    loggingBuilder.AddFilter("Microsoft", LogLevel.Warning);
    loggingBuilder.AddFilter("System", LogLevel.Warning);
});

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "DnsChef API V1");
    options.RoutePrefix = string.Empty;
});

app.UseRouting();
app.MapControllers();

// Configure Kestrel to use port 80
app.Urls.Add("http://*:80");

// Start DNS server
var dnsService = app.Services.GetRequiredService<IDnsServerService>();

try
{
    await dnsService.StartAsync();
    Console.WriteLine("✅ DnsChef API Server started on port 80");
    Console.WriteLine("📚 Swagger UI available at: http://localhost");
    Console.WriteLine("🎯 DNS Server started on port 5353");
    Console.WriteLine("");
    Console.WriteLine("API endpoints:");
    Console.WriteLine("  GET    /api/dnsserver/status");
    Console.WriteLine("  POST   /api/dnsserver/start");
    Console.WriteLine("  POST   /api/dnsserver/stop");
    Console.WriteLine("  GET    /api/dnsmappings");
    Console.WriteLine("  POST   /api/dnsmappings");
    Console.WriteLine("  DELETE /api/dnsmappings/{domain}");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Failed to start DNS server: {ex.Message}");
}

// Graceful shutdown
var cancellationTokenSource = new CancellationTokenSource();
app.Lifetime.ApplicationStopping.Register(() =>
{
    Console.WriteLine("🛑 Application is stopping...");
    cancellationTokenSource.Cancel();

    try
    {
        dnsService.StopAsync().Wait(5000); // 5 second timeout
        Console.WriteLine("✅ DNS server stopped gracefully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error stopping DNS server: {ex.Message}");
    }
});

// Handle Ctrl+C
Console.CancelKeyPress += (sender, e) =>
{
    Console.WriteLine("🛑 Received Ctrl+C, shutting down...");
    e.Cancel = true;
    cancellationTokenSource.Cancel();
    app.StopAsync().Wait(5000);
};

try
{
    await app.RunAsync(cancellationTokenSource.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("✅ Application shutdown completed");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Application error: {ex.Message}");
}