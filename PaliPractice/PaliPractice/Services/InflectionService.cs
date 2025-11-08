using PaliPractice.Models.Inflection;

namespace PaliPractice.Services;

public interface IInflectionService
{
    /// <summary>
    /// Generate noun declension forms for a given noun and grammatical parameters.
    /// Returns a list of possible forms (usually 1, but can be multiple when there are alternative endings).
    /// </summary>
    List<Declension> GenerateNounForms(
        Noun noun,
        NounCase nounCase,
        Number number);

    /// <summary>
    /// Generate verb conjugation forms for a given verb and grammatical parameters.
    /// Returns a list of possible forms (usually 1, but can be multiple when there are alternative endings).
    /// </summary>
    List<Conjugation> GenerateVerbForms(
        Verb verb,
        Person person,
        Number number,
        Tense tense,
        Voice voice);
}

// TODO: clean or reject stems with symbols

public class InflectionService : IInflectionService
{
    private readonly IDatabaseService _databaseService;

    public InflectionService(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public List<Declension> GenerateNounForms(
        Noun noun,
        NounCase nounCase,
        Number number)
    {
        if (string.IsNullOrEmpty(noun.Pattern) || string.IsNullOrEmpty(noun.Stem))
        {
            return new List<Declension>();
        }

        // Get all possible endings for this pattern and grammatical parameters
        var endings = NounPatterns.GetEndings(noun.Pattern, nounCase, number);

        if (endings.Length == 0)
        {
            return new List<Declension>();
        }

        var declensions = new List<Declension>();

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

            declensions.Add(new Declension
            {
                Form = form,
                Ending = ending,
                CaseName = nounCase,
                Number = number,
                Gender = noun.Gender,
                EndingIndex = i,
                InCorpus = inCorpus
            });
        }

        return declensions;
    }

    public List<Conjugation> GenerateVerbForms(
        Verb verb,
        Person person,
        Number number,
        Tense tense,
        Voice voice)
    {
        if (string.IsNullOrEmpty(verb.Pattern) || string.IsNullOrEmpty(verb.Stem))
        {
            return new List<Conjugation>();
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
            return new List<Conjugation>();
        }

        var conjugations = new List<Conjugation>();

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

            conjugations.Add(new Conjugation
            {
                Form = form,
                Ending = ending,
                Person = person,
                Tense = tense,
                Voice = voice,
                EndingIndex = i,
                InCorpus = inCorpus
            });
        }

        return conjugations;
    }
}
