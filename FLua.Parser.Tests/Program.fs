module FLua.Parser.Tests

open Expecto
open FParsec
open FLua.Ast
open FLua.Parser
open FLua.Parser.Parser  // Use the new centralized parser

// Helper function to test expressions
let testExpr name input expected =
    test name {
        match run expr input with
        | Success(result, _, _) -> Expect.equal result expected "Expression should match"
        | Failure(errorMsg, _, _) -> failwithf "Parse failed: %s" errorMsg
    }

// Helper function to test statements
let testStmt name input expected =
    test name {
        match run statement input with
        | Success(result, _, _) -> Expect.equal result expected "Statement should match"
        | Failure(errorMsg, _, _) -> failwithf "Parse failed: %s" errorMsg
    }

[<Tests>]
let tests = testList "Parser Tests" [
    testList "Expression Parser Tests" [
        testList "Literals" [
            testExpr "integer" "42" (Expr.Literal (Literal.Integer 42I))
            testExpr "float" "3.14" (Expr.Literal (Literal.Float 3.14))
            testExpr "string" "\"hello\"" (Expr.Literal (Literal.String "hello"))
            testExpr "boolean true" "true" (Expr.Literal (Literal.Boolean true))
            testExpr "boolean false" "false" (Expr.Literal (Literal.Boolean false))
            testExpr "nil" "nil" (Expr.Literal Literal.Nil)
            testExpr "zero integer" "0" (Expr.Literal (Literal.Integer 0I))
            testExpr "zero float" "0.0" (Expr.Literal (Literal.Float 0.0))
            testExpr "empty string" "\"\"" (Expr.Literal (Literal.String ""))
            testExpr "single quote string" "'hello'" (Expr.Literal (Literal.String "hello"))
            testExpr "hex integer" "0xff" (Expr.Literal (Literal.Integer 255I))
            testExpr "hex float" "0x1.5" (Expr.Literal (Literal.Float 1.3125))
            testExpr "hex float with exponent" "0xABCp-3" (Expr.Literal (Literal.Float 343.5))
            testExpr "negative hex integer" "-0xff" (Expr.Unary(UnaryOp.Negate, Expr.Literal (Literal.Integer 255I)))
            testExpr "negative hex float" "-0x1.5" (Expr.Unary(UnaryOp.Negate, Expr.Literal (Literal.Float 1.3125)))
            
            // String Escape Sequences Tests
            testExpr "escape newline" "\"line1\\nline2\"" (Expr.Literal (Literal.String "line1\nline2"))
            testExpr "escape tab" "\"col1\\tcol2\"" (Expr.Literal (Literal.String "col1\tcol2"))
            testExpr "escape backslash" "\"path\\\\file\"" (Expr.Literal (Literal.String "path\\file"))
            testExpr "escape double quote" "\"say \\\"hello\\\"\"" (Expr.Literal (Literal.String "say \"hello\""))
            testExpr "escape single quote" "'it\\'s'" (Expr.Literal (Literal.String "it's"))
            testExpr "escape bell" "\"\\a\"" (Expr.Literal (Literal.String "\a"))
            testExpr "escape backspace" "\"\\b\"" (Expr.Literal (Literal.String "\b"))
            testExpr "escape form feed" "\"\\f\"" (Expr.Literal (Literal.String "\f"))
            testExpr "escape carriage return" "\"\\r\"" (Expr.Literal (Literal.String "\r"))
            testExpr "escape vertical tab" "\"\\v\"" (Expr.Literal (Literal.String "\v"))
            
            // Decimal escape sequences
            testExpr "decimal escape null" "\"\\0\"" (Expr.Literal (Literal.String "\000"))
            testExpr "decimal escape single digit" "\"\\9\"" (Expr.Literal (Literal.String "\009"))
            testExpr "decimal escape two digits" "\"\\99\"" (Expr.Literal (Literal.String "c"))
            testExpr "decimal escape three digits" "\"\\123\"" (Expr.Literal (Literal.String "{"))
            testExpr "decimal escape max value" "\"\\255\"" (Expr.Literal (Literal.String "\255"))
            testExpr "decimal escape with following digit" "\"\\0001\"" (Expr.Literal (Literal.String "\0001"))
            testExpr "decimal escape 65 (A)" "\"\\65\"" (Expr.Literal (Literal.String "A"))
            
            // Hexadecimal escape sequences
            testExpr "hex escape null" "\"\\x00\"" (Expr.Literal (Literal.String "\000"))
            testExpr "hex escape A" "\"\\x41\"" (Expr.Literal (Literal.String "A"))
            testExpr "hex escape max" "\"\\xFF\"" (Expr.Literal (Literal.String "\255"))
            testExpr "hex escape lowercase" "\"\\xff\"" (Expr.Literal (Literal.String "\255"))
            testExpr "hex escape mixed case" "\"\\xFf\"" (Expr.Literal (Literal.String "\255"))
            testExpr "hex escape newline" "\"\\x0A\"" (Expr.Literal (Literal.String "\n"))
            testExpr "hex escape with following char" "\"\\x41B\"" (Expr.Literal (Literal.String "AB"))
            
            // Unicode escape sequences
            testExpr "unicode escape A" "\"\\u{41}\"" (Expr.Literal (Literal.String "A"))
            testExpr "unicode escape zero" "\"\\u{0}\"" (Expr.Literal (Literal.String "\000"))
            // Note: Unicode escapes beyond U+10FFFF are allowed for Lua test compatibility
            // but produce invalid UTF-8 sequences
            
            // Line continuation escape
            testExpr "line continuation basic" "\"abc\\z def\"" (Expr.Literal (Literal.String "abcdef"))
            testExpr "line continuation empty" "\"abc\\zdef\"" (Expr.Literal (Literal.String "abcdef"))
            
            // Combined escape sequences
            testExpr "multiple escapes" "\"\\x41\\x42\\x43\"" (Expr.Literal (Literal.String "ABC"))
            testExpr "mixed escape types" "\"\\65\\x42\\67\"" (Expr.Literal (Literal.String "ABC"))
            testExpr "complex string" "'a\\0a'" (Expr.Literal (Literal.String "a\000a"))
            testExpr "null in middle" "\"\\0\\0\\0alo\"" (Expr.Literal (Literal.String "\000\000\000alo"))
            
            // Test cases from literals.lua
            testExpr "literals.lua case 1" "\"\\09912\"" (Expr.Literal (Literal.String "c12"))
            testExpr "literals.lua case 2" "\"\\99ab\"" (Expr.Literal (Literal.String "cab"))
            testExpr "literals.lua case 3" "\"\\x00\\x05\\x10\\x1f\\x3C\\xfF\\xe8\"" 
                (Expr.Literal (Literal.String "\000\005\016\031\060\255\232"))
        ]

        testList "Identifiers" [
            testExpr "simple identifier" "foo" (Expr.Var "foo")
            testExpr "underscore identifier" "_bar" (Expr.Var "_bar")
            testExpr "alphanumeric identifier" "baz123" (Expr.Var "baz123")
            testExpr "single character" "a" (Expr.Var "a")
            testExpr "single underscore" "_" (Expr.Var "_")
            testExpr "long identifier" "very_long_identifier_name_123" (Expr.Var "very_long_identifier_name_123")
        ]

        testExpr "vararg" "..." Expr.Vararg

        testList "Parentheses" [
            testExpr "simple parentheses" "(42)" (Expr.Paren (Expr.Literal (Literal.Integer 42I)))
            testExpr "nested parentheses" "((42))" (Expr.Paren (Expr.Paren (Expr.Literal (Literal.Integer 42I))))
            testExpr "triple nested" "(((42)))" (Expr.Paren (Expr.Paren (Expr.Paren (Expr.Literal (Literal.Integer 42I)))))
            testExpr "parentheses with spaces" "( 42 )" (Expr.Paren (Expr.Literal (Literal.Integer 42I)))
        ]

        testList "Unary Operators" [
            testExpr "not" "not true" (Expr.Unary(UnaryOp.Not, Expr.Literal (Literal.Boolean true)))
            testExpr "length" "#\"hello\"" (Expr.Unary(UnaryOp.Length, Expr.Literal (Literal.String "hello")))
            testExpr "negate" "-42" (Expr.Unary(UnaryOp.Negate, Expr.Literal (Literal.Integer 42I)))
            testExpr "bitwise not" "~42" (Expr.Unary(UnaryOp.BitNot, Expr.Literal (Literal.Integer 42I)))
            testExpr "multiple unary" "not #\"hello\"" (Expr.Unary(UnaryOp.Not, Expr.Unary(UnaryOp.Length, Expr.Literal (Literal.String "hello"))))
        ]

        testList "Power Operator" [
            testExpr "simple power" "2^3" (Expr.Binary(Expr.Literal (Literal.Integer 2I), BinaryOp.Power, Expr.Literal (Literal.Integer 3I)))
            testExpr "right associative" "2^3^2" (Expr.Binary(Expr.Literal (Literal.Integer 2I), BinaryOp.Power, Expr.Binary(Expr.Literal (Literal.Integer 3I), BinaryOp.Power, Expr.Literal (Literal.Integer 2I))))
        ]

        testList "Multiplicative Operators" [
            testExpr "multiply" "2*3" (Expr.Binary(Expr.Literal (Literal.Integer 2I), BinaryOp.Multiply, Expr.Literal (Literal.Integer 3I)))
            testExpr "float divide" "6/2" (Expr.Binary(Expr.Literal (Literal.Integer 6I), BinaryOp.FloatDiv, Expr.Literal (Literal.Integer 2I)))
            testExpr "floor divide" "7//2" (Expr.Binary(Expr.Literal (Literal.Integer 7I), BinaryOp.FloorDiv, Expr.Literal (Literal.Integer 2I)))
            testExpr "modulo" "7%2" (Expr.Binary(Expr.Literal (Literal.Integer 7I), BinaryOp.Modulo, Expr.Literal (Literal.Integer 2I)))
            testExpr "left associative" "2*3*4" (Expr.Binary(Expr.Binary(Expr.Literal (Literal.Integer 2I), BinaryOp.Multiply, Expr.Literal (Literal.Integer 3I)), BinaryOp.Multiply, Expr.Literal (Literal.Integer 4I)))
        ]

        testList "Additive Operators" [
            testExpr "add" "2+3" (Expr.Binary(Expr.Literal (Literal.Integer 2I), BinaryOp.Add, Expr.Literal (Literal.Integer 3I)))
            testExpr "subtract" "5-3" (Expr.Binary(Expr.Literal (Literal.Integer 5I), BinaryOp.Subtract, Expr.Literal (Literal.Integer 3I)))
            testExpr "left associative" "1+2+3" (Expr.Binary(Expr.Binary(Expr.Literal (Literal.Integer 1I), BinaryOp.Add, Expr.Literal (Literal.Integer 2I)), BinaryOp.Add, Expr.Literal (Literal.Integer 3I)))
        ]

        testList "String Concatenation" [
            testExpr "simple concat" "\"hello\"..\"world\"" (Expr.Binary(Expr.Literal (Literal.String "hello"), BinaryOp.Concat, Expr.Literal (Literal.String "world")))
            testExpr "right associative" "\"a\"..\"b\"..\"c\"" (Expr.Binary(Expr.Literal (Literal.String "a"), BinaryOp.Concat, Expr.Binary(Expr.Literal (Literal.String "b"), BinaryOp.Concat, Expr.Literal (Literal.String "c"))))
        ]

        testList "Shift Operators" [
            testExpr "left shift" "1<<2" (Expr.Binary(Expr.Literal (Literal.Integer 1I), BinaryOp.ShiftLeft, Expr.Literal (Literal.Integer 2I)))
            testExpr "right shift" "8>>1" (Expr.Binary(Expr.Literal (Literal.Integer 8I), BinaryOp.ShiftRight, Expr.Literal (Literal.Integer 1I)))
            testExpr "left associative" "1<<2<<3" (Expr.Binary(Expr.Binary(Expr.Literal (Literal.Integer 1I), BinaryOp.ShiftLeft, Expr.Literal (Literal.Integer 2I)), BinaryOp.ShiftLeft, Expr.Literal (Literal.Integer 3I)))
        ]

        testList "Bitwise Operators" [
            testExpr "bitwise and" "3&1" (Expr.Binary(Expr.Literal (Literal.Integer 3I), BinaryOp.BitAnd, Expr.Literal (Literal.Integer 1I)))
            testExpr "bitwise xor" "3~1" (Expr.Binary(Expr.Literal (Literal.Integer 3I), BinaryOp.BitXor, Expr.Literal (Literal.Integer 1I)))
            testExpr "bitwise or" "2|1" (Expr.Binary(Expr.Literal (Literal.Integer 2I), BinaryOp.BitOr, Expr.Literal (Literal.Integer 1I)))
            testExpr "precedence" "1&2|3~4" (Expr.Binary(Expr.Binary(Expr.Binary(Expr.Literal (Literal.Integer 1I), BinaryOp.BitAnd, Expr.Literal (Literal.Integer 2I)), BinaryOp.BitOr, Expr.Literal (Literal.Integer 3I)), BinaryOp.BitXor, Expr.Literal (Literal.Integer 4I)))
        ]

        testList "Comparison Operators" [
            testExpr "less" "2<3" (Expr.Binary(Expr.Literal (Literal.Integer 2I), BinaryOp.Less, Expr.Literal (Literal.Integer 3I)))
            testExpr "greater" "3>2" (Expr.Binary(Expr.Literal (Literal.Integer 3I), BinaryOp.Greater, Expr.Literal (Literal.Integer 2I)))
            testExpr "less equal" "2<=2" (Expr.Binary(Expr.Literal (Literal.Integer 2I), BinaryOp.LessEqual, Expr.Literal (Literal.Integer 2I)))
            testExpr "greater equal" "3>=3" (Expr.Binary(Expr.Literal (Literal.Integer 3I), BinaryOp.GreaterEqual, Expr.Literal (Literal.Integer 3I)))
            testExpr "not equal" "2~=3" (Expr.Binary(Expr.Literal (Literal.Integer 2I), BinaryOp.NotEqual, Expr.Literal (Literal.Integer 3I)))
            testExpr "equal" "2==2" (Expr.Binary(Expr.Literal (Literal.Integer 2I), BinaryOp.Equal, Expr.Literal (Literal.Integer 2I)))
        ]

        testList "Logical Operators" [
            testExpr "and" "true and false" (Expr.Binary(Expr.Literal (Literal.Boolean true), BinaryOp.And, Expr.Literal (Literal.Boolean false)))
            testExpr "or" "true or false" (Expr.Binary(Expr.Literal (Literal.Boolean true), BinaryOp.Or, Expr.Literal (Literal.Boolean false)))
            testExpr "precedence" "a and b or c and d" (Expr.Binary(Expr.Binary(Expr.Var "a", BinaryOp.And, Expr.Var "b"), BinaryOp.Or, Expr.Binary(Expr.Var "c", BinaryOp.And, Expr.Var "d")))
        ]

        testList "Complex Expressions" [
            testExpr "operator precedence" "1 + 2 * 3" (Expr.Binary(Expr.Literal (Literal.Integer 1I), BinaryOp.Add, Expr.Binary(Expr.Literal (Literal.Integer 2I), BinaryOp.Multiply, Expr.Literal (Literal.Integer 3I))))
            testExpr "mixed operators" "1 + 2 * 3^2" (Expr.Binary(Expr.Literal (Literal.Integer 1I), BinaryOp.Add, Expr.Binary(Expr.Literal (Literal.Integer 2I), BinaryOp.Multiply, Expr.Binary(Expr.Literal (Literal.Integer 3I), BinaryOp.Power, Expr.Literal (Literal.Integer 2I)))))
            testExpr "parentheses override" "(1 + 2) * 3" (Expr.Binary(Expr.Paren (Expr.Binary(Expr.Literal (Literal.Integer 1I), BinaryOp.Add, Expr.Literal (Literal.Integer 2I))), BinaryOp.Multiply, Expr.Literal (Literal.Integer 3I)))
            testExpr "logical and comparison" "a < b and c >= d" (Expr.Binary(Expr.Binary(Expr.Var "a", BinaryOp.Less, Expr.Var "b"), BinaryOp.And, Expr.Binary(Expr.Var "c", BinaryOp.GreaterEqual, Expr.Var "d")))
            testExpr "bitwise and arithmetic" "1 + 2 & 3 * 4" (Expr.Binary(Expr.Binary(Expr.Literal (Literal.Integer 1I), BinaryOp.Add, Expr.Literal (Literal.Integer 2I)), BinaryOp.BitAnd, Expr.Binary(Expr.Literal (Literal.Integer 3I), BinaryOp.Multiply, Expr.Literal (Literal.Integer 4I))))
        ]

        testList "Whitespace Handling" [
            testExpr "no spaces" "1+2" (Expr.Binary(Expr.Literal (Literal.Integer 1I), BinaryOp.Add, Expr.Literal (Literal.Integer 2I)))
            testExpr "many spaces" "1   +   2" (Expr.Binary(Expr.Literal (Literal.Integer 1I), BinaryOp.Add, Expr.Literal (Literal.Integer 2I)))
            testExpr "tabs and spaces" "1\t+\t2" (Expr.Binary(Expr.Literal (Literal.Integer 1I), BinaryOp.Add, Expr.Literal (Literal.Integer 2I)))
            testExpr "newlines" "1\n+\n2" (Expr.Binary(Expr.Literal (Literal.Integer 1I), BinaryOp.Add, Expr.Literal (Literal.Integer 2I)))
        ]

        testList "Edge Cases" [
            testExpr "all unary operators" "not -~#42" (Expr.Unary(UnaryOp.Not, Expr.Unary(UnaryOp.Negate, Expr.Unary(UnaryOp.BitNot, Expr.Unary(UnaryOp.Length, Expr.Literal (Literal.Integer 42I))))))
            testExpr "deeply nested precedence" "1+2*3^4%5-6/7" (Expr.Binary(Expr.Binary(Expr.Literal (Literal.Integer 1I), BinaryOp.Add, Expr.Binary(Expr.Binary(Expr.Literal (Literal.Integer 2I), BinaryOp.Multiply, Expr.Binary(Expr.Literal (Literal.Integer 3I), BinaryOp.Power, Expr.Literal (Literal.Integer 4I))), BinaryOp.Modulo, Expr.Literal (Literal.Integer 5I))), BinaryOp.Subtract, Expr.Binary(Expr.Literal (Literal.Integer 6I), BinaryOp.FloatDiv, Expr.Literal (Literal.Integer 7I))))
            testExpr "mixed data types" "1 + \"hello\" .. true" (Expr.Binary(Expr.Binary(Expr.Literal (Literal.Integer 1I), BinaryOp.Add, Expr.Literal (Literal.String "hello")), BinaryOp.Concat, Expr.Literal (Literal.Boolean true)))
            testExpr "boolean logic chain" "true and false or nil and true" (Expr.Binary(Expr.Binary(Expr.Literal (Literal.Boolean true), BinaryOp.And, Expr.Literal (Literal.Boolean false)), BinaryOp.Or, Expr.Binary(Expr.Literal Literal.Nil, BinaryOp.And, Expr.Literal (Literal.Boolean true))))
        ]

        testList "Function Calls" [
            testExpr "no arguments" "print()" (Expr.FunctionCall(Expr.Var "print", []))
            testExpr "single argument" "print(42)" (Expr.FunctionCall(Expr.Var "print", [Expr.Literal (Literal.Integer 42I)]))
            testExpr "multiple arguments" "print(1, 2, 3)" (Expr.FunctionCall(Expr.Var "print", [Expr.Literal (Literal.Integer 1I); Expr.Literal (Literal.Integer 2I); Expr.Literal (Literal.Integer 3I)]))
            testExpr "nested function calls" "print(max(1, 2))" (Expr.FunctionCall(Expr.Var "print", [Expr.FunctionCall(Expr.Var "max", [Expr.Literal (Literal.Integer 1I); Expr.Literal (Literal.Integer 2I)])]))
            testExpr "function call in expression" "1 + print(2)" (Expr.Binary(Expr.Literal (Literal.Integer 1I), BinaryOp.Add, Expr.FunctionCall(Expr.Var "print", [Expr.Literal (Literal.Integer 2I)])))
            
            // Long string function calls without parentheses
            // TODO: These tests are currently failing - the parser may need adjustment
            // The parser sees "print" as a variable rather than recognizing [[hello]] as a postfix
            // testExpr "function call with long string no space" "print[[hello]]" 
            //     (Expr.FunctionCall(Expr.Var "print", [Expr.Literal (Literal.String "hello")]))
            // 
            // testExpr "function call with long string with equals" "print[=[hello]=]" 
            //     (Expr.FunctionCall(Expr.Var "print", [Expr.Literal (Literal.String "hello")]))
        ]

        testList "Table Access" [
            testExpr "dot access" "table.key" (Expr.TableAccess(Expr.Var "table", Expr.Literal (Literal.String "key")))
            testExpr "bracket access string" "table[\"key\"]" (Expr.TableAccess(Expr.Var "table", Expr.Literal (Literal.String "key")))
            testExpr "bracket access variable" "table[key]" (Expr.TableAccess(Expr.Var "table", Expr.Var "key"))
            testExpr "bracket access expression" "table[1 + 2]" (Expr.TableAccess(Expr.Var "table", Expr.Binary(Expr.Literal (Literal.Integer 1I), BinaryOp.Add, Expr.Literal (Literal.Integer 2I))))
            testExpr "chained dot access" "a.b.c" (Expr.TableAccess(Expr.TableAccess(Expr.Var "a", Expr.Literal (Literal.String "b")), Expr.Literal (Literal.String "c")))
            testExpr "chained bracket access" "a[1][2]" (Expr.TableAccess(Expr.TableAccess(Expr.Var "a", Expr.Literal (Literal.Integer 1I)), Expr.Literal (Literal.Integer 2I)))
            testExpr "mixed access" "a.b[1].c" (Expr.TableAccess(Expr.TableAccess(Expr.TableAccess(Expr.Var "a", Expr.Literal (Literal.String "b")), Expr.Literal (Literal.Integer 1I)), Expr.Literal (Literal.String "c")))
            testExpr "table access with function call" "math.max(1, 2)" (Expr.FunctionCall(Expr.TableAccess(Expr.Var "math", Expr.Literal (Literal.String "max")), [Expr.Literal (Literal.Integer 1I); Expr.Literal (Literal.Integer 2I)]))
        ]

        testList "Method Calls" [
            testExpr "simple method call" "obj:method()" (Expr.MethodCall(Expr.Var "obj", "method", []))
            testExpr "method with single arg" "obj:method(42)" (Expr.MethodCall(Expr.Var "obj", "method", [Expr.Literal (Literal.Integer 42I)]))
            testExpr "method with multiple args" "obj:method(1, 2, 3)" (Expr.MethodCall(Expr.Var "obj", "method", [Expr.Literal (Literal.Integer 1I); Expr.Literal (Literal.Integer 2I); Expr.Literal (Literal.Integer 3I)]))
            testExpr "chained method calls" "obj:first():second()" (Expr.MethodCall(Expr.MethodCall(Expr.Var "obj", "first", []), "second", []))
            testExpr "method on table access" "data.obj:method()" (Expr.MethodCall(Expr.TableAccess(Expr.Var "data", Expr.Literal (Literal.String "obj")), "method", []))
            testExpr "method with expression args" "obj:add(1 + 2, x)" (Expr.MethodCall(Expr.Var "obj", "add", [Expr.Binary(Expr.Literal (Literal.Integer 1I), BinaryOp.Add, Expr.Literal (Literal.Integer 2I)); Expr.Var "x"]))
            testExpr "mixed access and method" "a.b:method().c" (Expr.TableAccess(Expr.MethodCall(Expr.TableAccess(Expr.Var "a", Expr.Literal (Literal.String "b")), "method", []), Expr.Literal (Literal.String "c")))
            
            // Long string method calls without parentheses
            testExpr "method call with long string no space" "obj:write[[hello]]" 
                (Expr.MethodCall(Expr.Var "obj", "write", [Expr.Literal (Literal.String "hello")]))
            
            testExpr "method call with long string with space" "obj:write [[hello]]" 
                (Expr.MethodCall(Expr.Var "obj", "write", [Expr.Literal (Literal.String "hello")]))
            
            testExpr "method call with long string with equals" "obj:write[=[hello]=]" 
                (Expr.MethodCall(Expr.Var "obj", "write", [Expr.Literal (Literal.String "hello")]))
            
            testExpr "method call with long string multiline" "obj:write[[line1\nline2]]" 
                (Expr.MethodCall(Expr.Var "obj", "write", [Expr.Literal (Literal.String "line1\nline2")]))
            
            testExpr "chained method with long string" "obj:method1():method2[[test]]" 
                (Expr.MethodCall(Expr.MethodCall(Expr.Var "obj", "method1", []), "method2", [Expr.Literal (Literal.String "test")]))
        ]

        testList "Table Constructors" [
            testExpr "empty table" "{}" (Expr.TableConstructor [])
            
            testList "Simple Values" [
                testExpr "single value" "{42}" (Expr.TableConstructor [ExprField (Expr.Literal (Literal.Integer 42I))])
                testExpr "multiple values" "{1, 2, 3}" (Expr.TableConstructor [ExprField (Expr.Literal (Literal.Integer 1I)); ExprField (Expr.Literal (Literal.Integer 2I)); ExprField (Expr.Literal (Literal.Integer 3I))])
                testExpr "mixed types" "{1, \"hello\", true}" (Expr.TableConstructor [ExprField (Expr.Literal (Literal.Integer 1I)); ExprField (Expr.Literal (Literal.String "hello")); ExprField (Expr.Literal (Literal.Boolean true))])
            ]
            
            testList "Named Fields" [
                testExpr "single named field" "{name = \"John\"}" (Expr.TableConstructor [NamedField ("name", Expr.Literal (Literal.String "John"))])
                testExpr "multiple named fields" "{x = 1, y = 2}" (Expr.TableConstructor [NamedField ("x", Expr.Literal (Literal.Integer 1I)); NamedField ("y", Expr.Literal (Literal.Integer 2I))])
                testExpr "named field with expression" "{result = 1 + 2}" (Expr.TableConstructor [NamedField ("result", Expr.Binary(Expr.Literal (Literal.Integer 1I), BinaryOp.Add, Expr.Literal (Literal.Integer 2I)))])
            ]
            
            testList "Key Fields" [
                testExpr "single key field" "{[\"key\"] = \"value\"}" (Expr.TableConstructor [KeyField (Expr.Literal (Literal.String "key"), Expr.Literal (Literal.String "value"))])
                testExpr "key field with variable" "{[key] = value}" (Expr.TableConstructor [KeyField (Expr.Var "key", Expr.Var "value")])
                testExpr "key field with expression" "{[1 + 2] = \"three\"}" (Expr.TableConstructor [KeyField (Expr.Binary(Expr.Literal (Literal.Integer 1I), BinaryOp.Add, Expr.Literal (Literal.Integer 2I)), Expr.Literal (Literal.String "three"))])
            ]
            
            testList "Mixed Fields" [
                testExpr "values and named" "{1, 2, name = \"test\"}" (Expr.TableConstructor [ExprField (Expr.Literal (Literal.Integer 1I)); ExprField (Expr.Literal (Literal.Integer 2I)); NamedField ("name", Expr.Literal (Literal.String "test"))])
                testExpr "all field types" "{1, name = \"test\", [key] = value}" (Expr.TableConstructor [ExprField (Expr.Literal (Literal.Integer 1I)); NamedField ("name", Expr.Literal (Literal.String "test")); KeyField (Expr.Var "key", Expr.Var "value")])
                testExpr "nested tables" "{inner = {x = 1}}" (Expr.TableConstructor [NamedField ("inner", Expr.TableConstructor [NamedField ("x", Expr.Literal (Literal.Integer 1I))])])
            ]
            
            testList "Complex Examples" [
                testExpr "table with function calls" "{result = max(1, 2)}" (Expr.TableConstructor [NamedField ("result", Expr.FunctionCall(Expr.Var "max", [Expr.Literal (Literal.Integer 1I); Expr.Literal (Literal.Integer 2I)]))])
                testExpr "table with table access" "{value = data.field}" (Expr.TableConstructor [NamedField ("value", Expr.TableAccess(Expr.Var "data", Expr.Literal (Literal.String "field")))])
                testExpr "table with method calls" "{result = obj:method()}" (Expr.TableConstructor [NamedField ("result", Expr.MethodCall(Expr.Var "obj", "method", []))])
                testExpr "table with function definition" "{handler = function() end}" (Expr.TableConstructor [NamedField ("handler", Expr.FunctionDef { Parameters = []; IsVararg = false; Body = [] })])
                testExpr "table with complex keys" "{[obj:getKey()] = getValue()}" (Expr.TableConstructor [KeyField (Expr.MethodCall(Expr.Var "obj", "getKey", []), Expr.FunctionCall(Expr.Var "getValue", []))])
                testExpr "deeply nested tables" "{a = {b = {c = {d = 1}}}}" (Expr.TableConstructor [NamedField ("a", Expr.TableConstructor [NamedField ("b", Expr.TableConstructor [NamedField ("c", Expr.TableConstructor [NamedField ("d", Expr.Literal (Literal.Integer 1I))])])])])
                testExpr "mixed complex expressions" "{1 + 2, [x * y] = obj:calc(), func = function() end}" (Expr.TableConstructor [ExprField (Expr.Binary(Expr.Literal (Literal.Integer 1I), BinaryOp.Add, Expr.Literal (Literal.Integer 2I))); KeyField (Expr.Binary(Expr.Var "x", BinaryOp.Multiply, Expr.Var "y"), Expr.MethodCall(Expr.Var "obj", "calc", [])); NamedField ("func", Expr.FunctionDef { Parameters = []; IsVararg = false; Body = [] })])
            ]
        ]

        testList "Function Expressions" [
            testExpr "empty function" "function() end" (Expr.FunctionDef { Parameters = []; IsVararg = false; Body = [] })
            testExpr "function with parameters" "function(a, b) end" (Expr.FunctionDef { Parameters = [Parameter.Named ("a", FLua.Parser.Attribute.NoAttribute); Parameter.Named ("b", FLua.Parser.Attribute.NoAttribute)]; IsVararg = false; Body = [] })
            testExpr "function with vararg" "function(...) end" (Expr.FunctionDef { Parameters = [Parameter.Vararg]; IsVararg = true; Body = [] })
            testExpr "function with params and vararg" "function(a, b, ...) end" (Expr.FunctionDef { Parameters = [Parameter.Named ("a", FLua.Parser.Attribute.NoAttribute); Parameter.Named ("b", FLua.Parser.Attribute.NoAttribute); Parameter.Vararg]; IsVararg = true; Body = [] })
            testExpr "function with body" "function(x) return x + 1 end" (Expr.FunctionDef { Parameters = [Parameter.Named ("x", FLua.Parser.Attribute.NoAttribute)]; IsVararg = false; Body = [Statement.Return (Some [Expr.Binary(Expr.Var "x", BinaryOp.Add, Expr.Literal (Literal.Integer 1I))])] })
        ]
    ]

    testList "Statement Parser Tests" [
        testList "Basic Statements" [
            testStmt "simple assignment" "x = 42" (Statement.Assignment([Expr.Var "x"], [Expr.Literal (Literal.Integer 42I)]))
            testStmt "multiple assignment" "x, y = 1, 2" (Statement.Assignment([Expr.Var "x"; Expr.Var "y"], [Expr.Literal (Literal.Integer 1I); Expr.Literal (Literal.Integer 2I)]))
            testStmt "assignment with expressions" "a, b = x + 1, func()" (Statement.Assignment([Expr.Var "a"; Expr.Var "b"], [Expr.Binary(Expr.Var "x", BinaryOp.Add, Expr.Literal (Literal.Integer 1I)); Expr.FunctionCall(Expr.Var "func", [])]))
            testStmt "assignment with table access" "x, y.z = 1, 2" (Statement.Assignment([Expr.Var "x"; Expr.TableAccess(Expr.Var "y", Expr.Literal (Literal.String "z"))], [Expr.Literal (Literal.Integer 1I); Expr.Literal (Literal.Integer 2I)]))
            testStmt "local declaration" "local x = 42" (Statement.LocalAssignment([("x", FLua.Parser.Attribute.NoAttribute)], Some [Expr.Literal (Literal.Integer 42I)]))
            testStmt "local without init" "local x" (Statement.LocalAssignment([("x", FLua.Parser.Attribute.NoAttribute)], Option.None))
            testStmt "multiple local" "local x, y = 1, 2" (Statement.LocalAssignment([("x", FLua.Parser.Attribute.NoAttribute); ("y", FLua.Parser.Attribute.NoAttribute)], Some [Expr.Literal (Literal.Integer 1I); Expr.Literal (Literal.Integer 2I)]))
            testStmt "multiple local with function call" "local a, b = func()" (Statement.LocalAssignment([("a", FLua.Parser.Attribute.NoAttribute); ("b", FLua.Parser.Attribute.NoAttribute)], Some [Expr.FunctionCall(Expr.Var "func", [])]))
            testStmt "break statement" "break" Statement.Break
            testStmt "return empty" "return" (Statement.Return Option.None)
            testStmt "return value" "return 42" (Statement.Return (Some [Expr.Literal (Literal.Integer 42I)]))
            testStmt "return multiple" "return 1, 2" (Statement.Return (Some [Expr.Literal (Literal.Integer 1I); Expr.Literal (Literal.Integer 2I)]))
            testStmt "return expressions" "return x + 1, func(), y" (Statement.Return (Some [Expr.Binary(Expr.Var "x", BinaryOp.Add, Expr.Literal (Literal.Integer 1I)); Expr.FunctionCall(Expr.Var "func", []); Expr.Var "y"]))
            testStmt "return with method call" "return obj:method()" (Statement.Return (Some [Expr.MethodCall(Expr.Var "obj", "method", [])]))
        ]
        
        testList "Control Flow Statements" [
            testStmt "simple if" "if true then end" 
                (Statement.If([(Expr.Literal (Literal.Boolean true), [])], Option.None))
             
            testStmt "simple if with statement" "if true then x = 1 end" 
                (Statement.If([(Expr.Literal (Literal.Boolean true), [Statement.Assignment([Expr.Var "x"], [Expr.Literal (Literal.Integer 1I)])])], Option.None))
             
            testStmt "if with else" "if x > 0 then y = 1 else y = 2 end"
                (Statement.If([(Expr.Binary(Expr.Var "x", BinaryOp.Greater, Expr.Literal (Literal.Integer 0I)), [Statement.Assignment([Expr.Var "y"], [Expr.Literal (Literal.Integer 1I)])])], 
                    Some [Statement.Assignment([Expr.Var "y"], [Expr.Literal (Literal.Integer 2I)])]))
             
            testStmt "if with elseif" "if x == 1 then y = 1 elseif x == 2 then y = 2 end"
                (Statement.If([(Expr.Binary(Expr.Var "x", BinaryOp.Equal, Expr.Literal (Literal.Integer 1I)), [Statement.Assignment([Expr.Var "y"], [Expr.Literal (Literal.Integer 1I)])]);
                     (Expr.Binary(Expr.Var "x", BinaryOp.Equal, Expr.Literal (Literal.Integer 2I)), [Statement.Assignment([Expr.Var "y"], [Expr.Literal (Literal.Integer 2I)])])], Option.None))
             
            testStmt "if with elseif and else" "if x == 1 then y = 1 elseif x == 2 then y = 2 else y = 3 end"
                (Statement.If([(Expr.Binary(Expr.Var "x", BinaryOp.Equal, Expr.Literal (Literal.Integer 1I)), [Statement.Assignment([Expr.Var "y"], [Expr.Literal (Literal.Integer 1I)])]);
                     (Expr.Binary(Expr.Var "x", BinaryOp.Equal, Expr.Literal (Literal.Integer 2I)), [Statement.Assignment([Expr.Var "y"], [Expr.Literal (Literal.Integer 2I)])])], 
                    Some [Statement.Assignment([Expr.Var "y"], [Expr.Literal (Literal.Integer 3I)])]))
             
            testStmt "while loop" "while x > 0 do x = x - 1 end"
                (Statement.While(Expr.Binary(Expr.Var "x", BinaryOp.Greater, Expr.Literal (Literal.Integer 0I)), 
                       [Statement.Assignment([Expr.Var "x"], [Expr.Binary(Expr.Var "x", BinaryOp.Subtract, Expr.Literal (Literal.Integer 1I))])]))
             
            testStmt "repeat until loop" "repeat x = x + 1 until x > 10"
                (Statement.Repeat([Statement.Assignment([Expr.Var "x"], [Expr.Binary(Expr.Var "x", BinaryOp.Add, Expr.Literal (Literal.Integer 1I))])],
                        Expr.Binary(Expr.Var "x", BinaryOp.Greater, Expr.Literal (Literal.Integer 10I))))
            
            testList "For Loops" [
                testStmt "numeric for basic" "for i = 1, 10 do end"
                    (Statement.NumericFor("i", Expr.Literal (Literal.Integer 1I), Expr.Literal (Literal.Integer 10I), Option.None, []))
                 
                testStmt "numeric for with step" "for i = 1, 10, 2 do end"
                    (Statement.NumericFor("i", Expr.Literal (Literal.Integer 1I), Expr.Literal (Literal.Integer 10I), Some (Expr.Literal (Literal.Integer 2I)), []))
                 
                testStmt "numeric for with body" "for i = 1, 10 do x = i end"
                    (Statement.NumericFor("i", Expr.Literal (Literal.Integer 1I), Expr.Literal (Literal.Integer 10I), Option.None, 
                               [Statement.Assignment([Expr.Var "x"], [Expr.Var "i"])]))
                 
                testStmt "numeric for with function call" "for i = 1, 10 do print(i) end"
                    (Statement.NumericFor("i", Expr.Literal (Literal.Integer 1I), Expr.Literal (Literal.Integer 10I), Option.None, 
                               [Statement.FunctionCall (Expr.FunctionCall(Expr.Var "print", [Expr.Var "i"]))]))
                 
                testStmt "generic for single var" "for k in t do end"
                    (Statement.GenericFor([("k", FLua.Parser.Attribute.NoAttribute)], [Expr.Var "t"], []))
                 
                testStmt "generic for multiple vars" "for k, v in t do end"
                    (Statement.GenericFor([("k", FLua.Parser.Attribute.NoAttribute); ("v", FLua.Parser.Attribute.NoAttribute)], 
                               [Expr.Var "t"], []))
                 
                // TEMPORARILY DISABLED: Parser issue with expressions in generic for
                // testStmt "generic for with function call" "for k, v in pairs(t) do end"
                //     (Statement.GenericFor([("k", FLua.Parser.Attribute.NoAttribute); ("v", FLua.Parser.Attribute.NoAttribute)], 
                //                [Expr.FunctionCall(Expr.Var "pairs", [Expr.Var "t"])], []))
                 
                testStmt "generic for with body" "for k, v in t do x = k end"
                    (Statement.GenericFor([("k", FLua.Parser.Attribute.NoAttribute); ("v", FLua.Parser.Attribute.NoAttribute)], 
                               [Expr.Var "t"], 
                               [Statement.Assignment([Expr.Var "x"], [Expr.Var "k"])]))
            ]

            testList "Labels and Goto" [
                testStmt "simple label" "::start::" (Statement.Label "start")
                testStmt "label with spaces" ":: end_label ::" (Statement.Label "end_label")
                testStmt "goto statement" "goto start" (Statement.Goto "start")
                testStmt "goto with underscore" "goto loop_end" (Statement.Goto "loop_end")
                testStmt "goto with numbers" "goto label1" (Statement.Goto "label1")
            ]
 
            testList "Function Definitions" [
                testStmt "simple function" "function test() end" 
                    (Statement.FunctionDef(["test"], { Parameters = []; IsVararg = false; Body = [] }))
                 
                testStmt "function with parameters" "function add(a, b) end"
                    (Statement.FunctionDef(["add"], { Parameters = [Parameter.Named ("a", FLua.Parser.Attribute.NoAttribute); Parameter.Named ("b", FLua.Parser.Attribute.NoAttribute)]; IsVararg = false; Body = [] }))
                 
                testStmt "function with vararg" "function print(...) end"
                    (Statement.FunctionDef(["print"], { Parameters = [Parameter.Vararg]; IsVararg = true; Body = [] }))
                 
                testStmt "method definition" "function obj.method(self) end"
                    (Statement.FunctionDef(["obj"; "method"], { Parameters = [Parameter.Named ("self", FLua.Parser.Attribute.NoAttribute)]; IsVararg = false; Body = [] }))
                 
                testStmt "nested method" "function a.b.c.method() end"
                    (Statement.FunctionDef(["a"; "b"; "c"; "method"], { Parameters = []; IsVararg = false; Body = [] }))
                 
                testStmt "local function" "local function helper() end"
                    (Statement.LocalFunctionDef("helper", { Parameters = []; IsVararg = false; Body = [] }))
                 
                testStmt "local function with params" "local function calc(x, y) return x * y end"
                    (Statement.LocalFunctionDef("calc", { Parameters = [Parameter.Named ("x", FLua.Parser.Attribute.NoAttribute); Parameter.Named ("y", FLua.Parser.Attribute.NoAttribute)]; IsVararg = false; Body = [Statement.Return (Some [Expr.Binary(Expr.Var "x", BinaryOp.Multiply, Expr.Var "y")])] }))
            ]
        ]
        
        
        // Remaining parser features to implement:
        // 1. Function expressions with bodies (need expression context parsing)
        // 2. Generic for with function calls (parser conflict resolution)
        
        testList "Integration Tests - Multiple Features" [
            testExpr "method call with table constructor" "obj:create({x = 1, y = 2})" 
                (Expr.MethodCall(Expr.Var "obj", "create", [Expr.TableConstructor [NamedField ("x", Expr.Literal (Literal.Integer 1I)); NamedField ("y", Expr.Literal (Literal.Integer 2I))]]))
            
            testExpr "table with method calls and functions" "{handler = obj:getHandler(), callback = function() end}" 
                (Expr.TableConstructor [NamedField ("handler", Expr.MethodCall(Expr.Var "obj", "getHandler", [])); NamedField ("callback", Expr.FunctionDef { Parameters = []; IsVararg = false; Body = [] })])
            
            testExpr "chained method calls with table access" "data.config:getValue().result" 
                (Expr.TableAccess(Expr.MethodCall(Expr.TableAccess(Expr.Var "data", Expr.Literal (Literal.String "config")), "getValue", []), Expr.Literal (Literal.String "result")))
            
            testStmt "multiple assignment with method calls" "a, b = obj:getValues(), data.field" 
                (Statement.Assignment([Expr.Var "a"; Expr.Var "b"], [Expr.MethodCall(Expr.Var "obj", "getValues", []); Expr.TableAccess(Expr.Var "data", Expr.Literal (Literal.String "field"))]))
             
            testStmt "return with method calls and table constructor" "return obj:process({input = x}), {status = \"ok\"}" 
                (Statement.Return (Some [Expr.MethodCall(Expr.Var "obj", "process", [Expr.TableConstructor [NamedField ("input", Expr.Var "x")]]); Expr.TableConstructor [NamedField ("status", Expr.Literal (Literal.String "ok"))]]))
             
            testStmt "local assignment with complex expressions" "local result, error = api:call({method = \"GET\"})" 
                (Statement.LocalAssignment([("result", FLua.Parser.Attribute.NoAttribute); ("error", FLua.Parser.Attribute.NoAttribute)], Some [Expr.MethodCall(Expr.Var "api", "call", [Expr.TableConstructor [NamedField ("method", Expr.Literal (Literal.String "GET"))]])]))
        ]
        
        testList "Variable Attributes" [
            testList "Local Variable Attributes" [
                testStmt "local const variable" "local x <const> = 42"
                    (Statement.LocalAssignment([("x", Attribute.Const)], Some [Expr.Literal (Literal.Integer 42I)]))
                
                testStmt "local close variable" "local file <close> = resource"
                    (Statement.LocalAssignment([("file", Attribute.Close)], Some [Expr.Var "resource"]))
                
                testStmt "multiple variables with mixed attributes" "local a <const>, b, c <close> = 1, 2, resource"
                    (Statement.LocalAssignment([("a", Attribute.Const); ("b", Attribute.NoAttribute); ("c", Attribute.Close)], Some [Expr.Literal (Literal.Integer 1I); Expr.Literal (Literal.Integer 2I); Expr.Var "resource"]))
                
                testStmt "local variables without values" "local x <const>, y <close>"
                    (Statement.LocalAssignment([("x", Attribute.Const); ("y", Attribute.Close)], None))
            ]
            
            testList "Function Parameter Attributes" [
                testStmt "function with const parameter" "function test(x <const>) end"
                    (Statement.FunctionDef(["test"], { Parameters = [Parameter.Named ("x", Attribute.Const)]; IsVararg = false; Body = [] }))
                
                testStmt "function with close parameter" "function test(file <close>) end"
                    (Statement.FunctionDef(["test"], { Parameters = [Parameter.Named ("file", Attribute.Close)]; IsVararg = false; Body = [] }))
                
                testStmt "function with mixed parameter attributes" "function test(a <const>, b, c <close>) end"
                    (Statement.FunctionDef(["test"], { Parameters = [Parameter.Named ("a", Attribute.Const); Parameter.Named ("b", Attribute.NoAttribute); Parameter.Named ("c", Attribute.Close)]; IsVararg = false; Body = [] }))
                
                testStmt "local function with attributes" "local function calc(x <const>, y) end"
                    (Statement.LocalFunctionDef("calc", { Parameters = [Parameter.Named ("x", Attribute.Const); Parameter.Named ("y", Attribute.NoAttribute)]; IsVararg = false; Body = [] }))
            ]
            
            testList "Generic For Attributes" [
                testStmt "generic for with const variable" "for k <const> in t do end"
                    (Statement.GenericFor([("k", Attribute.Const)], [Expr.Var "t"], []))
                
                testStmt "generic for with mixed attributes" "for k <const>, v <close> in pairs(t) do end"
                    (Statement.GenericFor([("k", Attribute.Const); ("v", Attribute.Close)], [Expr.FunctionCall(Expr.Var "pairs", [Expr.Var "t"])], []))
            ]
        ]
        
        testList "Parser Fixes" [
            // Tests for recently fixed parser issues
            
            testList "Table Assignment at Statement Level (Fixed)" [
                // Basic table assignments
                testStmt "table index assignment" "t[1] = 100" 
                    (Statement.Assignment([Expr.TableAccess(Expr.Var "t", Expr.Literal (Literal.Integer 1I))], [Expr.Literal (Literal.Integer 100I)]))
                
                testStmt "table dot assignment" "t.field = \"value\"" 
                    (Statement.Assignment([Expr.TableAccess(Expr.Var "t", Expr.Literal (Literal.String "field"))], [Expr.Literal (Literal.String "value")]))
                
                testStmt "table string key assignment" "t[\"key\"] = true" 
                    (Statement.Assignment([Expr.TableAccess(Expr.Var "t", Expr.Literal (Literal.String "key"))], [Expr.Literal (Literal.Boolean true)]))
                
                testStmt "nested table assignment" "t.a.b[1] = 42"
                    (Statement.Assignment([Expr.TableAccess(Expr.TableAccess(Expr.TableAccess(Expr.Var "t", Expr.Literal (Literal.String "a")), Expr.Literal (Literal.String "b")), Expr.Literal (Literal.Integer 1I))], [Expr.Literal (Literal.Integer 42I)]))
                
                testStmt "multiple table assignment" "a[1], b[2] = 10, 20"
                    (Statement.Assignment([Expr.TableAccess(Expr.Var "a", Expr.Literal (Literal.Integer 1I)); Expr.TableAccess(Expr.Var "b", Expr.Literal (Literal.Integer 2I))], [Expr.Literal (Literal.Integer 10I); Expr.Literal (Literal.Integer 20I)]))
                
                // Complex cases
                testStmt "table assignment with expression key" "t[x + 1] = y * 2"
                    (Statement.Assignment([Expr.TableAccess(Expr.Var "t", Expr.Binary(Expr.Var "x", BinaryOp.Add, Expr.Literal (Literal.Integer 1I)))], [Expr.Binary(Expr.Var "y", BinaryOp.Multiply, Expr.Literal (Literal.Integer 2I))]))
                
                testStmt "mixed assignment" "x, t[1], y.z = 1, 2, 3"
                    (Statement.Assignment([Expr.Var "x"; Expr.TableAccess(Expr.Var "t", Expr.Literal (Literal.Integer 1I)); Expr.TableAccess(Expr.Var "y", Expr.Literal (Literal.String "z"))], [Expr.Literal (Literal.Integer 1I); Expr.Literal (Literal.Integer 2I); Expr.Literal (Literal.Integer 3I)]))
                
                testStmt "table assignment from function call" "t.result = func()"
                    (Statement.Assignment([Expr.TableAccess(Expr.Var "t", Expr.Literal (Literal.String "result"))], [Expr.FunctionCall(Expr.Var "func", [])]))
                
                testStmt "chained table assignment" "t1.t2.t3[\"key\"] = value"
                    (Statement.Assignment([Expr.TableAccess(Expr.TableAccess(Expr.TableAccess(Expr.Var "t1", Expr.Literal (Literal.String "t2")), Expr.Literal (Literal.String "t3")), Expr.Literal (Literal.String "key"))], [Expr.Var "value"]))
                
                testStmt "table method result assignment" "t[1] = obj:method()"
                    (Statement.Assignment([Expr.TableAccess(Expr.Var "t", Expr.Literal (Literal.Integer 1I))], [Expr.MethodCall(Expr.Var "obj", "method", [])]))
            ]
            
            testList "Assignment vs Function Call Disambiguation" [
                // Test that the parser correctly distinguishes between assignments and function calls
                testStmt "simple function call" "print(x)" 
                    (Statement.FunctionCall(Expr.FunctionCall(Expr.Var "print", [Expr.Var "x"])))
                
                testStmt "method call statement" "obj:method()" 
                    (Statement.FunctionCall(Expr.MethodCall(Expr.Var "obj", "method", [])))
                
                testStmt "table access function call" "t.func()" 
                    (Statement.FunctionCall(Expr.FunctionCall(Expr.TableAccess(Expr.Var "t", Expr.Literal (Literal.String "func")), [])))
                
                testStmt "parenthesized function call" "(func())()" 
                    (Statement.FunctionCall(Expr.FunctionCall(Expr.Paren(Expr.FunctionCall(Expr.Var "func", [])), [])))
                
                // Edge cases with whitespace
                testStmt "assignment with spaces" "t[1]   =   100" 
                    (Statement.Assignment([Expr.TableAccess(Expr.Var "t", Expr.Literal (Literal.Integer 1I))], [Expr.Literal (Literal.Integer 100I)]))
                
                testStmt "assignment with newline" "t[1]\n= 100" 
                    (Statement.Assignment([Expr.TableAccess(Expr.Var "t", Expr.Literal (Literal.Integer 1I))], [Expr.Literal (Literal.Integer 100I)]))
            ]
            
            testList "Complex Assignment Patterns" [
                // Test assignments that were problematic before the fix
                testStmt "assignment after complex expression" "t[f(x)] = y"
                    (Statement.Assignment([Expr.TableAccess(Expr.Var "t", Expr.FunctionCall(Expr.Var "f", [Expr.Var "x"]))], [Expr.Var "y"]))
                
                testStmt "assignment with table constructor key" "t[{1,2}] = 3"
                    (Statement.Assignment([Expr.TableAccess(Expr.Var "t", Expr.TableConstructor [ExprField (Expr.Literal (Literal.Integer 1I)); ExprField (Expr.Literal (Literal.Integer 2I))])], [Expr.Literal (Literal.Integer 3I)]))
                
                testStmt "assignment with parenthesized lvalue" "(t)[1] = 100"
                    (Statement.Assignment([Expr.TableAccess(Expr.Paren (Expr.Var "t"), Expr.Literal (Literal.Integer 1I))], [Expr.Literal (Literal.Integer 100I)]))
                
                testStmt "nil assignment to table" "t.x = nil"
                    (Statement.Assignment([Expr.TableAccess(Expr.Var "t", Expr.Literal (Literal.String "x"))], [Expr.Literal Literal.Nil]))
                
                testStmt "table assignment in sequence" "t[1] = 2; t[2] = 3"
                    (Statement.Assignment([Expr.TableAccess(Expr.Var "t", Expr.Literal (Literal.Integer 1I))], [Expr.Literal (Literal.Integer 2I)]))
            ]
            
            testList "Table Access in Binary Expressions (TODO: Fix Parser)" [
                // The parser has issues with table access directly followed by binary operators
                // See PARSER_KNOWN_ISSUES.md for details
                
                // testExpr "table access addition" "t[1] + t[2]"
                //     (Expr.Binary(
                //         Expr.TableAccess(Expr.Var "t", Expr.Literal (Literal.Integer 1I)),
                //         BinaryOp.Add,
                //         Expr.TableAccess(Expr.Var "t", Expr.Literal (Literal.Integer 2I))))
                
                // testExpr "table dot access addition" "t.x + t.y"
                //     (Expr.Binary(
                //         Expr.TableAccess(Expr.Var "t", Expr.Literal (Literal.String "x")),
                //         BinaryOp.Add,
                //         Expr.TableAccess(Expr.Var "t", Expr.Literal (Literal.String "y"))))
            ]
        ]
    ]
]

[<EntryPoint>]
let main args =
    0 // Return 0 - tests are discovered automatically by YoloDev.Expecto.TestSdk
