namespace PaliPractice.Services.UserData.Statistics;

/// <summary>
/// Overall statistics including streaks and today's progress.
/// </summary>
public record GeneralStatsDto
{
    /// <summary>Current consecutive days with any practice.</summary>
    public int CurrentPracticeStreak { get; init; }

    /// <summary>Longest practice streak ever achieved.</summary>
    public int LongestPracticeStreak { get; init; }

    /// <summary>Current consecutive days meeting noun daily goal.</summary>
    public int CurrentNounGoalStreak { get; init; }

    /// <summary>Current consecutive days meeting verb daily goal.</summary>
    public int CurrentVerbGoalStreak { get; init; }

    /// <summary>Total number of days with any practice.</summary>
    public int TotalPracticeDays { get; init; }

    /// <summary>Today's declensions completed.</summary>
    public int TodayDeclensions { get; init; }

    /// <summary>Today's conjugations completed.</summary>
    public int TodayConjugations { get; init; }

    /// <summary>Whether today's noun goal is met.</summary>
    public bool NounGoalMet { get; init; }

    /// <summary>Whether today's verb goal is met.</summary>
    public bool VerbGoalMet { get; init; }
}

/// <summary>
/// Calendar entry for practice history heatmap.
/// </summary>
public record CalendarDayDto
{
    /// <summary>Date in YYYY-MM-DD format.</summary>
    public required string Date { get; init; }

    /// <summary>Number of declensions practiced this day.</summary>
    public int DeclensionsCount { get; init; }

    /// <summary>Number of conjugations practiced this day.</summary>
    public int ConjugationsCount { get; init; }

    /// <summary>Whether any practice occurred.</summary>
    public bool HasPractice => DeclensionsCount > 0 || ConjugationsCount > 0;

    /// <summary>
    /// Intensity level for heatmap visualization (0-3).
    /// 0 = no practice, 1 = light, 2 = medium, 3 = heavy.
    /// </summary>
    public int Intensity => (DeclensionsCount + ConjugationsCount) switch
    {
        0 => 0,
        <= 15 => 1,
        <= 35 => 2,
        _ => 3
    };
}

/// <summary>
/// Statistics for a practice type (nouns or verbs).
/// </summary>
public record PracticeTypeStatsDto
{
    /// <summary>Total unique forms practiced at least once.</summary>
    public int TotalPracticed { get; init; }

    /// <summary>Forms currently due for review.</summary>
    public int DueForReview { get; init; }

    /// <summary>Distribution across SRS mastery levels.</summary>
    public SrsDistributionDto Distribution { get; init; } = new();

    /// <summary>Top 5 strongest combos (highest average mastery).</summary>
    public IReadOnlyList<ComboStatDto> StrongestCombos { get; init; } = [];

    /// <summary>Top 5 weakest combos (lowest average mastery).</summary>
    public IReadOnlyList<ComboStatDto> WeakestCombos { get; init; } = [];

    /// <summary>Period statistics (today, 7 days, all time).</summary>
    public PeriodStatsDto PeriodStats { get; init; } = new();
}

/// <summary>
/// SRS mastery level distribution for visualization.
/// </summary>
public record SrsDistributionDto
{
    /// <summary>Level 0: Never practiced.</summary>
    public int Unpracticed { get; init; }

    /// <summary>Levels 1-3: Struggling (1-6 day cooldowns).</summary>
    public int Struggling { get; init; }

    /// <summary>Levels 4-6: Learning (6-22 day cooldowns).</summary>
    public int Learning { get; init; }

    /// <summary>Levels 7-10: Strong (40-254 day cooldowns).</summary>
    public int Strong { get; init; }

    /// <summary>Level 11: Mastered/retired.</summary>
    public int Mastered { get; init; }

    /// <summary>Total forms across all categories.</summary>
    public int Total => Unpracticed + Struggling + Learning + Strong + Mastered;
}

/// <summary>
/// Statistics for a grammatical combination.
/// For nouns: case + gender + number (e.g., "nom_masc_sg").
/// For verbs: tense + person + number + voice (e.g., "pr_3rd_sg_reflx").
/// </summary>
public record ComboStatDto
{
    /// <summary>
    /// Combo key in DPD abbreviation format.
    /// Nouns: "nom_masc_sg", "acc_nt_pl", etc.
    /// Verbs: "pr_3rd_sg", "opt_1st_pl_reflx", etc.
    /// </summary>
    public required string ComboKey { get; init; }

    /// <summary>Human-readable localized label.</summary>
    public required string DisplayName { get; init; }

    /// <summary>Number of unique forms practiced in this combo.</summary>
    public int FormCount { get; init; }

    /// <summary>Average mastery level (1-11 scale).</summary>
    public double AverageMastery { get; init; }

    /// <summary>Average mastery as percentage (0-100).</summary>
    public int MasteryPercent => Math.Min(100, (int)Math.Round(AverageMastery * 10));

    /// <summary>Whether this is a placeholder entry (no data).</summary>
    public bool IsPlaceholder { get; init; }

    /// <summary>Placeholder entry for empty rows.</summary>
    public static ComboStatDto Placeholder => new()
    {
        ComboKey = "",
        DisplayName = "–",
        FormCount = 0,
        AverageMastery = 0,
        IsPlaceholder = true
    };
}

/// <summary>
/// Time-period based practice counts.
/// </summary>
public record PeriodStatsDto
{
    /// <summary>Unique forms practiced today.</summary>
    public int Today { get; init; }

    /// <summary>Unique forms practiced in last 7 days.</summary>
    public int Last7Days { get; init; }

    /// <summary>Total unique forms practiced all time.</summary>
    public int AllTime { get; init; }
}
