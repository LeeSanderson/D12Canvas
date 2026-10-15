namespace D12Canvas.App.Shared.Suggestions;

// Screenshot is a data URL as the browser's FileReader produces it, or null when none was pasted.
public sealed record SuggestionSubmission(
    string Title,
    string? Details,
    string? Screenshot,
    string? PagePath
);

public sealed record SavedSuggestion(string Markdown, string? Screenshot);
