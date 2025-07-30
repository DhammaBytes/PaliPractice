# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

PaliPractice is a cross-platform language learning app for practicing Pali noun declensions and verb conjugations. Built with .NET 9.0 and Uno Platform for Windows, Mac, Linux, Android, and iOS.

## Key Commands

### Build and Run
```bash
# Build for desktop (Windows/Mac/Linux)
dotnet build --framework net9.0-desktop

# Run on desktop
dotnet run --framework net9.0-desktop

# Build for other platforms
dotnet build --framework net9.0-ios
dotnet build --framework net9.0-android
```

### Database Generation
```bash
# Extract training data from DPD (requires Python environment)
cd scripts
python3 extract_nouns_and_verbs.py

# Validate extracted data
python3 validate_db.py

# Full database rebuild (if needed)
cd dpd-db
uv run scripts/build/db_rebuild_from_tsv.py
uv run python db/inflections/create_inflection_templates.py
uv run python db/inflections/generate_inflection_tables.py
```

## Architecture Overview

### Technology Stack
- **Framework**: Uno Platform 6.1.23 with .NET 9.0
- **UI Pattern**: MVVM with C# Markup (fluent API)
- **Database**: SQLite via sqlite-net-pcl
- **Data Source**: Digital Pāḷi Dictionary (DPD) as git submodule

### Project Structure
- `/PaliPractice/PaliPractice/` - Main app code
  - `Models/` - Entity classes (Headword, Declension, Conjugation, Pattern)
  - `Presentation/` - Pages and ViewModels
  - `Services/` - DatabaseService for SQLite access
  - `Data/training.db` - Embedded SQLite database
  - `Platforms/` - Platform-specific implementations

- `/scripts/` - Python extraction pipeline
  - `extract_nouns_and_verbs.py` - Main extraction script
  - `SETUP.md` - Comprehensive setup documentation
  
- `/dpd-db/` - DPD submodule with dictionary data

### Database Schema
The app uses a normalized SQLite database:
- **headwords**: Core word info (id, lemma_1, pos, type, meaning_1, ebt_count)
- **declensions**: Noun forms (headword_id, form, case_name, number, gender)
- **conjugations**: Verb forms (headword_id, form, person, tense, mood, voice)
- **patterns**: Inflection templates

### Key Implementation Details

1. **Data Selection**: Uses EBT frequency counts to select 1000 most common nouns and verbs
2. **Navigation**: Route-based navigation with Shell pattern
3. **Database Access**: Async SQLite operations via DatabaseService
4. **UI Construction**: C# Markup fluent API instead of XAML
5. **Localization**: Supports 6 languages (en, es, fr, pt, th, ru)

### Working with the Codebase

When modifying code:
- Follow existing C# Markup patterns in Presentation layer
- Maintain MVVM separation (ViewModels handle logic)
- Database models are in Models/ directory
- Platform-specific code goes in Platforms/ subdirectories
- The training.db is embedded as a resource and copied on first run

For database changes:
- Modify extraction script in scripts/extract_nouns_and_verbs.py
- Run validation with scripts/validate_db.py
- Regenerate C# models if schema changes

Example of Uno Fluent C# Markup for bulding UIs:

  ```csharp
  public sealed partial class MainPage : Page
  {
      public MainPage()
      {
          this.DataContext(new MainViewModel(), (page, vm) => page
              .Background(ThemeResource.Get<Brush>("ApplicationPageBackgroundThemeBrush"))
              .Content(
                  new StackPanel()
                      .VerticalAlignment(VerticalAlignment.Center)
                      .Children(
                          new Image()
                              .Margin(12)
                              .HorizontalAlignment(HorizontalAlignment.Center)
                              .Width(150)
                              .Height(150)
                              .Source("ms-appx:///Assets/logo.png"),
                          new TextBox()
                              .Margin(12)
                              .HorizontalAlignment(HorizontalAlignment.Center)
                              .TextAlignment(Microsoft.UI.Xaml.TextAlignment.Center)
                              .PlaceholderText("Step Size")
                              .Text(x => x.Binding(() => vm.Step).TwoWay()),
                          new TextBlock()
                              .Margin(12)
                              .HorizontalAlignment(HorizontalAlignment.Center)
                              .TextAlignment(Microsoft.UI.Xaml.TextAlignment.Center)
                              .Text(() => vm.Count, txt => $"Counter: {txt}"),
                          new Button()
                              .Margin(12)
                              .HorizontalAlignment(HorizontalAlignment.Center)
                              .Command(() => vm.IncrementCommand)
                              .Content("Increment Counter by Step Size")
                      )
              )
          );
      }
  }
  ```
