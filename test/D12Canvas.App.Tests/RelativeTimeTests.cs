using D12Canvas.App.Components;

namespace D12Canvas.App.Tests;

public sealed class RelativeTimeTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 11, 15, 0, 0, TimeSpan.Zero);

    public static TheoryData<TimeSpan, string> Ages =>
        new()
        {
            { TimeSpan.Zero, "just now" },
            { TimeSpan.FromSeconds(59), "just now" },
            { TimeSpan.FromMinutes(1), "1 min ago" },
            { TimeSpan.FromMinutes(59), "59 min ago" },
            { TimeSpan.FromHours(1), "1 hour ago" },
            { TimeSpan.FromHours(23.9), "23 hours ago" },
            { TimeSpan.FromHours(24), "yesterday" },
            { TimeSpan.FromHours(47), "yesterday" },
            { TimeSpan.FromDays(2), "2 days ago" },
            { TimeSpan.FromDays(6.9), "6 days ago" },
            { TimeSpan.FromDays(7), "on 4 Nov" },
            { TimeSpan.FromDays(30), "on 12 Oct" },
        };

    [Theory]
    [MemberData(nameof(Ages))]
    public void DescribesHowLongAgoSomethingHappened(TimeSpan age, string expected) =>
        Assert.Equal(expected, RelativeTime.Describe(Now - age, Now));

    [Fact]
    public void ATimeInTheFutureReadsAsJustNow() =>
        Assert.Equal("just now", RelativeTime.Describe(Now.AddMinutes(5), Now));

    [Fact]
    public void ADateInAnEarlierYearIncludesTheYear() =>
        Assert.Equal(
            "on 12 Oct 2025",
            RelativeTime.Describe(new DateTimeOffset(2025, 10, 12, 9, 0, 0, TimeSpan.Zero), Now)
        );

    [Fact]
    public void ADateEarlierThisYearLeavesTheYearOut() =>
        Assert.Equal(
            "on 1 Jan",
            RelativeTime.Describe(new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero), Now)
        );

    [Fact]
    public void TheDateIsReadInNowsTimeZone()
    {
        var localNow = new DateTimeOffset(2026, 11, 11, 15, 0, 0, TimeSpan.FromHours(10));
        var lateOnTheFirstInUtc = new DateTimeOffset(2026, 11, 1, 20, 0, 0, TimeSpan.Zero);

        Assert.Equal("on 2 Nov", RelativeTime.Describe(lateOnTheFirstInUtc, localNow));
    }
}
