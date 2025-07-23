module FLua.Parser.Tests

open Expecto
open FParsec
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
            testExpr "integer" "42" (Literal (Literal.Integer 42I))
            testExpr "float" "3.14" (Literal (Literal.Float 3.14))
            testExpr "string" "\"hello\"" (Literal (Literal.String "hello"))
            testExpr "boolean true" "true" (Literal (Literal.Boolean true))
            testExpr "boolean false" "false" (Literal (Literal.Boolean false))
            testExpr "nil" "nil" (Literal Literal.Nil)
            testExpr "zero integer" "0" (Literal (Literal.Integer 0I))
            testExpr "zero float" "0.0" (Literal (Literal.Float 0.0))
            testExpr "empty string" "\"\"" (Literal (Literal.String ""))
            testExpr "single quote string" "'hello'" (Literal (Literal.String "hello"))
            testExpr "hex integer" "0xff" (Literal (Literal.Integer 255I))
            testExpr "hex float" "0x1.5" (Literal (Literal.Float 1.3125))
        ]

        testList "Identifiers" [
            testExpr "simple identifier" "foo" (Var "foo")
            testExpr "underscore identifier" "_bar" (Var "_bar")
            testExpr "alphanumeric identifier" "baz123" (Var "baz123")
            testExpr "single character" "a" (Var "a")
            testExpr "single underscore" "_" (Var "_")
            testExpr "long identifier" "very_long_identifier_name_123" (Var "very_long_identifier_name_123")
        ]

        testExpr "vararg" "..." Vararg

        testList "Parentheses" [
            testExpr "simple parentheses" "(42)" (Paren (Literal (Literal.Integer 42I)))
            testExpr "nested parentheses" "((42))" (Paren (Paren (Literal (Literal.Integer 42I))))
            testExpr "triple nested" "(((42)))" (Paren (Paren (Paren (Literal (Literal.Integer 42I)))))
            testExpr "parentheses with spaces" "( 42 )" (Paren (Literal (Literal.Integer 42I)))
        ]

        testList "Unary Operators" [
            testExpr "not" "not true" (Unary(Not, Literal (Literal.Boolean true)))
            testExpr "length" "#\"hello\"" (Unary(Length, Literal (Literal.String "hello")))
            testExpr "negate" "-42" (Unary(Negate, Literal (Literal.Integer 42I)))
            testExpr "bitwise not" "~42" (Unary(BitNot, Literal (Literal.Integer 42I)))
            testExpr "multiple unary" "not #\"hello\"" (Unary(Not, Unary(Length, Literal (Literal.String "hello"))))
        ]

        testList "Power Operator" [
            testExpr "simple power" "2^3" (Binary(Literal (Literal.Integer 2I), Power, Literal (Literal.Integer 3I)))
            testExpr "right associative" "2^3^2" (Binary(Literal (Literal.Integer 2I), Power, Binary(Literal (Literal.Integer 3I), Power, Literal (Literal.Integer 2I))))
        ]

        testList "Multiplicative Operators" [
            testExpr "multiply" "2*3" (Binary(Literal (Literal.Integer 2I), Multiply, Literal (Literal.Integer 3I)))
            testExpr "float divide" "6/2" (Binary(Literal (Literal.Integer 6I), FloatDiv, Literal (Literal.Integer 2I)))
            testExpr "floor divide" "7//2" (Binary(Literal (Literal.Integer 7I), FloorDiv, Literal (Literal.Integer 2I)))
            testExpr "modulo" "7%2" (Binary(Literal (Literal.Integer 7I), Modulo, Literal (Literal.Integer 2I)))
            testExpr "left associative" "2*3*4" (Binary(Binary(Literal (Literal.Integer 2I), Multiply, Literal (Literal.Integer 3I)), Multiply, Literal (Literal.Integer 4I)))
        ]

        testList "Additive Operators" [
            testExpr "add" "2+3" (Binary(Literal (Literal.Integer 2I), Add, Literal (Literal.Integer 3I)))
            testExpr "subtract" "5-3" (Binary(Literal (Literal.Integer 5I), Subtract, Literal (Literal.Integer 3I)))
            testExpr "left associative" "1+2+3" (Binary(Binary(Literal (Literal.Integer 1I), Add, Literal (Literal.Integer 2I)), Add, Literal (Literal.Integer 3I)))
        ]

        testList "String Concatenation" [
            testExpr "simple concat" "\"hello\"..\"world\"" (Binary(Literal (Literal.String "hello"), Concat, Literal (Literal.String "world")))
            testExpr "right associative" "\"a\"..\"b\"..\"c\"" (Binary(Literal (Literal.String "a"), Concat, Binary(Literal (Literal.String "b"), Concat, Literal (Literal.String "c"))))
        ]

        testList "Shift Operators" [
            testExpr "left shift" "1<<2" (Binary(Literal (Literal.Integer 1I), ShiftLeft, Literal (Literal.Integer 2I)))
            testExpr "right shift" "8>>1" (Binary(Literal (Literal.Integer 8I), ShiftRight, Literal (Literal.Integer 1I)))
            testExpr "left associative" "1<<2<<3" (Binary(Binary(Literal (Literal.Integer 1I), ShiftLeft, Literal (Literal.Integer 2I)), ShiftLeft, Literal (Literal.Integer 3I)))
        ]

        testList "Bitwise Operators" [
            testExpr "bitwise and" "3&1" (Binary(Literal (Literal.Integer 3I), BitAnd, Literal (Literal.Integer 1I)))
            testExpr "bitwise xor" "3~1" (Binary(Literal (Literal.Integer 3I), BitXor, Literal (Literal.Integer 1I)))
            testExpr "bitwise or" "2|1" (Binary(Literal (Literal.Integer 2I), BitOr, Literal (Literal.Integer 1I)))
            testExpr "precedence" "1&2|3~4" (Binary(Binary(Binary(Literal (Literal.Integer 1I), BitAnd, Literal (Literal.Integer 2I)), BitOr, Literal (Literal.Integer 3I)), BitXor, Literal (Literal.Integer 4I)))
        ]

        testList "Comparison Operators" [
            testExpr "less" "2<3" (Binary(Literal (Literal.Integer 2I), Less, Literal (Literal.Integer 3I)))
            testExpr "greater" "3>2" (Binary(Literal (Literal.Integer 3I), Greater, Literal (Literal.Integer 2I)))
            testExpr "less equal" "2<=2" (Binary(Literal (Literal.Integer 2I), LessEqual, Literal (Literal.Integer 2I)))
            testExpr "greater equal" "3>=3" (Binary(Literal (Literal.Integer 3I), GreaterEqual, Literal (Literal.Integer 3I)))
            testExpr "not equal" "2~=3" (Binary(Literal (Literal.Integer 2I), NotEqual, Literal (Literal.Integer 3I)))
            testExpr "equal" "2==2" (Binary(Literal (Literal.Integer 2I), Equal, Literal (Literal.Integer 2I)))
        ]

        testList "Logical Operators" [
            testExpr "and" "true and false" (Binary(Literal (Literal.Boolean true), And, Literal (Literal.Boolean false)))
            testExpr "or" "true or false" (Binary(Literal (Literal.Boolean true), Or, Literal (Literal.Boolean false)))
            testExpr "precedence" "a and b or c and d" (Binary(Binary(Var "a", And, Var "b"), Or, Binary(Var "c", And, Var "d")))
        ]

        testList "Complex Expressions" [
            testExpr "operator precedence" "1 + 2 * 3" (Binary(Literal (Literal.Integer 1I), Add, Binary(Literal (Literal.Integer 2I), Multiply, Literal (Literal.Integer 3I))))
            testExpr "mixed operators" "1 + 2 * 3^2" (Binary(Literal (Literal.Integer 1I), Add, Binary(Literal (Literal.Integer 2I), Multiply, Binary(Literal (Literal.Integer 3I), Power, Literal (Literal.Integer 2I)))))
            testExpr "parentheses override" "(1 + 2) * 3" (Binary(Paren (Binary(Literal (Literal.Integer 1I), Add, Literal (Literal.Integer 2I))), Multiply, Literal (Literal.Integer 3I)))
            testExpr "logical and comparison" "a < b and c >= d" (Binary(Binary(Var "a", Less, Var "b"), And, Binary(Var "c", GreaterEqual, Var "d")))
            testExpr "bitwise and arithmetic" "1 + 2 & 3 * 4" (Binary(Binary(Literal (Literal.Integer 1I), Add, Literal (Literal.Integer 2I)), BitAnd, Binary(Literal (Literal.Integer 3I), Multiply, Literal (Literal.Integer 4I))))
        ]

        testList "Whitespace Handling" [
            testExpr "no spaces" "1+2" (Binary(Literal (Literal.Integer 1I), Add, Literal (Literal.Integer 2I)))
            testExpr "many spaces" "1   +   2" (Binary(Literal (Literal.Integer 1I), Add, Literal (Literal.Integer 2I)))
            testExpr "tabs and spaces" "1\t+\t2" (Binary(Literal (Literal.Integer 1I), Add, Literal (Literal.Integer 2I)))
            testExpr "newlines" "1\n+\n2" (Binary(Literal (Literal.Integer 1I), Add, Literal (Literal.Integer 2I)))
        ]

        testList "Edge Cases" [
            testExpr "all unary operators" "not -~#42" (Unary(Not, Unary(Negate, Unary(BitNot, Unary(Length, Literal (Literal.Integer 42I))))))
            testExpr "deeply nested precedence" "1+2*3^4%5-6/7" (Binary(Binary(Literal (Literal.Integer 1I), Add, Binary(Binary(Literal (Literal.Integer 2I), Multiply, Binary(Literal (Literal.Integer 3I), Power, Literal (Literal.Integer 4I))), Modulo, Literal (Literal.Integer 5I))), Subtract, Binary(Literal (Literal.Integer 6I), FloatDiv, Literal (Literal.Integer 7I))))
            testExpr "mixed data types" "1 + \"hello\" .. true" (Binary(Binary(Literal (Literal.Integer 1I), Add, Literal (Literal.String "hello")), Concat, Literal (Literal.Boolean true)))
            testExpr "boolean logic chain" "true and false or nil and true" (Binary(Binary(Literal (Literal.Boolean true), And, Literal (Literal.Boolean false)), Or, Binary(Literal Literal.Nil, And, Literal (Literal.Boolean true))))
        ]

        testList "Function Calls" [
            testExpr "no arguments" "print()" (FunctionCall(Var "print", []))
            testExpr "single argument" "print(42)" (FunctionCall(Var "print", [Literal (Literal.Integer 42I)]))
            testExpr "multiple arguments" "print(1, 2, 3)" (FunctionCall(Var "print", [Literal (Literal.Integer 1I); Literal (Literal.Integer 2I); Literal (Literal.Integer 3I)]))
            testExpr "nested function calls" "print(max(1, 2))" (FunctionCall(Var "print", [FunctionCall(Var "max", [Literal (Literal.Integer 1I); Literal (Literal.Integer 2I)])]))
            testExpr "function call in expression" "1 + print(2)" (Binary(Literal (Literal.Integer 1I), Add, FunctionCall(Var "print", [Literal (Literal.Integer 2I)])))
        ]

        testList "Table Access" [
            testExpr "dot access" "table.key" (TableAccess(Var "table", Literal (Literal.String "key")))
            testExpr "bracket access string" "table[\"key\"]" (TableAccess(Var "table", Literal (Literal.String "key")))
            testExpr "bracket access variable" "table[key]" (TableAccess(Var "table", Var "key"))
            testExpr "bracket access expression" "table[1 + 2]" (TableAccess(Var "table", Binary(Literal (Literal.Integer 1I), Add, Literal (Literal.Integer 2I))))
            testExpr "chained dot access" "a.b.c" (TableAccess(TableAccess(Var "a", Literal (Literal.String "b")), Literal (Literal.String "c")))
            testExpr "chained bracket access" "a[1][2]" (TableAccess(TableAccess(Var "a", Literal (Literal.Integer 1I)), Literal (Literal.Integer 2I)))
            testExpr "mixed access" "a.b[1].c" (TableAccess(TableAccess(TableAccess(Var "a", Literal (Literal.String "b")), Literal (Literal.Integer 1I)), Literal (Literal.String "c")))
            testExpr "table access with function call" "math.max(1, 2)" (FunctionCall(TableAccess(Var "math", Literal (Literal.String "max")), [Literal (Literal.Integer 1I); Literal (Literal.Integer 2I)]))
        ]

        testList "Method Calls" [
            testExpr "simple method call" "obj:method()" (MethodCall(Var "obj", "method", []))
            testExpr "method with single arg" "obj:method(42)" (MethodCall(Var "obj", "method", [Literal (Literal.Integer 42I)]))
            testExpr "method with multiple args" "obj:method(1, 2, 3)" (MethodCall(Var "obj", "method", [Literal (Literal.Integer 1I); Literal (Literal.Integer 2I); Literal (Literal.Integer 3I)]))
            testExpr "chained method calls" "obj:first():second()" (MethodCall(MethodCall(Var "obj", "first", []), "second", []))
            testExpr "method on table access" "data.obj:method()" (MethodCall(TableAccess(Var "data", Literal (Literal.String "obj")), "method", []))
            testExpr "method with expression args" "obj:add(1 + 2, x)" (MethodCall(Var "obj", "add", [Binary(Literal (Literal.Integer 1I), Add, Literal (Literal.Integer 2I)); Var "x"]))
            testExpr "mixed access and method" "a.b:method().c" (TableAccess(MethodCall(TableAccess(Var "a", Literal (Literal.String "b")), "method", []), Literal (Literal.String "c")))
        ]

        testList "Table Constructors" [
            testExpr "empty table" "{}" (TableConstructor [])
            
            testList "Simple Values" [
                testExpr "single value" "{42}" (TableConstructor [ExprField (Literal (Literal.Integer 42I))])
                testExpr "multiple values" "{1, 2, 3}" (TableConstructor [ExprField (Literal (Literal.Integer 1I)); ExprField (Literal (Literal.Integer 2I)); ExprField (Literal (Literal.Integer 3I))])
                testExpr "mixed types" "{1, \"hello\", true}" (TableConstructor [ExprField (Literal (Literal.Integer 1I)); ExprField (Literal (Literal.String "hello")); ExprField (Literal (Literal.Boolean true))])
            ]
            
            testList "Named Fields" [
                testExpr "single named field" "{name = \"John\"}" (TableConstructor [NamedField ("name", Literal (Literal.String "John"))])
                testExpr "multiple named fields" "{x = 1, y = 2}" (TableConstructor [NamedField ("x", Literal (Literal.Integer 1I)); NamedField ("y", Literal (Literal.Integer 2I))])
                testExpr "named field with expression" "{result = 1 + 2}" (TableConstructor [NamedField ("result", Binary(Literal (Literal.Integer 1I), Add, Literal (Literal.Integer 2I)))])
            ]
            
            testList "Key Fields" [
                testExpr "single key field" "{[\"key\"] = \"value\"}" (TableConstructor [KeyField (Literal (Literal.String "key"), Literal (Literal.String "value"))])
                testExpr "key field with variable" "{[key] = value}" (TableConstructor [KeyField (Var "key", Var "value")])
                testExpr "key field with expression" "{[1 + 2] = \"three\"}" (TableConstructor [KeyField (Binary(Literal (Literal.Integer 1I), Add, Literal (Literal.Integer 2I)), Literal (Literal.String "three"))])
            ]
            
            testList "Mixed Fields" [
                testExpr "values and named" "{1, 2, name = \"test\"}" (TableConstructor [ExprField (Literal (Literal.Integer 1I)); ExprField (Literal (Literal.Integer 2I)); NamedField ("name", Literal (Literal.String "test"))])
                testExpr "all field types" "{1, name = \"test\", [key] = value}" (TableConstructor [ExprField (Literal (Literal.Integer 1I)); NamedField ("name", Literal (Literal.String "test")); KeyField (Var "key", Var "value")])
                testExpr "nested tables" "{inner = {x = 1}}" (TableConstructor [NamedField ("inner", TableConstructor [NamedField ("x", Literal (Literal.Integer 1I))])])
            ]
            
            testList "Complex Examples" [
                testExpr "table with function calls" "{result = max(1, 2)}" (TableConstructor [NamedField ("result", FunctionCall(Var "max", [Literal (Literal.Integer 1I); Literal (Literal.Integer 2I)]))])
                testExpr "table with table access" "{value = data.field}" (TableConstructor [NamedField ("value", TableAccess(Var "data", Literal (Literal.String "field")))])
                testExpr "table with method calls" "{result = obj:method()}" (TableConstructor [NamedField ("result", MethodCall(Var "obj", "method", []))])
                testExpr "table with function definition" "{handler = function() end}" (TableConstructor [NamedField ("handler", FunctionDef { Parameters = []; IsVararg = false; Body = [] })])
                testExpr "table with complex keys" "{[obj:getKey()] = getValue()}" (TableConstructor [KeyField (MethodCall(Var "obj", "getKey", []), FunctionCall(Var "getValue", []))])
                testExpr "deeply nested tables" "{a = {b = {c = {d = 1}}}}" (TableConstructor [NamedField ("a", TableConstructor [NamedField ("b", TableConstructor [NamedField ("c", TableConstructor [NamedField ("d", Literal (Literal.Integer 1I))])])])])
                testExpr "mixed complex expressions" "{1 + 2, [x * y] = obj:calc(), func = function() end}" (TableConstructor [ExprField (Binary(Literal (Literal.Integer 1I), Add, Literal (Literal.Integer 2I))); KeyField (Binary(Var "x", Multiply, Var "y"), MethodCall(Var "obj", "calc", [])); NamedField ("func", FunctionDef { Parameters = []; IsVararg = false; Body = [] })])
            ]
        ]

        testList "Function Expressions" [
            testExpr "empty function" "function() end" (FunctionDef { Parameters = []; IsVararg = false; Body = [] })
            testExpr "function with parameters" "function(a, b) end" (FunctionDef { Parameters = [Param ("a", FLua.Parser.Attribute.NoAttribute); Param ("b", FLua.Parser.Attribute.NoAttribute)]; IsVararg = false; Body = [] })
            testExpr "function with vararg" "function(...) end" (FunctionDef { Parameters = [VarargParam]; IsVararg = true; Body = [] })
            testExpr "function with params and vararg" "function(a, b, ...) end" (FunctionDef { Parameters = [Param ("a", FLua.Parser.Attribute.NoAttribute); Param ("b", FLua.Parser.Attribute.NoAttribute); VarargParam]; IsVararg = true; Body = [] })
            // testExpr "function with body" "function(x) return x + 1 end" (FunctionDef { Parameters = [Param ("x", FLua.Parser.Attribute.NoAttribute)]; IsVararg = false; Body = [Return (Some [Binary(Var "x", Add, Literal (Literal.Integer 1I))])] })
        ]
    ]

    testList "Statement Parser Tests" [
        testList "Basic Statements" [
            testStmt "simple assignment" "x = 42" (Assignment([Var "x"], [Literal (Literal.Integer 42I)]))
            testStmt "multiple assignment" "x, y = 1, 2" (Assignment([Var "x"; Var "y"], [Literal (Literal.Integer 1I); Literal (Literal.Integer 2I)]))
            testStmt "assignment with expressions" "a, b = x + 1, func()" (Assignment([Var "a"; Var "b"], [Binary(Var "x", Add, Literal (Literal.Integer 1I)); FunctionCall(Var "func", [])]))
            testStmt "assignment with table access" "x, y.z = 1, 2" (Assignment([Var "x"; TableAccess(Var "y", Literal (Literal.String "z"))], [Literal (Literal.Integer 1I); Literal (Literal.Integer 2I)]))
            testStmt "local declaration" "local x = 42" (LocalAssignment([("x", FLua.Parser.Attribute.NoAttribute)], Some [Literal (Literal.Integer 42I)]))
            testStmt "local without init" "local x" (LocalAssignment([("x", FLua.Parser.Attribute.NoAttribute)], Option.None))
            testStmt "multiple local" "local x, y = 1, 2" (LocalAssignment([("x", FLua.Parser.Attribute.NoAttribute); ("y", FLua.Parser.Attribute.NoAttribute)], Some [Literal (Literal.Integer 1I); Literal (Literal.Integer 2I)]))
            testStmt "multiple local with function call" "local a, b = func()" (LocalAssignment([("a", FLua.Parser.Attribute.NoAttribute); ("b", FLua.Parser.Attribute.NoAttribute)], Some [FunctionCall(Var "func", [])]))
            testStmt "break statement" "break" Break
            testStmt "return empty" "return" (Return Option.None)
            testStmt "return value" "return 42" (Return (Some [Literal (Literal.Integer 42I)]))
            testStmt "return multiple" "return 1, 2" (Return (Some [Literal (Literal.Integer 1I); Literal (Literal.Integer 2I)]))
            testStmt "return expressions" "return x + 1, func(), y" (Return (Some [Binary(Var "x", Add, Literal (Literal.Integer 1I)); FunctionCall(Var "func", []); Var "y"]))
            testStmt "return with method call" "return obj:method()" (Return (Some [MethodCall(Var "obj", "method", [])]))
        ]
        
        testList "Control Flow Statements" [
            testStmt "simple if" "if true then end" 
                (If([(Literal (Literal.Boolean true), [])], Option.None))
            
            testStmt "simple if with statement" "if true then x = 1 end" 
                (If([(Literal (Literal.Boolean true), [Assignment([Var "x"], [Literal (Literal.Integer 1I)])])], Option.None))
            
            testStmt "if with else" "if x > 0 then y = 1 else y = 2 end"
                (If([(Binary(Var "x", Greater, Literal (Literal.Integer 0I)), [Assignment([Var "y"], [Literal (Literal.Integer 1I)])])], 
                    Some [Assignment([Var "y"], [Literal (Literal.Integer 2I)])]))
            
            testStmt "if with elseif" "if x == 1 then y = 1 elseif x == 2 then y = 2 end"
                (If([(Binary(Var "x", Equal, Literal (Literal.Integer 1I)), [Assignment([Var "y"], [Literal (Literal.Integer 1I)])]);
                     (Binary(Var "x", Equal, Literal (Literal.Integer 2I)), [Assignment([Var "y"], [Literal (Literal.Integer 2I)])])], Option.None))
            
            testStmt "if with elseif and else" "if x == 1 then y = 1 elseif x == 2 then y = 2 else y = 3 end"
                (If([(Binary(Var "x", Equal, Literal (Literal.Integer 1I)), [Assignment([Var "y"], [Literal (Literal.Integer 1I)])]);
                     (Binary(Var "x", Equal, Literal (Literal.Integer 2I)), [Assignment([Var "y"], [Literal (Literal.Integer 2I)])])], 
                    Some [Assignment([Var "y"], [Literal (Literal.Integer 3I)])]))
            
            testStmt "while loop" "while x > 0 do x = x - 1 end"
                (While(Binary(Var "x", Greater, Literal (Literal.Integer 0I)), 
                       [Assignment([Var "x"], [Binary(Var "x", Subtract, Literal (Literal.Integer 1I))])]))
            
            testStmt "repeat until loop" "repeat x = x + 1 until x > 10"
                (Repeat([Assignment([Var "x"], [Binary(Var "x", Add, Literal (Literal.Integer 1I))])],
                        Binary(Var "x", Greater, Literal (Literal.Integer 10I))))
            
            testList "For Loops" [
                testStmt "numeric for basic" "for i = 1, 10 do end"
                    (NumericFor("i", Literal (Literal.Integer 1I), Literal (Literal.Integer 10I), Option.None, []))
                
                testStmt "numeric for with step" "for i = 1, 10, 2 do end"
                    (NumericFor("i", Literal (Literal.Integer 1I), Literal (Literal.Integer 10I), Some (Literal (Literal.Integer 2I)), []))
                
                testStmt "numeric for with body" "for i = 1, 10 do x = i end"
                    (NumericFor("i", Literal (Literal.Integer 1I), Literal (Literal.Integer 10I), Option.None, 
                               [Assignment([Var "x"], [Var "i"])]))
                
                testStmt "numeric for with function call" "for i = 1, 10 do print(i) end"
                    (NumericFor("i", Literal (Literal.Integer 1I), Literal (Literal.Integer 10I), Option.None, 
                               [FunctionCallStmt (FunctionCall(Var "print", [Var "i"]))]))
                
                testStmt "generic for single var" "for k in t do end"
                    (GenericFor([("k", FLua.Parser.Attribute.NoAttribute)], [Var "t"], []))
                
                testStmt "generic for multiple vars" "for k, v in t do end"
                    (GenericFor([("k", FLua.Parser.Attribute.NoAttribute); ("v", FLua.Parser.Attribute.NoAttribute)], 
                               [Var "t"], []))
                
                // testStmt "generic for with function call" "for k, v in pairs(t) do end"
                //     (GenericFor([("k", FLua.Parser.Attribute.NoAttribute); ("v", FLua.Parser.Attribute.NoAttribute)], 
                //                [FunctionCall(Var "pairs", [Var "t"])], []))
                
                testStmt "generic for with body" "for k, v in t do x = k end"
                    (GenericFor([("k", FLua.Parser.Attribute.NoAttribute); ("v", FLua.Parser.Attribute.NoAttribute)], 
                               [Var "t"], 
                               [Assignment([Var "x"], [Var "k"])]))
            ]

            testList "Labels and Goto" [
                testStmt "simple label" "::start::" (Label "start")
                testStmt "label with spaces" ":: end_label ::" (Label "end_label")
                testStmt "goto statement" "goto start" (Goto "start")
                testStmt "goto with underscore" "goto loop_end" (Goto "loop_end")
                testStmt "goto with numbers" "goto label1" (Goto "label1")
            ]

            testList "Function Definitions" [
                testStmt "simple function" "function test() end" 
                    (FunctionDefStmt(["test"], { Parameters = []; IsVararg = false; Body = [] }))
                
                testStmt "function with parameters" "function add(a, b) end"
                    (FunctionDefStmt(["add"], { Parameters = [Param ("a", FLua.Parser.Attribute.NoAttribute); Param ("b", FLua.Parser.Attribute.NoAttribute)]; IsVararg = false; Body = [] }))
                
                testStmt "function with vararg" "function print(...) end"
                    (FunctionDefStmt(["print"], { Parameters = [VarargParam]; IsVararg = true; Body = [] }))
                
                testStmt "method definition" "function obj.method(self) end"
                    (FunctionDefStmt(["obj"; "method"], { Parameters = [Param ("self", FLua.Parser.Attribute.NoAttribute)]; IsVararg = false; Body = [] }))
                
                testStmt "nested method" "function a.b.c.method() end"
                    (FunctionDefStmt(["a"; "b"; "c"; "method"], { Parameters = []; IsVararg = false; Body = [] }))
                
                testStmt "local function" "local function helper() end"
                    (LocalFunctionDef("helper", { Parameters = []; IsVararg = false; Body = [] }))
                
                testStmt "local function with params" "local function calc(x, y) return x * y end"
                    (LocalFunctionDef("calc", { Parameters = [Param ("x", FLua.Parser.Attribute.NoAttribute); Param ("y", FLua.Parser.Attribute.NoAttribute)]; IsVararg = false; Body = [Return (Some [Binary(Var "x", Multiply, Var "y")])] }))
            ]
        ]
        
        // TODO: Fix remaining tech debt
        // 1. Generic for with function calls: "for k, v in pairs(t) do end" 
        //    Issue: Parser conflict between pGenericFor and pNumericFor
        // 2. Function expressions with bodies: "function(x) return x + 1 end"
        //    Issue: Need different parser architecture for expression vs statement contexts
        
        testList "Integration Tests - Multiple Features" [
            testExpr "method call with table constructor" "obj:create({x = 1, y = 2})" 
                (MethodCall(Var "obj", "create", [TableConstructor [NamedField ("x", Literal (Literal.Integer 1I)); NamedField ("y", Literal (Literal.Integer 2I))]]))
            
            testExpr "table with method calls and functions" "{handler = obj:getHandler(), callback = function() end}" 
                (TableConstructor [NamedField ("handler", MethodCall(Var "obj", "getHandler", [])); NamedField ("callback", FunctionDef { Parameters = []; IsVararg = false; Body = [] })])
            
            testExpr "chained method calls with table access" "data.config:getValue().result" 
                (TableAccess(MethodCall(TableAccess(Var "data", Literal (Literal.String "config")), "getValue", []), Literal (Literal.String "result")))
            
            testStmt "multiple assignment with method calls" "a, b = obj:getValues(), data.field" 
                (Assignment([Var "a"; Var "b"], [MethodCall(Var "obj", "getValues", []); TableAccess(Var "data", Literal (Literal.String "field"))]))
            
            testStmt "return with method calls and table constructor" "return obj:process({input = x}), {status = \"ok\"}" 
                (Return (Some [MethodCall(Var "obj", "process", [TableConstructor [NamedField ("input", Var "x")]]); TableConstructor [NamedField ("status", Literal (Literal.String "ok"))]]))
            
            testStmt "local assignment with complex expressions" "local result, error = api:call({method = \"GET\"})" 
                (LocalAssignment([("result", FLua.Parser.Attribute.NoAttribute); ("error", FLua.Parser.Attribute.NoAttribute)], Some [MethodCall(Var "api", "call", [TableConstructor [NamedField ("method", Literal (Literal.String "GET"))]])]))
        ]
    ]
]

[<EntryPoint>]
let main args =
    runTestsWithCLIArgs [] args tests
