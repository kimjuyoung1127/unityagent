using Unityctl.Shared.Exec;
using Xunit;

namespace Unityctl.Shared.Tests;

public sealed class ExecExpressionParserTests
{
    [Fact]
    public void Parse_GetMember_ReturnsMemberExpression()
    {
        var parsed = ExecExpressionParser.Parse("UnityEditor.EditorApplication.isPlaying");

        Assert.Equal(ExecExpressionKind.GetMember, parsed.Kind);
        Assert.Equal("UnityEditor.EditorApplication", parsed.TypeName);
        Assert.Equal("isPlaying", parsed.MemberName);
        Assert.Empty(parsed.Arguments);
    }

    [Fact]
    public void Parse_SetMember_ReturnsRightHandSide()
    {
        var parsed = ExecExpressionParser.Parse("UnityEditor.EditorApplication.isPlaying = true");

        Assert.Equal(ExecExpressionKind.SetMember, parsed.Kind);
        Assert.Equal("true", parsed.RightHandSide);
    }

    [Fact]
    public void Parse_InvokeMethod_SupportsQuotedCommasAndJsonLiterals()
    {
        var parsed = ExecExpressionParser.Parse("My.Type.Run(\"hello, world\", {'x':1}, [1,2], false)");

        Assert.Equal(ExecExpressionKind.InvokeMethod, parsed.Kind);
        Assert.Equal(4, parsed.Arguments.Count);
        Assert.Equal("\"hello, world\"", parsed.Arguments[0]);
        Assert.Equal("{'x':1}", parsed.Arguments[1]);
        Assert.Equal("[1,2]", parsed.Arguments[2]);
        Assert.Equal("false", parsed.Arguments[3]);
    }

    [Fact]
    public void Parse_InvokeMethod_SupportsSingleQuotedStrings()
    {
        var parsed = ExecExpressionParser.Parse("My.Type.Run('hello, world', 'x(y)')");

        Assert.Equal(2, parsed.Arguments.Count);
        Assert.Equal("'hello, world'", parsed.Arguments[0]);
        Assert.Equal("'x(y)'", parsed.Arguments[1]);
    }

    [Fact]
    public void Parse_EmptyArgument_ThrowsWithPosition()
    {
        var ex = Assert.Throws<ExecExpressionParseException>(() => ExecExpressionParser.Parse("My.Type.Run(1, , 2)"));

        Assert.Contains("char", ex.Message);
    }

    [Fact]
    public void Parse_InvalidMemberPath_Throws()
    {
        Assert.Throws<ExecExpressionParseException>(() => ExecExpressionParser.Parse("DebugLog"));
    }
}
