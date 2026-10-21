namespace Luma.Application.Services;

public static class TargetYearRules
{
    public static int CurrentYear => DateTime.UtcNow.Year;
    public static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    public static int Validate(int? targetYear)
    {
        var year = targetYear ?? CurrentYear;
        if (year < CurrentYear)
        {
            throw new ArgumentException($"Target year must be {CurrentYear} or later.", nameof(targetYear));
        }

        return year;
    }

    public static DateOnly ValidateDate(DateOnly? targetDate, int? fallbackYear = null)
    {
        var date = targetDate ?? new DateOnly(Validate(fallbackYear), 1, 1);
        if (date < Today)
        {
            throw new ArgumentException($"Target date must be {Today:dd/MM/yyyy} or later.", nameof(targetDate));
        }

        return date;
    }
}
