namespace PaliPractice.Models;

public enum Case
{
    None,
    Nominative = 1,
    Accusative = 2,
    Instrumental = 3,
    Dative = 4,
    Ablative = 5,
    Genitive = 6,
    Locative = 7,
    Vocative = 8
}

public enum Tense
{
    None,
    Present = 1,
    Imperative = 2,
    Optative = 3,
    Future = 4
    // Aorist = 5
}


public enum Gender
{
    None,
    Masculine = 1,
    Neuter = 2,
    Feminine = 3
}

public enum Person
{
    None,
    First = 1,
    Second = 2,
    Third = 3
}

public enum Number
{
    None,
    Singular = 1,
    Plural = 2
}

/// <summary>
/// Voice for verb conjugations.
/// Pali grammar traditionally distinguishes between active (parassapada)
/// and middle/reflexive (attanopada) forms
/// </summary>
public enum Voice
{
    None,
    /// <summary>Standard/active voice (parassapada).</summary>
    Active = 1,
    /// <summary>Reflexive/middle voice (attanopada).</summary>
    Reflexive = 2
}

// Transitivity - not actively used yet
public enum Transitivity
{
    None,
    Transitive = 1,
    Intransitive = 2,
    Ditransitive = 3
}

// ReSharper disable InconsistentNaming
/// <summary>
/// Verb conjugation patterns. All are present tense (pr).
///
/// Structure: Regular patterns → _Irregular → All Irregulars
/// Traditional order for regulars: a, ā, e, o
/// Irregulars grouped by parent, alphabetical within groups.
/// </summary>
public enum VerbPattern
{
    // ReSharper disable once UnusedMember.Global
    None = 0,
    // ═══════════════════════════════════════════
    // REGULAR PATTERNS (pattern < _Irregular)
    // Traditional order: a, ā, e, o
    // ═══════════════════════════════════════════
    Ati = 1,          // "ati pr"
    Āti = 2,          // "āti pr"
    Eti = 3,          // "eti pr"
    Oti = 4,          // "oti pr"

    // ═══ Breakpoint: End of regular patterns ═══
    _Irregular,

    // ═══════════════════════════════════════════
    // IRREGULAR PATTERNS (pattern > _Irregular)
    // Grouped by parent, alphabetical within groups.
    // ═══════════════════════════════════════════

    // Irregulars → Ati
    Atthi,        // "atthi pr"
    Dakkhati,     // "dakkhati pr"
    Dammi,        // "dammi pr"
    Hanati,       // "hanati pr"
    Hoti,         // "hoti pr"
    Kubbati,      // "kubbati pr"
    Natthi,       // "natthi pr"

    // Irregulars → Eti
    Eti2,         // "eti pr 2"

    // Irregulars → Oti
    Brūti,        // "brūti pr"
    Karoti,       // "karoti pr"
}

/// <summary>
/// Noun declension patterns organized by gender with breakpoint markers.
///
/// Three-tier structure per gender: Base → Variant → (next gender or Irregular)
///
/// Breakpoints enable O(1) categorization:
/// - pattern &lt; _VariantMasc → Base Masculine
/// - _VariantMasc &lt; pattern &lt; _BaseFem → Variant Masculine
/// - _BaseFem &lt; pattern &lt; _BaseNeut → Base Feminine (no variants currently)
/// - _BaseNeut &lt; pattern &lt; _VariantNeut → Base Neuter
/// - _VariantNeut &lt; pattern &lt; _Irregular → Variant Neuter
/// - pattern &gt; _Irregular → Irregular (forms from HTML, not stem+ending)
///
/// Base patterns: common stem+ending construction
/// Variant patterns: alternate ending tables, still computable
/// Irregular patterns: full forms must be read from database
/// </summary>
public enum NounPattern
{
    // ReSharper disable once UnusedMember.Global
    None = 0,

