#!/bin/bash

# Enhanced multi-platform publish script for FLua
# This script publishes FLua CLI for multiple target platforms
# with optimizations for size and performance

set -euo pipefail

# Configuration
PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PUBLISH_DIR="${PROJECT_ROOT}/publish"
CLI_PROJECT="${PROJECT_ROOT}/FLua.Cli/FLua.Cli.csproj"
INTERPRETER_PROJECT="${PROJECT_ROOT}/FLua.Interpreter/FLua.Interpreter.csproj"

# Color codes for output
readonly RED='\033[0;31m'
readonly GREEN='\033[0;32m'
readonly YELLOW='\033[1;33m'
readonly BLUE='\033[0;34m'
readonly PURPLE='\033[0;35m'
readonly CYAN='\033[0;36m'
readonly WHITE='\033[1;37m'
readonly NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${BLUE}ℹ${NC} $1"
}

print_success() {
    echo -e "${GREEN}✓${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}⚠${NC} $1"
}

print_error() {
    echo -e "${RED}✗${NC} $1"
}

print_section() {
    echo -e "\n${PURPLE}█${NC} ${WHITE}$1${NC}"
    echo -e "${PURPLE}${2:-=====================================}${NC}"
}

# Function to get file size in human readable format
get_file_size() {
    if [[ "$OSTYPE" == "darwin"* ]]; then
        stat -f%z "$1" 2>/dev/null | numfmt --to=iec || echo "0"
    else
        stat --printf="%s" "$1" 2>/dev/null | numfmt --to=iec || echo "0"
    fi
}

# Function to check if a command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Function to detect the current platform
detect_platform() {
    case "$OSTYPE" in
        linux*)     echo "linux" ;;
        darwin*)    echo "osx" ;;
        msys*|mingw*|cygwin*) echo "win" ;;
        *)          echo "unknown" ;;
    esac
}

# Function to get appropriate runtime identifier
get_runtime_identifier() {
    local platform="$1"
    local arch="${2:-x64}"
    
    case "$platform" in
        linux)  echo "linux-${arch}" ;;
        osx)    echo "osx-${arch}" ;;
        win)    echo "win-${arch}" ;;
        *)      echo "linux-x64" ;;  # Default
    esac
}

# Function to validate prerequisites
check_prerequisites() {
    print_section "Checking Prerequisites"
    
    if ! command_exists dotnet; then
        print_error "dotnet CLI is not installed or not in PATH"
        print_status "Please install .NET SDK from https://dotnet.microsoft.com/download"
        exit 1
    fi
    
    local dotnet_version
    dotnet_version=$(dotnet --version)
    print_success "Found .NET SDK version: $dotnet_version"
    
    if ! command_exists git; then
        print_warning "git is not installed - version information may be incomplete"
    else
        print_success "Found git"
    fi
    
    # Check if project files exist
    if [[ ! -f "$CLI_PROJECT" ]]; then
        print_error "CLI project file not found: $CLI_PROJECT"
        exit 1
    fi
    print_success "Found CLI project: $CLI_PROJECT"
    
    if [[ ! -f "$INTERPRETER_PROJECT" ]]; then
        print_error "Interpreter project file not found: $INTERPRETER_PROJECT"
        exit 1
    fi
    print_success "Found Interpreter project: $INTERPRETER_PROJECT"
}

# Function to clean previous builds
clean_build() {
    local configuration="${1:-Release}"
    print_section "Cleaning Previous Builds"
    
    if [[ -d "$PUBLISH_DIR" ]]; then
        print_status "Removing existing publish directory..."
        rm -rf "$PUBLISH_DIR"
        print_success "Cleaned publish directory"
    fi
    
    print_status "Cleaning solution..."
    dotnet clean "$PROJECT_ROOT/FLua.sln" --configuration "$configuration" --verbosity quiet
    print_success "Solution cleaned"
}

# Function to restore packages
restore_packages() {
    print_section "Restoring NuGet Packages"
    
    print_status "Restoring packages for solution..."
    dotnet restore "$PROJECT_ROOT/FLua.sln" --verbosity quiet
    print_success "Packages restored successfully"
}

