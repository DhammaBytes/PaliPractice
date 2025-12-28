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
    // ═══════════════════════════════════════════
    // REGULAR PATTERNS (pattern < _Irregular)
    // Traditional order: a, ā, e, o
    // ═══════════════════════════════════════════
    Ati,          // "ati pr"
    Āti,          // "āti pr"
    Eti,          // "eti pr"
    Oti,          // "oti pr"

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
/// Structure: Regular Masculine → _RegularFem → Regular Feminine → _RegularNeut → Regular Neuter → _Irregular → All Irregulars
///
/// This ordering enables simple comparisons:
/// - pattern before _RegularFem means regular masculine
/// - pattern after _RegularFem and before _RegularNeut means feminine
/// - pattern after _Irregular means irregular
///
/// Irregulars are grouped by their parent regular pattern for readability.
/// </summary>
public enum NounPattern
{
    // ═══════════════════════════════════════════
    // REGULAR MASCULINE (pattern < _RegularFem)
    // Traditional order: a, i, ī, u, ū, ar, ant, as
    // ═══════════════════════════════════════════
    AMasc,            // "a masc"
    IMasc,            // "i masc"
    ĪMasc,            // "ī masc"
    UMasc,            // "u masc"
    ŪMasc,            // "ū masc"
    ArMasc,           // "ar masc"
    AntMasc,          // "ant masc"
    AsMasc,           // "as masc"

    // ═══ Breakpoint: End of regular masculine ═══
    _RegularFem,

    // ═══════════════════════════════════════════
    // REGULAR FEMININE (_RegularFem < pattern < _RegularNeut)
    // Traditional order: ā, i, ī, u, ar
    // ═══════════════════════════════════════════
    ĀFem,             // "ā fem"
    IFem,             // "i fem"
    ĪFem,             // "ī fem"
    UFem,             // "u fem"
    ArFem,            // "ar fem"

    // ═══ Breakpoint: End of regular feminine ═══
    _RegularNeut,

    // ═══════════════════════════════════════════
    // REGULAR NEUTER (_RegularNeut < pattern < _Irregular)
    // Traditional order: a, i, u
    // ═══════════════════════════════════════════
    ANeut,            // "a nt"
    INeut,            // "i nt"
    UNeut,            // "u nt"

    // ═══ Breakpoint: End of regular patterns ═══
    _Irregular,

    // ═══════════════════════════════════════════
    // IRREGULAR PATTERNS (pattern >= _Irregular)
    // Grouped by parent, alphabetical within groups.
    // ═══════════════════════════════════════════

    // Irregulars → AMasc
    AddhaMasc,        // "addha masc"
    AMascEast,        // "a masc east"
    AMascPl,          // "a masc pl"
    A2Masc,           // "a2 masc"
    BrahmaMasc,       // "brahma masc"
    GoMasc,           // "go masc"
    RājaMasc,         // "rāja masc"
    YuvaMasc,         // "yuva masc"

    // Irregulars → ĪMasc
    ĪMascPl,          // "ī masc pl"

    // Irregulars → UMasc
    JantuMasc,        // "jantu masc"
    UMascPl,          // "u masc pl"

    // Irregulars → ArMasc
    Ar2Masc,          // "ar2 masc"

    // Irregulars → AntMasc
    AntaMasc,         // "anta masc"
    ArahantMasc,      // "arahant masc"
    BhavantMasc,      // "bhavant masc"
    SantaMasc,        // "santa masc"

    // Irregulars → ĀFem
    ParisāFem,        // "parisā fem"

    // Irregulars → IFem — note: jāti ends in short i
    JātiFem,          // "jāti fem"
    RattiFem,         // "ratti fem"

    // Irregulars → ĪFem
    NadīFem,          // "nadī fem"
    PokkharaṇīFem,    // "pokkharaṇī fem"

    // Irregulars → ArFem
    MātarFem,         // "mātar fem"

    // Irregulars → ANeut
    ANeutEast,        // "a nt east"
    ANeutIrreg,       // "a nt irreg"
    ANeutPl,          // "a nt pl"
    KammaNeut,        // "kamma nt"
}
// ReSharper restore InconsistentNaming
