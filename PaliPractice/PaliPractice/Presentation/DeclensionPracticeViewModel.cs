using PaliPractice.Presentation.Providers;
using PaliPractice.Presentation.ViewModels;

namespace PaliPractice.Presentation;

[Bindable]
public partial class DeclensionPracticeViewModel
    : PracticeViewModelBase<DeclensionAnswer>
{
    public EnumChoiceViewModel<Number> Number { get; }
    public EnumChoiceViewModel<Gender> Gender { get; }
    public EnumChoiceViewModel<NounCase> Cases { get; }

    protected override IReadOnlyList<IValidatableChoice> Groups =>
    [
        Number, Gender, Cases
    ];

    public DeclensionPracticeViewModel(
        [FromKeyedServices("noun")] IWordProvider words,
        CardViewModel card,
        INavigator navigator,
        ILogger<DeclensionPracticeViewModel> logger)
        : base(words, card, navigator, logger)
    {
        Number = new EnumChoiceViewModel<Number>([
            new EnumOption<Number>(Models.Number.Singular, "Singular"),
            new EnumOption<Number>(Models.Number.Plural, "Plural")
        ]);

        Gender = new EnumChoiceViewModel<Gender>([
            new EnumOption<Gender>(Models.Gender.Masculine, "Masculine"),
            new EnumOption<Gender>(Models.Gender.Neuter, "Neuter"),
            new EnumOption<Gender>(Models.Gender.Feminine, "Feminine")
        ]);

        Cases = new EnumChoiceViewModel<NounCase>([
            new EnumOption<NounCase>(NounCase.Nominative, "Nom"),
            new EnumOption<NounCase>(NounCase.Accusative, "Acc"),
            new EnumOption<NounCase>(NounCase.Instrumental, "Ins"),
            new EnumOption<NounCase>(NounCase.Dative, "Dat"),
            new EnumOption<NounCase>(NounCase.Ablative, "Abl"),
            new EnumOption<NounCase>(NounCase.Genitive, "Gen"),
            new EnumOption<NounCase>(NounCase.Locative, "Loc"),
            new EnumOption<NounCase>(NounCase.Vocative, "Voc")
        ]);

        _ = InitializeAsync();
    }

    protected override DeclensionAnswer BuildAnswerFor(IWord w)
    {
        // Cast to Noun if needed for noun-specific properties
        // var noun = (Noun)w;

        // MOCK deterministic key (replace with DB-backed key later)
        var id = w.Id;
        return new DeclensionAnswer(
            (id % 2 == 0) ? Models.Number.Singular : Models.Number.Plural,
            (Gender)(new[]
            {
                Models.Gender.Masculine,
                Models.Gender.Neuter,
                Models.Gender.Feminine
            }[id % 3]),
            (NounCase)(new[]
            {
                NounCase.Nominative,
                NounCase.Accusative,
                NounCase.Instrumental,
                NounCase.Dative,
                NounCase.Ablative,
                NounCase.Genitive,
                NounCase.Locative,
                NounCase.Vocative
            }[id % 8])
        );
    }

    protected override void ApplyAnswerToGroups(DeclensionAnswer a)
    {
        Number.SetExpected(a.Number);
        Gender.SetExpected(a.Gender);
        Cases.SetExpected(a.Case);
    }

    protected override void SetExamples(IWord w)
    {
        // Cast to Noun if needed for noun-specific properties
        // var noun = (Noun)w;

        Card.UsageExample = "virūp'akkhehi me mettaṃ, mettaṃ erāpathehi me";
        Card.SuttaReference = "AN4.67 ahirājasuttaṃ";
    }

    protected override void ResetAllGroups()
    {
        Number.Reset();
        Gender.Reset();
        Cases.Reset();
    }
}
