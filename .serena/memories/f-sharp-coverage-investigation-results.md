# F# Coverage Investigation Results

## Issue Summary
The user observed that Lexer and Parser showed 0% coverage in the initial coverage report, suspecting this was incorrect since torture tests should have covered most parser functionality.

## Root Cause Analysis

### 1. Missing F# Coverage Collection
- **Problem**: F# test project `FLua.Parser.Tests` was missing the `coverlet.collector` package
- **Impact**: F# code coverage was not being collected at all
- **Solution**: Added `<PackageReference Include="coverlet.collector" />` to `FLua.Parser.Tests.fsproj`

### 2. Lexer Module Unused (Architecture Finding)
- **Discovery**: The `FLua.Parser/Lexer.fs` module is completely unused by the parser
- **Evidence**: Comment in `Parser.fs` line 29: "We'll define our own parsers instead of using the Lexer"
- **Architecture**: FLua uses a **scannerless parser** that operates directly on character streams using FParsec
- **Conclusion**: Lexer showing 0% coverage is **accurate** - it's genuinely unused code

### 3. Parser Coverage Reality
- **Before Fix**: Parser showed 0% (missing F# coverage collection)
- **After Fix**: Parser shows ~27% coverage from F# tests
- **Validation**: User's intuition was correct - Parser should have coverage from torture tests

## Technical Details

### Coverage Collection Setup
```xml
<!-- Added to FLua.Parser.Tests.fsproj -->
<PackageReference Include="coverlet.collector" />
```

### Coverage Metrics (After Fix)
- **F# Parser Project**: 27.5% line coverage, 23.4% branch coverage  
- **Complete Solution**: 56.6% line coverage, 51.8% branch coverage (up from previous 55.4%)
- **Test Projects Included**: Now includes all 8 test projects including F# tests

### F# Testing Framework
- **Framework**: Expecto (functional-first F# testing framework)
- **Test Count**: 266 F# parser tests
- **Test Structure**: Uses `testList` and `testCase` with functional composition

## Resolution Status
✅ **Resolved**: F# coverage collection now working properly
✅ **Identified**: Lexer is unused code (architectural decision)
✅ **Improved**: Complete coverage report now includes F# projects
✅ **Validated**: User's observation about incomplete coverage was correct

## Recommendations
1. **Remove Lexer Module**: Consider removing unused `Lexer.fs` since it's not part of the scannerless parser architecture
2. **Document Architecture**: The scannerless parser design could be better documented to explain why a separate lexer isn't used
3. **Coverage Baseline**: The new 56.6% coverage is the accurate baseline for the project

## Key Files Modified
- `FLua.Parser.Tests/FLua.Parser.Tests.fsproj` - Added coverlet.collector package
- Generated comprehensive coverage reports in `TestResults/CompleteWithFSharpReport/`