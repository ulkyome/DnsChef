#!/bin/bash
# uninstall-dnschef.sh - Uninstall script for DnsChef

set -e

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

log_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

confirm_uninstall() {
    read -p "Are you sure you want to uninstall DnsChef? This will remove all data. (y/N): " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        log_info "Uninstall cancelled"
        exit 0
    fi
}

stop_service() {
    log_info "Stopping DnsChef service..."
    systemctl stop dnschef.service 2>/dev/null || true
    systemctl disable dnschef.service 2>/dev/null || true
}

remove_files() {
    log_info "Removing DnsChef files..."
    
    # Remove installation directories
    rm -rf /opt/dnschef
    rm -rf /etc/dnschef
    rm -rf /var/log/dnschef
    
    # Remove systemd service
    rm -f /etc/systemd/system/dnschef.service
    systemctl daemon-reload
    
    # Remove user
    if id "dnschef" &>/dev/null; then
        userdel dnschef 2>/dev/null || true
    fi
}

remove_firewall_rules() {
    log_info "Removing firewall rules..."
    
    if command -v ufw >/dev/null 2>&1; then
        ufw delete allow 80/tcp 2>/dev/null || true
        ufw delete allow 5353/udp 2>/dev/null || true
    fi
}

main() {
    log_warning "This will completely remove DnsChef from your system."
    confirm_uninstall
    
    stop_service
    remove_files
    remove_firewall_rules
    
    log_info "DnsChef has been completely uninstalled"
}

# Check if running as root
if [[ $EUID -ne 0 ]]; then
    log_error "This script must be run as root"
    exit 1
fi

main "$@"