// Models/DnsServerStatus.cs (дополнение)
public class DnsServerStatus
{
    public bool IsRunning { get; set; }
    public int Port { get; set; }
    public string UpstreamDns { get; set; } = "8.8.8.8";
    public int TotalMappings { get; set; }
    public int RequestsProcessed { get; set; }
    public DateTime StartTime { get; set; }
    public int LogEntriesCount { get; set; } // Новое поле
}