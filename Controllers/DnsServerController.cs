// Controllers/DnsServerController.cs
using Microsoft.AspNetCore.Mvc;
using DnsChef.Models;
using DnsChef.Services;

namespace DnsChef.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DnsServerController : ControllerBase
    {
        private readonly IDnsServerService _dnsServerService;
        private readonly ILogger<DnsServerController> _logger;

        public DnsServerController(IDnsServerService dnsServerService, ILogger<DnsServerController> logger)
        {
            _dnsServerService = dnsServerService;
            _logger = logger;
        }

        [HttpGet("status")]
        public ActionResult<DnsServerStatus> GetStatus()
        {
            try
            {
                var status = _dnsServerService.GetStatus();
                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting DNS server status");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("start")]
        public async Task<ActionResult> StartServer()
        {
            try
            {
                await _dnsServerService.StartAsync();
                return Ok(new { message = "DNS server started successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting DNS server");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("stop")]
        public async Task<ActionResult> StopServer()
        {
            try
            {
                await _dnsServerService.StopAsync();
                return Ok(new { message = "DNS server stopped successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping DNS server");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("restart")]
        public async Task<ActionResult> RestartServer()
        {
            try
            {
                await _dnsServerService.StopAsync();
                await Task.Delay(1000); // Small delay before restart
                await _dnsServerService.StartAsync();
                return Ok(new { message = "DNS server restarted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restarting DNS server");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}