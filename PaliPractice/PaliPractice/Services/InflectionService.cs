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
    /// <param name="reflexive">
    /// Boolean for API simplicity. Internally converted to Voice enum for FormId encoding.
    /// </param>
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

        // Irregular patterns: fetch full forms from database
        if (noun.Irregular)
        {
            return GenerateIrregularNounForms(noun, nounCase, number);
        }

        // Regular patterns: compute stem + ending
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

        for (int i = 0; i < endings.Length; i++)
        {
            var ending = endings[i];
            var form = noun.Stem + ending;

            var endingId = EndingIdFromIndex(i);
            var formId = Declension.ResolveId(noun.LemmaId, nounCase, noun.Gender, number, endingId);

            var inCorpus = _databaseService.Nouns.IsFormInCorpus(
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

    Declension GenerateIrregularNounForms(Noun noun, Case nounCase, Number number)
    {
        var irregularForms = _databaseService.Nouns.GetIrregularForms(
            noun.LemmaId, nounCase, noun.Gender, number);

        var forms = new List<DeclensionForm>();

        for (int i = 0; i < irregularForms.Count; i++)
        {
            var form = irregularForms[i];
            var endingId = EndingIdFromIndex(i);
            var formId = Declension.ResolveId(noun.LemmaId, nounCase, noun.Gender, number, endingId);

            // Irregular forms from DB are always corpus-attested
            forms.Add(new DeclensionForm(
                FormId: formId,
                Form: form,
                Ending: form, // For irregular, ending = full form
                EndingId: endingId,
                InCorpus: true
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
                Voice = reflexive ? Voice.Reflexive : Voice.Active,
                Forms = []
            };
        }

        // Irregular patterns: fetch full forms from database
        if (verb.Irregular)
        {
            return GenerateIrregularVerbForms(verb, person, number, tense, reflexive);
        }

        // Regular patterns: compute stem + ending
        var endings = VerbEndings.GetEndings(
            verb.Pattern,
            person,
            number,
            tense,
            reflexive
        );

        var voice = reflexive ? Voice.Reflexive : Voice.Active;

        if (endings.Length == 0)
        {
            return new Conjugation
            {
                LemmaId = verb.LemmaId,
                Person = person,
                Number = number,
                Tense = tense,
                Voice = voice,
                Forms = []
            };
        }

        var forms = new List<ConjugationForm>();
        for (int i = 0; i < endings.Length; i++)
        {
            var ending = endings[i];
            var form = verb.Stem + ending;

            var endingId = EndingIdFromIndex(i);
            var formId = Conjugation.ResolveId(verb.LemmaId, tense, person, number, voice, endingId);

            var inCorpus = _databaseService.Verbs.IsFormInCorpus(
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
            Voice = reflexive ? Voice.Reflexive : Voice.Active,
            Forms = forms
        };
    }

    Conjugation GenerateIrregularVerbForms(
        Verb verb,
        Person person,
        Number number,
        Tense tense,
        bool reflexive)
    {
        var irregularForms = _databaseService.Verbs.GetIrregularForms(
            verb.LemmaId, tense, person, number, reflexive);

        var forms = new List<ConjugationForm>();

        var voice = reflexive ? Voice.Reflexive : Voice.Active;
        for (int i = 0; i < irregularForms.Count; i++)
        {
            var form = irregularForms[i];
            var endingId = EndingIdFromIndex(i);
            var formId = Conjugation.ResolveId(verb.LemmaId, tense, person, number, voice, endingId);

            // Irregular forms from DB are always corpus-attested
            forms.Add(new ConjugationForm(
                FormId: formId,
                Form: form,
                Ending: form, // For irregular, ending = full form
                EndingId: endingId,
                InCorpus: true
            ));
        }

        return new Conjugation
        {
            LemmaId = verb.LemmaId,
            Person = person,
            Number = number,
            Tense = tense,
            Voice = voice,
            Forms = forms
        };
    }
}
