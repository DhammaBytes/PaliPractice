# AGENTS.md

Shared instructions for contributors and coding agents working in this repository.

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

Run from the repository root. See [scripts/SETUP.md](scripts/SETUP.md) for
pinned input acquisition; ordinary app builds use the bundled database.

```bash
# Build an isolated English candidate from pinned inputs (see scripts/SETUP.md)
.venv/bin/python scripts/extract_nouns_and_verbs.py build --manifest <inputs.json> --output <new-candidate-directory>

# Validate the candidate's current structural contract
.venv/bin/python scripts/extract_nouns_and_verbs.py validate <candidate-directory>

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
- **Database**: SQLite via sqlite-net-base and SQLitePCLRaw.bundle_e_sqlite3
- **Data Source**: Digital Pāḷi Dictionary (DPD) as git submodule

### Project Structure
- `/PaliPractice/PaliPractice/` - Main app code
  - `Models/` - Lemma, noun/verb, detail, and inflection models
  - `Presentation/` - Pages and ViewModels
  - `Services/` - DatabaseService for SQLite access
  - `Data/pali.db` - Bundled dictionary; version and provenance sidecars are adjacent
  - `Platforms/` - Platform-specific implementations

- `/scripts/` - Python extraction pipeline
  - `extract_nouns_and_verbs.py` - Main extraction script
  - `SETUP.md` - Comprehensive setup documentation
  
- `/dpd-db/` - DPD submodule with dictionary data

### Database Schema
The bundled dictionary uses these tables:
- **nouns / verbs**: DPD headwords, stable lemma IDs, EBT counts, stems/paradigms, and explicit practice-primary selection
- **nouns_details / verbs_details**: English meanings and language-neutral examples
- **localized_meanings**: Russian/Spanish meanings keyed by DPD headword and language
- **nouns_corpus_forms / verbs_corpus_forms**: Attested `(headword_id, form_id)` pairs
- **nouns_irregular_forms / verbs_irregular_forms**: Scoped form IDs plus spellings needed for reconstruction

Regular endings live in C# grammar tables. Full corpus spellings and primary-form
contracts live in `scripts/generated` as build/test evidence, not app assets.
User mastery, settings, and history live separately in `practice.db`.

### Key Implementation Details

1. **Data Selection**: Uses EBT frequency to select 1,500 noun lemmas and 750 verb lemmas; practice paradigms follow the append-only identity registry
2. **Navigation**: Route-based navigation with Shell pattern
3. **Database Access**: SQLite access through DatabaseService and repositories
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
- `pali.db` is a packaged asset. Platforms may read it directly or copy it into app storage; the existing version check replaces older copies.
- `history-v1.1.json.gz` is an immutable embedded resource for legacy history backfill, loaded only during migration when needed.

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
- Validate the isolated candidate with `scripts/extract_nouns_and_verbs.py validate <candidate-directory>`
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
                        RegularText()
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

## Repository Quality Gate

- Run `python3 quality/gate.py auto` before handing off changes.
- Use `--profile fast` for Python/data-only iteration and `--profile full` for the complete local gate.
- Pass `--base <commit>` when the comparison base is not the upstream merge base.
- The gate may regenerate English candidates only in its external evidence directory from an explicit `PALIPRACTICE_INPUT_MANIFEST`. It compares two isolated builds and tests that exact candidate. Never acquire inputs, promote a candidate, or write production database/registry outputs from the gate.
- Evidence and tool caches live outside the worktree; inspect the path printed by the gate.

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
        Action<Border> bindVisibility,
        Action<TextBlock> bindCurrentWord,
        Action<TextBlock> bindUsageExample)
    {
        var wordTextBlock = PaliText();
        bindCurrentWord(wordTextBlock);  // Binding happens at call site where generator can see it
        
        var exampleTextBlock = RegularText();
        bindUsageExample(exampleTextBlock);
        
        var card = new Border()
            .Child(
                new StackPanel().Children(
                    wordTextBlock.FontSize(48),
                    exampleTextBlock.FontSize(16)
                )
            );
        bindVisibility(card);
        return card;
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
                    bindVisibility: card => card.Visibility(() => vm.Card.IsLoading,
                        loading => loading ? Visibility.Collapsed : Visibility.Visible),
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
