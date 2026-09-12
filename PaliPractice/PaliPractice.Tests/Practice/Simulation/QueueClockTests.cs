using PaliPractice.Presentation.Practice.ViewModels.Common;
using PaliPractice.Services.Practice;
using PaliPractice.Tests.Practice.Builders;

namespace PaliPractice.Tests.Practice.Simulation;

[TestFixture]
public class QueueClockTests
{
    [Test]
    public void QueueSeed_UsesUtcClockDateAndPreservesExplicitSeedOverride()
    {
        var db = TestScenarioBuilder.Medium().Build();
        var clock = new SimulationTimeProvider(new DateTimeOffset(2035, 1, 2, 23, 59, 59, TimeSpan.Zero));
        var builder = new PracticeQueueBuilder(db, clock);
        var before = builder.BuildQueue(PracticeType.Declension, 50);
        var explicitBefore = new PracticeQueueBuilder(db).BuildQueue(PracticeType.Declension, 50, clock.UtcNow.UtcDateTime);
        before.Should().Equal(explicitBefore);

        clock.UtcNow += TimeSpan.FromSeconds(1);
        var after = builder.BuildQueue(PracticeType.Declension, 50);
        after.Should().NotEqual(before);
        builder.BuildQueue(PracticeType.Declension, 50, new DateTime(2035, 1, 2)).Should().Equal(before);
        new PracticeQueueBuilder(db, clock).BuildQueue(PracticeType.Declension, 50).Should().Equal(after);
    }
}
