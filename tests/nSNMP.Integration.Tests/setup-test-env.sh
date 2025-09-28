#!/bin/bash

# nSNMP Integration Test Environment Setup Script
# This script sets up the required Docker images and validates the test environment

set -euo pipefail

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Script directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Logging functions
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_warn() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

# Check prerequisites
check_prerequisites() {
    log_info "Checking prerequisites..."

    # Check Docker
    if ! command -v docker &> /dev/null; then
        log_error "Docker is not installed or not in PATH"
        exit 1
    fi

    # Check Docker daemon
    if ! docker info &> /dev/null; then
        log_error "Docker daemon is not running"
        exit 1
    fi

    # Check .NET
    if ! command -v dotnet &> /dev/null; then
        log_error ".NET SDK is not installed or not in PATH"
        exit 1
    fi

    # Check .NET version
    DOTNET_VERSION=$(dotnet --version)
    if [[ ! "$DOTNET_VERSION" =~ ^9\. ]]; then
        log_warn "Expected .NET 9.x, found $DOTNET_VERSION"
    fi

    log_success "Prerequisites check passed"
}

# Build or pull required Docker images
setup_docker_images() {
    log_info "Setting up Docker images..."

    # Pull SNMPsim image
    log_info "Pulling SNMPsim image..."
    if docker pull snmpsim/snmpsim:latest; then
        log_success "SNMPsim image pulled successfully"
    else
        log_warn "Failed to pull SNMPsim image, attempting to build..."
        # Fallback: build SNMPsim image if pull fails
        build_snmpsim_image
    fi

    # Pull Pumba image
    log_info "Pulling Pumba image..."
    if docker pull gaiaadm/pumba:latest; then
        log_success "Pumba image pulled successfully"
    else
        log_error "Failed to pull Pumba image"
        exit 1
    fi

    # Pull NET-SNMP tools image
    log_info "Pulling NET-SNMP tools image..."
    if docker pull net-snmp/net-snmp:latest; then
        log_success "NET-SNMP tools image pulled successfully"
    else
        log_warn "NET-SNMP tools image not available, will use alternative"
        build_netsnmp_image
    fi
}

