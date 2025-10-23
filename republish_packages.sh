#!/bin/bash

# FLua Package Republishing Script
# This script republishes all FLua packages on NuGet.org
# Packages are listed by default - use NuGet.org web interface to manage visibility
# Replace YOUR_API_KEY with your actual NuGet API key

# Use environment variable or command line argument
API_KEY="${NUGET_API_KEY:-$1}"

if [ -z "$API_KEY" ]; then
    echo "ERROR: Please provide your NuGet API key!"
    echo ""
    echo "Option 1: Set environment variable"
    echo "  export NUGET_API_KEY=your_key_here"
    echo "  ./republish_packages.sh"
    echo ""
    echo "Option 2: Pass as argument"
    echo "  ./republish_packages.sh your_key_here"
    echo ""
    echo "Get your API key from: https://www.nuget.org/account/apikeys"
    exit 1
fi

echo "🔄 Republishing FLua packages..."
echo "API Key: ${API_KEY:0:8}..."
echo ""
echo "NOTE: Packages are listed by default. If they're showing as unlisted:"
echo "1. Go to https://www.nuget.org/account/packages"
echo "2. Find each FLua package"
echo "3. Click 'Edit' and check 'Listed'"
echo ""

# Array of packages to publish
PACKAGES=(
    "FLua.Ast.1.0.0-alpha.4.nupkg"
    "FLua.Common.1.0.0-alpha.4.nupkg"
    "FLua.Parser.1.0.0-alpha.4.nupkg"
    "FLua.Runtime.1.0.0-alpha.4.nupkg"
    "FLua.Compiler.1.0.0-alpha.4.nupkg"
    "FLua.Interpreter.1.0.0-alpha.4.nupkg"
    "FLua.Hosting.1.0.0-alpha.4.nupkg"
    "flua.1.0.0-alpha.4.nupkg"
)

# Track results
SUCCESSFUL_PACKAGES=()
FAILED_PACKAGES=()

# Publish each package
for package in "${PACKAGES[@]}"; do
    echo "📦 Publishing $package..."
    if dotnet nuget push "packages/$package" \
        --source https://api.nuget.org/v3/index.json \
        --api-key "$API_KEY"; then
        echo "✅ Successfully published $package"
        SUCCESSFUL_PACKAGES+=("$package")
    else
        echo "❌ Failed to publish $package"
        FAILED_PACKAGES+=("$package")
    fi
    echo ""
done

# Show summary
echo "📊 Publishing Summary:"
echo "✅ Successful: ${#SUCCESSFUL_PACKAGES[@]} packages"
if [ ${#SUCCESSFUL_PACKAGES[@]} -gt 0 ]; then
    for pkg in "${SUCCESSFUL_PACKAGES[@]}"; do
        echo "   • $pkg"
    done
fi

echo "❌ Failed: ${#FAILED_PACKAGES[@]} packages"
if [ ${#FAILED_PACKAGES[@]} -gt 0 ]; then
    for pkg in "${FAILED_PACKAGES[@]}"; do
        echo "   • $pkg"
    done
fi
echo ""

# Show next steps for successful packages
if [ ${#SUCCESSFUL_PACKAGES[@]} -gt 0 ]; then
    echo "📋 Next steps for successful packages:"
    echo "1. Wait 5-15 minutes for packages to be indexed"
    echo "2. Check package visibility at: https://www.nuget.org/account/packages"
    echo "3. If packages show as 'Unlisted', click 'Edit' and check 'Listed' for each"
    echo "4. Test installation: dotnet add package FLua.Ast --version 1.0.0-alpha.4"
    echo ""
    echo "📦 Package list:"
    echo "- FLua.Ast (AST definitions)"
    echo "- FLua.Common (Shared utilities)"
    echo "- FLua.Parser (F# Lua parser)"
    echo "- FLua.Runtime (Runtime & stdlib)"
    echo "- FLua.Compiler (Compilation backends)"
    echo "- FLua.Interpreter (Lua interpreter)"
    echo "- FLua.Hosting (Hosting API)"
    echo "- flua (CLI tool)"
fi

# Exit with error if any packages failed
if [ ${#FAILED_PACKAGES[@]} -gt 0 ]; then
    echo "⚠️  Some packages failed to publish. You can retry failed packages individually."
    echo "   Run: dotnet nuget push packages/FAILED_PACKAGE.nupkg --source https://api.nuget.org/v3/index.json --api-key YOUR_API_KEY"
    exit 1
fi
