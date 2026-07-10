using System;
using System.Collections.Generic;

//   [0]: {Token}
//     Type: And
//     Value: "&"
//   [1]: {Token}
//     Type: Identifier
//     Value: "and"
//   [2]: {Token}
//     Type: Not
//     Value: "!"
//   [3]: {Token}
//     Type: Identifier
//     Value: "not"
//   [4]: {Token}
//     Type: Contains
//     Value: "%"
//   [5]: {Token}
//     Type: Identifier
//     Value: "like"
//   [6]: {Token}
//     Type: Or
//     Value: "|"
//   [7]: {Token}
//     Type: Identifier
//     Value: "or"

class ExpressionParser
{
    private readonly List<Token> _tokens;
    private int _position = 0;

    public ExpressionParser(List<Token> tokens) => _tokens = tokens;

    private Token Current => _position < _tokens.Count ? _tokens[_position] : null;

    private Token Consume()
    {
        var token = Current;
        _position++;
        return token;
    }

    public List<ExpressionNode> ParseExpression()
    {
        List<ExpressionNode> nodes = new();
        while (Current != null)
        {
            var node = ParseUnary();
            if (node != null)
                nodes.Add(node);
        }
        return nodes;
    }

    private ExpressionNode ParseUnary()
    {
        var token = Consume();
        if (token == null) return null;

        if (token.Type is TokenType.Or or TokenType.And or TokenType.Not or TokenType.Contains)
        {
            var operand = ParseUnary();
            if (operand == null) return null; // trailing operator with no operand — discard
            return new UnaryExpression(token.Type, operand);
        }

        return ParsePrimary(token);
    }

    private ExpressionNode ParsePrimary(Token token)
    {
        return token.Type switch
        {
            TokenType.Identifier => new IdentifierExpression(token.Value),
            _ => throw new Exception($"Unexpected token: {Current}")
        };
    }
}

public abstract class ExpressionNode { }

public class IdentifierExpression : ExpressionNode
{
    public object Value { get; }
    public IdentifierExpression(string value)
    {
        Value = value;
    }
    public override string ToString() => $"Identifier({Value})";
}

public class UnaryExpression : ExpressionNode
{
    public TokenType Operator { get; }
    public ExpressionNode Operand { get; }

    public object GetValue() {
        if (Operand is IdentifierExpression identifier)
            return identifier.Value;
        if (Operand is UnaryExpression unaryExpression)
            return unaryExpression.GetValue();
        return null;
    }

    public UnaryExpression(TokenType op, ExpressionNode operand)
    {
        Operator = op;
        Operand = operand;
    }

    public override string ToString() => $"Unary({Operator}, {Operand})";
}

// public class BinaryExpression : ExpressionNode
// {
//     public ExpressionNode Left { get; }
//     public TokenType Operator { get; }
//     public ExpressionNode Right { get; }
//
//     public BinaryExpression(ExpressionNode left, TokenType op, ExpressionNode right)
//     {
//         Left = left;
//         Operator = op;
//         Right = right;
//     }
//
//     public override string ToString() => $"Binary({Left}, {Operator}, {Right})";
// }

