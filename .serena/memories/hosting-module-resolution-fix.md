# Hosting Module Resolution Fix

## Problem
The hosting module resolution system was failing because the standard Lua package library was overriding custom require functions, causing module resolution to fail with "module not found" errors.

## Root Cause Analysis
1. `FilteredEnvironmentProvider.CreateEnvironment()` calls `LuaEnvironment.CreateStandardEnvironment()`
2. `CreateStandardEnvironment()` calls `LuaPackageLib.AddPackageLibrary()` 
3. `AddPackageLibrary()` sets up a standard `require` function that uses the built-in Lua searchers
4. Later, `ConfigureModuleSystem()` tries to override this with a custom require function
5. However, the standard require function was still being called somehow

## Solution Implemented
1. **Added conditional package library loading**: Modified `CreateStandardEnvironment()` to accept an optional `includePackageLibrary` parameter (defaults to true)
2. **Skip standard library when custom resolver provided**: In `FilteredEnvironmentProvider.CreateEnvironment()`, set `includePackageLibrary = false` when `options?.ModuleResolver != null`
3. **Handle missing package table**: Updated `ConfigureModuleSystem()` to create a package table if one doesn't exist when the standard library is skipped

## Results
- **Before**: 1/24 module resolution tests passing  
- **After**: 23/24 module resolution tests passing
- **Remaining issue**: 1 integration test (`Host_ModuleResolution_LoadsCustomModules`) still fails, but this appears to be a specific case that may need additional investigation

## Code Changes
### LuaEnvironment.cs
- Added optional `includePackageLibrary` parameter to `CreateStandardEnvironment()`
- Conditionally call `LuaPackageLib.AddPackageLibrary()` based on parameter

### FilteredEnvironmentProvider.cs  
- Calculate `includeStandardPackageLibrary = options?.ModuleResolver == null`
- Pass parameter to `CreateStandardEnvironment()`
- Handle null package table in `ConfigureModuleSystem()`

## Architecture Impact
The fix maintains backward compatibility by defaulting `includePackageLibrary = true` in `CreateStandardEnvironment()`. Only when a custom module resolver is provided does it skip the standard package library, allowing the custom require function to take precedence.

This resolves the core architectural conflict between the standard Lua package system and the FLua hosting model's custom module resolution.