namespace Tomix.Core.Dax;

/// <summary>What a run of DAX source text is, for syntax highlighting.</summary>
public enum DaxTextClassification
{
    Text,
    Keyword,
    Function,
    StringLiteral,
    Number,
    Comment,
    TableName,
    ColumnReference,
    Variable,
    QueryParameter,
    Parenthesis,
    DefinitionName,
    Operator,
    Punctuation,
}

/// <summary>A run of DAX source text and its classification. Spans are ordered and never overlap.</summary>
public readonly record struct DaxClassifiedSpan(
    int Start,
    int Length,
    DaxTextClassification Classification);

/// <summary>
/// The public surface of the DAX language engine: syntax-aware classification of DAX source for
/// highlighting. The classification comes from a full tokenize-and-parse pass, which is why a
/// string containing "--", a multi-line comment, or a variable named after a function are all
/// classified correctly.
/// </summary>
public static class DaxLanguage
{
    public static IReadOnlyList<DaxClassifiedSpan> Classify(string dax)
        => Engine.DaxClassifier.Classify(dax)
            .Select(span => new DaxClassifiedSpan(
                span.Start,
                span.Length,
                Map(span.Kind)))
            .ToArray();

    private static DaxTextClassification Map(Engine.DaxClassification classification) =>
        classification switch
        {
            Engine.DaxClassification.Keyword => DaxTextClassification.Keyword,
            Engine.DaxClassification.Function => DaxTextClassification.Function,
            Engine.DaxClassification.StringLiteral => DaxTextClassification.StringLiteral,
            Engine.DaxClassification.Number => DaxTextClassification.Number,
            Engine.DaxClassification.Comment => DaxTextClassification.Comment,
            Engine.DaxClassification.TableName => DaxTextClassification.TableName,
            Engine.DaxClassification.ColumnReference => DaxTextClassification.ColumnReference,
            Engine.DaxClassification.Variable => DaxTextClassification.Variable,
            Engine.DaxClassification.QueryParameter => DaxTextClassification.QueryParameter,
            Engine.DaxClassification.Parenthesis => DaxTextClassification.Parenthesis,
            Engine.DaxClassification.DefinitionName => DaxTextClassification.DefinitionName,
            Engine.DaxClassification.Operator => DaxTextClassification.Operator,
            Engine.DaxClassification.Punctuation => DaxTextClassification.Punctuation,
            _ => DaxTextClassification.Text,
        };
}
