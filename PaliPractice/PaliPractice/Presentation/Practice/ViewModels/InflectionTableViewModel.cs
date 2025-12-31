using PaliPractice.Models;
using PaliPractice.Models.Inflection;
using PaliPractice.Models.Words;
using PaliPractice.Services.Grammar;

namespace PaliPractice.Presentation.Practice.ViewModels;

/// <summary>
/// Navigation data for inflection table page.
/// DataViewMap requires reference types for navigation data.
/// </summary>
public record InflectionTableNavigationData(ILemma Lemma, PracticeType PracticeType);

/// <summary>
/// Represents a single form display with stem and ending parts.
/// </summary>
public record FormDisplay(string Stem, string Ending, bool InCorpus);

/// <summary>
/// Represents a table cell containing 1-N form variants.
/// </summary>
public record TableCell(IReadOnlyList<FormDisplay> Forms);

/// <summary>
/// ViewModel for the universal inflection table page.
/// Displays complete declension or conjugation tables for a lemma.
/// </summary>
[Bindable]
public partial class InflectionTableViewModel : ObservableObject
{
    readonly INavigator _navigator;

    public InflectionTableViewModel(
        INavigator navigator,
        IInflectionService inflectionService,
        InflectionTableNavigationData data)
    {
        _navigator = navigator;
        Lemma = data.Lemma;
        PracticeType = data.PracticeType;
        IsNoun = data.PracticeType == PracticeType.Declension;

        GenerateTableData(inflectionService);
    }

    public ILemma Lemma { get; }
    public PracticeType PracticeType { get; }
    public bool IsNoun { get; }
    public bool IsVerb => !IsNoun;

    /// <summary>
    /// Title for the page: "Declension Table" or "Conjugation Table"
    /// </summary>
    public string PageTitle => IsNoun ? "Declension Table" : "Conjugation Table";

    /// <summary>
    /// The lemma name (bold, Pali font)
    /// </summary>
    [ObservableProperty]
    string _lemmaName = "";

    /// <summary>
    /// The pattern name like "a masc" or "ati pr" (bold)
    /// </summary>
    [ObservableProperty]
    string _patternName = "";

    /// <summary>
    /// The type name: "declension" or "conjugation" (normal)
    /// </summary>
    public string TypeName => IsNoun ? "declension" : "conjugation";

    /// <summary>
    /// The "like" example lemma from DPD (bold, Pali font)
    /// </summary>
    [ObservableProperty]
    string _likeExample = "";

    /// <summary>
    /// Column headers: ["masc sg", "masc pl"] for nouns, ["sg", "pl", "reflx sg", "reflx pl"] for verbs
    /// </summary>
    [ObservableProperty]
    IReadOnlyList<string> _columnHeaders = [];

    /// <summary>
    /// Row headers: case names for nouns, tense+person for verbs
    /// </summary>
    [ObservableProperty]
    IReadOnlyList<string> _rowHeaders = [];

    /// <summary>
    /// 2D array of cells indexed by [row][column]
    /// </summary>
    [ObservableProperty]
    IReadOnlyList<IReadOnlyList<TableCell>> _cells = [];

    public ICommand GoBackCommand => new AsyncRelayCommand(() => _navigator.NavigateBackAsync(this));

    void GenerateTableData(IInflectionService inflectionService)
    {
        if (IsNoun)
            GenerateNounTable(inflectionService);
        else
            GenerateVerbTable(inflectionService);
    }

    void GenerateNounTable(IInflectionService inflectionService)
    {
        var noun = Lemma.Primary as Noun;
        if (noun == null) return;

        // Header info
        LemmaName = noun.Lemma;
        PatternName = noun.RawPattern;
        LikeExample = noun.Pattern.GetLikeExample();

        var genderLabel = noun.Gender switch
        {
            Gender.Masculine => "masc",
            Gender.Feminine => "fem",
            Gender.Neuter => "nt",
            _ => ""
        };

        // Column headers based on gender and plural-only status
        var columns = new List<string>();
        if (!noun.PluralOnly)
            columns.Add($"{genderLabel} sg");
        columns.Add($"{genderLabel} pl");
        ColumnHeaders = columns;

        // Row headers: 8 grammatical cases
        RowHeaders = new[]
        {
            "nom", "acc", "instr", "dat", "abl", "gen", "loc", "voc"
        };

        // Generate cells for each case × number
        var cases = new[] { Case.Nominative, Case.Accusative, Case.Instrumental, Case.Dative,
                           Case.Ablative, Case.Genitive, Case.Locative, Case.Vocative };

        var rows = new List<IReadOnlyList<TableCell>>();
        foreach (var @case in cases)
        {
            var rowCells = new List<TableCell>();

            if (!noun.PluralOnly)
            {
                var sgDeclension = inflectionService.GenerateNounForms(noun, @case, Number.Singular);
                rowCells.Add(CreateTableCell(sgDeclension.Forms, noun.Stem));
            }

            var plDeclension = inflectionService.GenerateNounForms(noun, @case, Number.Plural);
            rowCells.Add(CreateTableCell(plDeclension.Forms, noun.Stem));

            rows.Add(rowCells);
        }

        Cells = rows;
    }

