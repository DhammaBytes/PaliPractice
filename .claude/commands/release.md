Build release artifacts for all four platforms (macOS, Windows, Linux, Android) and gather them in /release/ ready for a GitHub release.

## Steps

### 1. Clean previous release

Remove the `/release/` directory if it exists, then recreate it.

### 2. Build all platforms

Run these builds sequentially from the `PaliPractice/` solution directory. The csproj is at `PaliPractice/PaliPractice/PaliPractice.csproj`.

**macOS (arm64):**
```
dotnet publish PaliPractice/PaliPractice.csproj -f net10.0-desktop -r osx-arm64 \
  -p:TargetFrameworks=net10.0-desktop -p:PackageFormat=app -c Release
```

**Windows (x64):**
```
dotnet publish PaliPractice/PaliPractice.csproj -f net10.0-desktop -r win-x64 \
  -p:TargetFrameworks=net10.0-desktop -p:SelfContained=true -c Release
```

**Linux (x64):**
```
dotnet publish PaliPractice/PaliPractice.csproj -f net10.0-desktop -r linux-x64 \
  -p:TargetFrameworks=net10.0-desktop -p:SelfContained=true -c Release
```

**Android:**
```
dotnet publish PaliPractice/PaliPractice.csproj -f net10.0-android -c Release
```

### 3. Sign and notarize macOS .app

Uno's built-in signing fails on macOS 15+ (UNOB0018 xattr bug), so sign manually.

**Sign:**
```
codesign --deep --force --options runtime \
  --entitlements PaliPractice/PaliPractice/Platforms/macOS/Entitlements.plist \
  --sign "Developer ID Application: YOUR NAME (YOUR_TEAM_ID)" \
  "PaliPractice/PaliPractice/bin/Release/net10.0-desktop/osx-arm64/publish/PāliPractice.app"
```

**Verify:**
```
codesign --verify --deep --strict --verbose=2 \
  "PaliPractice/PaliPractice/bin/Release/net10.0-desktop/osx-arm64/publish/PāliPractice.app"
```
Must end with "valid on disk" and "satisfies its Designated Requirement".

**Notarize:**
```
ditto -c -k --keepParent \
  "PaliPractice/PaliPractice/bin/Release/net10.0-desktop/osx-arm64/publish/PāliPractice.app" \
  "PaliPractice/PaliPractice/bin/Release/net10.0-desktop/osx-arm64/publish/PāliPractice-notarize.zip"

xcrun notarytool submit \
  "PaliPractice/PaliPractice/bin/Release/net10.0-desktop/osx-arm64/publish/PāliPractice-notarize.zip" \
  --keychain-profile pali-notary --wait
```
This blocks until Apple responds (usually a few minutes). If it fails, run `xcrun notarytool log <submission-id> --keychain-profile pali-notary` for details.

**Staple:**
```
xcrun stapler staple \
  "PaliPractice/PaliPractice/bin/Release/net10.0-desktop/osx-arm64/publish/PāliPractice.app"
```

### 4. Package release artifacts into /release/

**macOS** — use `ditto` (preserves code signatures, unlike `zip`):
```
ditto -c -k --keepParent \
  "PaliPractice/PaliPractice/bin/Release/net10.0-desktop/osx-arm64/publish/PāliPractice.app" \
  "release/PaliPractice-macos-arm64.zip"
```

**Windows:**
```
cd PaliPractice/PaliPractice/bin/Release/net10.0-desktop/win-x64/publish
zip -r /path/to/release/PaliPractice-windows-x64.zip . -x '*.pdb'
```

**Linux:**
```
cd PaliPractice/PaliPractice/bin/Release/net10.0-desktop/linux-x64/publish
tar -czf /path/to/release/PaliPractice-linux-x64.tar.gz --exclude='*.pdb' *
```

**Android:**
```
cp PaliPractice/PaliPractice/bin/Release/net10.0-android/org.dhammabytes.palipractice-Signed.apk \
   release/PaliPractice-android.apk
```

### 5. Verify

List the release directory and confirm four files exist:
- `PaliPractice-macos-arm64.zip`
- `PaliPractice-windows-x64.zip`
- `PaliPractice-linux-x64.tar.gz`
- `PaliPractice-android.apk`

Print their sizes. Do NOT create a GitHub release — just report that artifacts are ready.

## Notes

- Builds must run sequentially (concurrent dotnet restores cause NuGet lock contention)
- The Android `-Signed.apk` is debug-keystore signed, which is fine for sideloading
- The macOS zip MUST be created with `ditto`, not `zip`, to preserve code signatures
- If notarization credentials are missing, run: `xcrun notarytool store-credentials pali-notary --apple-id EMAIL --team-id YOUR_TEAM_ID --password APP_SPECIFIC_PASSWORD`
