namespace D12Canvas.App.Shared.Suggestions;

public sealed record SuggestionSubmission(
    string Title,
    string? Details,
    string? Screenshot,
    string? PagePath
);

public sealed record SavedSuggestion(string Markdown, string? Screenshot);