# Function to build and test
build_and_test() {
    local configuration="${1:-Release}"
    print_section "Building and Testing"
    
    print_status "Building solution in $configuration configuration..."
    dotnet build "$PROJECT_ROOT/FLua.sln" \
        --configuration "$configuration" \
        --no-restore \
        --verbosity quiet
    print_success "Build completed successfully"
    
    # Run tests if test projects exist
    if find "$PROJECT_ROOT" -name "*.Tests.csproj" -o -name "*.Tests.fsproj" | grep -q .; then
        print_status "Running tests..."
        dotnet test "$PROJECT_ROOT/FLua.sln" \
            --configuration "$configuration" \
            --no-build \
            --verbosity quiet \
            --logger "console;verbosity=minimal"
        print_success "All tests passed"
    else
        print_warning "No test projects found - skipping tests"
    fi
}

# Function to publish a single project for a target runtime
publish_project() {
    local project_path="$1"
    local project_name="$2"
    local runtime="$3"
    local output_dir="$4"
    local configuration="${5:-Release}"
    
    print_status "Publishing $project_name for $runtime..."
    
    mkdir -p "$output_dir"
    
    # Enhanced publish command with optimizations
    dotnet publish "$project_path" \
        --configuration "$configuration" \
        --runtime "$runtime" \
        --self-contained true \
        --output "$output_dir" \
        --verbosity quiet \
        -p:PublishAot=true \
        -p:StripSymbols=true \
        -p:PublishTrimmed=true \
        -p:TrimMode=partial \
        -p:PublishSingleFile=false
    
    print_success "$project_name published successfully"
}

# Function to publish for all target platforms
publish_all_platforms() {
    local configuration="${1:-Release}"
    print_section "Publishing FLua CLI"
    
    # Define target runtimes
    local runtimes=(
        "linux-x64"
        "linux-arm64" 
        "osx-x64"
        "osx-arm64"
        "win-x64"
        "win-arm64"
    )
    
    for runtime in "${runtimes[@]}"; do
        publish_single_platform "$runtime" "$configuration"
    done
}

# Function to publish for a single platform
publish_single_platform() {
    local runtime="$1"
    local configuration="${2:-Release}"
    local platform_dir="$PUBLISH_DIR/$runtime"
    
    print_status "Publishing for $runtime..."
    publish_project "$CLI_PROJECT" "FLua CLI" "$runtime" "$platform_dir" "$configuration"
    
    # Check the published executable
    local exe_name="flua"
    if [[ "$runtime" == win-* ]]; then
        exe_name="flua.exe"
    fi
    
    if [[ -f "$platform_dir/$exe_name" ]]; then
        local file_size
        file_size=$(get_file_size "$platform_dir/$exe_name")
        print_success "CLI: $platform_dir/$exe_name ($file_size)"
    else
        print_error "Failed to create executable for $runtime"
    fi
}

# Function to validate platform
validate_platform() {
    local platform="$1"
    local valid_platforms=("linux-x64" "linux-arm64" "osx-x64" "osx-arm64" "win-x64" "win-arm64")
    
    for valid in "${valid_platforms[@]}"; do
        if [[ "$platform" == "$valid" ]]; then
            return 0
        fi
    done
    
    return 1
}

# Function to validate configuration
validate_configuration() {
    local config="$1"
    if [[ "$config" != "Debug" && "$config" != "Release" ]]; then
        return 1
    fi
    return 0
}

