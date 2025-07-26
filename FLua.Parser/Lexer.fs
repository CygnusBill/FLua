module FLua.Parser.Lexer

open FParsec
open System.Numerics

// Helper for identifier characters
let isIdentifierChar c = isLetter c || isDigit c || c = '_'

// Token type for the lexer
type Token =
    | Identifier of string
    | Number of float
    | Integer of BigInteger
    | String of string
    | Keyword of string
    | Symbol of string
    | Whitespace
    | Comment of string
    | Eof

// Token with position
type TokenWithPos =
    { Token: Token
      Line: int
      Column: int }

// List of Lua keywords
let luaKeywords =
    [
        "and"; "break"; "do"; "else"; "elseif"; "end"; "false"; "for"; "function"; "goto";
        "if"; "in"; "local"; "nil"; "not"; "or"; "repeat"; "return"; "then"; "true"; "until"; "while"
    ]

// Parser for keywords
let pKeyword: Parser<Token, unit> =
    luaKeywords
    |> List.map (fun kw ->
        attempt (
            pstring kw >>? notFollowedBy (satisfy isIdentifierChar)
            |>> fun _ -> Keyword kw
        ))
    |> choice

// Parser for shebang line (#!... to end of line)
let pShebang: Parser<Token, unit> =
    attempt (pstring "#!" >>. skipRestOfLine true)
    |>> fun _ -> Whitespace

// Parser for single-line comments (--... to end of line)
let pSingleLineComment: Parser<Token, unit> =
    attempt (pstring "--" >>. manyCharsTill anyChar (eof <|> (newline >>% ())))
    |>> Comment

// Parser for multi-line comments (--[[ ... ]] and --[=[ ... ]=])
let pMultiLineComment: Parser<Token, unit> =
    let openBracket =
        pstring "--[" >>. manyChars (pchar '=') .>> pchar '['
    let closeBracket n =
        pstring "]" >>. pstring (String.replicate n "=") >>. pstring "]"
    let content n =
        manyCharsTill anyChar (attempt (closeBracket n))
    attempt (
        openBracket >>= fun eqs ->
            let n = eqs.Length in
            content n |>> Comment
    )

// Parser for angle-bracketed attributes (e.g., <const>)
let pAngleAttribute: Parser<Token, unit> =
    attempt (between (pchar '<') (pchar '>') (manyChars (noneOf ">\n\r")))
    |>> fun _ -> Whitespace

// Parser for whitespace (spaces, tabs, newlines)
let pWhitespace: Parser<Token, unit> =
    skipMany1 (anyOf " \t\r\n")
    |>> fun _ -> Whitespace

// Parser for identifiers (not keywords)
let isIdentifierFirstChar c = isLetter c || c = '_'
let pIdentifier: Parser<Token, unit> =
    many1Satisfy2L isIdentifierFirstChar isIdentifierChar "identifier"
    |>> Identifier

// Parser for numbers (integer or float, including hex with binary exponent)
let pNumber: Parser<Token, unit> =
    // Hexadecimal float with optional binary exponent: 0x1.5p-3
    let pHexFloatWithExponent =
        attempt (
            pstringCI "0x" >>. 
            many1Satisfy isHex .>>. 
            opt (pchar '.' >>. manySatisfy isHex) .>>.
            opt (anyOf "pP" >>. opt (anyOf "+-") .>>. many1Satisfy isDigit)
        )
        |>> fun ((intPart, fracPart), expPart) ->
            // Parse integer part
            let intVal = BigInteger.Parse(intPart, System.Globalization.NumberStyles.AllowHexSpecifier) |> float
            
            // Parse fractional part if present
            let fracVal =
                match fracPart with
                | Some (frac) when frac <> "" ->
                    frac
                    |> Seq.mapi (fun i c -> float (System.Convert.ToInt32(string c, 16)) / (16. ** float (i + 1)))
                    |> Seq.sum
                | _ -> 0.0
            
            let mantissa = intVal + fracVal
            
            // Parse binary exponent if present
            let finalValue =
                match expPart with
                | Some (sign, exp) ->
                    let expValue = int exp
                    let expSign = match sign with | Some '-' -> -1 | _ -> 1
                    mantissa * (2.0 ** float (expSign * expValue))
                | None -> mantissa
            
            Number finalValue
    
    // Regular hexadecimal integer: 0x123
    let pHex =
        attempt (
            pstringCI "0x" >>. many1Satisfy isHex .>> 
            notFollowedBy (anyOf "pP.")  // Make sure it's not a float
        )
        |>> (fun s -> 
            let value = BigInteger.Parse(s, System.Globalization.NumberStyles.HexNumber)
            Integer value)
    
    // Regular decimal float: 123.456
    let pFloat = 
        attempt (
            notFollowedBy (pchar '-') >>. 
            many1Satisfy isDigit .>>. pchar '.' .>>. many1Satisfy isDigit
        )
        |>> (fun ((intPart, _), fracPart) -> 
            let floatStr = intPart + "." + fracPart
            let n = float floatStr
            Number n)
    
    // Regular decimal integer: 123
    let pInt =
        notFollowedBy (pchar '-') >>.
        many1Satisfy isDigit |>> (fun s -> Integer(BigInteger.Parse(s)))
    
    // Try hex float with exponent first, then hex int, then decimal float, then decimal int
    let pNum = attempt pHexFloatWithExponent <|> attempt pHex <|> attempt pFloat <|> pInt
    pNum

