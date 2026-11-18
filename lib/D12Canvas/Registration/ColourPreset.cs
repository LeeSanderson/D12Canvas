namespace D12Canvas.Registration;

// One swatch in the Property bar's colour flyout. Name is the swatch's accessible name; Hex is
// "#RRGGBB", the only form the native picker behind "Custom…" accepts.
public sealed record ColourPreset(string Name, string Hex);
