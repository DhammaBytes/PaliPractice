# Publishing Pāli Practice

This is the authoritative release procedure, also used by the `/release`
command. Shell examples use Bash on macOS and start from the repository
root unless stated otherwise. iOS builds and macOS signing require macOS/Xcode.

## v1.2 distribution formats

The v1.1 GitHub release used portable desktop archives, not installers. v1.2
keeps that format and targets macOS ARM64, Windows x64, and Linux x64. Windows
and Linux desktop builds can be cross-published from macOS; a Windows host is
not needed to create the portable Windows ZIP. Runtime testing still requires
the target OS.

| Artifact | Delivery state required |
| --- | --- |
| `PaliPractice-android.apk` | Signed with the chosen sideload distribution key |
| `PaliPractice-android.aab` | Signed with the Play upload key |
| `PaliPractice-ios.ipa` | App Store distribution signature and provisioning profile |
| `PaliPractice-macos-arm64.zip` | Developer ID signature; notarize and staple before public distribution |
| `PaliPractice-windows-x64.zip` | Self-contained portable directory |
| `PaliPractice-linux-x64.tar.gz` | Self-contained portable directory; preserve executable permission |

When handing Android packages to a human for signing, use the explicit names
`PaliPractice-android-unsigned.apk` and `PaliPractice-android-unsigned.aab`.
They are intermediate artifacts, not ready for installation or store upload.
A Developer ID-signed Mac ZIP awaiting notarization must also be marked as such
in the release handoff. Signing, notarization, store upload, and public release
publication are separate steps.

## Prepare

Install the SDK pinned in `PaliPractice/global.json` and its Android/iOS workloads.
Use the matching Xcode and Android SDK versions required by those workloads.
Run `dotnet workload restore PaliPractice/PaliPractice.csproj` from the
`PaliPractice/` solution directory when setting up a release machine.