    void GenerateVerbTable(IInflectionService inflectionService)
    {
        var verb = Lemma.Primary as Verb;
        if (verb == null) return;

        // Header info
        LemmaName = verb.Lemma;
        PatternName = verb.RawPattern;
        LikeExample = verb.Pattern.GetLikeExample();

        // Check if verb has reflexive forms by testing one combination
        var testReflexive = inflectionService.GenerateVerbForms(verb, Person.Third, Number.Singular, Tense.Present, reflexive: true);
        var hasReflexive = testReflexive.Forms.Count > 0;

        // Column headers
        var columns = new List<string> { "sg", "pl" };
        if (hasReflexive)
        {
            columns.Add("reflx sg");
            columns.Add("reflx pl");
        }
        ColumnHeaders = columns;

        // Row headers: 4 tenses × 3 persons = 12 rows
        var rowHeadersList = new List<string>();
        var tenses = new[] { Tense.Present, Tense.Imperative, Tense.Optative, Tense.Future };
        var tenseAbbrevs = new Dictionary<Tense, string>
        {
            [Tense.Present] = "pr",
            [Tense.Imperative] = "imp",
            [Tense.Optative] = "opt",
            [Tense.Future] = "fut"
        };
        var persons = new[] { Person.Third, Person.Second, Person.First };
        var personLabels = new Dictionary<Person, string>
        {
            [Person.Third] = "3rd",
            [Person.Second] = "2nd",
            [Person.First] = "1st"
        };

        foreach (var tense in tenses)
        {
            foreach (var person in persons)
            {
                rowHeadersList.Add($"{tenseAbbrevs[tense]} {personLabels[person]}");
            }
        }
        RowHeaders = rowHeadersList;

        // Generate cells for each tense × person × number × voice
        var rows = new List<IReadOnlyList<TableCell>>();
        foreach (var tense in tenses)
        {
            foreach (var person in persons)
            {
                var rowCells = new List<TableCell>();

                // Active singular
                var activeSg = inflectionService.GenerateVerbForms(verb, person, Number.Singular, tense, reflexive: false);
                rowCells.Add(CreateTableCell(activeSg.Forms, verb.Stem));

                // Active plural
                var activePl = inflectionService.GenerateVerbForms(verb, person, Number.Plural, tense, reflexive: false);
                rowCells.Add(CreateTableCell(activePl.Forms, verb.Stem));

                if (hasReflexive)
                {
                    // Reflexive singular
                    var reflexSg = inflectionService.GenerateVerbForms(verb, person, Number.Singular, tense, reflexive: true);
                    rowCells.Add(CreateTableCell(reflexSg.Forms, verb.Stem));

                    // Reflexive plural
                    var reflexPl = inflectionService.GenerateVerbForms(verb, person, Number.Plural, tense, reflexive: true);
                    rowCells.Add(CreateTableCell(reflexPl.Forms, verb.Stem));
                }

                rows.Add(rowCells);
            }
        }

        Cells = rows;
    }

    static TableCell CreateTableCell(IReadOnlyList<DeclensionForm> forms, string? stem)
    {
        var formDisplays = new List<FormDisplay>();
        foreach (var form in forms)
        {
            // Calculate stem part (form minus ending)
            var stemPart = stem ?? "";
            var endingPart = form.Ending;

            // If stem exists and form starts with stem, use that split
            if (!string.IsNullOrEmpty(stem) && form.Form.StartsWith(stem))
            {
                stemPart = stem;
                endingPart = form.Form[stem.Length..];
            }
            else
            {
                // Fallback: use form without splitting
                stemPart = "";
                endingPart = form.Form;
            }

            formDisplays.Add(new FormDisplay(stemPart, endingPart, form.InCorpus));
        }
        return new TableCell(formDisplays);
    }

    static TableCell CreateTableCell(IReadOnlyList<ConjugationForm> forms, string? stem)
    {
        var formDisplays = new List<FormDisplay>();
        foreach (var form in forms)
        {
            // Calculate stem part (form minus ending)
            var stemPart = stem ?? "";
            var endingPart = form.Ending;

            // If stem exists and form starts with stem, use that split
            if (!string.IsNullOrEmpty(stem) && form.Form.StartsWith(stem))
            {
                stemPart = stem;
                endingPart = form.Form[stem.Length..];
            }
            else
            {
                // Fallback: use form without splitting
                stemPart = "";
                endingPart = form.Form;
            }

            formDisplays.Add(new FormDisplay(stemPart, endingPart, form.InCorpus));
        }
        return new TableCell(formDisplays);
    }
}
