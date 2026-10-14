using Luma.Application.Models;

namespace Luma.Application.Interfaces;

public interface IJourneyCalendarQuery
{
    Task<JourneyCalendarMonthResponse?> GetMonthAsync(
        Guid dreamId,
        int year,
        int month,
        CancellationToken cancellationToken = default);

    Task<JourneyCalendarDayResponse?> GetDayAsync(
        Guid dreamId,
        DateOnly planDate,
        CancellationToken cancellationToken = default);
}
