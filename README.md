# DnsChef 🍳

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![License](https://img.shields.io/badge/license-MIT-green)
![Platform](https://img.shields.io/badge/platform-linux--x64-lightgrey)

A powerful DNS proxy server with Web API for DNS spoofing and redirection. Written in C# for .NET 8, DnsChef allows you to intercept and manipulate DNS queries in real-time.

## 🚀 Features

- **DNS Spoofing**: Redirect specific domains to custom IP addresses
- **Web API**: RESTful API for managing DNS mappings
- **Swagger UI**: Interactive API documentation
- **Real-time Management**: Add/remove mappings without restart
- **Upstream DNS Fallback**: Forward unmatched queries to upstream DNS
- **Systemd Service**: Runs as a background service on Linux
- **Security**: Built with security best practices

## 📋 Requirements

- **OS**: Debian 11/12, Ubuntu 20.04+
- **Runtime**: .NET 8.0
- **Ports**: 80 (HTTP API), 5353 (DNS UDP)
- **Permissions**: Root access for installation

## 🛠️ Installation

### Automated Installation (Recommended)

1. **Build the application** (on development machine):
```bash
chmod +x build-dnschef.sh
./build-dnschef.sh
```
Copy to Debian server:
```scp dnschef-release.tar.gz user@your-server:/tmp/```

Install on Debian:
```
ssh user@your-server
tar -xzf /tmp/dnschef-release.tar.gz
chmod +x install-dnschef.sh
sudo ./install-dnschef.sh
```

Manual Installation
Install .NET 8 Runtime:

```
wget https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install -y dotnet-runtime-8.0
```

Deploy Application:
```
sudo mkdir -p /opt/dnschef /etc/dnschef /var/log/dnschef
sudo useradd -r -s /bin/false -d /opt/dnschef dnschef
```
# Copy published files to /opt/dnschef
```
sudo cp -r publish/* /opt/dnschef/
sudo cp appsettings.json /etc/dnschef/

sudo chown -R dnschef:dnschef /opt/dnschef /etc/dnschef /var/log/dnschef
```
Create Systemd Service:
```
sudo cp dnschef.service /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable dnschef
sudo systemctl start dnschef
```
Configuration
Configuration File
Edit /etc/dnschef/appsettings.json:
```
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "DnsSettings": {
    "Port": 5353,
    "UpstreamDns": "8.8.8.8",
    "Mappings": {
      "google.com": "127.0.0.1",
      "facebook.com": "127.0.0.1",
      "test.example.com": "192.168.1.100"
    }
  }
}
```

DNS Client Configuration
Configure your devices to use the DnsChef server:

Linux:
```
echo "nameserver YOUR_SERVER_IP" | sudo tee /etc/resolv.conf
```
Docker:
```
docker run --dns YOUR_SERVER_IP your-image
```
Web API Usage
Access Swagger UI
http://your-server-ip

API Endpoints
Get Server Status:
```GET /api/dnsserver/status```
Response:
```
{
  "isRunning": true,
  "port": 5353,
  "upstreamDns": "8.8.8.8",
  "totalMappings": 3,
  "requestsProcessed": 142,
  "startTime": "2024-01-15T10:30:00Z"
}
```
Get All Mappings:
```GET /api/dnsmappings```
Response:
```
[
  {
    "domain": "google.com",
    "ipAddress": "127.0.0.1",
    "createdAt": "2024-01-15T10:30:00Z",
    "enabled": true
  }
]
```
Add DNS Mapping:
```POST /api/dnsmappings```
```
Content-Type: application/json

{
  "domain": "example.com",
  "ipAddress": "192.168.1.50"
}
```
Remove DNS Mapping:
```DELETE /api/dnsmappings/example.com```

Control Server:
```
POST /api/dnsserver/start
POST /api/dnsserver/stop
POST /api/dnsserver/restart
```
Use Cases
Development & Testing
Test website redirects

Simulate DNS failures

Local development with custom domains

Security
Block malicious domains

Redirect phishing sites to safe locations

Network monitoring and analysis

Education
DNS protocol learning

Network security demonstrations

Red team exercises

Monitoring
Check Service Status
```
sudo systemctl status dnschef
```
View Logs
```
journalctl -u dnschef.service -f
```
Real-time DNS Query Monitoring
```
sudo tcpdump -i any port 5353 -n
```
Project Structure
```
DnsChef/
├── Controllers/          # Web API controllers
│   ├── DnsMappingsController.cs
│   └── DnsServerController.cs
├── Models/              # Data models
│   └── DnsMapping.cs
├── Services/            # Business logic
│   └── DnsServerService.cs
├── Converters/          # JSON converters
│   └── IPAddressConverter.cs
├── Program.cs           # Application entry point
├── DnsChef.csproj      # Project configuration
├── appsettings.json    # Configuration template
└── Scripts/
    ├── install-dnschef.sh
    ├── build-dnschef.sh
    └── uninstall-dnschef.sh
```
🔒 Security Considerations
The service runs under a dedicated non-root user dnschef

DNS service binds to port 5353 (non-privileged)

HTTP API uses port 80 (requires CAP_NET_BIND_SERVICE)

Configuration files are protected with proper permissions

Systemd service includes security hardening options

🐛 Troubleshooting

Common Issues
Service fails to start:

```
sudo journalctl -u dnschef.service -n 50
```
DNS queries not working:

```
# Test DNS resolution
dig @your-server-ip google.com
```
Port 80 already in use:

```
sudo netstat -tulpn | grep :80
```
Firewall blocking access:

```
sudo ufw allow 80/tcp
sudo ufw allow 5353/udp
```
Debug Mode
Enable debug logging by editing ```/etc/dnschef/appsettings.json```:
```
{
"Logging": {
"LogLevel": {
"Default": "Debug",
"Microsoft.AspNetCore": "Warning"
}
}
}
```
Then restart the service:

```
sudo systemctl restart dnschef
```
License
This project is licensed under the MIT License.

Contributing
Fork the project

Create your feature branch

Commit your changes

Push to the branch

Open a Pull Request

Support
If you encounter any issues:

Check the troubleshooting section

Review service logs: journalctl -u dnschef

Open an issue with detailed information

Acknowledgments
Inspired by original DNSChef project

.NET community for excellent tooling

Debian project for robust Linux distribution

Happy DNS Cooking!
