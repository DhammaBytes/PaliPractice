using PaliPractice.Presentation.Practice.ViewModels.Common;
using PaliPractice.Services.Practice;

namespace PaliPractice.Tests.Practice.Simulation;

public enum SrsStudentProfile { Legacy, AlwaysEasy, WeakPlural }

internal static class SrsStudent
{
    // Legacy remains solely for exact comparisons with the original reports.
    public static Func<PracticeItem, int, bool> Answers(SrsStudentProfile profile,
        PracticeType type, string scenario, int legacySeed) => profile switch
    {
        SrsStudentProfile.Legacy => (item, _) => scenario == "daily" ||
            (scenario != "short" && (item.FormId / 10 + legacySeed) % 4 != 0),
        SrsStudentProfile.AlwaysEasy => (_, _) => true,
        SrsStudentProfile.WeakPlural => (item, _) => NumberDigit(item.FormId, type) != 2,
        _ => throw new ArgumentOutOfRangeException(nameof(profile))
    };

    static long NumberDigit(long id, PracticeType type) =>
        type == PracticeType.Declension ? id % 100 / 10 : id % 1000 / 100;
}
