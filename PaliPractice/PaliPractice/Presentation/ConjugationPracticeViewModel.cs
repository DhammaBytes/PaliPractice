using PaliPractice.Presentation.Providers;
using PaliPractice.Presentation.ViewModels;

namespace PaliPractice.Presentation;

[Bindable]
public class ConjugationPracticeViewModel
    : PracticeViewModelBase<ConjugationAnswer>
{
    public EnumChoiceViewModel<Number> Number { get; }
    public EnumChoiceViewModel<Person> Person { get; }
    public EnumChoiceViewModel<Voice> Voice { get; }
    public EnumChoiceViewModel<Tense> Tense { get; }

    protected override IReadOnlyList<IValidatableChoice> Groups =>
    [
        Number, Person, Voice, Tense
    ];

    public ConjugationPracticeViewModel(
        [FromKeyedServices("verb")] IWordProvider words,
        CardViewModel card,
        INavigator navigator,
        ILogger<ConjugationPracticeViewModel> logger)
        : base(words, card, navigator, logger)
    {
        Number = new EnumChoiceViewModel<Number>([
            new EnumOption<Number>(Models.Number.Singular, "Singular"),
            new EnumOption<Number>(Models.Number.Plural, "Plural")
        ]);

        Person = new EnumChoiceViewModel<Person>([
            new EnumOption<Person>(Models.Person.First, "1st"),
            new EnumOption<Person>(Models.Person.Second, "2nd"),
            new EnumOption<Person>(Models.Person.Third, "3rd")
        ]);

        Voice = new EnumChoiceViewModel<Voice>([
            new EnumOption<Voice>(Models.Voice.Active, "Active"),
            new EnumOption<Voice>(Models.Voice.Reflexive, "Reflexive")
        ]);

        Tense = new EnumChoiceViewModel<Tense>([
            new EnumOption<Tense>(Models.Tense.Present, "Present"),
            new EnumOption<Tense>(Models.Tense.Aorist, "Aorist"),
            new EnumOption<Tense>(Models.Tense.Future, "Future")
        ]);

        _ = InitializeAsync();
    }

    protected override ConjugationAnswer BuildAnswerFor(IWord w)
    {
        // Cast to Verb if needed for verb-specific properties
        // var verb = (Verb)w;

        // MOCK deterministic key (replace with DB-backed key later)
        var id = w.Id;
        return new ConjugationAnswer(
            (id % 2 == 0) ? Models.Number.Singular : Models.Number.Plural,
            (Person)((id % 3) + 1),  // 1..3
            (id % 2 == 0) ? Models.Voice.Active : Models.Voice.Reflexive,
            (Tense)(new[]
            {
                Models.Tense.Present,
                Models.Tense.Aorist,
                Models.Tense.Future
            }[id % 3])
        );
    }

    protected override void ApplyAnswerToGroups(ConjugationAnswer a)
    {
        Number.SetExpected(a.Number);
        Person.SetExpected(a.Person);
        Voice.SetExpected(a.Voice);
        Tense.SetExpected(a.Tense);
    }

    protected override void SetExamples(IWord w)
    {
        // Cast to Verb if needed for verb-specific properties
        // var verb = (Verb)w;

        Card.UsageExample = "bhagavā etadavoca: sādhu sādhu, bhikkhu";
        Card.SuttaReference = "MN1 mūlapariyāya";
    }

    protected override void ResetAllGroups()
    {
        Number.Reset();
        Person.Reset();
        Voice.Reset();
        Tense.Reset();
    }
}
