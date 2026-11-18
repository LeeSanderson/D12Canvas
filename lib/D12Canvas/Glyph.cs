namespace D12Canvas;

public enum GlyphPaint
{
    Stroke,
    Fill,
    AccentStroke,
    AccentFill,
}

/// <summary>
/// One SVG path of a <see cref="Glyph"/>, drawn on the 16-unit grid and painted as
/// <paramref name="Paint"/> says.
/// </summary>
public sealed record GlyphPart(string Data, GlyphPaint Paint = GlyphPaint.Stroke);

/// <summary>
/// A named drawing on the library's 16-unit grid, rendered by <see cref="D12Icon"/>. Build one
/// from path data to give a custom component type or option a drawing that fits the
/// <see cref="Glyphs"/> set: 1.5 stroke, round caps and joins, every coordinate inside the 1 to 15
/// box.
/// </summary>
public sealed record Glyph
{
    public Glyph(params IEnumerable<GlyphPart> parts)
    {
        ArgumentNullException.ThrowIfNull(parts);
        var copied = parts.ToArray();
        if (copied.Length == 0)
        {
            throw new ArgumentException("A glyph needs at least one part.", nameof(parts));
        }

        if (copied.Any(part => part is null || string.IsNullOrWhiteSpace(part.Data)))
        {
            throw new ArgumentException("Every glyph part needs path data.", nameof(parts));
        }

        Parts = Array.AsReadOnly(copied);
    }

    public IReadOnlyList<GlyphPart> Parts { get; }

    public bool Equals(Glyph? other) => other is not null && Parts.SequenceEqual(other.Parts);

    public override int GetHashCode() =>
        Parts.Aggregate(0, (hash, part) => HashCode.Combine(hash, part));
}
