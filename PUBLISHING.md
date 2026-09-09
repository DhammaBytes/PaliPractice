# Publishing Pāli Practice

This is the authoritative release procedure, also used by the `/release`
command. Shell examples use Bash on macOS and start from the repository
root unless stated otherwise. iOS builds and macOS signing require macOS/Xcode.

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
set -e
release_version=1.2
release_dir="$PWD/release/v$release_version"
mkdir -p "$PWD/release"
mkdir "$release_dir"
cd PaliPractice
app_project=PaliPractice/PaliPractice.csproj
```

All remaining commands run from this solution directory in the same shell.
`release_dir` is absolute, so packaging commands can safely change directory.
Build sequentially. A runtime-specific desktop publish overrides
`TargetFrameworks` to prevent that runtime from applying to mobile targets.

## Desktop builds

These commands produce self-contained releases; users do not need to install .NET.
Fresh output directories avoid mixing old artifacts into a new archive.

```sh
dotnet publish "$app_project" -f net10.0-desktop -r osx-arm64 -c Release \
  -p:TargetFrameworks=net10.0-desktop -p:SelfContained=true \
  -p:PackageFormat=app -o "$release_dir/macos"

dotnet publish "$app_project" -f net10.0-desktop -r win-x64 -c Release \
  -p:TargetFrameworks=net10.0-desktop -p:SelfContained=true \
  -o "$release_dir/windows"

dotnet publish "$app_project" -f net10.0-desktop -r linux-x64 -c Release \
  -p:TargetFrameworks=net10.0-desktop -p:SelfContained=true \
  -o "$release_dir/linux"
```

### macOS signing and notarization

The entitlement file is `PaliPractice/Platforms/macOS/Entitlements.plist`.
It permits the .NET JIT and bundled native libraries. This is a Developer ID
release, not a sandboxed Mac App Store app.

Sign nested Mach-O files before the outer bundle, using the same Developer ID
Application identity. Replace the example identity below. Avoid `codesign --deep`
for signing; use it for verification. Keep archives outside the `.app` bundle.

```sh
mac_app="$release_dir/macos/PāliPractice.app"
signing_identity='Developer ID Application: YOUR NAME (YOUR_TEAM_ID)'
find "$mac_app" -type f -print0 | while IFS= read -r -d '' binary; do
  if file -b "$binary" | grep -q 'Mach-O'; then
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

Then notarize and staple:

```sh
ditto -c -k --keepParent "$mac_app" "$release_dir/notarization.zip"
xcrun notarytool submit "$release_dir/notarization.zip" \
  --keychain-profile pali-notary --wait
xcrun stapler staple "$mac_app"
xcrun stapler validate "$mac_app"
spctl --assess --type execute --verbose=2 "$mac_app"
```

If submission fails, inspect `xcrun notarytool log SUBMISSION_ID
--keychain-profile pali-notary` and resolve the errors before packaging.

## Android

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

For Play Console, repeat with `-p:AndroidPackageFormats=aab` and
`-o "$release_dir/play"`, using the Play upload key. Verify the signed artifact,
version code, target API, supported ABIs, and native library page alignment.
Upload the signed AAB to an internal test track, verify installation and upgrade,
then complete the store listing and rollout in Play Console.

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
ditto -c -k --keepParent "$mac_app" "$release_dir/PaliPractice-macos-arm64.zip"
(cd "$release_dir/windows" && zip -r "$release_dir/PaliPractice-windows-x64.zip" . -x '*.pdb')
COPYFILE_DISABLE=1 tar -czf "$release_dir/PaliPractice-linux-x64.tar.gz" \
  --exclude='*.pdb' -C "$release_dir/linux" .
cp "$release_dir/android/org.dhammabytes.palipractice-Signed.apk" \
  "$release_dir/PaliPractice-android.apk"
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