# Function to create archive packages
create_packages() {
    print_section "Creating Distribution Packages"
    
    if ! command_exists tar; then
        print_warning "tar not found - skipping package creation"
        return
    fi
    
    local packages_dir="$PUBLISH_DIR/packages"
    mkdir -p "$packages_dir"
    
    for runtime_dir in "$PUBLISH_DIR"/*; do
        if [[ -d "$runtime_dir" && "$(basename "$runtime_dir")" != "packages" ]]; then
            local runtime_name
            runtime_name=$(basename "$runtime_dir")
            local package_name="flua-$runtime_name"
            
            print_status "Creating package for $runtime_name..."
            
            # Create tar.gz package
            (cd "$PUBLISH_DIR" && tar -czf "packages/${package_name}.tar.gz" "$runtime_name")
            
            local package_size
            package_size=$(get_file_size "$packages_dir/${package_name}.tar.gz")
            print_success "Package created: ${package_name}.tar.gz ($package_size)"
        fi
    done
}

# Function to display summary
show_summary() {
    print_section "Publication Summary"
    
    echo -e "${WHITE}Published binaries:${NC}"
    
    for runtime_dir in "$PUBLISH_DIR"/*; do
        if [[ -d "$runtime_dir" && "$(basename "$runtime_dir")" != "packages" ]]; then
            local runtime_name
            runtime_name=$(basename "$runtime_dir")
            
            print_status "│   ├── $runtime_name/"
            
            local exe_name="flua"
            if [[ "$runtime_name" == win-* ]]; then
                exe_name="flua.exe"
            fi
            
            if [[ -f "$runtime_dir/$exe_name" ]]; then
                local file_size
                file_size=$(get_file_size "$runtime_dir/$exe_name")
                print_success "CLI: $runtime_dir/$exe_name ($file_size)"
            fi
        fi
    done
    
    if [[ -d "$PUBLISH_DIR/packages" ]]; then
        echo -e "\n${WHITE}Distribution packages:${NC}"
        for package in "$PUBLISH_DIR/packages"/*.tar.gz; do
            if [[ -f "$package" ]]; then
                local package_size
                package_size=$(get_file_size "$package")
                print_success "Package: $package ($package_size)"
            fi
        done
    fi
    
    print_section "Quick Test" "============"
    print_status "To test the CLI:"
    
    local current_platform
    current_platform=$(detect_platform)
    local current_runtime
    current_runtime=$(get_runtime_identifier "$current_platform")
    
    local exe_name="flua"
    if [[ "$current_runtime" == win-* ]]; then
        exe_name="flua.exe"
    fi
    
    if [[ -f "$PUBLISH_DIR/$current_runtime/$exe_name" ]]; then
        print_status "Interactive REPL: $PUBLISH_DIR/$current_runtime/$exe_name"
        print_status "Run script: $PUBLISH_DIR/$current_runtime/$exe_name script.lua"
        print_status "Show help: $PUBLISH_DIR/$current_runtime/$exe_name --help"
        
        echo -e "\n${WHITE}Testing CLI startup:${NC}"
        echo -e "print('Hello from FLua!')\n.quit" | "$PUBLISH_DIR/$current_runtime/$exe_name" 2>/dev/null || print_warning "CLI test failed - this is normal if dependencies are missing"
    else
        print_warning "No executable found for current platform ($current_runtime)"
    fi
}

# Function to show help
show_help() {
    cat << 'EOF'
FLua Multi-Platform Publisher
=============================

This script publishes FLua CLI (with integrated REPL) as AOT-compiled, 
self-contained executables for multiple platforms.

USAGE:
    ./publish.sh [command|platform] [configuration]

COMMANDS:
    (no args)      Full build and publish for all supported platforms
    help           Show this help message
    clean          Clean previous builds only  
    test           Build and test only (no publishing)
    [platform]     Build and publish for specific platform only
    [config]       Specify Debug or Release configuration (default: Release)

SUPPORTED PLATFORMS:
    • linux-x64     - Linux x64
    • linux-arm64   - Linux ARM64  
    • osx-x64       - macOS Intel
    • osx-arm64     - macOS Apple Silicon (M1/M2/M3)
    • win-x64       - Windows x64
    • win-arm64     - Windows ARM64

EXAMPLES:
    ./publish.sh                    # Full publish all platforms, Release (recommended)
    ./publish.sh Debug              # Full publish all platforms, Debug
    ./publish.sh help               # Show this help
    ./publish.sh clean              # Clean previous builds
    ./publish.sh test               # Build and test only
    ./publish.sh osx-arm64          # Build only for macOS Apple Silicon, Release
    ./publish.sh osx-arm64 Debug    # Build only for macOS Apple Silicon, Debug
    ./publish.sh linux-x64 Release  # Build only for Linux x64, Release
    ./publish.sh Debug osx-arm64    # Arguments can be in any order

OUTPUT:
    The script creates optimized executables in ./publish/ directory:
    
    ./publish/
    ├── linux-x64/flua
    ├── linux-arm64/flua  
    ├── osx-x64/flua
    ├── osx-arm64/flua
    ├── win-x64/flua.exe
    ├── win-arm64/flua.exe
    └── packages/           # Compressed distribution packages

CONFIGURATIONS:
    • Release (default) - Optimized builds for distribution
    • Debug           - Debug builds with symbols for development

REQUIREMENTS:
    • .NET SDK 10.0 or later
    • tar command (for package creation)
    • git command (optional, for version info)

For detailed documentation, see README-publish.md
EOF
}

# Main execution with platform and configuration support
main() {
    local target_platform=""
    local configuration="Release"
    
    # Parse arguments - can be platform, configuration, or both
    for arg in "$@"; do
        if validate_platform "$arg"; then
            target_platform="$arg"
        elif validate_configuration "$arg"; then
            configuration="$arg"
        elif [[ -n "$arg" ]]; then
            print_error "Invalid argument: $arg"
            echo
            print_status "Valid platforms: linux-x64, linux-arm64, osx-x64, osx-arm64, win-x64, win-arm64"
            print_status "Valid configurations: Debug, Release"
            exit 1
        fi
    done
    
    if [[ -n "$target_platform" ]]; then
        # Single platform build
        print_section "FLua Single Platform Publisher" "================================"
        print_status "Building for platform: $target_platform"
        print_status "Configuration: $configuration"
        echo
        
        # Execute build steps
        check_prerequisites
        clean_build "$configuration"
        restore_packages
        build_and_test "$configuration"
        
        print_section "Publishing FLua CLI for $target_platform ($configuration)"
        publish_single_platform "$target_platform" "$configuration"
        
        # Create single platform package
        create_single_package "$target_platform"
        show_single_summary "$target_platform"
    else
        # All platforms build (original behavior)
        print_section "FLua Multi-Platform Publisher" "==============================="
        print_status "Starting publication process..."
        print_status "Configuration: $configuration"
        echo
        
        # Execute all steps
        check_prerequisites
        clean_build "$configuration"
        restore_packages
        build_and_test "$configuration"
        publish_all_platforms "$configuration"
        create_packages
        show_summary
    fi
    
    print_section "Publication Complete!" "======================"
    print_success "Build completed successfully!"
    echo
}

# Function to create package for single platform
create_single_package() {
    local platform="$1"
    
    if ! command_exists tar; then
        print_warning "tar not found - skipping package creation"
        return
    fi
    
    print_section "Creating Distribution Package"
    
    local packages_dir="$PUBLISH_DIR/packages"
    mkdir -p "$packages_dir"
    
    local package_name="flua-$platform"
    
    print_status "Creating package for $platform..."
    
    # Create tar.gz package
    (cd "$PUBLISH_DIR" && tar -czf "packages/${package_name}.tar.gz" "$platform")
    
    local package_size
    package_size=$(get_file_size "$packages_dir/${package_name}.tar.gz")
    print_success "Package created: ${package_name}.tar.gz ($package_size)"
}

# Function to show summary for single platform
show_single_summary() {
    local platform="$1"
    
    print_section "Publication Summary"
    
    echo -e "${WHITE}Published binary:${NC}"
    
    local exe_name="flua"
    if [[ "$platform" == win-* ]]; then
        exe_name="flua.exe"
    fi
    
    if [[ -f "$PUBLISH_DIR/$platform/$exe_name" ]]; then
        local file_size
        file_size=$(get_file_size "$PUBLISH_DIR/$platform/$exe_name")
        print_success "CLI: $PUBLISH_DIR/$platform/$exe_name ($file_size)"
    fi
    
    if [[ -f "$PUBLISH_DIR/packages/flua-$platform.tar.gz" ]]; then
        local package_size
        package_size=$(get_file_size "$PUBLISH_DIR/packages/flua-$platform.tar.gz")
        print_success "Package: $PUBLISH_DIR/packages/flua-$platform.tar.gz ($package_size)"
    fi
    
    print_section "Quick Test" "============"
    print_status "To test the CLI:"
    print_status "Binary: $PUBLISH_DIR/$platform/$exe_name"
    print_status "Help: $PUBLISH_DIR/$platform/$exe_name --help"
    
    echo -e "\n${WHITE}Testing CLI startup:${NC}"
    echo -e "print('Hello from FLua!')\n.quit" | "$PUBLISH_DIR/$platform/$exe_name" 2>/dev/null || print_warning "CLI test failed - this is normal if dependencies are missing"
}

# Handle script arguments
case "${1:-}" in
    help|--help|-h)
        show_help
        ;;
    clean)
        # Clean can optionally take configuration
        clean_build "${2:-Release}"
        ;;
    test)
        # Test can optionally take configuration
        check_prerequisites
        restore_packages
        build_and_test "${2:-Release}"
        ;;
    *)
        main "$@"
        ;;
esac 