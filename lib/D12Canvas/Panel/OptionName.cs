namespace D12Canvas.Panel;

// Options are stored as their raw values ("center", "Orthogonal"); a control that names an option
// to the user shows it with a leading capital.
internal static class OptionName
{
    public static string For(string option) =>
        option.Length == 0 ? option : char.ToUpperInvariant(option[0]) + option[1..];
}
