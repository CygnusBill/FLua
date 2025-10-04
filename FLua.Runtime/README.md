# FLua.Runtime

Lua 5.4 runtime and standard library implementation for .NET.

This package provides the core execution environment for Lua, including all built-in functions, value types, environments, and standard library implementations.

## Features

### Core Runtime
- `LuaValue` hierarchy (nil, boolean, number, string, table, function)
- `LuaEnvironment` for variable scoping and closures
- `LuaTable` with array/hash parts and metatable support
- Function execution and call stack management

### Standard Library (Complete Lua 5.4 Implementation)

#### Basic Functions
- `print()`, `type()`, `tostring()`, `tonumber()`
- `assert()`, `error()`, `pcall()`, `xpcall()`

#### Table Library
- `table.insert()`, `table.remove()`, `table.sort()`
- `table.concat()`, `table.pack()`, `table.unpack()`
- `table.move()`

#### String Library
- `string.len()`, `string.sub()`, `string.find()`, `string.gsub()`
- `string.char()`, `string.byte()`, `string.format()`
- `string.upper()`, `string.lower()`, `string.reverse()`
- Pattern matching and UTF-8 support

#### Math Library
- All trigonometric functions: `sin()`, `cos()`, `tan()`, `asin()`, `acos()`, `atan()`
- Rounding: `floor()`, `ceil()`, `fmod()`, `modf()`
- Random: `random()`, `randomseed()`
- Constants: `pi`, `huge`, `mininteger`, `maxinteger`

#### I/O Library
- File operations: `io.open()`, `io.close()`, `file:read()`, `file:write()`
- Standard streams: `io.input()`, `io.output()`, `io.stderr()`
- Utilities: `io.type()`, `io.flush()`

#### OS Library
- Time functions: `os.time()`, `os.date()`, `os.difftime()`
- Environment: `os.getenv()`, `os.setlocale()`
- File system: `os.remove()`, `os.rename()`, `os.tmpname()`

#### Coroutine Library
- `coroutine.create()`, `coroutine.resume()`, `coroutine.yield()`
- `coroutine.status()`, `coroutine.running()`

#### Debug Library
- `debug.getinfo()`, `debug.traceback()`
- Stack inspection and error reporting

#### Package Library
- Module loading and `require()` functionality
- Path resolution and package management

## Usage

```csharp
using FLua.Runtime;

// Create a standard Lua environment
var env = LuaEnvironment.CreateStandardEnvironment();

// Execute code with access to all standard libraries
var result = env.Execute("return math.sqrt(144) + string.len('hello')");

// Use individual library functions
var mathTable = new LuaMathLib();
mathTable.AddMathLibrary(env);

// Values work seamlessly with .NET
var luaNumber = new LuaNumber(42.5);
var luaString = new LuaString("hello");
var luaTable = new LuaTable();
```

## Architecture

The runtime is designed with clean separation:
- **Value System**: Strongly typed Lua values with proper conversions
- **Environment**: Lexical scoping with parent environment chaining
- **Tables**: Dual array/hash implementation with metatable support
- **Functions**: Support for both interpreted and compiled functions

## Performance

- Efficient value boxing/unboxing
- Lazy table operations
- Optimized standard library implementations
- Cross-platform compatibility (Windows, macOS, Linux)

## Dependencies

- FLua.Common (diagnostics and utilities)

## License

MIT