// Parser for strings (single, double, or long bracket, with basic escapes and line continuation)
let pString: Parser<Token, unit> =
    let lineContinuation =
        attempt (pchar '\\' .>> followedBy newline >>. (newline <|> (pchar '\r' >>. opt (pchar '\n') >>% '\n')))
        >>% ""
    let decimalEscape =
        attempt (pchar '\\' >>. followedBy (satisfy isDigit) >>. manyMinMaxSatisfyL 1 3 isDigit "decimal escape")
        |>> (fun digits -> 
            let value = int digits
            if value > 255 then failwithf "Decimal escape out of range: \\%s" digits
            else string (char value)
        )
    let hexEscape =
        attempt (pstring "\\x" >>. manyMinMaxSatisfyL 2 2 isHex "hex escape")
        |>> (fun hex ->
            let value = System.Convert.ToInt32(hex, 16)
            if value > 255 then failwithf "Hex escape out of range: \\x%s" hex
            else string (char value)
        )
    let unicodeEscape =
        attempt (pstring "\\u{" >>. many1Satisfy isHex .>> pchar '}')
        >>= fun hex ->
            let value = System.Convert.ToInt32(hex, 16)
            if value < 0x0 || value > 0x10FFFF || (value >= 0xD800 && value <= 0xDFFF) then
                fail $"Unicode escape out of range: \\u{{%s{hex}}}"
            else
                preturn (System.Char.ConvertFromUtf32(value))
    let zEscape =
        attempt (pstring "\\z" >>. skipMany (satisfy System.Char.IsWhiteSpace))
        >>% ""
    let escape =
        zEscape
        <|> unicodeEscape
        <|> hexEscape
        <|> decimalEscape
        <|> (pchar '\\' >>.
            (anyOf "\\\"'abfnrtv" |>> function
                | '\\' -> "\\"
                | '"' -> "\""
                | '\'' -> "'"
                | 'a' -> "\a"
                | 'b' -> "\b"
                | 'f' -> "\f"
                | 'n' -> "\n"
                | 'r' -> "\r"
                | 't' -> "\t"
                | 'v' -> "\v"
                | c -> string c))
    let normalChar quote = noneOf (string quote + "\\") |>> string
    let quotedString quote =
        between (pchar quote) (pchar quote)
            (manyStrings (
                lineContinuation
                <|> escape
                <|> normalChar quote
            ))
    // Long bracket string: [=*[ ... ]=*]
    let longBracketString : Parser<string, unit> =
        let openBracket =
            pchar '[' >>. manyChars (pchar '=') .>> pchar '['
        let closeBracket n =
            pchar ']' >>. skipString (String.replicate n "=") >>. pchar ']'
        let content n =
            manyCharsTill anyChar (attempt (closeBracket n))
        attempt (
            openBracket >>= fun eqs ->
                let n = eqs.Length in
                content n
        )
    let pShortString = quotedString '"' <|> quotedString '\''
    let pLongString = longBracketString
    let pAnyString = attempt pLongString <|> pShortString
    pAnyString |>> String

// List of Lua symbols (longest first for greedy matching)
let luaSymbols =
    [
        "..."; "=="; "~="; "<="; ">="; "//"; "::"; ".."; "<<"; ">>";
        "+"; "-"; "*"; "/"; "%"; "^"; "#"; "&"; "|"; "~"; "<"; ">"; "=";
        "("; ")"; "{"; "}"; "["; "]"; ":"; ";"; ","; "."
    ]

// Parser for symbols
let pSymbol: Parser<Token, unit> =
    luaSymbols
    |> List.map (fun sym ->
        attempt (pstring sym)
        |>> fun _ -> Symbol sym)
    |> choice

// Parser for comments (now properly implemented)
let pComment: Parser<Token, unit> =
    choice [
        attempt pMultiLineComment
        pSingleLineComment
    ] 