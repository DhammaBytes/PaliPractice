# Publishing PaliPractice

Reference for building and distributing PaliPractice. The `/release` skill automates
the build + package steps — this document explains the why and the manual steps.

All commands run from the `PaliPractice/` solution directory.

## Platforms and Formats

| Platform | Format | Signing |
|----------|--------|---------|
| macOS arm64 | .app in .zip | Developer ID + notarization (required) |
| Windows x64 | .zip | None (SmartScreen warning is acceptable) |
| Linux x64 | .tar.gz | None |
| Android | .apk | Debug keystore (fine for sideloading) |

macOS builds require a Mac. All other platforms cross-compile from any OS.

When specifying a runtime identifier (`-r`) for desktop, you must also pass
`-p:TargetFrameworks=net10.0-desktop` due to a .NET SDK requirement.

## Build Commands

**macOS:**
```bash
dotnet publish PaliPractice/PaliPractice.csproj -f net10.0-desktop -r osx-arm64 \
  -p:TargetFrameworks=net10.0-desktop -p:PackageFormat=app -c Release
```

**Windows:**
```bash
dotnet publish PaliPractice/PaliPractice.csproj -f net10.0-desktop -r win-x64 \
  -p:TargetFrameworks=net10.0-desktop -p:SelfContained=true -c Release
```

**Linux:**
```bash
dotnet publish PaliPractice/PaliPractice.csproj -f net10.0-desktop -r linux-x64 \
  -p:TargetFrameworks=net10.0-desktop -p:SelfContained=true -c Release
```

**Android:**
```bash
dotnet publish PaliPractice/PaliPractice.csproj -f net10.0-android -c Release
```

Run builds sequentially — concurrent `dotnet restore` causes NuGet lock contention.

## macOS Signing and Notarization

### Entitlements

`PaliPractice/Platforms/macOS/Entitlements.plist` declares entitlements required by
any .NET app distributed with Developer ID:

| Entitlement | Purpose |
|-------------|---------|
| `com.apple.security.cs.allow-jit` | .NET JIT compilation |
| `com.apple.security.cs.allow-unsigned-executable-memory` | .NET runtime memory management |
| `com.apple.security.cs.disable-library-validation` | Loading bundled native libraries (SkiaSharp, SQLite) |

Not sandboxed (no `com.apple.security.app-sandbox`) — distributed directly, not via
Mac App Store.

### Why Two-Step Signing (UNOB0018)

Uno's `GenerateAppBundle` task (in `Uno.Sdk.Extras`) pre-checks files for extended
attributes before signing. On macOS 15 (Sequoia), the system applies
`com.apple.provenance` xattrs to NuGet-extracted files. This triggers error `UNOB0018`
when passing `-p:CodesignKey` to `dotnet publish`.

Apple's `codesign` handles these xattrs without issue — the bug is in Uno's pre-check.

**Workaround:** publish unsigned, then sign manually.

If a future Uno SDK fixes this, the correct MSBuild properties are:

| Property | Purpose |
|----------|---------|
| `CodesignKey` | Signing identity for the .app |
| `UnoMacOSEntitlements` | Path to entitlements plist |
| `UnoMacOSHardenedRuntime` | Enable hardened runtime |
| `UnoMacOSNotarizeKeychainProfile` | Keychain profile for notarization |

### Sign

```bash
codesign --deep --force --options runtime \
  --entitlements PaliPractice/PaliPractice/Platforms/macOS/Entitlements.plist \
  --sign "Developer ID Application: YOUR NAME (YOUR_TEAM_ID)" \
  "PaliPractice/PaliPractice/bin/Release/net10.0-desktop/osx-arm64/publish/PāliPractice.app"
```

- `--deep` — signs all nested code (dylibs, executables)
- `--options runtime` — hardened runtime (required for notarization)
- `--entitlements` — JIT/memory/library entitlements .NET needs

### Verify

```bash
codesign --verify --deep --strict --verbose=2 \
  "PaliPractice/PaliPractice/bin/Release/net10.0-desktop/osx-arm64/publish/PāliPractice.app"
```

Must end with "valid on disk" and "satisfies its Designated Requirement".

### Notarize

**One-time credential setup:**

```bash
xcrun notarytool store-credentials pali-notary \
  --apple-id YOUR_APPLE_ID \
  --team-id YOUR_TEAM_ID \
  --password YOUR_APP_SPECIFIC_PASSWORD
```

The app-specific password is generated at appleid.apple.com > Sign-In and Security >
App-Specific Passwords. This is not your Apple ID password.

**Submit and staple:**

```bash
ditto -c -k --keepParent \
  "PaliPractice/PaliPractice/bin/Release/net10.0-desktop/osx-arm64/publish/PāliPractice.app" \
  "/tmp/PaliPractice-notarize.zip"

xcrun notarytool submit /tmp/PaliPractice-notarize.zip \
  --keychain-profile pali-notary --wait

xcrun stapler staple \
  "PaliPractice/PaliPractice/bin/Release/net10.0-desktop/osx-arm64/publish/PāliPractice.app"
```

If notarization fails, check the log:
```bash
xcrun notarytool log SUBMISSION_ID --keychain-profile pali-notary
```

## Packaging

**macOS** — use `ditto` (preserves code signatures; `zip` does not):
```bash
ditto -c -k --keepParent \
  "PaliPractice/PaliPractice/bin/Release/net10.0-desktop/osx-arm64/publish/PāliPractice.app" \
  release/PaliPractice-macos-arm64.zip
```

**Windows:**
```bash
cd PaliPractice/PaliPractice/bin/Release/net10.0-desktop/win-x64/publish
zip -r release/PaliPractice-windows-x64.zip . -x '*.pdb'
```

**Linux:**
```bash
cd PaliPractice/PaliPractice/bin/Release/net10.0-desktop/linux-x64/publish
tar -czf release/PaliPractice-linux-x64.tar.gz --exclude='*.pdb' *
```

**Android:**
```bash
cp PaliPractice/PaliPractice/bin/Release/net10.0-android/org.dhammabytes.palipractice-Signed.apk \
   release/PaliPractice-android.apk
```

The Android `-Signed.apk` uses the debug keystore, which is fine for sideloading.
Users cannot upgrade from this APK to the Play Store version without uninstalling
(different signing keys).

## GitHub Release

Requires `gh` CLI (`brew install gh && gh auth login`).

```bash
gh release create v1.1 \
  release/PaliPractice-macos-arm64.zip \
  release/PaliPractice-windows-x64.zip \
  release/PaliPractice-linux-x64.tar.gz \
  release/PaliPractice-android.apk \
  --title "v1.1" \
  --notes "Release notes here"
```

The version tag should match `ApplicationDisplayVersion` in the csproj.

## Icons

`Assets/Icons/icon.png` is used by Uno.Resizetizer to generate platform-native icons:

- **macOS**: `.icns` embedded in `.app` bundle
- **Windows**: PNGs bundled with the app

Android and iOS use their own platform-native icons (excluded from Resizetizer).
