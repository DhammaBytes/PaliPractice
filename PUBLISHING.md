# Publishing PaliPractice

This guide covers packaging and distributing PaliPractice for Windows, macOS, and Linux.

All commands are run from the `PaliPractice/` solution directory (containing the `.sln` file).

## Prerequisites

- .NET 10 SDK installed
- For macOS signing: Apple Developer Program membership ($99/year)

## Icons

The app icon at `Assets/Icons/icon.png` is used by Uno.Resizetizer to generate platform-native icons:

- **macOS**: `.icns` embedded in the `.app` bundle
- **Windows**: Icon PNGs bundled with the app (used at runtime by the window)
- **Linux**: Use `Assets/Icons/icon.png` directly for AppImage packaging

Android and iOS use their own platform-native icons and are excluded from Resizetizer icon generation.

## macOS

Publishing for macOS is only supported when running on macOS.

> **Note**: When specifying a runtime identifier (`-r`), you must also pass
> `-p:TargetFrameworks=net10.0-desktop` due to .NET SDK requirements.

### Entitlements

The file `PaliPractice/Platforms/macOS/Entitlements.plist` declares the macOS entitlements required for .NET apps distributed with Developer ID (notarization):

| Entitlement | Purpose |
|-------------|---------|
| `com.apple.security.cs.allow-jit` | .NET JIT compilation |
| `com.apple.security.cs.allow-unsigned-executable-memory` | .NET runtime memory management |
| `com.apple.security.cs.disable-library-validation` | Loading bundled native libraries (SkiaSharp, SQLite) |

These are the standard entitlements required by any .NET desktop app on macOS. The app is not sandboxed (no `com.apple.security.app-sandbox`) since it is distributed directly, not through the Mac App Store.

All signing and notarization commands below include `-p:CodesignEntitlements` to apply these entitlements.

### Create a Signed .app Bundle

```bash
# Find your signing identity
security find-identity -v
# Look for "Developer ID Application: Your Name (XXXXXXXXXX)"

# Build signed .app bundle
dotnet publish PaliPractice/PaliPractice.csproj -f net10.0-desktop -r osx-arm64 \
  -p:TargetFrameworks=net10.0-desktop \
  -p:PackageFormat=app \
  -p:CodesignKey="Developer ID Application: Your Name (XXXXXXXXXX)" \
  -p:CodesignEntitlements=Platforms/macOS/Entitlements.plist \
  -c Release
```

Output: `PaliPractice/bin/Release/net10.0-desktop/osx-arm64/publish/PāliPractice.app`

App bundles with `PackageFormat=app` are always self-contained (since Uno 6.1).

For Intel Macs, use `-r osx-x64` instead.

### Create a Signed .dmg (Recommended)

```bash
dotnet publish PaliPractice/PaliPractice.csproj -f net10.0-desktop -r osx-arm64 \
  -p:TargetFrameworks=net10.0-desktop \
  -p:PackageFormat=dmg \
  -p:CodesignKey="Developer ID Application: Your Name (XXXXXXXXXX)" \
  -p:DiskImageSigningKey="Developer ID Application: Your Name (XXXXXXXXXX)" \
  -p:CodesignEntitlements=Platforms/macOS/Entitlements.plist \
  -c Release
```

Output: `PaliPractice/bin/Release/net10.0-desktop/osx-arm64/publish/PāliPractice.dmg`

### Notarization (Required for Gatekeeper)

Without notarization, users will see "app is damaged" or "unidentified developer" warnings.

**One-time setup** - store Apple credentials in keychain:

```bash
xcrun notarytool store-credentials pali-notary \
  --apple-id your@email.com \
  --team-id XXXXXXXXXX \
  --password xxxx-xxxx-xxxx-xxxx
```

- `--apple-id`: Your Apple ID email
- `--team-id`: 10-character team ID (find at developer.apple.com/account)
- `--password`: App-specific password (create at appleid.apple.com > Security > App-Specific Passwords)

**Build with notarization:**

