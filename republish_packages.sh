#!/bin/bash

# FLua Package Republishing Script
# This script republishes all FLua packages on NuGet.org
# Packages are listed by default - use NuGet.org web interface to manage visibility
# Replace YOUR_API_KEY with your actual NuGet API key

API_KEY="$YOUR_API_KEY"

if [ -z "$API_KEY" ] || [ "$API_KEY" = "YOUR_API_KEY" ]; then
    echo "ERROR: Please set your NuGet API key!"
    echo "Replace YOUR_API_KEY in this script with your actual NuGet API key"
    echo "You can get your API key from: https://www.nuget.org/account/apikeys"
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
    "FLua.Ast.1.0.0-alpha.2.nupkg"
    "FLua.Common.1.0.0-alpha.2.nupkg"
    "FLua.Parser.1.0.0-alpha.2.nupkg"
    "FLua.Runtime.1.0.0-alpha.2.nupkg"
    "FLua.Compiler.1.0.0-alpha.2.nupkg"
    "FLua.Interpreter.1.0.0-alpha.2.nupkg"
    "FLua.Hosting.1.0.0-alpha.2.nupkg"
    "flua.1.0.0-alpha.2.nupkg"
)

# Publish each package
for package in "${PACKAGES[@]}"; do
    echo "📦 Publishing $package..."
    if dotnet nuget push "packages/$package" \
        --source https://api.nuget.org/v3/index.json \
        --api-key "$API_KEY"; then
        echo "✅ Successfully published $package"
    else
        echo "❌ Failed to publish $package"
        exit 1
    fi
    echo ""
done

echo "🎉 All packages republished successfully!"
echo ""
echo "📋 Next steps:"
echo "1. Wait 5-15 minutes for packages to be indexed"
echo "2. Check package visibility at: https://www.nuget.org/account/packages"
echo "3. If packages show as 'Unlisted', click 'Edit' and check 'Listed' for each"
echo "4. Test installation: dotnet add package FLua.Ast --version 1.0.0-alpha.0"
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
