# Accessibility (A11y) TODO

> **When to implement**: During localization work when strings are moved to resources.
> All `AutomationProperties.Name` values should be localized alongside visible text.

## Overview

The app currently has **zero accessibility support**. Screen readers cannot announce any UI elements meaningfully.

**Skia Note**: Uno's Skia renderer has "work in progress" a11y support. Adding semantic markup now ensures the app is ready as Uno improves.

---

## 1. BUTTONS WITH ICONS

### 1.1 Translation Carousel Arrows
**File**: `PracticePageBuilder.cs:567-603`

```csharp
// Current (no accessible name)
new FontIcon().Glyph("\uE76B") // ChevronLeft

// Needs (localize "Previous translation")
new FontIcon()
    .Glyph("\uE76B")
    .SetValue(AutomationProperties.NameProperty, Strings.PreviousTranslation)
```

Same for:
- Next arrow (\uE76C) → "Next translation"

### 1.2 Title Bar Buttons
**File**: `AppTitleBar.cs:66-106`

The Back and History buttons have visible text but the FontIcons inside need names:
- Back arrow (\uE72B) → "Go back"
- History icon (\uE81C) → "View history"

Consider adding AutomationProperties.Name to the entire button combining icon + text:
```csharp
var button = new SquircleButton()
    .SetValue(AutomationProperties.NameProperty, Strings.GoBack)
```

### 1.3 Action Buttons (Reveal, Easy, Hard)
**File**: `PracticePageBuilder.cs:713-767`

```csharp
// Reveal button
BuildRevealButton(...)
    .SetValue(AutomationProperties.NameProperty, Strings.RevealAnswer)

// Hard button
BuildActionButton("Hard", "\uE711", ...)
    // Name should be "Mark as hard" not just "Hard"

// Easy button
BuildActionButton("Easy", "\uE73E", ...)
    // Name should be "Mark as easy" not just "Easy"
```

---

## 2. GRAMMAR BADGES

### 2.1 Badge AutomationProperties
**File**: `PracticePageBuilder.cs:428-461`

Badges display grammar info (Case: Nominative, Gender: Masculine, etc.) but have no accessible representation.

```csharp
// In BuildBadge method, add to SquircleBorder:
var badge = new SquircleBorder()
    .AutomationName<TVM>(labelPath) // Bind to same text shown visually
```

**Badges to annotate**:
- Declension: Case, Gender, Number
- Conjugation: Tense, Person, Number, Voice

### 2.2 Badge Container Grouping
Consider grouping all badges under one accessible element:
```csharp
var panel = new StackPanel()
    .SetValue(AutomationProperties.NameProperty, "Grammar: Nominative, Masculine, Singular")
```

This requires building a composite string from all badge labels.

---

## 3. SETTINGS CONTROLS

### 3.1 Label Association
**File**: `SettingsRow.cs` (all Build* methods)

Every form control needs `AutomationProperties.LabeledBy` pointing to its label:

```csharp
// Current
var labelText = RegularText().Text(label);
var toggle = new ToggleSwitch();

// Needs
var labelText = RegularText().Text(label);
var toggle = new ToggleSwitch()
    .SetValue(AutomationProperties.LabeledByProperty, labelText);
```

**Methods to update**:
- `BuildToggle` (lines 13-65)
- `BuildToggleWithHint` (lines 379-418)
- `BuildDropdown` (all overloads, lines 237-335)
- `BuildDropdownWithHint` (lines 423-462)
- `BuildNumberBox` (lines 340-374)

### 3.2 Checkbox Patterns (Declension/Conjugation Settings)
**Files**:
- `DeclensionSettingsPage.cs:109-159` (GenderPatternSection)
- `ConjugationSettingsPage.cs:133-221` (Endings/Person checkboxes)

Each checkbox needs accessible name:
```csharp
var cbAti = new CheckBox()
    .SetValue(AutomationProperties.NameProperty, "ati ending")
```

