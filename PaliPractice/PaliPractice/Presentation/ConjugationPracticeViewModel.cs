using PaliPractice.Presentation.Providers;
using PaliPractice.Presentation.ViewModels;
using Uno.Extensions.Navigation;

namespace PaliPractice.Presentation;

[Microsoft.UI.Xaml.Data.Bindable]
public partial class ConjugationPracticeViewModel
    : PracticeViewModelBase<ConjugationAnswer>
{
    public EnumChoiceViewModel<Number> Number { get; }
    public EnumChoiceViewModel<Person> Person { get; }
    public EnumChoiceViewModel<Voice> Voice { get; }
    public EnumChoiceViewModel<Tense> Tense { get; }

    protected override IReadOnlyList<IValidatableChoice> Groups => new IValidatableChoice[]
    {
        Number, Person, Voice, Tense
    };

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
        ], Models.Number.None);

        Person = new EnumChoiceViewModel<Person>([
            new EnumOption<Person>(Models.Person.First, "1st"),
            new EnumOption<Person>(Models.Person.Second, "2nd"),
            new EnumOption<Person>(Models.Person.Third, "3rd")
        ], Models.Person.None);

        Voice = new EnumChoiceViewModel<Voice>([
            new EnumOption<Voice>(Models.Voice.Normal, "Active"),
            new EnumOption<Voice>(Models.Voice.Reflexive, "Reflexive")
        ], Models.Voice.None);

        Tense = new EnumChoiceViewModel<Tense>([
            new EnumOption<Tense>(Models.Tense.Present, "Present"),
            new EnumOption<Tense>(Models.Tense.Imperative, "Imper."),
            new EnumOption<Tense>(Models.Tense.Aorist, "Aorist"),
            new EnumOption<Tense>(Models.Tense.Optative, "Opt."),
            new EnumOption<Tense>(Models.Tense.Future, "Future")
        ], Models.Tense.None);

        _ = InitializeAsync();
    }

    protected override ConjugationAnswer BuildAnswerFor(Headword w)
    {
        // MOCK deterministic key (replace with DB-backed key later)
        var id = w.Id;
        return new ConjugationAnswer(
            (id % 2 == 0) ? Models.Number.Singular : Models.Number.Plural,
            (Models.Person)((id % 3) + 1),  // 1..3
            (id % 2 == 0) ? Models.Voice.Normal : Models.Voice.Reflexive,
            (Models.Tense)(new[]
            {
                Models.Tense.Present,
                Models.Tense.Imperative,
                Models.Tense.Aorist,
                Models.Tense.Optative,
                Models.Tense.Future
            }[id % 5])
        );
    }

    protected override void ApplyAnswerToGroups(ConjugationAnswer a)
    {
        Number.SetExpected(a.Number);
        Person.SetExpected(a.Person);
        Voice.SetExpected(a.Voice);
        Tense.SetExpected(a.Tense);
    }

    protected override void SetExamples(Headword w)
    {
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