    // ═══════════════════════════════════════════
    // BASE MASCULINE (pattern < _VariantMasc)
    // Traditional order: a, i, ī, u, ū, ar, ant, as
    // ═══════════════════════════════════════════
    AMasc = 1,        // "a masc"
    IMasc,            // "i masc"
    ĪMasc,            // "ī masc"
    UMasc,            // "u masc"
    ŪMasc,            // "ū masc"
    ArMasc,           // "ar masc"
    AntMasc,          // "ant masc"
    AsMasc,           // "as masc"

    // ═══ Breakpoint: End of base masculine ═══
    _VariantMasc,

    // ═══════════════════════════════════════════
    // VARIANT MASCULINE (_VariantMasc < pattern < _BaseFem)
    // Grouped by parent base pattern
    // ═══════════════════════════════════════════
    A2Masc,           // "a2 masc" → AMasc
    AMascEast,        // "a masc east" → AMasc
    AMascPl,          // "a masc pl" → AMasc
    AntaMasc,         // "anta masc" → AntMasc
    Ar2Masc,          // "ar2 masc" → ArMasc
    ĪMascPl,          // "ī masc pl" → ĪMasc
    UMascPl,          // "u masc pl" → UMasc

    // ═══ Breakpoint: End of masculine ═══
    _BaseFem,

    // ═══════════════════════════════════════════
    // BASE FEMININE (_BaseFem < pattern < _BaseNeut)
    // Traditional order: ā, i, ī, u, ar
    // No variant feminines currently
    // ═══════════════════════════════════════════
    ĀFem,             // "ā fem"
    IFem,             // "i fem"
    ĪFem,             // "ī fem"
    UFem,             // "u fem"
    ArFem,            // "ar fem"

    // ═══ Breakpoint: End of feminine ═══
    _BaseNeut,

    // ═══════════════════════════════════════════
    // BASE NEUTER (_BaseNeut < pattern < _VariantNeut)
    // Traditional order: a, i, u
    // ═══════════════════════════════════════════
    ANeut,            // "a nt"
    INeut,            // "i nt"
    UNeut,            // "u nt"

    // ═══ Breakpoint: End of base neuter ═══
    _VariantNeut,

    // ═══════════════════════════════════════════
    // VARIANT NEUTER (_VariantNeut < pattern < _Irregular)
    // All derived from ANeut
    // ═══════════════════════════════════════════
    ANeutEast,        // "a nt east" → ANeut
    ANeutIrreg,       // "a nt irreg" → ANeut
    ANeutPl,          // "a nt pl" → ANeut

    // ═══ Breakpoint: End of computable patterns ═══
    _Irregular,

    // ═══════════════════════════════════════════
    // IRREGULAR PATTERNS (pattern > _Irregular)
    // Forms must be read from database (DPD like='irreg')
    // Grouped by gender, alphabetical within groups
    // ═══════════════════════════════════════════

    // Irregular Masculine (9) — grouped by parent base
    AddhaMasc,        // "addha masc" → AMasc
    ArahantMasc,      // "arahant masc" → AntMasc
    BhavantMasc,      // "bhavant masc" → AntMasc
    BrahmaMasc,       // "brahma masc" → AMasc
    GoMasc,           // "go masc" → AMasc
    JantuMasc,        // "jantu masc" → UMasc
    RājaMasc,         // "rāja masc" → AMasc
    SantaMasc,        // "santa masc" → AntMasc
    YuvaMasc,         // "yuva masc" → AMasc

    // Irregular Feminine (6)
    JātiFem,          // "jāti fem" → IFem
    MātarFem,         // "mātar fem" → ArFem
    NadīFem,          // "nadī fem" → ĪFem
    ParisāFem,        // "parisā fem" → ĀFem
    PokkharaṇīFem,    // "pokkharaṇī fem" → ĪFem
    RattiFem,         // "ratti fem" → IFem

    // Irregular Neuter (1)
    KammaNeut,        // "kamma nt" → ANeut
}
// ReSharper restore InconsistentNaming
