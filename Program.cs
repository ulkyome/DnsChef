// Program.cs
using DnsChef.Services;
using DnsChef.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure DNS Server Service
builder.Services.AddSingleton<IDnsServerService, DnsServerService>();

// Configure logging
builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.AddConsole();
    loggingBuilder.AddDebug();
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.MapControllers();

// Start DNS server automatically
var dnsService = app.Services.GetRequiredService<IDnsServerService>();
await dnsService.StartAsync();

// Graceful shutdown
app.Lifetime.ApplicationStopping.Register(() =>
{
    dnsService.StopAsync().Wait();
});

app.Run();