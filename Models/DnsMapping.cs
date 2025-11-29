// Models/DnsMapping.cs
using System.Net;

namespace DnsChef.Models
{
    public class DnsMapping
    {
        public string Domain { get; set; } = string.Empty;
        public IPAddress IpAddress { get; set; } = IPAddress.Any;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool Enabled { get; set; } = true;
    }

    public class CreateMappingRequest
    {
        public string Domain { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty; // Для API остаётся string
    }

    public class DnsServerStatus
    {
        public bool IsRunning { get; set; }
        public int Port { get; set; }
        public IPAddress UpstreamDns { get; set; } = IPAddress.Parse("8.8.8.8");
        public int TotalMappings { get; set; }
        public int RequestsProcessed { get; set; }
        public DateTime StartTime { get; set; }
    }
}