using Microsoft.AspNetCore.Mvc;
using DnsChef.Models;
using DnsChef.Services;

namespace DnsChef.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LogsController : ControllerBase
    {
        private readonly ILogService _logService;
        private readonly ILogger<LogsController> _logger;

        public LogsController(ILogService logService, ILogger<LogsController> logger)
        {
            _logService = logService;
            _logger = logger;
        }

        [HttpGet]
        public ActionResult<LogResponse> GetLogs([FromQuery] LogQuery query)
        {
            try
            {
                var response = _logService.GetLogs(query);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving logs");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("stats")]
        public ActionResult<object> GetLogStats()
        {
            try
            {
                var allLogs = _logService.GetLogs(new LogQuery { PageSize = int.MaxValue });

                var stats = new
                {
                    TotalLogs = _logService.GetTotalLogCount(),
                    SpoofedCount = allLogs.Logs.Count(x => x.Action == "spoofed"),
                    ForwardedCount = allLogs.Logs.Count(x => x.Action == "forwarded"),
                    ErrorCount = allLogs.Logs.Count(x => x.Level == "Error"),
                    RecentDomains = allLogs.Logs
                        .Where(x => !string.IsNullOrEmpty(x.Domain))
                        .GroupBy(x => x.Domain)
                        .OrderByDescending(g => g.Count())
                        .Take(10)
                        .Select(g => new { Domain = g.Key, Count = g.Count() })
                        .ToList()
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving log statistics");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpDelete]
        public ActionResult ClearLogs()
        {
            try
            {
                _logService.ClearLogs();
                _logger.LogInformation("Logs cleared via API");
                return Ok(new { message = "Logs cleared successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing logs");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("realtime")]
        public async Task GetRealtimeLogs()
        {
            Response.ContentType = "text/event-stream";
            Response.Headers.Add("Cache-Control", "no-cache");
            Response.Headers.Add("Connection", "keep-alive");

            // Простая реализация Server-Sent Events для реального времени
            try
            {
                var initialLogs = _logService.GetLogs(new LogQuery { PageSize = 20 });
                foreach (var log in initialLogs.Logs.OrderBy(x => x.Timestamp))
                {
                    await WriteEventAsync(log);
                }

                // затычка вместо реальной подписки событий
                // Для пример просто ждем и отправляем пустые события
                while (!HttpContext.RequestAborted.IsCancellationRequested)
                {
                    await Task.Delay(5000);
                    await Response.WriteAsync(": keepalive\n\n");
                    await Response.Body.FlushAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in real-time logs stream");
            }
        }

        private async Task WriteEventAsync(LogEntry log)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(log);
            await Response.WriteAsync($"data: {json}\n\n");
            await Response.Body.FlushAsync();
        }
    }
}