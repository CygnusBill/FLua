# Lua 5.4 Language Definition (Reference)

_Source: [Lua 5.4 Reference Manual](https://www.lua.org/manual/5.4/manual.html)_

---

## Table of Contents
- [Lexical Conventions](#lexical-conventions)
- [Operator Precedence and Associativity](#operator-precedence-and-associativity)
- [Grammar (EBNF)](#grammar-ebnf)
- [Semantics and Language Features](#semantics-and-language-features)
- [Standard Library](#standard-library)
- [References](#references)

---

## Lexical Conventions

### Identifiers
- Names (identifiers) can be any string of letters, digits, and underscores, not beginning with a digit and not being a reserved word.
- Lua is case-sensitive.

### Reserved Words
```
and       break     do        else      elseif    end
false     for       function  goto      if        in
local     nil       not       or        repeat    return
then      true      until     while
```

### Literals
- **Nil**: `nil`
- **Boolean**: `true`, `false`
- **Numbers**: Decimal and hexadecimal, with optional fractional part and exponent. Examples:
  - `3`, `3.14`, `0xff`, `0x1.921fb54442d18p+1`
- **Strings**: Delimited by single or double quotes, or long brackets (`[[ ... ]]`). Supports escape sequences.
- **Vararg**: `...`

### Comments
- Short comment: `--` to end of line
- Long comment: `--[[ ... ]]` (can nest equal signs: `--[=[ ... ]=]`)

### Whitespace
- Spaces, tabs, newlines, and form feeds are ignored except as separators.

---

## Operator Precedence and Associativity

From lowest to highest precedence:

| Operators                | Associativity    |
|--------------------------|-----------------|
| or                       | left            |
| and                      | left            |
| < > <= >= ~= ==          | left            |
| |                        | left            |
| ~                        | left            |
| &                        | left            |
| << >>                    | left            |
| ..                       | right           |
| + -                      | left            |
| * / // %                 | left            |
| not # - ~ (unary)        | right           |
| ^                        | right           |

- Parentheses can be used to override precedence.
- The concatenation operator (`..`) and exponentiation (`^`) are right-associative; all others are left-associative.

---

## Grammar (EBNF)

```ebnf
chunk ::= block
block ::= {stat [`;`]} [laststat [`;`]]

stat ::=  varlist `=` explist  |
         functioncall  |
         do block end  |
         while exp do block end  |
         repeat block until exp  |
         if exp then block {elseif exp then block} [else block] end  |
         for Name `=` exp `,` exp [`,` exp] do block end  |
         for namelist in explist do block end  |
         function funcname funcbody  |
         local function Name funcbody  |
         local namelist [`=` explist]

laststat ::= return [explist]  |  break

funcname ::= Name {`.` Name} [`:` Name]
varlist ::= var {`,` var}
var ::=  Name  |  prefixexp `[` exp `]`  |  prefixexp `.` Name
namelist ::= Name {`,` Name}
explist ::= {exp `,`} exp

exp ::=  nil  |  false  |  true  |  Numeral  |  LiteralString  |  ...  |
         functiondef  |  prefixexp  |  tableconstructor  |  exp binop exp  |  unop exp

prefixexp ::= var  |  functioncall  |  `(` exp `)`
functioncall ::=  prefixexp args  |  prefixexp `:` Name args
args ::=  `(` [explist] `)`  |  tableconstructor  |  LiteralString
functiondef ::= function funcbody
funcbody ::= `(` [parlist] `)` block end
parlist ::= namelist [`,` `...`]  |  `...`
tableconstructor ::= `{` [fieldlist] `}`
fieldlist ::= field {fieldsep field} [fieldsep]
field ::= `[` exp `]` `=` exp  |  Name `=` exp  |  exp
fieldsep ::= `,`  |  `;`
binop ::= `+`  |  `-`  |  `*`  |  `/`  |  `^`  |  `%`  |  `..`  |
         `<`  |  `<=`  |  `>`  |  `>=`  |  `==`  |  `~=`  |
         and  |  or
unop ::= `-`  |  not  |  `#`
```

---

## Semantics and Language Features

### Blocks and Scope
- A block is a list of statements; local variables are scoped to the block.
- Functions are first-class values and can be nested.
- Local variables are lexically scoped.

### Variables
- Three kinds: global, local, and table fields.
- Global variables are entries in the global environment table (`_ENV`).
- Table fields use `[]` or dot notation.

### Functions
- Defined with `function` keyword.
- Can be anonymous or named.
- Support for variadic arguments (`...`).
- Methods use the colon syntax (`function t:f(...) ... end`), which adds an implicit `self` parameter.

### Control Structures
- `if`, `while`, `repeat ... until`, `for` (numeric and generic), `goto`, `break`, `return`.
- `do ... end` creates a new block scope.

### Expressions
- Literals, variables, function calls, table constructors, unary and binary operations.
- Table constructors: `{ ... }` with fields as `[key]=value`, `name=value`, or just `value`.
- Function calls: `f(a, b)`, `obj:method(a, b)`, `f{...}`, `f"string"`.

### Metatables and Metamethods
- Tables and userdata can have metatables to override operators and behaviors (e.g., `__add`, `__index`, `__call`, etc.).

### Coroutines
- Lua supports coroutines (cooperative multitasking) via the `coroutine` library.

### Error Handling
- Use `error`, `pcall`, and `xpcall` for error handling and protected calls.

---

## Standard Library
- The standard library provides modules for math, string, table, coroutine, io, os, utf8, and more.
- Not part of the core grammar, but essential for most Lua programs.
- See: [Standard Libraries](https://www.lua.org/manual/5.4/manual.html#6)

---

## References
- [Lua 5.4 Reference Manual](https://www.lua.org/manual/5.4/manual.html)
- [Section 3: The Language](https://www.lua.org/manual/5.4/manual.html#3)
- [Section 6: Standard Libraries](https://www.lua.org/manual/5.4/manual.html#6)

---

This document summarizes the essential details needed to implement a Lua 5.4 parser, interpreter, or related tooling. For full details, always refer to the official manual. 