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
        private readonly ILogService _logService;
        private UdpClient? _udpServer;
        private bool _isRunning;
        private readonly Dictionary<string, DnsMapping> _dnsMappings;
        private readonly string _upstreamDns;
        private readonly int _port;
        private readonly ILogger<DnsServerService> _logger;
        private int _requestsProcessed;
        private DateTime _startTime;
        private CancellationTokenSource? _cancellationTokenSource;

        public DnsServerService(IConfiguration configuration,ILogger<DnsServerService> logger,ILogService logService)
        {
            _logService = logService;
            _logger = logger;
            _port = configuration.GetValue<int>("DnsSettings:Port", 5353);
            _upstreamDns = configuration.GetValue<string>("DnsSettings:UpstreamDns", "8.8.8.8") ?? "8.8.8.8";
            _dnsMappings = new Dictionary<string, DnsMapping>(StringComparer.OrdinalIgnoreCase);
            _cancellationTokenSource = new CancellationTokenSource();

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
                _cancellationTokenSource = new CancellationTokenSource();

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

            _logger.LogInformation("Stopping DNS server...");

            _isRunning = false;

            // Отменяем все операции
            _cancellationTokenSource?.Cancel();

            try
            {
                _udpServer?.Close();
                _udpServer?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error while closing UDP server");
            }
            finally
            {
                _udpServer = null;
            }

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
            if (!IPAddress.TryParse(ipAddress, out _))
            {
                throw new ArgumentException("Invalid IP address format");
            }

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
            while (_isRunning && _udpServer != null && !_cancellationTokenSource!.Token.IsCancellationRequested)
            {
                try
                {
                    var result = await _udpServer.ReceiveAsync(_cancellationTokenSource.Token);
                    _ = Task.Run(async () => await ProcessRequestAsync(result));
                }
                catch (OperationCanceledException)
                {
                    _logger.LogDebug("DNS server operation cancelled - shutting down");
                    break;
                }
                catch (ObjectDisposedException)
                {
                    _logger.LogDebug("UDP client disposed - shutting down");
                    break;
                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.OperationAborted)
                {
                    _logger.LogDebug("Socket operation aborted - shutting down");
                    break;
                }
                catch (Exception ex)
                {
                    if (_isRunning) // Логируем только если сервер должен быть запущен
                    {
                        _logger.LogError(ex, "Error receiving DNS request");
                    }
                    await Task.Delay(1000); // Задержка перед повторной попыткой
                }
            }

            _logger.LogDebug("DNS request handler stopped");
        }

        private async Task ProcessRequestAsync(UdpReceiveResult request)
        {
            try
            {
                Interlocked.Increment(ref _requestsProcessed);
                var clientEndPoint = request.RemoteEndPoint;
                var requestData = request.Buffer;

                // Логируем входящий запрос
                _logService.AddLog(new LogEntry
                {
                    Timestamp = DateTime.UtcNow,
                    Level = "Information",
                    Message = $"DNS request received from {clientEndPoint}",
                    ClientIp = clientEndPoint.Address.ToString(),
                    Action = "received"
                });

                var dnsRequest = DnsPacket.FromBytes(requestData);
                var responsePacket = await ProcessDnsRequestAsync(dnsRequest, clientEndPoint.Address.ToString());

                var responseData = responsePacket.ToBytes();
                if (_udpServer != null && _isRunning)
                {
                    await _udpServer.SendAsync(responseData, responseData.Length, clientEndPoint);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing DNS request");

                // Логируем ошибку
                _logService.AddLog(new LogEntry
                {
                    Timestamp = DateTime.UtcNow,
                    Level = "Error",
                    Message = $"Error processing DNS request: {ex.Message}",
                    Action = "error"
                });
            }
        }

        private async Task<DnsPacket> ProcessDnsRequestAsync(DnsPacket request, string clientIp)
        {
            var response = new DnsPacket
            {
                TransactionId = request.TransactionId,
                Flags = 0x8180,
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

                if (mapping != null && mapping.Enabled && question.Type == 1)
                {
                    if (IPAddress.TryParse(mapping.IpAddress, out var ipAddress))
                    {
                        var answer = new DnsResourceRecord
                        {
                            Name = question.Name,
                            Type = question.Type,
                            Class = question.Class,
                            TTL = 300,
                            IPAddress = ipAddress
                        };
                        response.AnswerSection.Add(answer);
                        response.AnswerRRs++;

                        // Логируем подмену DNS
                        _logService.AddLog(new LogEntry
                        {
                            Timestamp = DateTime.UtcNow,
                            Level = "Information",
                            Message = $"DNS spoofed: {question.Name} -> {mapping.IpAddress}",
                            Domain = question.Name,
                            IpAddress = mapping.IpAddress,
                            ClientIp = clientIp,
                            QueryType = "A",
                            Action = "spoofed"
                        });

                        _logger.LogInformation("Spoofed DNS: {Domain} -> {IpAddress}", question.Name, mapping.IpAddress);
                    }
                }
                else
                {
                    try
                    {
                        var realResponse = await ForwardToUpstreamDns(request.ToBytes());
                        var realDnsResponse = DnsPacket.FromBytes(realResponse);

                        foreach (var realAnswer in realDnsResponse.AnswerSection)
                        {
                            response.AnswerSection.Add(realAnswer);
                            response.AnswerRRs++;
                        }

                        // Логируем перенаправление запроса
                        _logService.AddLog(new LogEntry
                        {
                            Timestamp = DateTime.UtcNow,
                            Level = "Information",
                            Message = $"DNS forwarded: {question.Name}",
                            Domain = question.Name,
                            ClientIp = clientIp,
                            QueryType = "A",
                            Action = "forwarded"
                        });

                        _logger.LogDebug("Forwarded DNS: {Domain}", question.Name);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error forwarding DNS request for {Domain}", question.Name);

                        // Логируем ошибку перенаправления
                        _logService.AddLog(new LogEntry
                        {
                            Timestamp = DateTime.UtcNow,
                            Level = "Error",
                            Message = $"Error forwarding DNS for {question.Name}: {ex.Message}",
                            Domain = question.Name,
                            ClientIp = clientIp,
                            Action = "error"
                        });
                    }
                }
            }

            return response;
        }

        private async Task<byte[]> ForwardToUpstreamDns(byte[] requestData)
        {
            using var upstreamClient = new UdpClient();
            var upstreamEndPoint = new IPEndPoint(IPAddress.Parse(_upstreamDns), 53);

            await upstreamClient.SendAsync(requestData, requestData.Length, upstreamEndPoint);
            var response = await upstreamClient.ReceiveAsync();

            return response.Buffer;
        }

        public void Dispose()
        {
            StopAsync().Wait();
            _cancellationTokenSource?.Dispose();
        }
    }
}