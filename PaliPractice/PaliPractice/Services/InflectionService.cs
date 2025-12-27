using PaliPractice.Models.Inflection;
using PaliPractice.Models.Words;

namespace PaliPractice.Services;

public interface IInflectionService
{
    /// <summary>
    /// Generate a grouped noun declension for a given noun and grammatical parameters.
    /// Returns a single Declension containing 1-N possible form variants.
    /// </summary>
    Declension GenerateNounForms(
        Noun noun,
        Case nounCase,
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
        bool reflexive);
}

public class InflectionService : IInflectionService
{
    readonly IDatabaseService _databaseService;

    public InflectionService(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    /// <summary>
    /// Convert 0-based array index to 1-based EndingId.
    /// EndingId=0 is reserved for combination references.
    /// </summary>
    static int EndingIdFromIndex(int index) => index + 1;

    public Declension GenerateNounForms(
        Noun noun,
        Case nounCase,
        Number number)
    {
        if (string.IsNullOrEmpty(noun.RawPattern) || string.IsNullOrEmpty(noun.Stem))
        {
            return new Declension
            {
                LemmaId = noun.LemmaId,
                Case = nounCase,
                Number = number,
                Gender = noun.Gender,
                Forms = []
            };
        }

        // Get all possible endings for this pattern and grammatical parameters
        var endings = NounEndings.GetEndings(noun.Pattern, nounCase, number);

        if (endings.Length == 0)
        {
            return new Declension
            {
                LemmaId = noun.LemmaId,
                Case = nounCase,
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
            // For irregular patterns, endings ARE the full forms
            // For regular patterns, concatenate stem + ending
            var form = noun.Irregular ? ending : noun.Stem + ending;

            // Convert array index to 1-based EndingId
            var endingId = EndingIdFromIndex(i);
            var formId = Declension.ResolveId(noun.LemmaId, nounCase, noun.Gender, number, endingId);

            // Check if this specific form appears in the corpus
            var inCorpus = _databaseService.IsNounFormInCorpus(
                noun.LemmaId,
                nounCase,
                noun.Gender,
                number,
                endingId
            );

            forms.Add(new DeclensionForm(
                FormId: formId,
                Form: form,
                Ending: ending,
                EndingId: endingId,
                InCorpus: inCorpus
            ));
        }

        return new Declension
        {
            LemmaId = noun.LemmaId,
            Case = nounCase,
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
        bool reflexive)
    {
        if (string.IsNullOrEmpty(verb.RawPattern) || string.IsNullOrEmpty(verb.Stem))
        {
            return new Conjugation
            {
                LemmaId = verb.LemmaId,
                Person = person,
                Number = number,
                Tense = tense,
                Reflexive = reflexive,
                Forms = []
            };
        }

        // Get all possible endings for this pattern and grammatical parameters
        var endings = VerbEndings.GetEndings(
            verb.Pattern,
            person,
            number,
            tense,
            reflexive
        );

        if (endings.Length == 0)
        {
            return new Conjugation
            {
                LemmaId = verb.LemmaId,
                Person = person,
                Number = number,
                Tense = tense,
                Reflexive = reflexive,
                Forms = []
            };
        }

        var forms = new List<ConjugationForm>();

        // Generate a form for each possible ending
        for (int i = 0; i < endings.Length; i++)
        {
            var ending = endings[i];
            // For irregular patterns, endings ARE the full forms
            // For regular patterns, concatenate stem + ending
            var form = verb.Irregular ? ending : verb.Stem + ending;

            // Convert array index to 1-based EndingId
            var endingId = EndingIdFromIndex(i);
            var formId = Conjugation.ResolveId(verb.LemmaId, tense, person, number, reflexive, endingId);

            // Check if this specific form appears in the corpus
            var inCorpus = _databaseService.IsVerbFormInCorpus(
                verb.LemmaId,
                tense,
                person,
                number,
                reflexive,
                endingId
            );

            forms.Add(new ConjugationForm(
                FormId: formId,
                Form: form,
                Ending: ending,
                EndingId: endingId,
                InCorpus: inCorpus
            ));
        }

        return new Conjugation
        {
            LemmaId = verb.LemmaId,
            Person = person,
            Number = number,
            Tense = tense,
            Reflexive = reflexive,
            Forms = forms
        };
    }
}