**Pattern checkboxes needing names**:
- Declension: a, i, ī, u, ū, as, ar, ant (masc), ā, i, ī, u, ar (fem), a, i, u (neut)
- Conjugation: ati, āti, eti, oti endings; 1st/2nd/3rd person

---

## 4. OPACITY vs VISIBILITY ISSUES

**Uno Docs**: "Screen readers can still focus elements with Opacity=0. Use Visibility=Collapsed."

### 4.1 Answer Spacer
**File**: `PracticePageBuilder.cs:353-365`
```csharp
// Current
var answerSpacer = new StackPanel().Opacity(0)

// Should add
    .SetValue(AutomationProperties.AccessibilityViewProperty, AccessibilityView.Raw)
```

### 4.2 Single Line Reference
**File**: `PracticePageBuilder.cs:506-518`
```csharp
// Invisible measurement reference - exclude from a11y tree
var singleLineReference = new StackPanel()
    .Opacity(0)
    .SetValue(AutomationProperties.AccessibilityViewProperty, AccessibilityView.Raw)
```

### 4.3 SquircleButton Overlays
**File**: `SquircleButton.cs:154-155`
```xml
<!-- In XAML template, add accessibility exclusion -->
<Path x:Name="PART_Hover" Fill="Black" Opacity="0"
      AutomationProperties.AccessibilityView="Raw"/>
<Path x:Name="PART_Pressed" Fill="Black" Opacity="0"
      AutomationProperties.AccessibilityView="Raw"/>
```

---

## 5. LIVE REGIONS (Dynamic Content)

### 5.1 Answer Reveal Announcement
**File**: `PracticePageBuilder.cs:384-386`

When the answer is revealed, screen readers should announce it:
```csharp
var answerContainer = new Grid()
    .SetValue(AutomationProperties.LiveSettingProperty, AutomationLiveSetting.Polite)
```

### 5.2 Translation Change Announcement
When carousel navigates to new translation, announce:
```csharp
translationTextBlock
    .SetValue(AutomationProperties.LiveSettingProperty, AutomationLiveSetting.Polite)
```

---

## 6. CUSTOM AUTOMATION PEERS

### 6.1 SquircleBorder
**File**: `SquircleBorder.cs`

```csharp
public class SquircleBorder : ContentControl
{
    protected override AutomationPeer OnCreateAutomationPeer()
        => new SquircleBorderAutomationPeer(this);
}

class SquircleBorderAutomationPeer : FrameworkElementAutomationPeer
{
    public SquircleBorderAutomationPeer(SquircleBorder owner) : base(owner) { }

    protected override AutomationControlType GetAutomationControlTypeCore()
        => AutomationControlType.Group;

    protected override string GetClassNameCore() => "SquircleBorder";
}
```

---

## 7. AUTOMATION IDS (UI Testing)

For UI test automation, add AutomationId to key controls:

```csharp
// Practice page
RevealButton: "RevealAnswerButton"
HardButton: "HardButton"
EasyButton: "EasyButton"
PrevTranslation: "PreviousTranslationButton"
NextTranslation: "NextTranslationButton"

// Settings
DeclensionSettings: "DeclensionSettingsButton"
ConjugationSettings: "ConjugationSettingsButton"
```

---

## 8. SIMPLE ACCESSIBILITY MODE

**File**: `App.cs` (or `App.xaml.cs`)

Enable for iOS/Android where nested accessible elements don't work:
```csharp
public App()
{
    #if __IOS__ || __ANDROID__
    FeatureConfiguration.AutomationPeer.UseSimpleAccessibility = true;
    #endif
}
```

---

## 9. STRINGS TO LOCALIZE

When extracting for localization, create these accessibility-specific strings:

