using PaliPractice.Presentation.Providers;
using PaliPractice.Presentation.ViewModels;

namespace PaliPractice.Presentation;

[Bindable]
public partial class DeclensionPracticeViewModel
    : PracticeViewModelBase<DeclensionAnswer>
{
    readonly IInflectionService _inflectionService;
    readonly Random _random = new();
    Declension? _currentDeclension;

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
        ILogger<DeclensionPracticeViewModel> logger,
        IInflectionService inflectionService)
        : base(words, card, navigator, logger)
    {
        _inflectionService = inflectionService;
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
        var noun = (Noun)w;

        // Generate all possible declensions for this noun
        var allDeclensions = new List<Declension>();
        foreach (var nounCase in Enum.GetValues<NounCase>())
        {
            foreach (var number in Enum.GetValues<Models.Number>())
            {
                var declensions = _inflectionService.GenerateNounForms(noun, nounCase, number);
                allDeclensions.AddRange(declensions);
            }
        }

        // Filter to only InCorpus forms
        var corpusForms = allDeclensions.Where(d => d.InCorpus).ToList();

        if (corpusForms.Count == 0)
        {
            Logger.LogWarning("No corpus forms found for noun {Lemma} (id={Id}), falling back to all forms",
                noun.Lemma, noun.Id);
            corpusForms = allDeclensions;
        }

        if (corpusForms.Count == 0)
        {
            Logger.LogError("No declensions generated for noun {Lemma} (id={Id})", noun.Lemma, noun.Id);
            // Fallback to a default answer
            return new DeclensionAnswer(Models.Number.Singular, noun.Gender, NounCase.Nominative);
        }

        // Pick a random corpus form
        _currentDeclension = corpusForms[_random.Next(corpusForms.Count)];

        // Set the card to show the declined form
        Card.CurrentWord = _currentDeclension.Form;

        return new DeclensionAnswer(
            _currentDeclension.Number,
            _currentDeclension.Gender,
            _currentDeclension.CaseName
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
        var noun = (Noun)w;

        // Use real data from the noun
        if (!string.IsNullOrEmpty(noun.Example1))
        {
            Card.UsageExample = noun.Example1;

            // Combine source and sutta reference if available
            var reference = "";
            if (!string.IsNullOrEmpty(noun.Source1))
                reference = noun.Source1;
            if (!string.IsNullOrEmpty(noun.Sutta1))
                reference = string.IsNullOrEmpty(reference) ? noun.Sutta1 : $"{reference} {noun.Sutta1}";

            Card.SuttaReference = reference;
        }
        else if (!string.IsNullOrEmpty(noun.Example2))
        {
            // Fall back to second example if first is not available
            Card.UsageExample = noun.Example2;

            var reference = "";
            if (!string.IsNullOrEmpty(noun.Source2))
                reference = noun.Source2;
            if (!string.IsNullOrEmpty(noun.Sutta2))
                reference = string.IsNullOrEmpty(reference) ? noun.Sutta2 : $"{reference} {noun.Sutta2}";

            Card.SuttaReference = reference;
        }
        else
        {
            // No examples available
            Card.UsageExample = "";
            Card.SuttaReference = "";
        }
    }

    protected override void ResetAllGroups()
    {
        Number.Reset();
        Gender.Reset();
        Cases.Reset();
    }
}