```bash
dotnet publish PaliPractice/PaliPractice.csproj -f net10.0-desktop -r osx-arm64 \
  -p:TargetFrameworks=net10.0-desktop \
  -p:PackageFormat=dmg \
  -p:CodesignKey="Developer ID Application: Your Name (XXXXXXXXXX)" \
  -p:DiskImageSigningKey="Developer ID Application: Your Name (XXXXXXXXXX)" \
  -p:CodesignEntitlements=Platforms/macOS/Entitlements.plist \
  -p:UnoMacOSNotarizeKeychainProfile=pali-notary \
  -c Release
```

This will wait for Apple's notarization service to complete (can take several minutes).

## Windows

### Self-Contained Executable

```bash
dotnet publish PaliPractice/PaliPractice.csproj -f net10.0-desktop -r win-x64 \
  -p:TargetFrameworks=net10.0-desktop \
  -p:SelfContained=true \
  -c Release
```

Output: `PaliPractice/bin/Release/net10.0-desktop/win-x64/publish/`

Distribute as a zip file. Users run `PaliPractice.exe` directly.

Without code signing, Windows SmartScreen will show a warning. Users click "More info" > "Run anyway". This is acceptable for a niche app.

### Single-File Executable (Optional)

```bash
dotnet publish PaliPractice/PaliPractice.csproj -f net10.0-desktop -r win-x64 \
  -p:TargetFrameworks=net10.0-desktop \
  -p:SelfContained=true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:IncludeAllContentForSelfExtract=true \
  -c Release
```

Produces a single `PaliPractice.exe` that extracts and runs.

## Linux

### Self-Contained Tarball

```bash
# For x64
dotnet publish PaliPractice/PaliPractice.csproj -f net10.0-desktop -r linux-x64 \
  -p:TargetFrameworks=net10.0-desktop \
  -p:SelfContained=true \
  -c Release

# Package as tarball
cd PaliPractice/bin/Release/net10.0-desktop/linux-x64/publish
tar -czvf PaliPractice-linux-x64.tar.gz *
```

For ARM64, use `-r linux-arm64` instead.

**Users extract and run:**

```bash
tar -xzvf PaliPractice-linux-x64.tar.gz -C PaliPractice
cd PaliPractice
chmod +x PaliPractice
./PaliPractice
```

**Runtime dependencies** users may need:

```bash
# Debian/Ubuntu
sudo apt install libfontconfig1 libfreetype6 libx11-6 libxrandr2 libxi6

# Fedora/RHEL
sudo dnf install fontconfig freetype libX11 libXrandr libXi

# Arch
sudo pacman -S fontconfig freetype2 libx11 libxrandr libxi
```

### AppImage (Single Executable)

AppImage bundles everything into a single file that runs on most Linux distros without installation.

1. Build the self-contained app (see above)

2. Download appimagetool from https://github.com/AppImage/AppImageKit/releases

3. Create AppDir structure:

```bash
mkdir -p PaliPractice.AppDir/usr/bin
cp -r PaliPractice/bin/Release/net10.0-desktop/linux-x64/publish/* PaliPractice.AppDir/usr/bin/

# Create .desktop file
cat > PaliPractice.AppDir/PaliPractice.desktop << 'EOF'
[Desktop Entry]
Name=PaliPractice
Exec=PaliPractice
Icon=palipractice
Type=Application
Categories=Education;
EOF

# Create AppRun script
cat > PaliPractice.AppDir/AppRun << 'EOF'
#!/bin/bash
SELF=$(readlink -f "$0")
HERE=${SELF%/*}
exec "$HERE/usr/bin/PaliPractice" "$@"
EOF
chmod +x PaliPractice.AppDir/AppRun

# Add icon (1024x1024 PNG from Assets/Icons)
cp PaliPractice/PaliPractice/Assets/Icons/icon.png PaliPractice.AppDir/palipractice.png
```

4. Build AppImage:

```bash
./appimagetool-x86_64.AppImage PaliPractice.AppDir PaliPractice-x86_64.AppImage
```

**Users download and run:**

```bash
chmod +x PaliPractice-x86_64.AppImage
./PaliPractice-x86_64.AppImage
```

## Cross-Platform Build Matrix

| Target | Build From | Runtime ID |
|--------|------------|------------|
| macOS arm64 | macOS only | osx-arm64 |
| macOS x64 | macOS only | osx-x64 |
| Windows x64 | Any OS | win-x64 |
| Windows arm64 | Any OS | win-arm64 |
| Linux x64 | Any OS | linux-x64 |
| Linux arm64 | Any OS | linux-arm64 |