```
# Navigation
GoBack=Go back
ViewHistory=View practice history

# Practice Actions
RevealAnswer=Reveal answer
MarkAsHard=Mark as hard
MarkAsEasy=Mark as easy

# Translation Carousel
PreviousTranslation=Previous translation
NextTranslation=Next translation

# Grammar Badges (compound accessible names)
GrammarInfo=Grammar information
CaseBadge=Case: {0}
GenderBadge=Gender: {0}
NumberBadge=Number: {0}
TenseBadge=Tense: {0}
PersonBadge=Person: {0}
VoiceBadge=Voice: {0}

# Pattern Checkboxes (examples)
EndingAti=ati ending
EndingEti=eti ending
Person1st=First person
Person2nd=Second person
Person3rd=Third person
```

---

## 10. TEXT SIZE & READABILITY

> **User report**: Inflection tables are hard to read because the text is too small.

### 10.1 Inflection Table — No Explicit Font Sizes
**File**: `FrozenHeaderTable.cs`

All text in the inflection table uses the platform default (~14px), which is too small for Pali diacritics that need character-by-character reading.

**Data cells** (line 267):
```csharp
// Current — no FontSize set
var textBlock = PaliText()
    .TextWrapping(TextWrapping.NoWrap);

// Needs minimum 16px, ideally 17-18px
var textBlock = PaliText()
    .FontSize(17)
    .TextWrapping(TextWrapping.NoWrap);
```

**Column headers** (line 213):
```csharp
// Current — no FontSize
RegularText()
    .Text(headers[col])
    .FontWeight(Microsoft.UI.Text.FontWeights.SemiBold)

// Needs ~15-16px
    .FontSize(15)
```

**Row headers** (line 247):
```csharp
// Current — no FontSize
RegularText()
    .Text(header)
    .FontWeight(Microsoft.UI.Text.FontWeights.SemiBold)

// Needs ~15-16px
    .FontSize(15)
```

### 10.2 Fixed Cell Dimensions Too Tight
**File**: `FrozenHeaderTable.cs:315-317`

Cell dimensions are hardcoded constants that don't scale with text size:
```csharp
const double CellWidth = 120;      // Too narrow for longer forms at larger font
const double RowHeaderWidth = 70;   // Barely fits "Accusative" etc.
const double CellMinHeight = 36;    // Too short if font grows
```

Consider increasing these alongside the font size bump:
- `CellWidth`: 120 → 140
- `RowHeaderWidth`: 70 → 85
- `CellMinHeight`: 36 → 40

### 10.3 Hint Text Too Small
**File**: `InflectionTablePage.cs:33`

The non-corpus forms hint is 12px — below the 16px minimum for body text:
```csharp
// Current
_hintTextBlock = RegularText().FontSize(12)

// Should be at least 14px (secondary) or 16px (body)
_hintTextBlock = RegularText().FontSize(14)
```

### 10.4 No Responsive Scaling for Tables
The practice page uses `LayoutConstants` with `HeightClass` for responsive font sizing across window heights (Tall/Medium/Short/Minimum). The inflection table page has no equivalent — text stays the same size regardless of window or device.

Consider adding table-specific entries to `LayoutConstants`, or a simpler approach: use `Responsive` markup extension on the table card to switch between compact (mobile) and comfortable (desktop) cell sizes.

---

## 11. TESTING CHECKLIST

After implementing:

- [ ] Windows: Test with Narrator (Win+Enter)
- [ ] macOS: Test with VoiceOver (Cmd+F5)
- [ ] iOS Simulator: Test with VoiceOver
- [ ] Android Emulator: Test with TalkBack
- [ ] Keyboard-only navigation works for all interactive elements
- [ ] Tab order is logical (top→bottom, left→right)
- [ ] Focus indicators are visible
- [ ] No focusable invisible elements

---

## References

- [Uno Accessibility Docs](https://platform.uno/docs/articles/features/working-with-accessibility.html)
- [WinUI AutomationProperties](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.automation.automationproperties)
- [Skia Renderer Limitations](https://platform.uno/docs/articles/features/using-skia-rendering.html#limitations)
