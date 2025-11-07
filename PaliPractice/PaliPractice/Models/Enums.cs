namespace PaliPractice.Models;

public enum Number
{
    None,
    Singular = 1,
    Plural = 2
}

public enum Person
{
    None,
    First = 1,
    Second = 2,
    Third = 3
}

public enum Voice
{
    None,
    Active = 1,
    Reflexive = 2,
    Passive = 3,
    Causative = 4
}

public enum Tense
{
    None,
    Present = 1,
    Future = 2,
    Aorist = 3,
    Imperfect = 4,
    Perfect = 5
}

public enum Mood
{
    None,
    Indicative = 1,
    Optative = 2,
    Imperative = 3,
    Conditional = 4
}

public enum Gender
{
    None,
    Masculine = 1,
    Neuter = 2,
    Feminine = 3
}

public enum NounCase
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

public enum Transitivity
{
    None,
    Transitive = 1,
    Intransitive = 2,
    Ditransitive = 3
}
