# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

PaliPractice is a cross-platform language learning app for practicing Pali noun declensions and verb conjugations. Built with .NET 9 and Uno Platform for Windows, Mac, Linux, Android, and iOS.

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
- **Framework**: Uno Platform 6.1 with .NET 9
- **UI Pattern**: MVVM with C# Markup (fluent API)
- **Database**: SQLite via sqlite-net-pcl
- **Data Source**: Digital Pāḷi Dictionary (DPD) as git submodule

### Project Structure
- `/PaliPractice/PaliPractice/` - Main app code
  - `Models/` - Entity classes (Headword, Declension, Conjugation, Pattern)
  - `Presentation/` - Pages and their components, ViewModels and their parts (behaviors)
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

### Working with the Codebase

When modifying code:
- Follow existing C# Markup patterns in Presentation layer
- Prefer composition, avoid inheritance
- Maintain MVVM separation (ViewModels handle logic)
- Database models are in Models/ directory
- Platform-specific code goes in Platforms/ subdirectories

For database changes:
- Modify extraction script in scripts/extract_nouns_and_verbs.py
- Run validation with scripts/validate_db.py
- Regenerate C# models if schema changes

## Uno Fluent C# Markup for bulding UIs

### Simple example

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

## Composition rules

When using `this.DataContext<TViewModel>((page, vm) => ...)`, the *only* legal way to access `vm` is **inside binding lambdas**. You must not pass `vm.Property` eagerly to helper/build methods.

**Rules**

* Access the view model only as `() => vm.Property` or `() => vm.Command`.
* Make reusable UI builders accept **providers** (`Func<T>`), not instances.
* Dereference providers **only inside binding lambdas**.
* Build controls **eagerly** and pass them to `.Children(...)` / `.Content(control)`.
* Bind a property with `.Content(() => vm.SomeUiElement)` only if you’re binding to an existing property; never construct UI inside a `.Content(() => ...)` binding.

### Minimal example

**A small selector component that binds to a behavior**

```csharp
// Component: accepts provider, binds inside lambdas
public static class NumberSelector
{
    public static UIElement Build(Func<NumberSelectionBehavior> number) =>
        new StackPanel().Orientation(Orientation.Horizontal).Spacing(8).Children(
            new ToggleButton()
                .Content(new TextBlock().Text("Singular"))
                .IsChecked(() => number().IsSingularSelected)
                .Command(() => number().SelectSingularCommand),
            new ToggleButton()
                .Content(new TextBlock().Text("Plural"))
                .IsChecked(() => number().IsPluralSelected)
                .Command(() => number().SelectPluralCommand)
        );
}
```

**A card component that uses a behavior provider**

```csharp
public static class WordCard
{
    public static Border Build(Func<CardStateBehavior> card, string rankPrefix) =>
        new Border()
            .Visibility(() => card().IsLoading, l => !l ? Visibility.Visible : Visibility.Collapsed)
            .Padding(24)
            .Child(
                new Grid().RowDefinitions("Auto,Auto,Auto").Children(
                    new TextBlock().Text(() => card().AnkiState).Grid(row:0),
                    new TextBlock().Text(() => card().CurrentWord).FontSize(48).Grid(row:1),
                    new TextBlock().Text(() => card().UsageExample).Grid(row:2)
                )
            );
}
```

**Page composition (gluing pieces correctly)**

```csharp
public sealed partial class PracticePage : Page
{
    public PracticePage()
    {
        this.DataContext<MyViewModel>((page, vm) => page
            .Content(new Grid().Children(
                // Build concrete UI now; all VM access stays inside lambdas:
                WordCard.Build(() => vm.Card, rankPrefix: "N"),
                NumberSelector.Build(() => vm.Number)
            )));
    }
}
```
