# Getting Started with FLua

Welcome to FLua! This guide will help you get started with FLua, a complete Lua 5.4 implementation for .NET.

## 🎯 Quick Start Options

### Option 1: Install CLI Tool (Recommended)

The easiest way to get started is with the FLua CLI tool:

```bash
# Install globally
dotnet tool install --global flua --version 1.0.0-alpha.0

# Verify installation
flua --version

# Start interactive REPL
flua repl
```

### Option 2: Use NuGet Packages

Add FLua to your .NET project:

```xml
<PackageReference Include="FLua.Hosting" Version="1.0.0-alpha.0" />
```

```csharp
using FLua.Hosting;

var environment = LuaEnvironment.CreateStandardEnvironment();
var result = environment.ExecuteScript("return 1 + 2");
Console.WriteLine(result[0].AsInteger()); // Output: 3
```

### Option 3: Build from Source

```bash
# Clone the repository
git clone https://github.com/your-repo/flua.git
cd flua

# Build everything
dotnet build --configuration Release

# Run tests
dotnet test

# Start REPL
dotnet run --project FLua.Cli
```

## 🏃‍♂️ Your First Lua Script

Create a file called `hello.lua`:

```lua
-- hello.lua
print("Hello, FLua!")

-- Variables and math
local name = "World"
local answer = 42
print("Hello, " .. name .. "! The answer is " .. answer)

-- Functions
function factorial(n)
    if n <= 1 then
        return 1
    else
        return n * factorial(n - 1)
    end
end

print("5! =", factorial(5))

-- Tables (Lua's primary data structure)
local person = {
    name = "Alice",
    age = 30,
    hobbies = {"reading", "coding", "gaming"}
}

print(person.name .. " is " .. person.age .. " years old")
print("Her hobbies: " .. table.concat(person.hobbies, ", "))
```

Run it:

```bash
flua run hello.lua
```

## 🎮 Interactive REPL

The REPL (Read-Eval-Print Loop) is perfect for learning and experimenting:

```bash
flua repl
```

```lua
-- Try these commands:
lua> 1 + 2 * 3
= 7

lua> local x = "Hello"
lua> print(x, "World!")
Hello   World!

lua> -- Create a table
lua> local t = {name = "FLua", version = 1.0}
lua> print(t.name, t.version)
FLua   1

lua> -- Functions
lua> function greet(name) return "Hello, " .. name .. "!" end
lua> greet("Developer")
= Hello, Developer!

lua> .help    -- Show help
lua> .quit    -- Exit
```

## 📚 Lua Language Features

FLua supports all major Lua 5.4 features:

### Variables and Scoping
```lua
-- Global variables
global_var = "I'm global"

-- Local variables
local local_var = "I'm local"

-- Constants (Lua 5.4)
local <const> PI = 3.14159
local <close> resource = open_file()
```

### Control Structures
```lua
-- If statements
if x > 10 then
    print("x is greater than 10")
elseif x > 5 then
    print("x is greater than 5")
else
    print("x is small")
end

-- Loops
for i = 1, 10 do
    print("Count:", i)
end

while x < 100 do
    x = x * 2
end

-- Generic for
for key, value in pairs(my_table) do
    print(key, value)
end
```

### Functions and Closures
```lua
-- Simple function
function add(a, b)
    return a + b
end

-- Function with variable arguments
function sum(...)
    local total = 0
    for _, v in ipairs({...}) do
        total = total + v
    end
    return total
end

-- Closure
function counter()
    local count = 0
    return function()
        count = count + 1
        return count
    end
end

local c = counter()
print(c()) -- 1
print(c()) -- 2
```

### Tables (Lua's Swiss Army Knife)
```lua
-- Array-style table
local numbers = {1, 2, 3, 4, 5}

-- Dictionary-style table
local person = {
    name = "John",
    age = 30,
    ["full name"] = "John Doe"  -- Keys with spaces need brackets
}

-- Mixed table
local complex = {
    "first",        -- 1
    "second",       -- 2
    name = "Complex", -- "name"
    [42] = "answer"   -- 42
}

-- Metatables for OOP
local Vector = {}
Vector.__index = Vector

function Vector.new(x, y)
    return setmetatable({x = x, y = y}, Vector)
end

function Vector:__add(other)
    return Vector.new(self.x + other.x, self.y + other.y)
end

local v1 = Vector.new(1, 2)
local v2 = Vector.new(3, 4)
local v3 = v1 + v2  -- Calls __add
print(v3.x, v3.y)   -- Output: 4, 6
```

