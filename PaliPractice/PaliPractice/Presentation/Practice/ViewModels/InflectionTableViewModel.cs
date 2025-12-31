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
    /// Whether the pattern is irregular (forms from database lookup).
    /// </summary>
    [ObservableProperty]
    bool _isIrregular;

    /// <summary>
    /// Whether the pattern is a variant (non-standard endings).
    /// </summary>
    [ObservableProperty]
    bool _isVariantPattern;

    /// <summary>
    /// The type name: "declension" or "conjugation", with "irregular" or "non-standard" prefix if applicable.
    /// </summary>
    public string TypeName
    {
        get
        {
            var baseType = IsNoun ? "declension" : "conjugation";
            if (IsIrregular) return $"irregular {baseType}";
            if (IsVariantPattern) return $"non-standard {baseType}";
            return baseType;
        }
    }

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
        if (Lemma.Primary is not Noun noun) return;

        // Header info
        LemmaName = noun.Lemma;
        PatternName = noun.RawPattern;
        LikeExample = noun.Pattern.GetLikeExample();
        IsIrregular = noun.Irregular;
        IsVariantPattern = noun.IsVariant;

        var genderLabel = Declension.GenderAbbreviations.GetValueOrDefault(noun.Gender, "");
        var sgLabel = Declension.NumberAbbreviations[Number.Singular];
        var plLabel = Declension.NumberAbbreviations[Number.Plural];

        // Column headers based on gender and plural-only status
        var columns = new List<string>();
        if (!noun.PluralOnly)
            columns.Add($"{genderLabel} {sgLabel}");
        columns.Add($"{genderLabel} {plLabel}");
        ColumnHeaders = columns;

        // Row headers: 8 grammatical cases
        var cases = new[] { Case.Nominative, Case.Accusative, Case.Instrumental, Case.Dative,
                           Case.Ablative, Case.Genitive, Case.Locative, Case.Vocative };
        RowHeaders = cases.Select(c => Declension.CaseAbbreviations[c]).ToList();

        // Generate cells for each case × number

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
        IsIrregular = verb.Irregular;
        IsVariantPattern = false; // Verbs don't have variant patterns

        // Check if verb has reflexive forms by testing one combination
        var testReflexive = inflectionService.GenerateVerbForms(verb, Person.Third, Number.Singular, Tense.Present, reflexive: true);
        var hasReflexive = testReflexive.Forms.Count > 0;

        var sgLabel = Conjugation.NumberAbbreviations[Number.Singular];
        var plLabel = Conjugation.NumberAbbreviations[Number.Plural];
        var reflxLabel = Conjugation.ReflexiveAbbrev;

        // Column headers
        var columns = new List<string> { sgLabel, plLabel };
        if (hasReflexive)
        {
            columns.Add($"{reflxLabel} {sgLabel}");
            columns.Add($"{reflxLabel} {plLabel}");
        }
        ColumnHeaders = columns;

        // Row headers: 4 tenses × 3 persons = 12 rows
        var rowHeadersList = new List<string>();
        var tenses = new[] { Tense.Present, Tense.Imperative, Tense.Optative, Tense.Future };
        var persons = new[] { Person.Third, Person.Second, Person.First };

        foreach (var tense in tenses)
        {
            foreach (var person in persons)
            {
                var tenseAbbrev = Conjugation.TenseAbbreviations[tense];
                var personAbbrev = Conjugation.PersonAbbreviations[person];
                rowHeadersList.Add($"{tenseAbbrev} {personAbbrev}");
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
