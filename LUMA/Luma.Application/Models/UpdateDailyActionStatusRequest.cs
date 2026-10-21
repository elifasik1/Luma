namespace Luma.Application.Models;

public sealed record UpdateDailyActionStatusRequest(string Status, DateOnly? ScheduledDate = null);
