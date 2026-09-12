namespace PaliPractice.Tests.Practice.Simulation;

/// <summary>Explicit UTC time and time zone; advancing a simulation never sleeps.</summary>
internal sealed class SimulationTimeProvider(DateTimeOffset utcNow, TimeZoneInfo? timeZone = null) : TimeProvider
{
    public DateTimeOffset UtcNow { get; set; } = utcNow;
    public override DateTimeOffset GetUtcNow() => UtcNow;
    public override TimeZoneInfo LocalTimeZone { get; } = timeZone ?? TimeZoneInfo.Utc;
}
