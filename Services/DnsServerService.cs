// Services/DnsServerService.cs
using System.Net;
using System.Net.Sockets;
using DnsChef.Models;

namespace DnsChef.Services
{
    public interface IDnsServerService
    {
        Task StartAsync();
        Task StopAsync();
        DnsServerStatus GetStatus();
        void AddMapping(string domain, string ipAddress);
        bool RemoveMapping(string domain);
        IEnumerable<DnsMapping> GetMappings();
        DnsMapping? GetMapping(string domain);
    }

    public class DnsServerService : IDnsServerService
    {
        private UdpClient? _udpServer;
        private bool _isRunning;
        private readonly Dictionary<string, DnsMapping> _dnsMappings;
        private readonly IPAddress _upstreamDns;
        private readonly int _port;
        private readonly ILogger<DnsServerService> _logger;
        private int _requestsProcessed;
        private DateTime _startTime;

        public DnsServerService(IConfiguration configuration, ILogger<DnsServerService> logger)
        {
            _logger = logger;
            _port = configuration.GetValue<int>("DnsSettings:Port", 5353);

            // Парсим IPAddress для upstream DNS
            var upstreamDnsString = configuration.GetValue<string>("DnsSettings:UpstreamDns", "8.8.8.8") ?? "8.8.8.8";
            _upstreamDns = IPAddress.Parse(upstreamDnsString);

            _dnsMappings = new Dictionary<string, DnsMapping>(StringComparer.OrdinalIgnoreCase);

            // Загрузка начальных маппингов из конфигурации
            var initialMappings = configuration.GetSection("DnsSettings:Mappings").Get<Dictionary<string, string>>();
            if (initialMappings != null)
            {
                foreach (var mapping in initialMappings)
                {
                    AddMapping(mapping.Key, mapping.Value);
                }
            }
        }

        public async Task StartAsync()
        {
            if (_isRunning) return;

            try
            {
                _udpServer = new UdpClient(_port);
                _isRunning = true;
                _requestsProcessed = 0;
                _startTime = DateTime.UtcNow;

                _ = Task.Run(async () => await HandleRequestsAsync());

                _logger.LogInformation("DNS Server started on port {Port}", _port);
                _logger.LogInformation("Upstream DNS: {UpstreamDns}", _upstreamDns);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start DNS server");
                throw;
            }
        }

        public async Task StopAsync()
        {
            if (!_isRunning) return;

            _isRunning = false;
            _udpServer?.Close();
            _udpServer = null;

            _logger.LogInformation("DNS Server stopped");
        }

        public DnsServerStatus GetStatus()
        {
            return new DnsServerStatus
            {
                IsRunning = _isRunning,
                Port = _port,
                UpstreamDns = _upstreamDns,
                TotalMappings = _dnsMappings.Count,
                RequestsProcessed = _requestsProcessed,
                StartTime = _startTime
            };
        }

        public void AddMapping(string domain, string ipAddress)
        {
            if (!IPAddress.TryParse(ipAddress, out var ip))
            {
                throw new ArgumentException("Invalid IP address format");
            }

            AddMapping(domain, ip);
        }

        public void AddMapping(string domain, IPAddress ipAddress)
        {
            var mapping = new DnsMapping
            {
                Domain = domain.ToLower(),
                IpAddress = ipAddress
            };

            lock (_dnsMappings)
            {
                _dnsMappings[domain.ToLower()] = mapping;
            }

            _logger.LogInformation("Added DNS mapping: {Domain} -> {IpAddress}", domain, ipAddress);
        }

        public bool RemoveMapping(string domain)
        {
            lock (_dnsMappings)
            {
                var result = _dnsMappings.Remove(domain.ToLower());
                if (result)
                {
                    _logger.LogInformation("Removed DNS mapping: {Domain}", domain);
                }
                return result;
            }
        }

        public IEnumerable<DnsMapping> GetMappings()
        {
            lock (_dnsMappings)
            {
                return _dnsMappings.Values.OrderBy(m => m.Domain).ToList();
            }
        }

        public DnsMapping? GetMapping(string domain)
        {
            lock (_dnsMappings)
            {
                return _dnsMappings.GetValueOrDefault(domain.ToLower());
            }
        }

        private async Task HandleRequestsAsync()
        {
            while (_isRunning && _udpServer != null)
            {
                try
                {
                    var result = await _udpServer.ReceiveAsync();
                    _ = Task.Run(async () => await ProcessRequestAsync(result));
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error receiving DNS request");
                }
            }
        }

        private async Task ProcessRequestAsync(UdpReceiveResult request)
        {
            try
            {
                Interlocked.Increment(ref _requestsProcessed);
                var clientEndPoint = request.RemoteEndPoint;
                var requestData = request.Buffer;

                _logger.LogDebug("Received DNS request from {ClientEndPoint}", clientEndPoint);

                var dnsRequest = DnsPacket.FromBytes(requestData);
                var responsePacket = await ProcessDnsRequestAsync(dnsRequest);

                var responseData = responsePacket.ToBytes();
                if (_udpServer != null)
                {
                    await _udpServer.SendAsync(responseData, responseData.Length, clientEndPoint);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing DNS request");
            }
        }

        private async Task<DnsPacket> ProcessDnsRequestAsync(DnsPacket request)
        {
            var response = new DnsPacket
            {
                TransactionId = request.TransactionId,
                Flags = 0x8180, // Response + Success
                Questions = request.Questions,
                AnswerRRs = 0,
                AuthorityRRs = 0,
                AdditionalRRs = 0
            };

            foreach (var question in request.QuestionSection)
            {
                response.QuestionSection.Add(question);

                DnsMapping? mapping;
                lock (_dnsMappings)
                {
                    _dnsMappings.TryGetValue(question.Name.ToLower(), out mapping);
                }

                if (mapping != null && mapping.Enabled && question.Type == 1) // A record
                {
                    var answer = new DnsResourceRecord
                    {
                        Name = question.Name,
                        Type = question.Type,
                        Class = question.Class,
                        TTL = 300,
                        IPAddress = mapping.IpAddress
                    };
                    response.AnswerSection.Add(answer);
                    response.AnswerRRs++;

                    _logger.LogInformation("Spoofed DNS: {Domain} -> {IpAddress}", question.Name, mapping.IpAddress);
                }
                else
                {
                    // Forward to upstream DNS
                    var realResponse = await ForwardToUpstreamDns(request.ToBytes());
                    var realDnsResponse = DnsPacket.FromBytes(realResponse);

                    foreach (var realAnswer in realDnsResponse.AnswerSection)
                    {
                        response.AnswerSection.Add(realAnswer);
                        response.AnswerRRs++;
                    }

                    _logger.LogDebug("Forwarded DNS: {Domain}", question.Name);
                }
            }

            return response;
        }

        private async Task<byte[]> ForwardToUpstreamDns(byte[] requestData)
        {
            using var upstreamClient = new UdpClient();
            var upstreamEndPoint = new IPEndPoint(_upstreamDns, 53);

            await upstreamClient.SendAsync(requestData, requestData.Length, upstreamEndPoint);
            var response = await upstreamClient.ReceiveAsync();

            return response.Buffer;
        }
    }
}