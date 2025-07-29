#!/usr/bin/env python3
"""Generate C# model classes for the inflection database."""

def generate_csharp_models():
    """Generate C# Entity Framework model classes."""
    csharp_code = '''using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PaliTrainer.Models
{
    [Table("headwords")]
    public class Headword
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        
        [Required]
        [Column("lemma_1")]
        public string Lemma1 { get; set; }
        
        [Required]
        [Column("lemma_clean")]
        public string LemmaClean { get; set; }
        
        [Required]
        [Column("pos")]
        public string PartOfSpeech { get; set; }
        
        [Column("stem")]
        public string Stem { get; set; }
        
        [Column("pattern")]
        public string Pattern { get; set; }
        
        [Column("meaning_1")]
        public string Meaning { get; set; }
        
        [Column("ebt_count")]
        public int EbtCount { get; set; }
        
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        
        // Navigation property
        public virtual ICollection<Inflection> Inflections { get; set; } = new List<Inflection>();
    }
    
    [Table("inflections")]
    public class Inflection
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        
        [Column("headword_id")]
        public int HeadwordId { get; set; }
        
        [Required]
        [Column("inflected_form")]
        public string InflectedForm { get; set; }
        
        // Nominal inflections
        [Column("case_name")]
        public string CaseName { get; set; }
        
        [Column("number")]
        public string Number { get; set; }
        
        [Column("gender")]
        public string Gender { get; set; }
        
        // Verbal inflections
        [Column("person")]
        public string Person { get; set; }
        
        [Column("tense")]
        public string Tense { get; set; }
        
        [Column("mood")]
        public string Mood { get; set; }
        
        [Column("voice")]
        public string Voice { get; set; }
        
        [Column("grammar_info")]
        public string GrammarInfo { get; set; }
        
        [Column("in_tipitaka")]
        public bool InTipitaka { get; set; }
        
        // Navigation property
        [ForeignKey("HeadwordId")]
        public virtual Headword Headword { get; set; }
    }
    
    [Table("patterns")]
    public class Pattern
    {
        [Key]
        [Column("pattern_name")]
        public string PatternName { get; set; }
        
        [Column("like_word")]
        public string LikeWord { get; set; }
        
        [Column("pos_category")]
        public string PosCategory { get; set; }
        
        [Column("template_data")]
        public string TemplateData { get; set; }
    }
}

// DbContext class for Entity Framework
using Microsoft.EntityFrameworkCore;

namespace PaliTrainer.Data
{
    public class PaliTrainerContext : DbContext
    {
        public DbSet<Headword> Headwords { get; set; }
        public DbSet<Inflection> Inflections { get; set; }
        public DbSet<Pattern> Patterns { get; set; }
        
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=training.db");
        }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure relationships
            modelBuilder.Entity<Inflection>()
                .HasOne(i => i.Headword)
                .WithMany(h => h.Inflections)
                .HasForeignKey(i => i.HeadwordId);
            
            // Add unique constraint for inflections
            modelBuilder.Entity<Inflection>()
                .HasIndex(i => new { i.HeadwordId, i.InflectedForm, i.GrammarInfo })
                .IsUnique();
        }
    }
}

// Enhanced service class for training logic
namespace PaliTrainer.Services
{
    public class TrainingService
    {
        private readonly PaliTrainerContext _context;
        private readonly Random _random = new Random();
        
        public TrainingService(PaliTrainerContext context)
        {
            _context = context;
        }
        
        // Get headwords by part of speech
        public async Task<List<Headword>> GetHeadwordsByPosAsync(string pos)
        {
            return await _context.Headwords
                .Where(h => h.PartOfSpeech == pos)
                .Include(h => h.Inflections)
                .ToListAsync();
        }
        
        // Get random noun for declension practice
        public async Task<Headword> GetRandomNounAsync()
        {
            var nouns = await _context.Headwords
                .Where(h => h.PartOfSpeech == "masc" || h.PartOfSpeech == "fem" || h.PartOfSpeech == "nt")
                .Include(h => h.Inflections)
                .ToListAsync();
            
            return nouns[_random.Next(nouns.Count)];
        }
        
        // Get random verb for conjugation practice
        public async Task<Headword> GetRandomVerbAsync()
        {
            var verbs = await _context.Headwords
                .Where(h => h.PartOfSpeech == "pr" || h.PartOfSpeech == "aor")
                .Include(h => h.Inflections)
                .ToListAsync();
            
            return verbs[_random.Next(verbs.Count)];
        }
        
        // Get inflection by specific grammatical form
        public async Task<Inflection> GetInflectionByCaseAsync(int headwordId, string caseName, string number)
        {
            return await _context.Inflections
                .FirstOrDefaultAsync(i => 
                    i.HeadwordId == headwordId && 
                    i.CaseName == caseName && 
                    i.Number == number);
        }
        
        // Get inflection by verbal form
        public async Task<Inflection> GetInflectionByVerbFormAsync(int headwordId, string person, string number, string tense)
        {
            return await _context.Inflections
                .FirstOrDefaultAsync(i => 
                    i.HeadwordId == headwordId && 
                    i.Person == person && 
                    i.Number == number &&
                    i.Tense == tense);
        }
        
        // Create a declension quiz question
        public async Task<DeclensonQuizQuestion> CreateDeclensionQuizAsync()
        {
            var noun = await GetRandomNounAsync();
            var cases = new[] { "nominative", "accusative", "instrumental", "dative", "ablative", "genitive", "locative", "vocative" };
            var numbers = new[] { "singular", "plural" };
            
            var targetCase = cases[_random.Next(cases.Length)];
            var targetNumber = numbers[_random.Next(numbers.Length)];
            
            var correctInflection = await GetInflectionByCaseAsync(noun.Id, targetCase, targetNumber);
            
            return new DeclensonQuizQuestion
            {
                Headword = noun,
                TargetCase = targetCase,
                TargetNumber = targetNumber,
                CorrectForm = correctInflection?.InflectedForm,
                Question = $"Decline '{noun.LemmaClean}' in {targetCase} {targetNumber}"
            };
        }
        
        // Create a conjugation quiz question
        public async Task<ConjugationQuizQuestion> CreateConjugationQuizAsync()
        {
            var verb = await GetRandomVerbAsync();
            var persons = new[] { "first", "second", "third" };
            var numbers = new[] { "singular", "plural" };
            
            var targetPerson = persons[_random.Next(persons.Length)];
            var targetNumber = numbers[_random.Next(numbers.Length)];
            var targetTense = verb.PartOfSpeech == "pr" ? "present" : "aorist";
            
            var correctInflection = await GetInflectionByVerbFormAsync(verb.Id, targetPerson, targetNumber, targetTense);
            
            return new ConjugationQuizQuestion
            {
                Headword = verb,
                TargetPerson = targetPerson,
                TargetNumber = targetNumber,
                TargetTense = targetTense,
                CorrectForm = correctInflection?.InflectedForm,
                Question = $"Conjugate '{verb.LemmaClean}' in {targetPerson} person {targetNumber} {targetTense}"
            };
        }
        
        // Verify user answer
        public bool VerifyAnswer(string userAnswer, string correctAnswer)
        {
            return string.Equals(userAnswer?.Trim(), correctAnswer?.Trim(), StringComparison.OrdinalIgnoreCase);
        }
    }
    
    // Quiz question models
    public class DeclensonQuizQuestion
    {
        public Headword Headword { get; set; }
        public string TargetCase { get; set; }
        public string TargetNumber { get; set; }
        public string CorrectForm { get; set; }
        public string Question { get; set; }
    }
    
    public class ConjugationQuizQuestion
    {
        public Headword Headword { get; set; }
        public string TargetPerson { get; set; }
        public string TargetNumber { get; set; }
        public string TargetTense { get; set; }
        public string CorrectForm { get; set; }
        public string Question { get; set; }
    }
}
'''
    
    with open("CSharpModels.cs", "w", encoding="utf-8") as f:
        f.write(csharp_code)
    
    print("✅ Generated enhanced C# model classes in CSharpModels.cs")
    print("\nKey improvements:")
    print("- Normalized database structure with proper relationships")
    print("- Individual columns for grammatical categories")
    print("- Enhanced service methods for quiz generation")
    print("- Specific methods for declension and conjugation practice")


if __name__ == "__main__":
    generate_csharp_models()
