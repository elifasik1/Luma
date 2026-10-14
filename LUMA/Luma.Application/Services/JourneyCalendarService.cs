using Luma.Application.Interfaces;
using Luma.Application.Models;

namespace Luma.Application.Services;

public sealed class JourneyCalendarService
{
    private readonly IJourneyCalendarQuery _query;

    public JourneyCalendarService(IJourneyCalendarQuery query)
    {
        _query = query;
    }

    public Task<JourneyCalendarMonthResponse?> GetMonthAsync(
        Guid dreamId,
        int year,
        int month,
        CancellationToken cancellationToken = default) =>
        _query.GetMonthAsync(dreamId, year, month, cancellationToken);

    public Task<JourneyCalendarDayResponse?> GetDayAsync(
        Guid dreamId,
        DateOnly planDate,
        CancellationToken cancellationToken = default) =>
        _query.GetDayAsync(dreamId, planDate, cancellationToken);
}
