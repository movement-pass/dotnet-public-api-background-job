namespace MovementPass.Public.Api.BackgroundJob.Infrastructure;

using System;

public static class Clock
{
    public static Func<DateTime> Now { get; set; } = () => DateTime.UtcNow;
}