## GitHub Actions

```yaml
name: Build and Release

on:
  push:
    tags: ['v*']

jobs:
  build-macos:
    runs-on: macos-latest
    steps:
      - uses: actions/checkout@v4
        with:
          submodules: false
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - name: Build macOS (arm64)
        run: |
          dotnet publish PaliPractice/PaliPractice.csproj \
            -f net10.0-desktop -r osx-arm64 -c Release \
            -p:TargetFrameworks=net10.0-desktop \
            -p:PackageFormat=app
          cd PaliPractice/bin/Release/net10.0-desktop/osx-arm64/publish
          zip -r PaliPractice-macos-arm64.zip *.app
      - name: Build macOS (x64)
        run: |
          dotnet publish PaliPractice/PaliPractice.csproj \
            -f net10.0-desktop -r osx-x64 -c Release \
            -p:TargetFrameworks=net10.0-desktop \
            -p:PackageFormat=app
          cd PaliPractice/bin/Release/net10.0-desktop/osx-x64/publish
          zip -r PaliPractice-macos-x64.zip *.app
      - uses: actions/upload-artifact@v4
        with:
          name: macos
          path: PaliPractice/bin/Release/net10.0-desktop/*/publish/*.zip

  build-windows:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
        with:
          submodules: false
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - name: Build Windows
        run: |
          dotnet publish PaliPractice/PaliPractice.csproj \
            -f net10.0-desktop -r win-x64 -c Release \
            -p:TargetFrameworks=net10.0-desktop \
            -p:SelfContained=true
          cd PaliPractice/bin/Release/net10.0-desktop/win-x64/publish
          zip -r PaliPractice-windows-x64.zip .
      - uses: actions/upload-artifact@v4
        with:
          name: windows-x64
          path: PaliPractice/bin/Release/net10.0-desktop/win-x64/publish/*.zip

  build-linux:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
        with:
          submodules: false
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - name: Build Linux
        run: |
          dotnet publish PaliPractice/PaliPractice.csproj \
            -f net10.0-desktop -r linux-x64 -c Release \
            -p:TargetFrameworks=net10.0-desktop \
            -p:SelfContained=true
          cd PaliPractice/bin/Release/net10.0-desktop/linux-x64/publish
          tar -czvf PaliPractice-linux-x64.tar.gz *
      - name: Build AppImage
        run: |
          wget -q https://github.com/AppImage/AppImageKit/releases/download/continuous/appimagetool-x86_64.AppImage
          chmod +x appimagetool-x86_64.AppImage

          mkdir -p PaliPractice.AppDir/usr/bin
          cp -r PaliPractice/bin/Release/net10.0-desktop/linux-x64/publish/* PaliPractice.AppDir/usr/bin/

          cat > PaliPractice.AppDir/PaliPractice.desktop << 'EOF'
          [Desktop Entry]
          Name=PaliPractice
          Exec=PaliPractice
          Icon=palipractice
          Type=Application
          Categories=Education;
          EOF

          cat > PaliPractice.AppDir/AppRun << 'EOF'
          #!/bin/bash
          SELF=$(readlink -f "$0")
          HERE=${SELF%/*}
          exec "$HERE/usr/bin/PaliPractice" "$@"
          EOF
          chmod +x PaliPractice.AppDir/AppRun

          cp PaliPractice/PaliPractice/Assets/Icons/icon.png PaliPractice.AppDir/palipractice.png

          ./appimagetool-x86_64.AppImage PaliPractice.AppDir PaliPractice-x86_64.AppImage
      - uses: actions/upload-artifact@v4
        with:
          name: linux-x64
          path: |
            PaliPractice/bin/Release/net10.0-desktop/linux-x64/publish/*.tar.gz
            PaliPractice-x86_64.AppImage
```

## Summary

| Platform | Format | Signing |
|----------|--------|---------|
| macOS | .dmg (notarized) | Required for smooth UX |
| Windows | .zip | Optional (SmartScreen warning OK) |
| Linux | .tar.gz or .AppImage | Not needed |