Check `ApplicationDisplayVersion` and `ApplicationVersion` in
`PaliPractice/PaliPractice/PaliPractice.csproj`. The display version is `1.2`;
the build number must increase for each store upload. Check the final manifests,
not just the source properties. App Store uploads require the current Apple SDK;
Google Play uploads require the current target Android API. See
[Apple's submission requirements](https://developer.apple.com/news/upcoming-requirements/)
and [Google's target API policy](https://support.google.com/googleplay/android-developer/answer/11926878).

From the repository root, run `python3 quality/gate.py full`. Provision the
pinned comparison inputs described in [scripts/SETUP.md](scripts/SETUP.md) first.
Also test fresh installation and upgrade from v1.1, preserved practice history,
EN/ES/RU resources, dictionary-language fallback, and layouts on each release
platform. The local gate covers tests and desktop compilation; it does not
certify signed native packages or store acceptance.

Create a new staging directory; do not reuse or delete a previous release:

```sh
set -euo pipefail
release_version=1.2
release_build=1201
release_dir="$PWD/release/v$release_version/build-$release_build"
mkdir -p "$PWD/release/v$release_version"
mkdir "$release_dir"
cd PaliPractice
app_project=PaliPractice/PaliPractice.csproj
```

All remaining commands run from this solution directory in the same shell.
`release_dir` is absolute, so packaging commands can safely change directory.
Build sequentially. Build a fixed source commit and record it with the artifacts.
Use a distinct intermediate/artifact directory for each target, for example
`-p:UseArtifactsOutput=true -p:ArtifactsPath="$release_dir/build/artifacts/windows"`.
Keep each target's absolute source and artifact paths consistent on retries:
`/tmp` and `/private/tmp` refer to the same location on macOS but mixing these
spellings has produced duplicate or misplaced Uno resources. Reuse valid
intermediates for incremental builds; deleting them forces native iOS AOT
compilation again. Keep build logs, signing records, and SHA-256 checksums.

A runtime-specific desktop publish overrides
`TargetFrameworks` to prevent that runtime from applying to mobile targets.

## Desktop builds

These commands produce self-contained releases; users do not need to install .NET.
Fresh output directories avoid mixing old artifacts into a new archive. The
.NET runtime is included, but Linux still requires compatible system libraries
and a graphical session. Cross-publishing is not a substitute for launch tests
on Windows and Linux.

```sh
dotnet publish "$app_project" -f net10.0-desktop -r osx-arm64 -c Release \
  -p:TargetFrameworks=net10.0-desktop -p:SelfContained=true \
  -p:PackageFormat=app -o "$release_dir/macos-arm64"

dotnet publish "$app_project" -f net10.0-desktop -r win-x64 -c Release \
  -p:TargetFrameworks=net10.0-desktop -p:SelfContained=true \
  -o "$release_dir/windows"

dotnet publish "$app_project" -f net10.0-desktop -r linux-x64 -c Release \
  -p:TargetFrameworks=net10.0-desktop -p:SelfContained=true \
  -o "$release_dir/linux"
```

### macOS signing and notarization

The app project sets `UnoMacOSMinimumSystemVersion` to `10.15`; the native menu
compiler uses the same value. ARM64 has a hardware/OS floor of macOS 11.
An optional `osx-x64` build is needed to include Intel/Catalina users; it is not
part of the requested v1.2 artifact set. Verify each bundle's
`LSMinimumSystemVersion` and the executable/menu-library deployment targets.
The ICU dylibs' macOS 15 stamp is an upstream build bug tracked in
[Uno #24496](https://github.com/unoplatform/uno/issues/24496); do not infer the
app's deployment target from that stamp. Test on the oldest supported OS;
deployment targets alone do not prove runtime compatibility.

The entitlement file is `PaliPractice/Platforms/macOS/Entitlements.plist`.
It permits the .NET JIT and bundled native libraries. This is a Developer ID
release, not a sandboxed Mac App Store app.

Sign nested Mach-O files before the outer bundle, using the same Developer ID
Application identity. Replace the example identity below. Avoid `codesign --deep`
for signing; use it for verification. Keep archives outside the `.app` bundle.
For the requested v1.2 release, use `mac_arch=arm64`. Repeat only if an additional
Intel build is deliberately included.

Uno 6.7 places `PaliPractice.deps.json` and `PaliPractice.runtimeconfig.json` in
`Contents/MacOS`. Apple treats that directory as code, so strict signing can fail
with “code object is not signed at all” for a JSON file. Move these resources to
`Contents/Resources` and leave relative symlinks before signing. This preserves
the runtime paths and follows Apple's
[guidance for nonstandard bundle structures](https://developer.apple.com/documentation/xcode/embedding-nonstandard-code-structures-in-a-bundle).
Sign nested native libraries first; sign the main executable through the outer
bundle last, rather than signing it early in an unordered file traversal.

```sh
mac_arch=arm64
mac_app="$release_dir/macos-$mac_arch/PāliPractice.app"
signing_identity='Developer ID Application: YOUR NAME (YOUR_TEAM_ID)'
for resource in PaliPractice.deps.json PaliPractice.runtimeconfig.json; do
  if [ ! -L "$mac_app/Contents/MacOS/$resource" ]; then
    mv "$mac_app/Contents/MacOS/$resource" "$mac_app/Contents/Resources/$resource"
    ln -s "../Resources/$resource" "$mac_app/Contents/MacOS/$resource"
  fi
done
find "$mac_app" -type f -print0 | while IFS= read -r -d '' binary; do
  if [ "$binary" != "$mac_app/Contents/MacOS/PaliPractice" ] && file -b "$binary" | grep -q 'Mach-O'; then
    codesign --force --options runtime --timestamp \
      --entitlements PaliPractice/Platforms/macOS/Entitlements.plist \
      --sign "$signing_identity" "$binary" || exit 1
  fi
done
codesign --force --options runtime --timestamp \
  --entitlements PaliPractice/Platforms/macOS/Entitlements.plist \
  --sign "$signing_identity" "$mac_app"
codesign --verify --deep --strict --verbose=2 "$mac_app"
```

Inspect the bundle for nested framework/app bundles before signing; if present,
sign their containers from the inside out before signing the outer `.app`.
The earlier Uno 6.4 release workflow encountered `UNOB0018` during integrated
signing. Manual signing avoids that step; this historical workaround is not a
claim that every later Uno SDK has the same defect.

Store notarization credentials once with the interactive command (it prompts for
Apple ID, team ID, and an app-specific password):

```sh
xcrun notarytool store-credentials pali-notary
```

Check the credentials before submitting:

```sh
xcrun notarytool history --keychain-profile pali-notary
```

HTTP 401 means the saved notarization credentials need refreshing. The
`Pali_Dist` provisioning profile signs the iOS IPA; it is not a Mac notarization
credential. If notarization is delegated, hand off the signed ZIP and record that
stapling and Gatekeeper acceptance are still pending.

Then notarize and staple:

```sh
ditto -c -k --keepParent "$mac_app" "$release_dir/notarization-$mac_arch.zip"
xcrun notarytool submit "$release_dir/notarization-$mac_arch.zip" \
  --keychain-profile pali-notary --wait
xcrun stapler staple "$mac_app"
xcrun stapler validate "$mac_app"
spctl --assess --type execute --verbose=2 "$mac_app"
```

If submission fails, inspect `xcrun notarytool log SUBMISSION_ID
--keychain-profile pali-notary` and resolve the errors before packaging.

## Android

The public v1.1 APK was signed with `CN=Android Debug, O=Android, C=US`;
its signing certificate SHA-256 is
`05eb7cd68cc8d093512086740b67e8ca5f4f804fba443755e8aad7e3bcb8a6b6`.
Changing to a release keystore will not permit an in-place update of that APK
unless the certificates match or a supported signing lineage is present.
Do not assume that the current machine's debug key is the old release key.

Use a persistent signing key for each distribution channel so users can upgrade
without uninstalling. A debug key is unsuitable as a reproducible public release
identity. For Google Play, use the registered upload key; Play App Signing
controls the certificate installed on users' devices. A sideloaded APK can
replace a Play installation only when the installed signing certificates match.

Provide the existing keystore path and alias in `android_keystore` and
`android_key_alias`. Put passwords in private files outside the repository,
referenced by `android_store_password_file` and `android_key_password_file`.
Do not put passwords in command history or committed project files.

```sh
dotnet publish "$app_project" -f net10.0-android -c Release \
  -p:TargetFrameworks=net10.0-android -p:AndroidPackageFormats=apk \
  -p:AndroidKeyStore=true -p:AndroidSigningKeyStore="$android_keystore" \
  -p:AndroidSigningKeyAlias="$android_key_alias" \
  -p:AndroidSigningStorePass="file:$android_store_password_file" \
  -p:AndroidSigningKeyPass="file:$android_key_password_file" \
  -o "$release_dir/android"
```

Only when the build above used the intended persistent release key, copy its
signed APK to the final handoff name:

```sh
cp "$release_dir/android/org.dhammabytes.palipractice-Signed.apk" \
  "$release_dir/PaliPractice-android.apk"
```

Skip this copy for the deferred-signing workflow below; preserve the APK signed
by the release owner. Never replace it with an SDK-generated debug-signed file.

For Play Console, repeat with `-p:AndroidPackageFormats=aab` and
`-o "$release_dir/play"`, using the Play upload key. Verify the signed artifact,
version code, target API, supported ABIs, and native library page alignment.
Upload the signed AAB to an internal test track, verify installation and upgrade,
then complete the store listing and rollout in Play Console.

### Defer Android signing to the release owner

The following workflow was used for the v1.2 handoff. Build each format
sequentially, reusing the same Android artifact directory:

```sh
for android_format in apk aab; do
  dotnet build "$app_project" -t:BuildApk -f net10.0-android -c Release \
    -p:TargetFrameworks=net10.0-android \
    -p:AndroidPackageFormats="$android_format" \
    -p:AndroidBuildApplicationPackage=true \
    -p:UseArtifactsOutput=true \
    -p:ArtifactsPath="$release_dir/build/artifacts/android" \
    -o "$release_dir/android"
done
```

The SDK also emits `-Signed` packages using its default debug key when no release
key is supplied. Do not hand those off as release-signed packages. Select the
original files without the `-Signed` suffix, and verify they have no signature.
The unsigned APK is not yet aligned; align it before signing:

```sh
android_build_tools="$HOME/Library/Android/sdk/build-tools/36.0.0"
"$android_build_tools/zipalign" -P 16 4 \
  "$release_dir/android/org.dhammabytes.palipractice.apk" \
  "$release_dir/PaliPractice-android-unsigned.apk"
"$android_build_tools/zipalign" -c -P 16 4 \
  "$release_dir/PaliPractice-android-unsigned.apk"
cp "$release_dir/android/org.dhammabytes.palipractice.aab" \
  "$release_dir/PaliPractice-android-unsigned.aab"
```

Select the installed build-tools version when it differs. ZIP alignment and ELF
load-segment alignment are separate checks; verify both for 64-bit native
libraries. The v1.2 APK targets API 36, supports API 24+, and includes
`armeabi-v7a`, `arm64-v8a`, and `x86_64`.

The owner signs the aligned APK with `apksigner` and the AAB with `jarsigner`
(or their existing signing workflow), then verifies each signature. APK example:

```sh
"$android_build_tools/apksigner" sign \
  --ks "$android_keystore" --ks-key-alias "$android_key_alias" \
  --ks-pass "file:$android_store_password_file" \
  --key-pass "file:$android_key_password_file" \
  --out "$release_dir/PaliPractice-android.apk" \
  "$release_dir/PaliPractice-android-unsigned.apk"
"$android_build_tools/apksigner" verify --print-certs \
  "$release_dir/PaliPractice-android.apk"
```

Do not alter the APK after signing. An AAB uses JAR signing, not `apksigner`, and
is uploaded to Play Console rather than installed directly.

## iOS / App Store

Use the existing Apple Distribution identity and App Store provisioning profile
for `org.dhammabytes.palipractice`. Set `ios_signing_identity` and
`ios_provisioning_profile` to their installed names or identifiers.

```sh
dotnet publish "$app_project" -f net10.0-ios -r ios-arm64 -c Release \
  -p:TargetFrameworks=net10.0-ios -p:ArchiveOnBuild=true -p:BuildIpa=true \
  -p:CodesignKey="$ios_signing_identity" \
  -p:CodesignProvision="$ios_provisioning_profile" \
  -p:IpaPackageDir="$release_dir/ios"
```

Verify bundle ID, version/build number, localizations, privacy manifest, and
signing. Upload the IPA using Transporter or the Xcode archive workflow, test
through TestFlight, and complete screenshots, localized metadata, and review
information in App Store Connect before submitting for review.

## Package and publish

Package the stapled macOS bundle with `ditto`. Use subshells for the other
archives so the working directory remains unchanged:

```sh
ditto -c -k --keepParent "$release_dir/macos-arm64/PāliPractice.app" \
  "$release_dir/PaliPractice-macos-arm64.zip"
(cd "$release_dir/windows" && zip -r "$release_dir/PaliPractice-windows-x64.zip" . -x '*.pdb')
COPYFILE_DISABLE=1 tar -czf "$release_dir/PaliPractice-linux-x64.tar.gz" \
  --exclude='*.pdb' -C "$release_dir/linux" .
```

Inspect each archive and launch the extracted app on its target OS. Keep release
notes in `$release_dir/notes.md`, including user-visible changes and compatibility
information. Create a draft after the release commit is pushed:

```sh
gh release create "v$release_version" --draft --target RELEASE_COMMIT \
  "$release_dir/PaliPractice-macos-arm64.zip" \
  "$release_dir/PaliPractice-windows-x64.zip" \
  "$release_dir/PaliPractice-linux-x64.tar.gz" \
  "$release_dir/PaliPractice-android.apk" \
  --title "v$release_version" --notes-file "$release_dir/notes.md"
```

Replace `RELEASE_COMMIT` with the verified commit SHA. Review the draft and
artifacts before publishing. Store uploads and public release publication are
separate actions from preparing the binaries.
