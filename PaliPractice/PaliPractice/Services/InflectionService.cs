using PaliPractice.Models.Inflection;

namespace PaliPractice.Services;

public interface IInflectionService
{
    /// <summary>
    /// Generate a grouped noun declension for a given noun and grammatical parameters.
    /// Returns a single Declension containing 1-N possible form variants.
    /// </summary>
    Declension GenerateNounForms(
        Noun noun,
        NounCase nounCase,
        Number number);

    /// <summary>
    /// Generate a grouped verb conjugation for a given verb and grammatical parameters.
    /// Returns a single Conjugation containing 1-N possible form variants.
    /// </summary>
    Conjugation GenerateVerbForms(
        Verb verb,
        Person person,
        Number number,
        Tense tense,
        Voice voice);
}

// TODO: clean or reject stems with symbols

public class InflectionService : IInflectionService
{
    readonly IDatabaseService _databaseService;

    public InflectionService(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public Declension GenerateNounForms(
        Noun noun,
        NounCase nounCase,
        Number number)
    {
        if (string.IsNullOrEmpty(noun.Pattern) || string.IsNullOrEmpty(noun.Stem))
        {
            return new Declension
            {
                CaseName = nounCase,
                Number = number,
                Gender = noun.Gender,
                Forms = []
            };
        }

        // Get all possible endings for this pattern and grammatical parameters
        var endings = NounPatterns.GetEndings(noun.Pattern, nounCase, number);

        if (endings.Length == 0)
        {
            return new Declension
            {
                CaseName = nounCase,
                Number = number,
                Gender = noun.Gender,
                Forms = []
            };
        }

        var forms = new List<DeclensionForm>();

        // Generate a form for each possible ending
        for (int i = 0; i < endings.Length; i++)
        {
            var ending = endings[i];
            var form = noun.Stem + ending;

            // Check if this specific form appears in the corpus
            var inCorpus = _databaseService.IsNounFormInCorpus(
                noun.Id,
                nounCase,
                number,
                noun.Gender,
                endingIndex: i
            );

            forms.Add(new DeclensionForm(
                Form: form,
                Ending: ending,
                EndingIndex: i,
                InCorpus: inCorpus
            ));
        }

        return new Declension
        {
            CaseName = nounCase,
            Number = number,
            Gender = noun.Gender,
            Forms = forms
        };
    }

    public Conjugation GenerateVerbForms(
        Verb verb,
        Person person,
        Number number,
        Tense tense,
        Voice voice)
    {
        if (string.IsNullOrEmpty(verb.Pattern) || string.IsNullOrEmpty(verb.Stem))
        {
            return new Conjugation
            {
                Person = person,
                Number = number,
                Tense = tense,
                Voice = voice,
                Forms = []
            };
        }

        // Get all possible endings for this pattern and grammatical parameters
        var endings = VerbPatterns.GetEndings(
            verb.Pattern,
            person,
            number,
            tense,
            voice
        );

        if (endings.Length == 0)
        {
            return new Conjugation
            {
                Person = person,
                Number = number,
                Tense = tense,
                Voice = voice,
                Forms = []
            };
        }

        var forms = new List<ConjugationForm>();

        // Generate a form for each possible ending
        for (int i = 0; i < endings.Length; i++)
        {
            var ending = endings[i];
            var form = verb.Stem + ending;

            // Check if this specific form appears in the corpus
            var inCorpus = _databaseService.IsVerbFormInCorpus(
                verb.Id,
                person,
                tense,
                voice,
                endingIndex: i
            );

            forms.Add(new ConjugationForm(
                Form: form,
                Ending: ending,
                EndingIndex: i,
                InCorpus: inCorpus
            ));
        }

        return new Conjugation
        {
            Person = person,
            Number = number,
            Tense = tense,
            Voice = voice,
            Forms = forms
        };
    }
}
