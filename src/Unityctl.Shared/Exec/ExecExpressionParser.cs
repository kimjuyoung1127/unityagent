namespace Unityctl.Shared.Exec;

public enum ExecExpressionKind
{
    GetMember,
    SetMember,
    InvokeMethod
}

public sealed class ExecExpression
{
    public ExecExpressionKind Kind { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public string MemberName { get; set; } = string.Empty;
    public string? RightHandSide { get; set; }
    public IReadOnlyList<string> Arguments { get; set; } = Array.Empty<string>();
}

public sealed class ExecExpressionParseException : Exception
{
    public ExecExpressionParseException(string message, int position)
        : base($"Parse error at char {position + 1}: {message}")
    {
        Position = position;
    }

    public int Position { get; }
}

public static class ExecExpressionParser
{
    public static ExecExpression Parse(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
            throw new ExecExpressionParseException("expression must not be empty.", 0);

        var trimmed = expression.Trim();
        var assignmentIndex = FindTopLevelAssignment(trimmed);
        if (assignmentIndex >= 0)
        {
            var lhs = trimmed[..assignmentIndex].Trim();
            var rhs = trimmed[(assignmentIndex + 1)..].Trim();
            if (string.IsNullOrWhiteSpace(lhs))
                throw new ExecExpressionParseException("expected a member path before '='.", assignmentIndex);
            if (string.IsNullOrWhiteSpace(rhs))
                throw new ExecExpressionParseException("expected a value after '='.", assignmentIndex + 1);

            var (typeName, memberName) = SplitTypeMember(lhs);
            return new ExecExpression
            {
                Kind = ExecExpressionKind.SetMember,
                TypeName = typeName,
                MemberName = memberName,
                RightHandSide = rhs
            };
        }

        var invokeOpenIndex = FindInvocationOpenParen(trimmed);
        if (invokeOpenIndex >= 0)
        {
            var target = trimmed[..invokeOpenIndex].Trim();
            var argsSegment = trimmed[(invokeOpenIndex + 1)..^1];
            var (typeName, memberName) = SplitTypeMember(target);
            return new ExecExpression
            {
                Kind = ExecExpressionKind.InvokeMethod,
                TypeName = typeName,
                MemberName = memberName,
                Arguments = SplitArguments(argsSegment)
            };
        }

        var (getTypeName, getMemberName) = SplitTypeMember(trimmed);
        return new ExecExpression
        {
            Kind = ExecExpressionKind.GetMember,
            TypeName = getTypeName,
            MemberName = getMemberName
        };
    }

    private static (string TypeName, string MemberName) SplitTypeMember(string value)
    {
        var lastDot = value.LastIndexOf('.');
        if (lastDot <= 0 || lastDot == value.Length - 1)
            throw new ExecExpressionParseException("expected 'TypeName.MemberName'.", Math.Max(lastDot, 0));

        var typeName = value[..lastDot].Trim();
        var memberName = value[(lastDot + 1)..].Trim();
        if (string.IsNullOrWhiteSpace(typeName) || string.IsNullOrWhiteSpace(memberName))
            throw new ExecExpressionParseException("expected 'TypeName.MemberName'.", Math.Max(lastDot, 0));

        return (typeName, memberName);
    }

    private static int FindTopLevelAssignment(string expression)
    {
        var state = new ParseState();
        for (var i = 0; i < expression.Length; i++)
        {
            var current = expression[i];
            var next = i + 1 < expression.Length ? expression[i + 1] : '\0';
            state.Advance(current);
            if (!state.IsTopLevel)
                continue;

            if (current != '=')
                continue;

            if (next == '=' || next == '>')
                continue;

            var previous = i > 0 ? expression[i - 1] : '\0';
            if (previous is '=' or '!' or '<' or '>')
                continue;

            return i;
        }

        return -1;
    }

    private static int FindInvocationOpenParen(string expression)
    {
        if (expression.Length < 3 || expression[^1] != ')')
            return -1;

        var state = new ParseState();
        for (var i = 0; i < expression.Length; i++)
        {
            var current = expression[i];
            if (i == expression.Length - 1 && current == ')' && state.ParenDepth == 1 && !state.InString)
                return state.LastTopLevelOpenParenIndex;

            state.Advance(current, trackInvocationParen: true, index: i);
        }

        if (state.InString || state.ParenDepth != 0 || state.BracketDepth != 0 || state.BraceDepth != 0)
            throw new ExecExpressionParseException("unterminated string or bracketed expression.", expression.Length - 1);

        return -1;
    }

    private static IReadOnlyList<string> SplitArguments(string arguments)
    {
        if (string.IsNullOrWhiteSpace(arguments))
            return Array.Empty<string>();

        var parts = new List<string>();
        var start = 0;
        var state = new ParseState();
        for (var i = 0; i < arguments.Length; i++)
        {
            var current = arguments[i];
            state.Advance(current);
            if (!state.IsTopLevel || current != ',')
                continue;

            parts.Add(TrimArgument(arguments[start..i], i));
            start = i + 1;
        }

        if (state.InString || state.ParenDepth != 0 || state.BracketDepth != 0 || state.BraceDepth != 0)
            throw new ExecExpressionParseException("unterminated string or bracketed argument.", arguments.Length - 1);

        parts.Add(TrimArgument(arguments[start..], arguments.Length - 1));
        return parts;
    }

    private static string TrimArgument(string value, int position)
    {
        var trimmed = value.Trim();
        if (trimmed.Length == 0)
            throw new ExecExpressionParseException("empty arguments are not allowed.", position);

        return trimmed;
    }

    private sealed class ParseState
    {
        private char _stringDelimiter;
        private bool _escaped;

        public bool InString => _stringDelimiter != '\0';
        public int ParenDepth { get; private set; }
        public int BracketDepth { get; private set; }
        public int BraceDepth { get; private set; }
        public int LastTopLevelOpenParenIndex { get; private set; } = -1;
        public bool IsTopLevel => !InString && ParenDepth == 0 && BracketDepth == 0 && BraceDepth == 0;

        public void Advance(char current, bool trackInvocationParen = false, int index = -1)
        {
            if (InString)
            {
                if (_escaped)
                {
                    _escaped = false;
                    return;
                }

                if (current == '\\')
                {
                    _escaped = true;
                    return;
                }

                if (current == _stringDelimiter)
                    _stringDelimiter = '\0';

                return;
            }

            if (current is '"' or '\'')
            {
                _stringDelimiter = current;
                return;
            }

            switch (current)
            {
                case '(':
                    if (trackInvocationParen && ParenDepth == 0 && BracketDepth == 0 && BraceDepth == 0)
                        LastTopLevelOpenParenIndex = index;
                    ParenDepth++;
                    break;
                case ')':
                    ParenDepth = Math.Max(0, ParenDepth - 1);
                    break;
                case '[':
                    BracketDepth++;
                    break;
                case ']':
                    BracketDepth = Math.Max(0, BracketDepth - 1);
                    break;
                case '{':
                    BraceDepth++;
                    break;
                case '}':
                    BraceDepth = Math.Max(0, BraceDepth - 1);
                    break;
            }
        }
    }
}
