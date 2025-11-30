namespace DnsChef.Models
{
    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Level { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Domain { get; set; }
        public string? IpAddress { get; set; }
        public string? ClientIp { get; set; }
        public string? QueryType { get; set; }
        public string? Action { get; set; } // "spoofed", "forwarded", "error"
    }

    public class LogQuery
    {
        public int? Page { get; set; } = 1;
        public int? PageSize { get; set; } = 50;
        public string? Level { get; set; }
        public string? Domain { get; set; }
        public string? Action { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class LogResponse
    {
        public List<LogEntry> Logs { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}