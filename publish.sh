#!/bin/bash

# FLua Publishing Script
# Publishes both REPL and CLI as AOT-compiled executables

set -e  # Exit on any error

# Default configuration
DEFAULT_RUNTIME="osx-arm64"
DEFAULT_CONFIGURATION="Release"
DEFAULT_PUBLISH_DIR="./publish"

# Supported runtimes
SUPPORTED_RUNTIMES=("osx-arm64" "osx-x64" "linux-x64" "linux-arm64" "win-x64" "win-arm64")

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${BLUE}ℹ️  $1${NC}"
}

print_success() {
    echo -e "${GREEN}✅ $1${NC}"
}

print_warning() {
    echo -e "${YELLOW}⚠️  $1${NC}"
}

print_error() {
    echo -e "${RED}❌ $1${NC}"
}

echo "🚀 FLua Publishing Script"
echo "========================="
echo

# Show usage if help requested
if [[ "$1" == "-h" || "$1" == "--help" ]]; then
    echo "Usage: $0 [runtime|--all] [configuration] [output_dir]"
    echo
    echo "Arguments:"
    echo "  runtime       Target runtime (default: ${DEFAULT_RUNTIME})"
    echo "  --all         Build for all supported runtimes"
    echo "  configuration Build configuration (default: ${DEFAULT_CONFIGURATION})"
    echo "  output_dir    Output directory (default: ${DEFAULT_PUBLISH_DIR})"
    echo
    echo "Supported runtimes:"
    for runtime in "${SUPPORTED_RUNTIMES[@]}"; do
        echo "  - $runtime"
    done
    echo
    echo "Examples:"
    echo "  $0                          # Use defaults (${DEFAULT_RUNTIME}, ${DEFAULT_CONFIGURATION})"
    echo "  $0 --all                    # Build for all runtimes"
    echo "  $0 --all Debug              # Build all runtimes in Debug mode"
    echo "  $0 linux-x64               # Linux x64"
    echo "  $0 win-x64 Debug           # Windows x64, Debug build"
    echo "  $0 osx-x64 Release ./dist  # macOS Intel, custom output dir"
    exit 0
fi

# Parse command line arguments
BUILD_ALL_RUNTIMES=false
if [[ "$1" == "--all" ]]; then
    BUILD_ALL_RUNTIMES=true
    TARGET_RUNTIME="all"
    CONFIGURATION="${2:-$DEFAULT_CONFIGURATION}"
    PUBLISH_DIR="${3:-$DEFAULT_PUBLISH_DIR}"
else
    TARGET_RUNTIME="${1:-$DEFAULT_RUNTIME}"
    CONFIGURATION="${2:-$DEFAULT_CONFIGURATION}"
    PUBLISH_DIR="${3:-$DEFAULT_PUBLISH_DIR}"
    
    # Validate runtime
    if [[ ! " ${SUPPORTED_RUNTIMES[@]} " =~ " ${TARGET_RUNTIME} " ]]; then
        print_error "Unsupported runtime: $TARGET_RUNTIME"
        echo "Supported runtimes: ${SUPPORTED_RUNTIMES[*]}"
        exit 1
    fi
fi

print_status "Configuration:"
if [[ "$BUILD_ALL_RUNTIMES" == "true" ]]; then
    print_status "  Runtime:      All supported runtimes (${#SUPPORTED_RUNTIMES[@]} platforms)"
else
    print_status "  Runtime:      $TARGET_RUNTIME"
fi
print_status "  Configuration: $CONFIGURATION"
print_status "  Output:       $PUBLISH_DIR"
echo

# Clean previous publish directory
print_status "Cleaning previous publish directory..."
if [ -d "$PUBLISH_DIR" ]; then
    rm -rf "$PUBLISH_DIR"
    print_success "Cleaned $PUBLISH_DIR"
fi

# Create publish directory
mkdir -p "$PUBLISH_DIR"

# Function to publish a project for a specific runtime
publish_project_runtime() {
    local project_path=$1
    local project_name=$2
    local project_short=$3
    local runtime=$4
    local output_dir="$PUBLISH_DIR/$runtime/$project_short"
    
    print_status "Publishing $project_name for $runtime..."
    
    dotnet publish "$project_path" \
        -c "$CONFIGURATION" \
        -r "$runtime" \
        --self-contained \
        -o "$output_dir" \
        --force \
        -p:PublishAot=true
    
    if [ $? -eq 0 ]; then
        print_success "$project_name ($runtime) published successfully"
        
        # Get executable info
        local executable_name="FLua.$project_short"
        if [[ "$runtime" == win-* ]]; then
            executable_name="${executable_name}.exe"
        fi
        local executable="$output_dir/$executable_name"
        
        if [ -f "$executable" ]; then
            local size=$(du -h "$executable" | cut -f1)
            print_status "$project_name ($runtime) executable: $executable ($size)"
        fi
    else
        print_error "Failed to publish $project_name for $runtime"
        exit 1
    fi
    
    echo
}