# Build SNMPsim image if pull fails
build_snmpsim_image() {
    log_info "Building SNMPsim Docker image..."

    cat > "${SCRIPT_DIR}/Dockerfile.snmpsim" << 'EOF'
FROM python:3.11-slim

RUN apt-get update && apt-get install -y \
    snmp \
    snmp-mibs-downloader \
    && rm -rf /var/lib/apt/lists/*

RUN pip install snmpsim

EXPOSE 161/udp

ENTRYPOINT ["snmpsim.py"]
EOF

    if docker build -t snmpsim/snmpsim:latest -f "${SCRIPT_DIR}/Dockerfile.snmpsim" "${SCRIPT_DIR}"; then
        log_success "SNMPsim image built successfully"
        rm -f "${SCRIPT_DIR}/Dockerfile.snmpsim"
    else
        log_error "Failed to build SNMPsim image"
        exit 1
    fi
}

# Build NET-SNMP tools image if needed
build_netsnmp_image() {
    log_info "Building NET-SNMP tools Docker image..."

    cat > "${SCRIPT_DIR}/Dockerfile.netsnmp" << 'EOF'
FROM ubuntu:22.04

RUN apt-get update && apt-get install -y \
    snmp \
    snmp-mibs-downloader \
    && rm -rf /var/lib/apt/lists/*

# Download and configure MIBs
RUN download-mibs && \
    echo "mibs +ALL" > /etc/snmp/snmp.conf

ENTRYPOINT ["/bin/bash"]
EOF

    if docker build -t net-snmp/net-snmp:latest -f "${SCRIPT_DIR}/Dockerfile.netsnmp" "${SCRIPT_DIR}"; then
        log_success "NET-SNMP tools image built successfully"
        rm -f "${SCRIPT_DIR}/Dockerfile.netsnmp"
    else
        log_error "Failed to build NET-SNMP tools image"
        exit 1
    fi
}

# Build integration test project
build_project() {
    log_info "Building integration test project..."

    cd "${SCRIPT_DIR}"

    # Restore dependencies
    if dotnet restore Testbed.sln; then
        log_success "Dependencies restored"
    else
        log_error "Failed to restore dependencies"
        exit 1
    fi

    # Build project
    if dotnet build Testbed.sln --configuration Release --no-restore; then
        log_success "Project built successfully"
    else
        log_error "Failed to build project"
        exit 1
    fi
}

# Validate test scenarios
validate_scenarios() {
    log_info "Validating test scenarios..."

    cd "${SCRIPT_DIR}"

    if dotnet run --project nSNMP.Integration.Tests --configuration Release -- validate; then
        log_success "Test scenarios validation passed"
    else
        log_error "Test scenarios validation failed"
        exit 1
    fi
}

# Test Docker setup
test_docker_setup() {
    log_info "Testing Docker setup with sample containers..."

    cd "${SCRIPT_DIR}"

    # Start a sample container
    log_info "Starting sample SNMP container..."
    if docker-compose -f docker-compose.test.yml up -d snmp-mib2-basic; then
        log_success "Sample container started"

        # Wait for container to be ready
        log_info "Waiting for container to be ready..."
        sleep 10

        # Test SNMP connectivity
        if docker exec snmp-mib2-basic snmpget -v2c -c public localhost 1.3.6.1.2.1.1.1.0 &> /dev/null; then
            log_success "SNMP connectivity test passed"
        else
            log_warn "SNMP connectivity test failed, but container is running"
        fi

        # Clean up
        log_info "Cleaning up test container..."
        docker-compose -f docker-compose.test.yml down
        log_success "Test container cleaned up"
    else
        log_error "Failed to start sample container"
        exit 1
    fi
}

# Create test output directory
setup_output_directory() {
    OUTPUT_DIR="${SCRIPT_DIR}/TestResults"
    log_info "Setting up output directory: $OUTPUT_DIR"

    mkdir -p "$OUTPUT_DIR"
    log_success "Output directory created"
}

# Display usage information
usage() {
    echo "nSNMP Integration Test Environment Setup"
    echo ""
    echo "Usage: $0 [OPTIONS]"
    echo ""
    echo "Options:"
    echo "  --skip-docker     Skip Docker image setup"
    echo "  --skip-build      Skip project build"
    echo "  --skip-validate   Skip scenario validation"
    echo "  --skip-test       Skip Docker connectivity test"
    echo "  --help            Show this help message"
    echo ""
    echo "This script will:"
    echo "1. Check prerequisites (Docker, .NET)"
    echo "2. Pull/build required Docker images"
    echo "3. Build the integration test project"
    echo "4. Validate test scenarios"
    echo "5. Test Docker setup"
    echo "6. Create output directory"
}

# Main function
main() {
    local SKIP_DOCKER=false
    local SKIP_BUILD=false
    local SKIP_VALIDATE=false
    local SKIP_TEST=false

    # Parse command line arguments
    while [[ $# -gt 0 ]]; do
        case $1 in
            --skip-docker)
                SKIP_DOCKER=true
                shift
                ;;
            --skip-build)
                SKIP_BUILD=true
                shift
                ;;
            --skip-validate)
                SKIP_VALIDATE=true
                shift
                ;;
            --skip-test)
                SKIP_TEST=true
                shift
                ;;
            --help)
                usage
                exit 0
                ;;
            *)
                log_error "Unknown option: $1"
                usage
                exit 1
                ;;
        esac
    done

    log_info "Starting nSNMP Integration Test Environment Setup"

    # Run setup steps
    check_prerequisites

    if [[ "$SKIP_DOCKER" != true ]]; then
        setup_docker_images
    fi

    if [[ "$SKIP_BUILD" != true ]]; then
        build_project
    fi

    if [[ "$SKIP_VALIDATE" != true ]]; then
        validate_scenarios
    fi

    setup_output_directory

    if [[ "$SKIP_TEST" != true ]]; then
        test_docker_setup
    fi

    log_success "Environment setup completed successfully!"
    echo ""
    log_info "You can now run integration tests with:"
    log_info "  cd ${SCRIPT_DIR}"
    log_info "  dotnet run -- run"
    echo ""
    log_info "Or start manual testing with:"
    log_info "  docker-compose -f docker-compose.test.yml up -d"
}

# Run main function with all arguments
main "$@"