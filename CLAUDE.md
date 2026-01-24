# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

PaliPractice is a cross-platform language learning app for practicing Pali noun declensions and verb conjugations. Built with .NET 10 and Uno Platform for Windows, Mac, Linux, Android, and iOS.

Uno Platform implements the entire WinUI and WinRT API (like Microsoft.UI) surface across platforms. So when developing in Uno, think as an experienced WinUI/WinRT developer.

## Key Commands

### Build and Run
```bash
# Build for desktop (Windows/Mac/Linux) - target project directly to avoid test project issues
cd PaliPractice
dotnet build PaliPractice/PaliPractice.csproj -f net10.0-desktop

# Run on desktop
dotnet run --project PaliPractice/PaliPractice.csproj -f net10.0-desktop

# Build for other platforms
dotnet build PaliPractice/PaliPractice.csproj -f net10.0-ios
dotnet build PaliPractice/PaliPractice.csproj -f net10.0-android

# Run tests (uses net10.0, not platform-specific)
dotnet test PaliPractice.Tests/PaliPractice.Tests.csproj
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
- **Framework**: Uno Platform 6 with .NET 10
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

### State Management
- **Minimize mutable state**: Only introduce new state when absolutely necessary; prefer derived/computed values over stored state
- **Avoid state duplication**: Maintain a single source of truth; never store the same information in multiple places
- **Prefer enums over constants**: Use enums for finite sets of related values instead of multiple booleans or string/int constants
- **Favor pure functions**: Pass data as explicit parameters rather than relying on instance state; this improves testability and reduces side effects
- **Reuse existing state**: Before adding a new boolean or flag, check if existing state can express the same condition
- **Avoid state explosion**: Multiple independent booleans create exponential state combinations; consolidate into enums or state objects when states are mutually exclusive

### Working with the Codebase

When modifying code:
- Follow existing C# Markup patterns in Presentation layer
- Maintain MVVM separation (ViewModels handle logic)
- Database models are in Models/ directory
- Platform-specific code goes in Platforms/ subdirectories
- The training.db is embedded as a resource and copied on first run

**UI Text Guidelines:**
- Never use raw `new TextBlock()` - always use the font helpers from `TextHelpers`:
  - `RegularText()` - For UI labels, descriptions, and translations (uses SourceSans3 font)
  - `PaliText()` - For Pali words and inflected forms (uses LibertinusSans font)
- Add `using static PaliPractice.Presentation.Common.TextHelpers;` to use these helpers directly

**UI Shapes Guidelines:**
- For rounded backgrounds and buttons, use squircle helpers from `Presentation/Common/Squircle/`:
  - `SquircleBorder` - For card backgrounds (use `.Fill()` instead of `.Background()`)
  - `SquircleButton` - For buttons with squircle shape
- Squircles provide smoother, more natural curves than standard `CornerRadius`

For database changes:
- Modify extraction script in scripts/extract_nouns_and_verbs.py
- Run validation with scripts/validate_db.py
- Regenerate C# models if schema changes

Example of Uno Fluent C# Markup for building UIs:

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

## Composition Rules for Uno C# Markup

When using `this.DataContext<TViewModel>((page, vm) => ...)`, the *only* legal way to access `vm` is **inside binding lambdas**. You must not pass `vm.Property` eagerly to helper/build methods.

### Critical: Uno C# Markup Compiled Binding Limitation

Uno's compiled C# Markup bindings rely on source generators that must "see" the lambda at the call site. When you capture a lambda in a `Func<T>` parameter and pass it through another method, the generator can't analyze it, so no binding is produced. Instead, a non-binding overload is used (often the `object` overload), resulting in `ToString()` output.

**Correct approach (bind at call site):**
```csharp
// Component that accepts Action<T> for binding
public static class WordCard
{
    public static Border Build(
        Func<bool> isLoading,  // OK - Visibility accepts Func<bool> directly
        Action<TextBlock> bindCurrentWord,
        Action<TextBlock> bindUsageExample)
    {
        var wordTextBlock = new TextBlock();
        bindCurrentWord(wordTextBlock);  // Binding happens at call site where generator can see it
        
        var exampleTextBlock = new TextBlock();
        bindUsageExample(exampleTextBlock);
        
        return new Border()
            .Visibility(isLoading, l => !l ? Visibility.Visible : Visibility.Collapsed)
            .Child(
                new StackPanel().Children(
                    wordTextBlock.FontSize(48),
                    exampleTextBlock.FontSize(16)
                )
            );
    }
}

// Page using the component - binding lambdas are visible to generator
public sealed partial class PracticePage : Page
{
    public PracticePage()
    {
        this.DataContext<MyViewModel>((page, vm) => page
            .Content(
                WordCard.Build(
                    isLoading: () => vm.Card.IsLoading,
                    bindCurrentWord: tb => tb.Text(() => vm.Card.CurrentWord),  // Generator sees this!
                    bindUsageExample: tb => tb.Text(() => vm.Card.UsageExample)
                )
            ));
    }
}
```

### Rules for Composition

1. **Never pass `Func<T>` for bindings** - The generator won't see the lambda
2. **Use `Action<TControl>` parameters** - Apply bindings at the call site
3. **Keep lambdas visible** - All `() => vm.Property` must be at the call site
4. **Build controls eagerly** - Create UI structure, then apply bindings
5. **Don't use ContentPresenter for composition** - It has similar limitations

### Why This Works

- The Uno source generator analyzes the C# code at compile time
- It looks for patterns like `.Text(() => vm.Property)` to generate bindings
- When the lambda is inside a parameter, the generator can't "see" it
- By using `Action<T>`, we ensure the binding lambda stays at the call site
- The generator can then properly create `INotifyPropertyChanged` subscriptions

### Binding 101

It is important to consider that within the `DataContext` method the vm property will **always** be null.

### Accessing ViewModel Outside Bindings

When you need to imperatively access the ViewModel (e.g., to populate UI elements that can't use bindings), **never use the `Loaded` event**. The `Loaded` event may fire before `DataContext` is set.

**Use `DataContextChanged` instead:**
```csharp
public sealed partial class MyPage : Page
{
    readonly StackPanel _dynamicContent = new();

    public MyPage()
    {
        this.DataContext<MyViewModel>((page, vm) => page
            .Content(_dynamicContent));

        // WRONG: DataContext may be null when Loaded fires
        // Loaded += (s, e) => { if (DataContext is MyViewModel vm) ... };

        // CORRECT: DataContextChanged fires when ViewModel is assigned
        DataContextChanged += OnDataContextChanged;
    }

    void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs e)
    {
        if (e.NewValue is not MyViewModel vm) return;

        // Safe to access vm properties here
        foreach (var item in vm.Items)
            _dynamicContent.Children.Add(BuildItemRow(item));
    }
}
```
