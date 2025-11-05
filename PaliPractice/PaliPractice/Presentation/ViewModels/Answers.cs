namespace PaliPractice.Presentation.ViewModels;

public sealed record ConjugationAnswer(Number Number, Person Person, Voice Voice, Tense Tense);

public sealed record DeclensionAnswer(Number Number, Gender Gender, NounCase Case);
