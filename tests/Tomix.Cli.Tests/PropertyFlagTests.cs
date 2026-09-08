using System.CommandLine;
using Tomix.Cli.Commands;
using Tomix.Core.Models;

namespace Tomix.Cli.Tests;

/// <summary>
/// The long-form property flags (issue #146): `add --expression/--set name=value` and
/// `set --set name=value` replace the bare order-sensitive -q/-i pairs, which remain as
/// silent compatibility aliases. Malformed name=value fails at parse time; mixing the two
/// forms fails with a documented code before any model is opened.
/// </summary>
[Collection(ConsoleStateCollection.Name)]
public sealed class PropertyFlagTests
{
    // ── add ─────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("formatString=#,0")]
    [InlineData("description=my measure")]
    [InlineData("props.path.0=x=y")]
    public void AddSetAssignments_ParseNameValue(string raw)
    {
        var assignments = AddCommand.ParseSetAssignments([raw]);

        var assignment = Assert.Single(assignments);
        Assert.Equal(AddCommand.SplitSetName(raw), assignment.Property);
    }

    [Fact]
    public void AddSetAssignments_RepeatableAndOrdered()
    {
        var assignments = AddCommand.ParseSetAssignments(["formatString=$#,0", "description=Units sold"]);

        Assert.Collection(assignments,
            a => Assert.Equal("formatString", a.Property),
            a => Assert.Equal("Units sold", a.Value));
    }

    [Fact]
    public void AddSet_MalformedNameValue_FailsAtParseTime()
    {
        var parsed = BuildAddRoot().Parse(["add", "Sales/M", "-t", "Measure", "--set", "formatString"]);

        Assert.Contains(parsed.Errors, e => e.Message.Contains("name=value"));
    }

    [Fact]
    public void AddSet_ValidValueContainingEquals_Parses()
    {
        var parsed = BuildAddRoot().Parse(["add", "Sales/M", "-t", "Measure", "--set", "description=a=b"]);

        Assert.Empty(parsed.Errors);
    }

    [Fact]
    public void AddExpressionWithUnpairedI_Conflicts()
    {
        var captured = ConsoleCapture.Invoke(BuildAddRoot().Parse(
            ["add", "Sales/M", "-t", "Measure", "--expression", "1", "-i", "2"]));

        Assert.Equal(2, captured.ExitCode);
        // Text mode renders prose; the code is pinned by the JSON variant below.
        Assert.Contains("Pass either --expression", captured.Stderr);
    }

    [Fact]
    public void AddExpressionWithUnpairedI_ConflictCarriesCodeInJson()
    {
        var captured = ConsoleCapture.Invoke(BuildAddRoot().Parse(
            ["add", "Sales/M", "-t", "Measure", "--expression", "1", "-i", "2", "--error-format", "json"]));

        Assert.Equal(2, captured.ExitCode);
        Assert.Equal("TOMIX_ADD_INPUT_CONFLICT",
            System.Text.Json.JsonDocument.Parse(captured.Stderr).RootElement.GetProperty("code").GetString());
    }

    // ── set ─────────────────────────────────────────────────────────────────

    [Fact]
    public void SetSet_MalformedNameValue_FailsAtParseTime()
    {
        var parsed = BuildSetRoot().Parse(["set", "Sales/Amount", "--set", "expression"]);

        Assert.Contains(parsed.Errors, e => e.Message.Contains("name=value"));
    }

    [Fact]
    public void SetSet_ValidNameValue_Parses()
    {
        var parsed = BuildSetRoot().Parse(["set", "Sales/Amount", "--set", "expression=SUM(Sales[Amount])"]);

        Assert.Empty(parsed.Errors);
    }

    [Fact]
    public void SetSetWithCompatibilityQi_Conflicts()
    {
        var captured = ConsoleCapture.Invoke(BuildSetRoot().Parse(
            ["set", "Sales/Amount", "--set", "expression=1", "-q", "expression", "-i", "2"]));

        Assert.Equal(2, captured.ExitCode);
        Assert.Contains("Pass either --set", captured.Stderr);
    }

    [Fact]
    public void SetSetWithCompatibilityQi_ConflictCarriesCodeInJson()
    {
        var captured = ConsoleCapture.Invoke(BuildSetRoot().Parse(
            ["set", "Sales/Amount", "--set", "expression=1", "-q", "expression", "-i", "2", "--error-format", "json"]));

        Assert.Equal(2, captured.ExitCode);
        Assert.Equal("TOMIX_SET_INPUT_CONFLICT",
            System.Text.Json.JsonDocument.Parse(captured.Stderr).RootElement.GetProperty("code").GetString());
    }

    // ── -q freedom on get/query ─────────────────────────────────────────────

    [Fact]
    public void Get_LongQuery_Parses_ShortQIsUnknown()
    {
        // -q is being freed for the global --quiet standard once the add/set compat
        // aliases retire at 1.0; get/query keep only the long form.
        var services = TestServices.Create();
        var root = TestRoot.With(new GetCommand([], services.State).Build());

        Assert.Empty(root.Parse(["get", "Sales", "--query", "expression"]).Errors);
        Assert.NotEmpty(root.Parse(["get", "Sales", "-q", "expression"]).Errors);
    }

    [Fact]
    public void Query_LongQuery_Parses_ShortQIsUnknown()
    {
        var services = TestServices.Create();
        var root = TestRoot.With(new QueryCommand([], () => null).Build());

        Assert.Empty(root.Parse(["query", "--query", "EVALUATE Sales"]).Errors);
        Assert.NotEmpty(root.Parse(["query", "-q", "EVALUATE Sales"]).Errors);
    }

    private static RootCommand BuildAddRoot()
    {
        var services = TestServices.Create();
        return TestRoot.With(new AddCommand([], services.State, services.Mutations).Build());
    }

    private static RootCommand BuildSetRoot()
    {
        var services = TestServices.Create();
        return TestRoot.With(new SetCommand([], services.State, services.Mutations).Build());
    }
}
