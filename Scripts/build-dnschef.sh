#!/bin/bash
# build-dnschef.sh - Build script for DnsChef

set -e

RED='\033[0;31m'
GREEN='\033[0;32m'
BLUE='\033[0;34m'
NC='\033[0m'

log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check for .NET SDK
check_dotnet_sdk() {
    if ! command -v dotnet >/dev/null 2>&1; then
        log_error ".NET SDK not found. Please install .NET 8 SDK first."
        exit 1
    fi
    
    local version=$(dotnet --version)
    log_info "Found .NET SDK version: $version"
}

# Clean previous builds
clean_build() {
    log_info "Cleaning previous builds..."
    dotnet clean
    rm -rf bin/ obj/ publish/
}

# Build the application
build_app() {
    log_info "Building DnsChef..."
    dotnet build -c Release
}

# Publish the application
publish_app() {
    log_info "Publishing DnsChef for linux-x64..."
    dotnet publish -c Release -r linux-x64 --self-contained false -o publish/
    
    # Make sure the binary is executable
    chmod +x publish/DnsChef
}

# Create deployment package
create_package() {
    log_info "Creating deployment package..."
    tar -czf dnschef-release.tar.gz -C publish .
    log_success "Deployment package created: dnschef-release.tar.gz"
}

main() {
    log_info "Starting DnsChef build process..."
    
    check_dotnet_sdk
    clean_build
    build_app
    publish_app
    create_package
    
    log_success "Build completed successfully!"
    echo ""
    echo "Next steps:"
    echo "1. Copy dnschef-release.tar.gz to your Debian server"
    echo "2. Extract: tar -xzf dnschef-release.tar.gz"
    echo "3. Run the installer: sudo ./install-dnschef.sh"
}

main "$@"