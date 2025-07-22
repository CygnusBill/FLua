module FLua.Parser.Lexer

open FParsec

// Helper for identifier characters
let isIdentifierChar c = isLetter c || isDigit c || c = '_'

// Token type for the lexer
type Token =
    | Identifier of string
    | Number of float
    | Integer of int64
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
let pKeyword: Parser<TokenWithPos, unit> =
    luaKeywords
    |> List.map (fun kw ->
        attempt (
            getPosition .>>. (pstring kw >>? notFollowedBy (satisfy isIdentifierChar))
            |>> fun (pos, _) -> { Token = Keyword kw; Line = int pos.Line; Column = int pos.Column }
        ))
    |> choice

// Parser for shebang line (#!... to end of line)
let pShebang: Parser<TokenWithPos, unit> =
    getPosition .>>. (attempt (pstring "#!" >>. skipRestOfLine true))
    |>> fun (pos, _) -> { Token = Whitespace; Line = int pos.Line; Column = int pos.Column }

// Parser for single-line comments (--... to end of line)
let pSingleLineComment: Parser<TokenWithPos, unit> =
    getPosition .>>. (attempt (pstring "--" >>. skipRestOfLine true))
    |>> fun (pos, _) -> { Token = Whitespace; Line = int pos.Line; Column = int pos.Column }

// Parser for angle-bracketed attributes (e.g., <const>)
let pAngleAttribute: Parser<TokenWithPos, unit> =
    getPosition .>>. (attempt (between (pchar '<') (pchar '>') (manyChars (noneOf ">\n\r"))))
    |>> fun (pos, _) -> { Token = Whitespace; Line = int pos.Line; Column = int pos.Column }

// Parser for whitespace (spaces, tabs, newlines)
let pWhitespace: Parser<TokenWithPos, unit> =
    getPosition .>>. skipMany1 (anyOf " \t\r\n")
    |>> fun (pos, _) -> { Token = Whitespace; Line = int pos.Line; Column = int pos.Column }

// Parser for identifiers (not keywords)
let isIdentifierFirstChar c = isLetter c || c = '_'
let pIdentifier: Parser<TokenWithPos, unit> =
    getPosition .>>. many1Satisfy2L isIdentifierFirstChar isIdentifierChar "identifier"
    |>> fun (pos, s) -> { Token = Identifier s; Line = int pos.Line; Column = int pos.Column }

// Parser for numbers (integer or float, including hex)
let pNumber: Parser<TokenWithPos, unit> =
    let pHex =
        attempt (pstringCI "0x" >>. many1Satisfy isHex) |>> (fun s -> Integer(System.Convert.ToInt64(s, 16)))
    let pInt = pint64 |>> Integer
    let pFloat = pfloat |>> Number
    let pNum = attempt pHex <|> attempt pFloat <|> pInt
    getPosition .>>. pNum
    |>> fun (pos, t) -> { Token = t; Line = int pos.Line; Column = int pos.Column }

// Parser for strings (single, double, or long bracket, with basic escapes and line continuation)
let pString: Parser<TokenWithPos, unit> =
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
    let escape =
        decimalEscape
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
    let pShortString = quotedString '"' <|> quotedString '\''
    // TODO: Add support for long bracket strings
    getPosition .>>. pShortString
    |>> fun (pos, s) -> { Token = String s; Line = int pos.Line; Column = int pos.Column }

// List of Lua symbols (longest first for greedy matching)
let luaSymbols =
    [
        "..."; "=="; "~="; "<="; ">="; "//"; "::"; ".."; "<<"; ">>";
        "+"; "-"; "*"; "/"; "%"; "^"; "#"; "&"; "|"; "~"; "<"; ">"; "=";
        "("; ")"; "{"; "}"; "["; "]"; ":"; ";"; ","; "."
    ]

// Parser for symbols
let pSymbol: Parser<TokenWithPos, unit> =
    luaSymbols
    |> List.map (fun sym ->
        attempt (getPosition .>>. pstring sym)
        |>> fun (pos, _) -> { Token = Symbol sym; Line = int pos.Line; Column = int pos.Column })
    |> choice

// Parser for comments (stub)
let pComment: Parser<TokenWithPos, unit> =
    fail "Comment parser not implemented" 