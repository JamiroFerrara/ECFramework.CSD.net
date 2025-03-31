using System;
using System.Collections.Generic;
using System.Text;

namespace ECFramework;

public enum TokenType
{
    And, Or, Not, Contains, Equals, Minor, Major, Equal, Mineq, Majeq, Identifier
}

public class Token
{
    public TokenType Type { get; }
    public string Value { get; }

    public Token(TokenType type, string value)
    {
        Type = type;
        Value = value;
    }

    public override string ToString() => $"{Type}: \"{Value}\"";
}

public class ExpressionTokenizer
{
    private static readonly Dictionary<string, TokenType> Operators = new()
    {
        ["|"] = TokenType.Or,
        ["&"] = TokenType.And,
        ["!"] = TokenType.Not,
        ["%"] = TokenType.Contains,
        ["<"] = TokenType.Minor,
        [">"] = TokenType.Major,
        ["="] = TokenType.Equal,
        ["<="] = TokenType.Mineq,
        [">="] = TokenType.Majeq
    };

    public static List<Token> Tokenize(string input)
    {
        List<Token> tokens = new();
        StringBuilder current = new();
        bool inIdentifier = false;
        bool escapeNext = false;

        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];

            if (escapeNext) // Handle escape sequences
            {
                current.Append(c);
                escapeNext = false;
                continue;
            }

            if (c == '\\') // Escape character detected
            {
                escapeNext = true;
                continue;
            }

            if (char.IsWhiteSpace(c))
            {
                if (inIdentifier) // Keep spaces inside identifiers
                    current.Append(c);
                continue; // Ignore whitespace otherwise
            }

            string op = c.ToString();

            if (i + 1 < input.Length)
            {
                string twoCharOp = op + input[i + 1];
                if (Operators.ContainsKey(twoCharOp))
                {
                    if (current.Length > 0)
                    {
                        tokens.Add(new Token(TokenType.Identifier, current.ToString().Trim()));
                        current.Clear();
                        inIdentifier = false;
                    }
                    tokens.Add(new Token(Operators[twoCharOp], twoCharOp));
                    i++; // Skip next char
                    continue;
                }
            }

            if (Operators.ContainsKey(op))
            {
                if (current.Length > 0)
                {
                    tokens.Add(new Token(TokenType.Identifier, current.ToString().Trim()));
                    current.Clear();
                    inIdentifier = false;
                }
                tokens.Add(new Token(Operators[op], op));
                continue;
            }

            if (!inIdentifier)
            {
                inIdentifier = true;
            }
            current.Append(c);
        }

        if (current.Length > 0)
        {
            tokens.Add(new Token(TokenType.Identifier, current.ToString().Trim()));
        }

        return tokens;
    }
}
