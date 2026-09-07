using PaliPractice.Presentation.Settings.ViewModels;
using PaliPractice.Tests.Practice.Fakes;

namespace PaliPractice.Tests.Practice;

[TestFixture]
public class DeclensionPatternNotificationTests
{
    public sealed record PatternControl(string Selection, string CanDisable)
    {
        public override string ToString() => Selection;
    }

    static readonly PatternControl[] Controls =
    [
        new(nameof(DeclensionSettingsViewModel.PatternMascA), nameof(DeclensionSettingsViewModel.CanDisablePatternMascA)),
        new(nameof(DeclensionSettingsViewModel.PatternMascI), nameof(DeclensionSettingsViewModel.CanDisablePatternMascI)),
        new(nameof(DeclensionSettingsViewModel.PatternMascILong), nameof(DeclensionSettingsViewModel.CanDisablePatternMascILong)),
        new(nameof(DeclensionSettingsViewModel.PatternMascU), nameof(DeclensionSettingsViewModel.CanDisablePatternMascU)),
        new(nameof(DeclensionSettingsViewModel.PatternMascULong), nameof(DeclensionSettingsViewModel.CanDisablePatternMascULong)),
        new(nameof(DeclensionSettingsViewModel.PatternMascAs), nameof(DeclensionSettingsViewModel.CanDisablePatternMascAs)),
        new(nameof(DeclensionSettingsViewModel.PatternMascAr), nameof(DeclensionSettingsViewModel.CanDisablePatternMascAr)),
        new(nameof(DeclensionSettingsViewModel.PatternMascAnt), nameof(DeclensionSettingsViewModel.CanDisablePatternMascAnt)),
        new(nameof(DeclensionSettingsViewModel.PatternNtA), nameof(DeclensionSettingsViewModel.CanDisablePatternNtA)),
        new(nameof(DeclensionSettingsViewModel.PatternNtI), nameof(DeclensionSettingsViewModel.CanDisablePatternNtI)),
        new(nameof(DeclensionSettingsViewModel.PatternNtU), nameof(DeclensionSettingsViewModel.CanDisablePatternNtU)),
        new(nameof(DeclensionSettingsViewModel.PatternFemALong), nameof(DeclensionSettingsViewModel.CanDisablePatternFemA)),
        new(nameof(DeclensionSettingsViewModel.PatternFemI), nameof(DeclensionSettingsViewModel.CanDisablePatternFemI)),
        new(nameof(DeclensionSettingsViewModel.PatternFemILong), nameof(DeclensionSettingsViewModel.CanDisablePatternFemILong)),
        new(nameof(DeclensionSettingsViewModel.PatternFemU), nameof(DeclensionSettingsViewModel.CanDisablePatternFemU)),
        new(nameof(DeclensionSettingsViewModel.PatternFemAr), nameof(DeclensionSettingsViewModel.CanDisablePatternFemAr))
    ];

    static IEnumerable<TestCaseData> EveryLastPatternTransition()
    {
        foreach (var remaining in Controls)
        foreach (var changed in Controls.Where(control => control != remaining))
            yield return new TestCaseData(remaining, changed);
    }

    [TestCaseSource(nameof(EveryLastPatternTransition))]
    public void LastPattern_UpdatesBoundGuardWhenAnotherPatternIsDisabledAndReenabled(
        PatternControl remaining,
        PatternControl changed)
    {
        var database = new FakeDatabaseService();
        var settings = new DeclensionSettingsViewModel(null!, database);
        foreach (var control in Controls.Where(control => control != remaining && control != changed))
            SetSelection(settings, control, false);

        // Model the UI binding: its value changes only when the view model notifies it.
        var boundCanDisable = ReadBoolean(settings, remaining.CanDisable);
        settings.PropertyChanged += (_, args) =>
        {
            if (string.IsNullOrEmpty(args.PropertyName) || args.PropertyName == remaining.CanDisable)
                boundCanDisable = ReadBoolean(settings, remaining.CanDisable);
        };

        SetSelection(settings, changed, false);

        boundCanDisable.Should().BeFalse("the last selected pattern must become disabled in the UI");
        ReadBoolean(settings, remaining.Selection).Should().BeTrue();
        var reloaded = new DeclensionSettingsViewModel(null!, database);
        ReadBoolean(reloaded, changed.Selection).Should().BeFalse("the pattern change must be saved");
        ReadBoolean(reloaded, remaining.Selection).Should().BeTrue();

        SetSelection(settings, changed, true);

        boundCanDisable.Should().BeTrue("selecting a second pattern must unlock the previous last pattern");
    }

    static bool ReadBoolean(DeclensionSettingsViewModel settings, string name) =>
        (bool)typeof(DeclensionSettingsViewModel).GetProperty(name)!.GetValue(settings)!;

    static void SetSelection(DeclensionSettingsViewModel settings, PatternControl control, bool value) =>
        typeof(DeclensionSettingsViewModel).GetProperty(control.Selection)!.SetValue(settings, value);
}
