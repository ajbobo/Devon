using Devon.Models;

namespace Devon.Services;

/// <summary>
/// Parses and evaluates condition expressions built from function calls and combinators
/// </summary>
public class ConditionEvaluator : IConditionEvaluator
{
    public ConditionEvaluator()
    {
    }

    public bool Evaluate(string expression, GameState state)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return true; // Empty condition is true

        var parser = new Parser(expression);
        parser.NextToken(); // Advance to first token
        var node = ParseCondition(parser);
        return node.Evaluate(state);
    }

    #region Parser Implementation

    private abstract class ConditionNode
    {
        public abstract bool Evaluate(GameState state);
    }

    private class FunctionNode : ConditionNode
    {
        private readonly string _functionName;
        private readonly List<ConditionNode> _args;
        private readonly string? _stringArg; // For HasItem, HasCondition, RoomHasCondition

        public FunctionNode(string functionName, List<ConditionNode>? args, string? stringArg = null)
        {
            _functionName = functionName;
            _args = args ?? new List<ConditionNode>();
            _stringArg = stringArg;
        }

        public override bool Evaluate(GameState state)
        {
            return _functionName switch
            {
                "HasItem" => _stringArg != null && state.Player.HasItem(_stringArg),
                "HasCondition" or "PlayerHasCondition" => _stringArg != null && state.Player.HasCondition(_stringArg),
                "RoomHasCondition" => _stringArg != null && state.CurrentRoom != null && state.CurrentRoom.Conditions.Contains(_stringArg),
                // Combinators: these evaluate their condition arguments (only one arg for Not, two+ for And/Or)
                "Not" => _args.Count == 1 && !_args[0].Evaluate(state),
                "And" => _args.All(arg => arg.Evaluate(state)),
                "Or" => _args.Any(arg => arg.Evaluate(state)),
                _ => throw new InvalidOperationException($"Unknown condition function: {_functionName}")
            };
        }
    }

    private class LiteralNode : ConditionNode
    {
        private readonly bool _value;
        public LiteralNode(bool value) => _value = value;
        public override bool Evaluate(GameState state) => _value;
    }

    private static ConditionNode ParseCondition(Parser parser)
    {
        // parser.NextToken() removed; caller ensures current token is set

        if (parser.Token.Type == TokenType.Identifier)
        {
            var funcName = parser.Token.Value!;
            parser.NextToken();
            if (parser.Token.Type != TokenType.LeftParen)
            {
                throw new InvalidOperationException($"Expected '(' after function name {funcName}");
            }
            parser.NextToken();

            var args = new List<ConditionNode>();
            string? stringArg = null;

            // Check if the function expects a single string argument (the built-in checks)
            bool expectsStringArg = funcName is "HasItem" or "HasCondition" or "PlayerHasCondition" or "RoomHasCondition";

            if (expectsStringArg)
            {
                // Accept a string argument that may contain spaces.
                // Read all tokens until the closing parenthesis and concatenate them with spaces.
                var parts = new List<string>();
                while (parser.Token.Type != TokenType.RightParen)
                {
                    if (parser.Token.Type == TokenType.End)
                        throw new InvalidOperationException($"Unexpected end of input in argument to {funcName}");
                    if (parser.Token.Type == TokenType.String)
                    {
                        parts.Add(parser.Token.Value ?? "");
                    }
                    else if (parser.Token.Type == TokenType.Identifier)
                    {
                        parts.Add(parser.Token.Value ?? "");
                    }
                    else
                    {
                        throw new InvalidOperationException($"Unexpected token {parser.Token.Type} in argument to {funcName}");
                    }
                    parser.NextToken();
                }
                stringArg = string.Join(" ", parts);
            }
            else
            {
                // Parse comma-separated condition arguments (for Not, And, Or)
                if (parser.Token.Type != TokenType.RightParen)
                {
                    do
                    {
                        args.Add(ParseCondition(parser));
                        if (parser.Token.Type == TokenType.Comma)
                            parser.NextToken();
                        else
                            break;
                    } while (true);
                }
            }

            if (parser.Token.Type != TokenType.RightParen)
                throw new InvalidOperationException($"Expected ')' after arguments to {funcName}");
            parser.NextToken();

            return new FunctionNode(funcName, args, stringArg);
        }
        else if (parser.Token.Type == TokenType.Exclamation)
        {
            parser.NextToken();
            var arg = ParseCondition(parser);
            return new FunctionNode("Not", new List<ConditionNode> { arg });
        }
        else if (parser.Token.Type == TokenType.True)
        {
            parser.NextToken();
            return new LiteralNode(true);
        }
        else if (parser.Token.Type == TokenType.False)
        {
            parser.NextToken();
            return new LiteralNode(false);
        }
        else
        {
            throw new InvalidOperationException($"Unexpected token: {parser.Token.Type} '{parser.Token.Value}'");
        }
    }

    #endregion

    #region Simple Lexer

    private enum TokenType
    {
        Identifier,
        String,
        Number,
        LeftParen,
        RightParen,
        Comma,
        Exclamation,
        True,
        False,
        End
    }

    private class Token
    {
        public TokenType Type { get; set; }
        public string? Value { get; set; }
    }

    private class Parser
    {
        private readonly string _input;
        private int _pos;
        public Token Token { get; private set; } = new();

        public Parser(string input)
        {
            _input = input;
            // Do not call NextToken() here; ParseCondition will call it
        }

        public void NextToken()
        {
            SkipWhitespace();

            if (_pos >= _input.Length)
            {
                Token = new Token { Type = TokenType.End };
                return;
            }

            char ch = _input[_pos];

            // Single-character tokens
            switch (ch)
            {
                case '(':
                    _pos++;
                    Token = new Token { Type = TokenType.LeftParen };
                    return;
                case ')':
                    _pos++;
                    Token = new Token { Type = TokenType.RightParen };
                    return;
                case ',':
                    _pos++;
                    Token = new Token { Type = TokenType.Comma };
                    return;
                case '!':
                    _pos++;
                    Token = new Token { Type = TokenType.Exclamation };
                    return;
                case '"':
                    // String literal
                    var start = _pos;
                    _pos++;
                    while (_pos < _input.Length && _input[_pos] != '"')
                        _pos++;
                    if (_pos >= _input.Length)
                        throw new InvalidOperationException("Unterminated string literal");
                    var end = _pos; // position of closing quote
                    _pos++; // skip closing quote
                    var length = end - start - 1; // number of characters inside quotes
                    var str = _input.Substring(start + 1, length);
                    Token = new Token { Type = TokenType.String, Value = str };
                    return;
            }

            // Identifier or boolean literal
            var sb = new System.Text.StringBuilder();
            while (_pos < _input.Length && !char.IsWhiteSpace(_input[_pos]) && _input[_pos] is not '(' and not ')' and not ',' and not '!')
            {
                sb.Append(_input[_pos]);
                _pos++;
            }
            var tokenValue = sb.ToString();

            if (tokenValue == "true")
            {
                Token = new Token { Type = TokenType.True };
            }
            else if (tokenValue == "false")
            {
                Token = new Token { Type = TokenType.False };
            }
            else
            {
                Token = new Token { Type = TokenType.Identifier, Value = tokenValue };
            }
        }

        private void SkipWhitespace()
        {
            while (_pos < _input.Length && char.IsWhiteSpace(_input[_pos]))
                _pos++;
        }
    }

    #endregion
}
