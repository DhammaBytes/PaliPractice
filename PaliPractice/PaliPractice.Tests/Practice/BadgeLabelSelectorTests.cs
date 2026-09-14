using PaliPractice.Presentation.Practice.Common;

namespace PaliPractice.Tests.Practice;

[TestFixture]
public class BadgeLabelSelectorTests
{
    static readonly BadgeWidths[] NounBadges =
    [
        new(120, 65),  // Case
        new(110, 60),  // Gender
        new(70, 45)    // Number
    ];

    [Test]
    public void UsesFullLabelsWhenTheCurrentCardFits()
    {
        BadgeLabelSelector.SelectAbbreviatedMask(330, NounBadges, spacing: 7)
            .Should().Be(0);
    }

    [Test]
    public void KeepsOnlyTheFirstLabelFullWhenTheWholeRowDoesNotFit()
    {
        // The full row needs 314 units; the first full plus two short needs 239.
        BadgeLabelSelector.SelectAbbreviatedMask(270, NounBadges, spacing: 7)
            .Should().Be(0b110);
    }

    [Test]
    public void OptionalVoiceAlsoShortensBeforeTheFirstBadge()
    {
        BadgeLabelSelector.SelectAbbreviatedMask(340,
            [.. NounBadges, new BadgeWidths(130, 80)], spacing: 7)
            .Should().Be(0b1110);
    }

    [Test]
    public void UsesCompactLabelsOnlyWhenNoFullLabelFits()
    {
        BadgeLabelSelector.SelectAbbreviatedMask(190, NounBadges, spacing: 7)
            .Should().Be(0b111);
    }
}
