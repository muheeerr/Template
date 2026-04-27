namespace Template.Modules.Common.Utilities;

public static class DateRangePresetResolver
{
    public static bool TryResolve(
        string? preset,
        DateOnly? dateFrom,
        DateOnly? dateTo,
        DateOnly today,
        out (DateOnly From, DateOnly To) range,
        out string? error)
    {
        error = null;

        if (string.IsNullOrWhiteSpace(preset))
        {
            var from = dateFrom ?? today;
            var to = dateTo ?? today;

            if (from > to)
            {
                range = default;
                error = "dateFrom must be before or equal to dateTo.";
                return false;
            }

            range = (from, to);
            return true;
        }

        switch (preset.Trim().ToLowerInvariant())
        {
            case "today":
                range = (today, today);
                return true;
            case "yesterday":
                var yesterday = today.AddDays(-1);
                range = (yesterday, yesterday);
                return true;
            case "this_week":
                var startOfWeek = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
                range = (startOfWeek, startOfWeek.AddDays(6));
                return true;
            case "this_month":
                var startOfMonth = new DateOnly(today.Year, today.Month, 1);
                range = (startOfMonth, startOfMonth.AddMonths(1).AddDays(-1));
                return true;
            case "last_month":
                var firstOfThisMonth = new DateOnly(today.Year, today.Month, 1);
                var firstOfLastMonth = firstOfThisMonth.AddMonths(-1);
                range = (firstOfLastMonth, firstOfThisMonth.AddDays(-1));
                return true;
            case "custom":
                if (dateFrom is null || dateTo is null)
                {
                    range = default;
                    error = "dateFrom and dateTo are required when dateRange is custom.";
                    return false;
                }

                if (dateFrom > dateTo)
                {
                    range = default;
                    error = "dateFrom must be before or equal to dateTo.";
                    return false;
                }

                range = (dateFrom.Value, dateTo.Value);
                return true;
            default:
                range = default;
                error = $"Unsupported dateRange preset '{preset}'.";
                return false;
        }
    }
}
