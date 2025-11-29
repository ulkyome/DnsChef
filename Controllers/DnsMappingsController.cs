// Controllers/DnsMappingsController.cs
using Microsoft.AspNetCore.Mvc;
using DnsChef.Models;
using DnsChef.Services;

namespace DnsChef.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DnsMappingsController : ControllerBase
    {
        private readonly IDnsServerService _dnsServerService;
        private readonly ILogger<DnsMappingsController> _logger;

        public DnsMappingsController(IDnsServerService dnsServerService, ILogger<DnsMappingsController> logger)
        {
            _dnsServerService = dnsServerService;
            _logger = logger;
        }

        [HttpGet]
        public ActionResult<IEnumerable<DnsMapping>> GetMappings()
        {
            try
            {
                var mappings = _dnsServerService.GetMappings();
                return Ok(mappings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting DNS mappings");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{domain}")]
        public ActionResult<DnsMapping> GetMapping(string domain)
        {
            try
            {
                var mapping = _dnsServerService.GetMapping(domain);
                if (mapping == null)
                {
                    return NotFound($"Mapping for domain '{domain}' not found");
                }
                return Ok(mapping);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting DNS mapping for domain {Domain}", domain);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        public ActionResult<DnsMapping> CreateMapping([FromBody] CreateMappingRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Domain))
                {
                    return BadRequest("Domain is required");
                }

                if (string.IsNullOrWhiteSpace(request.IpAddress))
                {
                    return BadRequest("IP address is required");
                }

                _dnsServerService.AddMapping(request.Domain, request.IpAddress);
                var mapping = _dnsServerService.GetMapping(request.Domain);

                return CreatedAtAction(nameof(GetMapping), new { domain = request.Domain }, mapping);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating DNS mapping");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("{domain}")]
        public ActionResult DeleteMapping(string domain)
        {
            try
            {
                var result = _dnsServerService.RemoveMapping(domain);
                if (!result)
                {
                    return NotFound($"Mapping for domain '{domain}' not found");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting DNS mapping for domain {Domain}", domain);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}