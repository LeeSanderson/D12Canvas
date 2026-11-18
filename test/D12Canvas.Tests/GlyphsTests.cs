using System.Globalization;
using System.Reflection;
using D12Canvas.Demo.Pages;

namespace D12Canvas.Tests;

public class GlyphsTests
{
    private const double PaddingBoxMin = 1;
    private const double PaddingBoxMax = 15;

    public static TheoryData<string> GlyphNames => [.. CatalogueGlyphs().Keys];

    [Theory]
    [MemberData(nameof(GlyphNames))]
    public void EveryCoordinateLiesInsideThePaddingBox(string glyphName)
    {
        var glyph = CatalogueGlyphs()[glyphName];

        var outside = glyph
            .Parts.SelectMany(part => PathCoordinates.Of(part.Data))
            .Where(point =>
                point.X is < PaddingBoxMin or > PaddingBoxMax
                || point.Y is < PaddingBoxMin or > PaddingBoxMax
            )
            .ToList();

        Assert.Empty(outside);
    }

    [Fact]
    public void EveryGlyphAppearsInTheGalleryUnderItsOwnName()
    {
        var galleryEntries = GlyphGalleryEntries
            .Sections.SelectMany(section => section.Entries)
            .ToList();

        var missing = CatalogueGlyphs()
            .Where(named =>
                !galleryEntries.Any(entry =>
                    entry.Name == named.Key && ReferenceEquals(entry.Glyph, named.Value)
                )
            )
            .Select(named => named.Key);

        Assert.Empty(missing);
    }

    [Fact]
    public void ThePaddingBoxCheckCatchesACoordinateOutsideTheBox()
    {
        var points = PathCoordinates.Of("M2 2h13.5 M3 3c0 0 4 .5 4-2.5 M8 8a1 1 0 1 1 0 2");

        Assert.Contains(points, point => point.X == 15.5);
        Assert.Contains(points, point => point.Y == 0.5);
    }

    [Fact]
    public void AGlyphNeedsAtLeastOnePartWithPathData()
    {
        Assert.Throws<ArgumentException>(() => new Glyph());
        Assert.Throws<ArgumentException>(() => new Glyph(new GlyphPart(" ")));
    }

    [Fact]
    public void GlyphsWithTheSamePartsAreEqual()
    {
        var first = new Glyph(new GlyphPart("M2 2h4"), new GlyphPart("M3 3h1", GlyphPaint.Fill));
        var second = new Glyph(new GlyphPart("M2 2h4"), new GlyphPart("M3 3h1", GlyphPaint.Fill));

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    private static Dictionary<string, Glyph> CatalogueGlyphs() =>
        typeof(Glyphs)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType == typeof(Glyph))
            .ToDictionary(field => field.Name, field => (Glyph)field.GetValue(null)!);
}

internal readonly record struct PathPoint(double X, double Y);

// Arc radii and flags are not coordinates, so only an arc's endpoint is returned.
internal static class PathCoordinates
{
    public static IReadOnlyList<PathPoint> Of(string data)
    {
        var tokens = new PathTokens(data);
        var points = new List<PathPoint>();
        var current = new PathPoint(0, 0);
        var subpathStart = current;
        var command = ' ';

        while (tokens.HasMore)
        {
            if (tokens.NextIsCommand)
            {
                command = tokens.ReadCommand();
            }
            else if (command is 'M')
            {
                command = 'L';
            }
            else if (command is 'm')
            {
                command = 'l';
            }

            var relative = char.IsLower(command);
            PathPoint Point(double x, double y) =>
                relative ? new(current.X + x, current.Y + y) : new(x, y);

            switch (char.ToUpperInvariant(command))
            {
                case 'M':
                    current = Point(tokens.ReadNumber(), tokens.ReadNumber());
                    subpathStart = current;
                    points.Add(current);
                    break;
                case 'L':
                case 'T':
                    current = Point(tokens.ReadNumber(), tokens.ReadNumber());
                    points.Add(current);
                    break;
                case 'H':
                    var x = tokens.ReadNumber();
                    current = new(relative ? current.X + x : x, current.Y);
                    points.Add(current);
                    break;
                case 'V':
                    var y = tokens.ReadNumber();
                    current = new(current.X, relative ? current.Y + y : y);
                    points.Add(current);
                    break;
                case 'C':
                    points.Add(Point(tokens.ReadNumber(), tokens.ReadNumber()));
                    points.Add(Point(tokens.ReadNumber(), tokens.ReadNumber()));
                    current = Point(tokens.ReadNumber(), tokens.ReadNumber());
                    points.Add(current);
                    break;
                case 'S':
                case 'Q':
                    points.Add(Point(tokens.ReadNumber(), tokens.ReadNumber()));
                    current = Point(tokens.ReadNumber(), tokens.ReadNumber());
                    points.Add(current);
                    break;
                case 'A':
                    tokens.ReadNumber();
                    tokens.ReadNumber();
                    tokens.ReadNumber();
                    tokens.ReadFlag();
                    tokens.ReadFlag();
                    current = Point(tokens.ReadNumber(), tokens.ReadNumber());
                    points.Add(current);
                    break;
                case 'Z':
                    current = subpathStart;
                    break;
                default:
                    throw new FormatException($"Unknown path command '{command}' in \"{data}\".");
            }
        }

        return points;
    }

    private sealed class PathTokens(string data)
    {
        private int _position;

        public bool HasMore
        {
            get
            {
                SkipSeparators();
                return _position < data.Length;
            }
        }

        public bool NextIsCommand
        {
            get
            {
                SkipSeparators();
                return char.IsLetter(data[_position]) && data[_position] is not ('e' or 'E');
            }
        }

        public char ReadCommand()
        {
            SkipSeparators();
            return data[_position++];
        }

        public char ReadFlag()
        {
            SkipSeparators();
            return data[_position++];
        }

        public double ReadNumber()
        {
            SkipSeparators();
            var start = _position;
            if (data[_position] is '+' or '-')
            {
                _position++;
            }

            var seenPoint = false;
            while (_position < data.Length)
            {
                var character = data[_position];
                if (char.IsDigit(character))
                {
                    _position++;
                }
                else if (character == '.' && !seenPoint)
                {
                    seenPoint = true;
                    _position++;
                }
                else
                {
                    break;
                }
            }

            return double.Parse(data[start.._position], CultureInfo.InvariantCulture);
        }

        private void SkipSeparators()
        {
            while (
                _position < data.Length
                && (char.IsWhiteSpace(data[_position]) || data[_position] == ',')
            )
            {
                _position++;
            }
        }
    }
}