### Coroutines
```lua
function producer()
    for i = 1, 10 do
        print("Producing:", i)
        coroutine.yield(i * 2)
    end
end

function consumer()
    local co = coroutine.create(producer)
    while coroutine.status(co) ~= "dead" do
        local success, value = coroutine.resume(co)
        if success then
            print("Consumed:", value)
        end
    end
end

consumer()
```

## 🔧 Standard Library

FLua includes a complete implementation of Lua's standard library:

### Table Library
```lua
local t = {3, 1, 4, 1, 5}

table.insert(t, 9)      -- Add to end
table.insert(t, 1, 2)   -- Insert at position 1
table.remove(t, 3)      -- Remove from position 3
table.sort(t)           -- Sort the table

print(table.concat(t, ", "))  -- Join with separator
```

### String Library
```lua
local s = "Hello, World!"

print(string.len(s))           -- Length
print(string.sub(s, 8, 12))    -- Substring: "World"
print(string.find(s, "World"))  -- Find position: 8
print(string.upper(s))         -- Uppercase
print(string.lower(s))         -- Lowercase

-- Format strings
print(string.format("Pi is approximately %.2f", math.pi))
```

### Math Library
```lua
print(math.max(1, 5, 3, 9, 2))  -- Maximum: 9
print(math.min(1, 5, 3, 9, 2))  -- Minimum: 1
print(math.floor(3.7))           -- Floor: 3
print(math.ceil(3.2))            -- Ceiling: 4
print(math.sqrt(16))             -- Square root: 4
print(math.sin(math.pi / 2))     -- Trigonometry: 1
```

### I/O Operations
```lua
-- Write to file
local file = io.open("output.txt", "w")
file:write("Hello, File!")
file:close()

-- Read from file
local file = io.open("output.txt", "r")
local content = file:read("*a")
file:close()
print(content)
```

## 🏗️ Advanced Usage

### Embedding in .NET Applications

```csharp
using FLua.Hosting;
using FLua.Runtime;

// Create environment
var environment = LuaEnvironment.CreateStandardEnvironment();

// Execute scripts
var result = environment.ExecuteScript(@"
    function calculate_fib(n)
        if n <= 1 then return n end
        return calculate_fib(n-1) + calculate_fib(n-2)
    end
    return calculate_fib(10)
");

Console.WriteLine($"Fibonacci(10) = {result[0].AsInteger()}");

// Call Lua functions from C#
var fibFunction = environment.GetVariable("calculate_fib").AsFunction();
var fibResult = environment.CallFunction(fibFunction, LuaValue.Integer(15));
Console.WriteLine($"Fibonacci(15) = {fibResult[0].AsInteger()}");
```

### Custom Modules and Libraries

```lua
-- mymodule.lua
local M = {}

function M.greet(name)
    return "Hello, " .. name .. " from my module!"
end

function M.add(a, b)
    return a + b
end

return M
```

```lua
-- main.lua
local mymodule = require("mymodule")

print(mymodule.greet("World"))
print("1 + 2 =", mymodule.add(1, 2))
```

## 🚀 Performance Tips

1. **Use local variables** - They're faster than global lookups
2. **Preallocate tables** when possible
3. **Use table.concat()** instead of string concatenation in loops
4. **Consider compilation** for performance-critical code
5. **Profile with the debug library** for bottlenecks

## 🐛 Troubleshooting

### Common Issues

**"Attempt to index non-table"**
- Check variable assignments and table creation
- Ensure functions return tables when expected

**"Parse error"**
- Verify syntax against Lua 5.4 specification
- Check for missing `end` keywords or parentheses

**"Coroutine yielded value"**
- Handle coroutine.resume() return values properly
- Check coroutine status before resuming

### Getting Help

- **Documentation**: Check the full README.md and API docs
- **Issues**: Report bugs on GitHub with minimal reproduction cases
- **Discussions**: Join community discussions for help and feedback

## 🎉 Next Steps

Now that you're familiar with FLua basics:

1. **Explore the examples** in the repository
2. **Run the full test suite** to see FLua in action
3. **Try embedding FLua** in your .NET applications
4. **Contribute** by reporting issues or submitting pull requests

Happy coding with FLua! 🚀
