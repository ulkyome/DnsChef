#!/bin/bash

# DnsChef Auto-Installer for Debian
# Version: 1.0
# Author: DnsChef

set -e  # Exit on any error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Logging functions
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check if running as root
check_root() {
    if [[ $EUID -eq 0 ]]; then
        log_warning "Running as root user"
    else
        log_error "This script must be run as root"
        exit 1
    fi
}

# Check Debian version
check_debian() {
    if [[ ! -f /etc/debian_version ]]; then
        log_error "This script is designed for Debian-based systems only"
        exit 1
    fi
    
    local version=$(lsb_release -rs 2>/dev/null || cat /etc/debian_version)
    log_info "Detected Debian/Ubuntu version: $version"
}

# Update system packages
update_system() {
    log_info "Updating system packages..."
    apt-get update
    apt-get upgrade -y
}

# Install .NET 8 runtime
install_dotnet() {
    log_info "Checking for .NET 8 runtime..."
    
    if dotnet --list-runtimes | grep -q "Microsoft.NETCore.App 8"; then
        log_success ".NET 8 runtime is already installed"
        return
    fi
    
    log_info "Installing .NET 8 runtime..."
    
    # Install dependencies
    apt-get install -y wget gnupg
    
    # Add Microsoft package repository
    wget -q https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
    dpkg -i packages-microsoft-prod.deb
    rm packages-microsoft-prod.deb
    
    # Update and install .NET
    apt-get update
    apt-get install -y dotnet-runtime-8.0
    
    log_success ".NET 8 runtime installed successfully"
}

# Install dependencies
install_dependencies() {
    log_info "Installing dependencies..."
    apt-get install -y curl wget systemd
}

# Create dnschef user
create_user() {
    log_info "Creating dnschef user..."
    
    if id "dnschef" &>/dev/null; then
        log_warning "User dnschef already exists"
    else
        useradd -r -s /bin/false -d /opt/dnschef dnschef
        log_success "User dnschef created"
    fi
}

# Download and install DnsChef
install_dnschef() {
    local INSTALL_DIR="/opt/dnschef"
    local CONFIG_DIR="/etc/dnschef"
    local LOG_DIR="/var/log/dnschef"
    local SERVICE_FILE="/etc/systemd/system/dnschef.service"
    
    log_info "Installing DnsChef to $INSTALL_DIR..."
    
    # Create directories
    mkdir -p $INSTALL_DIR $CONFIG_DIR $LOG_DIR
    
    # Check if we're running from a build directory
    if [[ -f "DnsChef" ]] || [[ -f "DnsChef.dll" ]]; then
        log_info "Found local build, copying files..."
        
        # Copy all files from current directory
        cp -r . $INSTALL_DIR/
    else
        log_error "No DnsChef binary found in current directory"
        log_info "Please build the application first using: dotnet publish -c Release"
        exit 1
    fi
    
    # Create configuration file
    log_info "Creating configuration file..."
    cat > $CONFIG_DIR/appsettings.json << 'EOF'
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "DnsSettings": {
    "Port": 5353,
    "UpstreamDns": "8.8.8.8",
    "Mappings": {
      "example.com": "127.0.0.1",
      "test.local": "192.168.1.100"
    }
  }
}
EOF
    
    # Create environment file
    cat > $CONFIG_DIR/dnschef.env << EOF
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://*:80
DNSCHED_CONFIG_DIR=$CONFIG_DIR
EOF
    
    # Set permissions
    chown -R dnschef:dnschef $INSTALL_DIR $CONFIG_DIR $LOG_DIR
    chmod 755 $INSTALL_DIR
    chmod 750 $CONFIG_DIR $LOG_DIR
    chmod 644 $CONFIG_DIR/appsettings.json
    
    log_success "DnsChef files installed"
}

