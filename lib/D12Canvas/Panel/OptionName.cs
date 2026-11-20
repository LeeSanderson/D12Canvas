namespace D12Canvas.Panel;

internal static class OptionName
{
    public static string For(string option) =>
        option.Length == 0 ? option : char.ToUpperInvariant(option[0]) + option[1..];
}
