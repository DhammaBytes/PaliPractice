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


// Transitivity - not actively used yet
public enum Transitivity
{
    None,
    Transitive = 1,
    Intransitive = 2,
    Ditransitive = 3
}

/// <summary>
/// Verb conjugation patterns. All are present tense (pr).
///
/// IMPORTANT: Irregulars MUST be grouped immediately after their parent regular.
/// This grouping is for code readability only - the actual parent-child relationship
/// is defined in VerbPatternHelper.ParentRegular() and ChildIrregulars().
/// When adding new patterns, place them under the correct parent and update the helper.
/// </summary>
public enum VerbPattern
{
    // ═══ Regular: -ati ═══
    Ati,          // "ati pr"
    // Irregulars → Ati
    Hoti,         // "hoti pr"
    Atthi,        // "atthi pr"
    Natthi,       // "natthi pr"
    Dakkhati,     // "dakkhati pr"
    Dammi,        // "dammi pr"
    Hanati,       // "hanati pr"

    // ═══ Regular: -eti ═══
    Eti,          // "eti pr"
    // Irregulars → Eti
    Eti2,         // "eti pr 2"

    // ═══ Regular: -oti ═══
    Oti,          // "oti pr"
    // Irregulars → Oti
    Karoti,       // "karoti pr"
    Brūti,        // "brūti pr"
    Kubbati,      // "kubbati pr"

    // ═══ Regular: -āti ═══
    Āti,          // "āti pr"
    // (no irregulars)
}

/// <summary>
/// Noun declension patterns by gender.
///
/// IMPORTANT: Irregulars MUST be grouped immediately after their parent regular.
/// This grouping is for code readability only - the actual parent-child relationship
/// is defined in NounPatternHelper.ParentRegular() and ChildIrregulars().
/// When adding new patterns, place them under the correct parent and update the helper.
/// Special patterns like DviCard (cardinal numbers) have no parent and are always excluded.
/// </summary>
public enum NounPattern
{
    // ═══════════════════════════════════════════
    // MASCULINE
    // ═══════════════════════════════════════════

    // ═══ Regular: -a masc ═══
    AMasc,            // "a masc"
    // Irregulars → AMasc
    RājaMasc,         // "rāja masc"
    BrahmaMasc,       // "brahma masc"
    A2Masc,           // "a2 masc"
    AMascEast,        // "a masc east"
    AMascPl,          // "a masc pl"
    AddhaMasc,        // "addha masc"
    GoMasc,           // "go masc"
    YuvaMasc,         // "yuva masc"

    // ═══ Regular: -i masc ═══
    IMasc,            // "i masc"
    // (no irregulars)

    // ═══ Regular: -ī masc ═══
    ĪMasc,            // "ī masc"
    // Irregulars → ĪMasc
    ĪMascPl,          // "ī masc pl"

    // ═══ Regular: -u masc ═══
    UMasc,            // "u masc"
    // Irregulars → UMasc
    JantuMasc,        // "jantu masc"
    UMascPl,          // "u masc pl"

    // ═══ Regular: -ū masc ═══
    ŪMasc,            // "ū masc"
    // (no irregulars)

    // ═══ Regular: -as masc ═══
    AsMasc,           // "as masc"
    // (no irregulars)

    // ═══ Regular: -ar masc ═══
    ArMasc,           // "ar masc"
    // Irregulars → ArMasc
    Ar2Masc,          // "ar2 masc"

    // ═══ Regular: -ant masc ═══
    AntMasc,          // "ant masc"
    // Irregulars → AntMasc
    AntaMasc,         // "anta masc"
    ArahantMasc,      // "arahant masc"
    BhavantMasc,      // "bhavant masc"
    SantaMasc,        // "santa masc"

    // ═══════════════════════════════════════════
    // NEUTER
    // ═══════════════════════════════════════════

    // ═══ Regular: -a nt ═══
    ANeut,            // "a nt"
    // Irregulars → ANeut
    KammaNeut,        // "kamma nt"
    ANeutEast,        // "a nt east"
    ANeutIrreg,       // "a nt irreg"
    ANeutPl,          // "a nt pl"

    // ═══ Regular: -i nt ═══
    INeut,            // "i nt"
    // (no irregulars)

    // ═══ Regular: -u nt ═══
    UNeut,            // "u nt"
    // (no irregulars)

    // ═══════════════════════════════════════════
    // FEMININE
    // ═══════════════════════════════════════════

    // ═══ Regular: -ā fem ═══
    ĀFem,             // "ā fem"
    // Irregulars → ĀFem
    ParisāFem,        // "parisā fem"

    // ═══ Regular: -i fem ═══
    IFem,             // "i fem"
    // Irregulars → IFem
    RattiFem,         // "ratti fem"

    // ═══ Regular: -ī fem ═══
    ĪFem,             // "ī fem"
    // Irregulars → ĪFem
    JātiFem,          // "jāti fem"
    NadīFem,          // "nadī fem"
    PokkharaṇīFem,    // "pokkharaṇī fem"

    // ═══ Regular: -u fem ═══
    UFem,             // "u fem"
    // (no irregulars)

    // ═══ Regular: -ar fem ═══
    ArFem,            // "ar fem"
    // Irregulars → ArFem
    MātarFem,         // "mātar fem"

    // ═══════════════════════════════════════════
    // SPECIAL (no parent)
    // ═══════════════════════════════════════════
    DviCard,          // "dvi card" - cardinal number
}