# Create systemd service
create_systemd_service() {
    local SERVICE_FILE="/etc/systemd/system/dnschef.service"
    
    log_info "Creating systemd service..."
    
    cat > $SERVICE_FILE << 'EOF'
[Unit]
Description=DnsChef DNS Proxy Server
After=network.target
Wants=network.target

[Service]
Type=exec
User=dnschef
Group=dnschef
WorkingDirectory=/opt/dnschef
EnvironmentFile=/etc/dnschef/dnschef.env
ExecStart=/usr/bin/dotnet /opt/dnschef/DnsChef.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
StandardOutput=journal
StandardError=journal

# Security settings
NoNewPrivileges=yes
PrivateTmp=yes
ProtectSystem=strict
ProtectHome=yes
ReadWritePaths=/var/log/dnschef /etc/dnschef
ProtectKernelTunables=yes
ProtectKernelModules=yes
ProtectControlGroups=yes

# Capabilities
CapabilityBoundingSet=CAP_NET_BIND_SERVICE
AmbientCapabilities=CAP_NET_BIND_SERVICE

[Install]
WantedBy=multi-user.target
EOF
    
    systemctl daemon-reload
    log_success "Systemd service created"
}

# Configure firewall
configure_firewall() {
    log_info "Configuring firewall..."
    
    if command -v ufw >/dev/null 2>&1 && ufw status | grep -q "Status: active"; then
        ufw allow 80/tcp comment "DnsChef API"
        ufw allow 5353/udp comment "DnsChef DNS"
        log_success "Firewall configured"
    elif command -v iptables >/dev/null 2>&1; then
        iptables -A INPUT -p tcp --dport 80 -j ACCEPT
        iptables -A INPUT -p udp --dport 5353 -j ACCEPT
        log_success "iptables rules added"
    else
        log_warning "No firewall manager detected, please configure manually"
    fi
}

# Start and enable service
enable_service() {
    log_info "Starting DnsChef service..."
    
    systemctl enable dnschef.service
    systemctl start dnschef.service
    
    # Wait a moment for service to start
    sleep 3
    
    if systemctl is-active --quiet dnschef.service; then
        log_success "DnsChef service started successfully"
    else
        log_error "Failed to start DnsChef service"
        journalctl -u dnschef.service -n 10 --no-pager
        exit 1
    fi
}

# Display installation summary
show_summary() {
    log_success "DnsChef installation completed!"
    echo ""
    echo -e "${GREEN}=== Installation Summary ===${NC}"
    echo "Installation directory: /opt/dnschef"
    echo "Configuration directory: /etc/dnschef"
    echo "Log directory: /var/log/dnschef"
    echo "Service user: dnschef"
    echo ""
    echo -e "${GREEN}=== Access Information ===${NC}"
    echo "Web API: http://$(hostname -I | awk '{print $1}'):80"
    echo "Swagger UI: http://$(hostname -I | awk '{print $1}')"
    echo "DNS Server: udp://$(hostname -I | awk '{print $1}'):5353"
    echo ""
    echo -e "${GREEN}=== Service Management ===${NC}"
    echo "Start service: systemctl start dnschef"
    echo "Stop service: systemctl stop dnschef"
    echo "Restart service: systemctl restart dnschef"
    echo "View logs: journalctl -u dnschef.service -f"
    echo ""
    echo -e "${GREEN}=== Next Steps ===${NC}"
    echo "1. Configure your DNS settings to use $(hostname -I | awk '{print $1}'):5353"
    echo "2. Access the web interface at http://$(hostname -I | awk '{print $1}')"
    echo "3. Add DNS mappings via the API or edit /etc/dnschef/appsettings.json"
    echo ""
}

# Main installation function
main() {
    log_info "Starting DnsChef installation..."
    
    check_root
    check_debian
    update_system
    install_dependencies
    install_dotnet
    create_user
    install_dnschef
    create_systemd_service
    configure_firewall
    enable_service
    show_summary
    
    log_success "Installation completed successfully!"
}

# Run main function
main "$@"