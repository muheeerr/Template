namespace Template.Modules.Common.Utilities;

public static class WorkingDayCodec
{
    private static readonly IReadOnlyDictionary<string, string> DayMap =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["mon"] = "Mon",
            ["monday"] = "Mon",
            ["tue"] = "Tue",
            ["tues"] = "Tue",
            ["tuesday"] = "Tue",
            ["wed"] = "Wed",
            ["wednesday"] = "Wed",
            ["thu"] = "Thu",
            ["thur"] = "Thu",
            ["thurs"] = "Thu",
            ["thursday"] = "Thu",
            ["fri"] = "Fri",
            ["friday"] = "Fri",
            ["sat"] = "Sat",
            ["saturday"] = "Sat",
            ["sun"] = "Sun",
            ["sunday"] = "Sun"
        };

    private static readonly IReadOnlyDictionary<string, int> DayOrder =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["Mon"] = 1,
            ["Tue"] = 2,
            ["Wed"] = 3,
            ["Thu"] = 4,
            ["Fri"] = 5,
            ["Sat"] = 6,
            ["Sun"] = 7
        };

    public static bool TrySerialize(IEnumerable<string>? days, out string serialized, out string? error)
    {
        serialized = string.Empty;
        error = null;

        if (days is null)
        {
            error = "At least one working day is required.";
            return false;
        }

        var normalized = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rawDay in days)
        {
            if (string.IsNullOrWhiteSpace(rawDay))
            {
                continue;
            }

            if (!DayMap.TryGetValue(rawDay.Trim(), out var canonical))
            {
                error = $"Unsupported working day '{rawDay}'.";
                return false;
            }

            normalized.Add(canonical);
        }

        if (normalized.Count == 0)
        {
            error = "At least one working day is required.";
            return false;
        }

        serialized = string.Join(',', normalized.OrderBy(day => DayOrder[day]));
        return true;
    }

    public static IReadOnlyList<string> Deserialize(string? serialized)
    {
        if (string.IsNullOrWhiteSpace(serialized))
        {
            return [];
        }

        return serialized
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(day => DayOrder.ContainsKey(day))
            .OrderBy(day => DayOrder[day])
            .ToArray();
    }

    public static bool Contains(string? serialized, DateOnly date)
    {
        var canonical = ToAbbreviation(date.DayOfWeek);
        return Deserialize(serialized).Contains(canonical, StringComparer.OrdinalIgnoreCase);
    }

    public static string ToAbbreviation(DayOfWeek dayOfWeek) => dayOfWeek switch
    {
        DayOfWeek.Monday => "Mon",
        DayOfWeek.Tuesday => "Tue",
        DayOfWeek.Wednesday => "Wed",
        DayOfWeek.Thursday => "Thu",
        DayOfWeek.Friday => "Fri",
        DayOfWeek.Saturday => "Sat",
        DayOfWeek.Sunday => "Sun",
        _ => throw new ArgumentOutOfRangeException(nameof(dayOfWeek), dayOfWeek, null)
    };
}
