using System.Diagnostics.CodeAnalysis;

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
}


public enum Gender
{
    None,
    Masculine = 1,
    Feminine = 2,
    Neuter = 3
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
/// Structure: Regular patterns (1-9) → _Irregular (100) → Irregulars (101+)
/// Traditional order for regulars: a, ā, e, o
/// Irregulars grouped by parent, alphabetical within groups.
///
/// Values are explicitly numbered for database stability.
/// </summary>
public enum VerbPattern
{
    // ReSharper disable once UnusedMember.Global
    None = 0,

    // ═══════════════════════════════════════════
    // REGULAR PATTERNS (1-9)
    // Traditional order: a, ā, e, o
    // ═══════════════════════════════════════════
    Ati = 1,          // "ati pr"
    Āti = 2,          // "āti pr"
    Eti = 3,          // "eti pr"
    Oti = 4,          // "oti pr"

    // ═══ Breakpoint: End of regular patterns ═══
    _Irregular = 100,

    // ═══════════════════════════════════════════
    // IRREGULAR PATTERNS (101+)
    // Grouped by parent, alphabetical within groups.
    // ═══════════════════════════════════════════

    // Irregulars → Ati
    Atthi = 101,      // "atthi pr"
    Dakkhati = 102,   // "dakkhati pr"
    Dammi = 103,      // "dammi pr"
    Hanati = 104,     // "hanati pr"
    Hoti = 105,       // "hoti pr"
    Kubbati = 106,    // "kubbati pr"
    Natthi = 107,     // "natthi pr"

    // Irregulars → Eti
    Eti2 = 111,       // "eti pr 2"

    // Irregulars → Oti
    Brūti = 121,      // "brūti pr"
    Karoti = 122,     // "karoti pr"
}

/// <summary>
/// Noun declension patterns organized by gender with breakpoint markers.
///
/// Gender-based numbering reflects Gender enum (Masc=1, Fem=2, Neut=3):
/// - Masculine: 101-149 base, 151-199 variant
/// - Feminine:  201-249 base, 251-299 variant
/// - Neuter:    301-349 base, 351-399 variant
/// - Irregular: 1101+ masc, 1201+ fem, 1301+ neut
///
/// Breakpoints enable O(1) categorization:
/// - 101 ≤ pattern &lt; 150 → Base Masculine
/// - 151 ≤ pattern &lt; 200 → Variant Masculine
/// - 201 ≤ pattern &lt; 250 → Base Feminine
/// - 251 ≤ pattern &lt; 300 → Variant Feminine (reserved)
/// - 301 ≤ pattern &lt; 350 → Base Neuter
/// - 351 ≤ pattern &lt; 1000 → Variant Neuter
/// - pattern ≥ 1001 → Irregular (forms from database)
///
/// Values are explicitly numbered for database stability.
/// </summary>
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public enum NounPattern
{
    // ReSharper disable once UnusedMember.Global
    None = 0,

    // ═══════════════════════════════════════════
    // BASE MASCULINE (101-149)
    // Traditional order: a, i, ī, u, ū, ar, ant, as
    // ═══════════════════════════════════════════
    AMasc = 101,      // "a masc"
    IMasc = 102,      // "i masc"
    ĪMasc = 103,      // "ī masc"
    UMasc = 104,      // "u masc"
    ŪMasc = 105,      // "ū masc"
    ArMasc = 106,     // "ar masc"
    AntMasc = 107,    // "ant masc"
    AsMasc = 108,     // "as masc"

    // ═══ Breakpoint: End of base masculine ═══
    _VariantMasc = 150,

    // ═══════════════════════════════════════════
    // VARIANT MASCULINE (151-199)
    // Grouped by parent base pattern
    // ═══════════════════════════════════════════
    A2Masc = 151,     // "a2 masc" → AMasc
    AMascEast = 152,  // "a masc east" → AMasc
    AMascPl = 153,    // "a masc pl" → AMasc
    AntaMasc = 154,   // "anta masc" → AntMasc
    Ar2Masc = 155,    // "ar2 masc" → ArMasc
    ĪMascPl = 156,    // "ī masc pl" → ĪMasc
    UMascPl = 157,    // "u masc pl" → UMasc

    // ═══ Breakpoint: End of masculine ═══
    _BaseFem = 200,

    // ═══════════════════════════════════════════
    // BASE FEMININE (201-249)
    // Traditional order: ā, i, ī, u, ar
    // ═══════════════════════════════════════════
    ĀFem = 201,       // "ā fem"
    IFem = 202,       // "i fem"
    ĪFem = 203,       // "ī fem"
    UFem = 204,       // "u fem"
    ArFem = 205,      // "ar fem"

    // ═══ Breakpoint: End of base feminine ═══
    _VariantFem = 250,

    // (No variant feminines currently — reserved 251-299)

    // ═══ Breakpoint: End of feminine ═══
    _BaseNeut = 300,

    // ═══════════════════════════════════════════
    // BASE NEUTER (301-349)
    // Traditional order: a, i, u
    // ═══════════════════════════════════════════
    ANeut = 301,      // "a nt"
    INeut = 302,      // "i nt"
    UNeut = 303,      // "u nt"

    // ═══ Breakpoint: End of base neuter ═══
    _VariantNeut = 350,

    // ═══════════════════════════════════════════
    // VARIANT NEUTER (351-399)
    // All derived from ANeut
    // ═══════════════════════════════════════════
    ANeutEast = 351,  // "a nt east" → ANeut
    ANeutIrreg = 352, // "a nt irreg" → ANeut
    ANeutPl = 353,    // "a nt pl" → ANeut

    // ═══ Breakpoint: End of computable patterns ═══
    _Irregular = 1000,

    // ═══════════════════════════════════════════
    // IRREGULAR PATTERNS (1101+ masc, 1201+ fem, 1301+ neut)
    // Forms must be read from database (DPD like='irreg')
    // Grouped by gender, alphabetical within groups
    // ═══════════════════════════════════════════

    // Irregular Masculine (9) — grouped by parent base
    AddhaMasc = 1101,     // "addha masc" → AMasc
    ArahantMasc = 1102,   // "arahant masc" → AntMasc
    BhavantMasc = 1103,   // "bhavant masc" → AntMasc
    BrahmaMasc = 1104,    // "brahma masc" → AMasc
    GoMasc = 1105,        // "go masc" → AMasc
    JantuMasc = 1106,     // "jantu masc" → UMasc
    RājaMasc = 1107,      // "rāja masc" → AMasc
    SantaMasc = 1108,     // "santa masc" → AntMasc
    YuvaMasc = 1109,      // "yuva masc" → AMasc

    // Irregular Feminine (6)
    JātiFem = 1201,       // "jāti fem" → IFem
    MātarFem = 1202,      // "mātar fem" → ArFem
    NadīFem = 1203,       // "nadī fem" → ĪFem
    ParisāFem = 1204,     // "parisā fem" → ĀFem
    PokkharaṇīFem = 1205, // "pokkharaṇī fem" → ĪFem
    RattiFem = 1206,      // "ratti fem" → IFem

    // Irregular Neuter (1)
    KammaNeut = 1301,     // "kamma nt" → ANeut
}
// ReSharper restore InconsistentNaming