# Function to publish a project (single runtime or all runtimes)
publish_project() {
    local project_path=$1
    local project_name=$2
    local project_short=$3
    
    if [[ "$BUILD_ALL_RUNTIMES" == "true" ]]; then
        for runtime in "${SUPPORTED_RUNTIMES[@]}"; do
            publish_project_runtime "$project_path" "$project_name" "$project_short" "$runtime"
        done
    else
        local output_dir="$PUBLISH_DIR/$project_short"
        publish_project_runtime "$project_path" "$project_name" "$project_short" "$TARGET_RUNTIME"
        # Move from runtime subdirectory to project subdirectory for single runtime builds
        if [ -d "$PUBLISH_DIR/$TARGET_RUNTIME/$project_short" ]; then
            mv "$PUBLISH_DIR/$TARGET_RUNTIME/$project_short" "$output_dir"
            rmdir "$PUBLISH_DIR/$TARGET_RUNTIME" 2>/dev/null || true
        fi
    fi
}

# Publish REPL
publish_project "FLua.Repl/FLua.Repl.csproj" "FLua REPL" "Repl"

# Publish CLI
publish_project "FLua.Cli/FLua.Cli.csproj" "FLua CLI" "Cli"

# Summary
echo "📦 Publishing Summary"
echo "===================="
echo

if [[ "$BUILD_ALL_RUNTIMES" == "true" ]]; then
    # Multi-runtime build summary
    total_executables=0
    for runtime in "${SUPPORTED_RUNTIMES[@]}"; do
        print_status "Runtime: $runtime"
        
        # Check REPL
        repl_exe="FLua.Repl"
        if [[ "$runtime" == win-* ]]; then
            repl_exe="${repl_exe}.exe"
        fi
        if [ -f "$PUBLISH_DIR/$runtime/Repl/$repl_exe" ]; then
            repl_size=$(du -h "$PUBLISH_DIR/$runtime/Repl/$repl_exe" | cut -f1)
            print_success "  REPL: $PUBLISH_DIR/$runtime/Repl/$repl_exe ($repl_size)"
            ((total_executables++))
        else
            print_error "  REPL executable not found"
        fi
        
        # Check CLI
        cli_exe="FLua.Cli"
        if [[ "$runtime" == win-* ]]; then
            cli_exe="${cli_exe}.exe"
        fi
        if [ -f "$PUBLISH_DIR/$runtime/Cli/$cli_exe" ]; then
            cli_size=$(du -h "$PUBLISH_DIR/$runtime/Cli/$cli_exe" | cut -f1)
            print_success "  CLI:  $PUBLISH_DIR/$runtime/Cli/$cli_exe ($cli_size)"
            ((total_executables++))
        else
            print_error "  CLI executable not found"
        fi
        echo
    done
    
    print_success "Publishing completed successfully!"
    print_status "Built $total_executables executables across ${#SUPPORTED_RUNTIMES[@]} runtimes"
    echo
    print_status "Directory structure:"
    print_status "$PUBLISH_DIR/"
    for runtime in "${SUPPORTED_RUNTIMES[@]}"; do
        print_status "├── $runtime/"
        print_status "│   ├── Repl/FLua.Repl$([ "$runtime" == win-* ] && echo ".exe")"
        print_status "│   └── Cli/FLua.Cli$([ "$runtime" == win-* ] && echo ".exe")"
    done
else
    # Single runtime build summary
    if [ -f "$PUBLISH_DIR/Repl/FLua.Repl" ]; then
        repl_size=$(du -h "$PUBLISH_DIR/Repl/FLua.Repl" | cut -f1)
        print_success "REPL: $PUBLISH_DIR/Repl/FLua.Repl ($repl_size)"
    else
        print_error "REPL executable not found"
    fi

    if [ -f "$PUBLISH_DIR/Cli/FLua.Cli" ]; then
        cli_size=$(du -h "$PUBLISH_DIR/Cli/FLua.Cli" | cut -f1)
        print_success "CLI:  $PUBLISH_DIR/Cli/FLua.Cli ($cli_size)"
    else
        print_error "CLI executable not found"
    fi

    echo
    print_success "Publishing completed successfully!"
    echo
    print_status "To test the REPL: $PUBLISH_DIR/Repl/FLua.Repl"
    print_status "To test the CLI:  $PUBLISH_DIR/Cli/FLua.Cli <file.lua>"
    echo

    # Optional: Test the executables (only for single runtime builds)
    read -p "Would you like to test the REPL? (y/n): " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        print_status "Testing REPL with sample input..."
        echo -e "print('Hello from published REPL!')\n1 + 2 * 3\n.quit" | "$PUBLISH_DIR/Repl/FLua.Repl"
    fi
fi 