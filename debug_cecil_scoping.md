# Cecil Scoping Debug Notes

## The Problem
When we have:
```lua
local i = 0
while i < 3 do
    i = i + 1
end
```

The assignment `i = i + 1` inside the while loop is not updating the outer `i`.

## Current Implementation
1. `GenerateLocalAssignment` for `local i = 0`:
   - Creates a new VariableDefinition for IL local
   - Stores in `_currentScope.Locals["i"] = localDef`

2. `GenerateAssignment` for `i = i + 1`:
   - Calls `FindLocal("i")` to find the variable
   - Should find it in the current scope (no new scope for while loops)
   - Should emit `stloc` to the same IL local

## Investigation Needed
1. Is `FindLocal` actually finding the variable?
2. Is the `stloc` instruction using the correct local index?
3. Is there a scope push/pop we're not aware of?

## IL Analysis
From the generated IL:
- Local slot 1 (V_1) is used for `i`
- The assignment correctly uses `stloc 1`
- The condition correctly uses `ldloc 1`
- Yet the value doesn't seem to persist

## Hypothesis
The issue might be that we're creating multiple locals with the same name, or the FindLocal is returning null and falling back to environment variables.