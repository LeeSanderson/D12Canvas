using System.Globalization;

namespace D12Canvas.App.Components;

public static class RelativeTime
{
    public static string Describe(DateTimeOffset when, DateTimeOffset now)
    {
        var age = now - when;
        if (age < TimeSpan.FromMinutes(1))
        {
            return "just now";
        }

        if (age < TimeSpan.FromHours(1))
        {
            return $"{(int)age.TotalMinutes} min ago";
        }

        if (age < TimeSpan.FromDays(1))
        {
            var hours = (int)age.TotalHours;
            return hours == 1 ? "1 hour ago" : $"{hours} hours ago";
        }

        if (age < TimeSpan.FromDays(2))
        {
            return "yesterday";
        }

        if (age < TimeSpan.FromDays(7))
        {
            return $"{(int)age.TotalDays} days ago";
        }

        var date = when.ToOffset(now.Offset);
        var format = date.Year == now.Year ? "d MMM" : "d MMM yyyy";
        return "on " + date.ToString(format, CultureInfo.InvariantCulture);
    }
}
