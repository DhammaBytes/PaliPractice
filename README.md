<img src="PaliPractice/PaliPractice/Assets/Svg/quail.svg?v=2" alt="Pāli Practice app icon: a quail hiding behind rocks" width="128">

# Pāli Practice

A cross-platform Pāli language learning app designed to train noun declensions and verb conjugations with flashcards, using spaced repetition. Built with .NET and Uno Platform, so that it works on:

- Windows x64 (Windows 10/11; see [.NET OS support](https://github.com/dotnet/core/blob/main/release-notes/10.0/supported-os.md#windows))
- macOS 10.15+ (Catalina)
- Linux x64 ([.NET-supported distros](https://github.com/dotnet/core/blob/main/release-notes/10.0/supported-os.md#linux))
- Android 7+ (Nougat)
- iOS 15+ (iPhone & iPad)

Install from [Google Play](https://play.google.com/store/apps/details?id=org.dhammabytes.palipractice) or the [App Store](https://apps.apple.com/us/app/pāli-practice/id6742040410), or directly download an Android APK and desktop versions from the [Releases page](https://github.com/DhammaBytes/PaliPractice/releases).

## Build and run

Install the .NET SDK pinned in [global.json](PaliPractice/global.json). On macOS,
also install Xcode command-line tools for the native menu library. See
[Uno's development setup](https://platform.uno/docs/articles/get-started.html)
for platform prerequisites, including Linux native libraries.

```sh
git clone https://github.com/DhammaBytes/PaliPractice.git
cd PaliPractice/PaliPractice
dotnet run --project PaliPractice/PaliPractice.csproj -f net10.0-desktop \
  -p:TargetFrameworks=net10.0-desktop
```

The app includes its dictionary database. An ordinary app build does not require
the DPD submodule, corpus downloads, or database generation. The desktop-only
command above avoids requiring mobile workloads. Android and iOS development
requires the corresponding .NET workloads and platform SDKs; iOS requires macOS
and Xcode.

The distributed desktop architectures are listed above. Other architectures or
older OS versions are not claimed as tested. Mobile deployment minimums are
Android 7 and iOS 15; these are distinct from the OS versions supported upstream
by the current .NET runtime.

## Tests and data development

The full suite compares the bundled dictionary with pinned DPD and corpus inputs.
Provision those inputs using [scripts/SETUP.md](scripts/SETUP.md), then run from
the repository root:

```sh
python3 quality/gate.py full
```

[quality/README.md](quality/README.md) explains test prerequisites, dependency
restoration, and evidence. The gate validates an isolated copy and does not
replace the bundled dictionary. Database rebuilding is a separate, optional
workflow described in [scripts/SETUP.md](scripts/SETUP.md).

For release builds, signing, and packaging, see [PUBLISHING.md](PUBLISHING.md).

## Icon

The app's icon shows the quail from SN 47:6, the Sakuṇagghi Sutta ("The Hawk"), hiding behind rocks in its ancestral territory – a newly plowed field with clumps of earth all turned up.

> "Wander, monks, in what is your proper range, your own ancestral territory. In one who wanders in what is his proper range, his own ancestral territory, Māra gains no opening, Māra gains no foothold. And what, for a monk, is his proper range, his own ancestral territory? The four establishings of mindfulness."

Read the [full sutta](https://www.dhammatalks.org/suttas/SN/SN47_6.html).

Icon illustration and app design by [Irina Mir](https://www.instagram.com/irmirx/)

## License

*Pāli Practice* builds on the hard work of the contributors to the [Digital Pāḷi Dictionary](https://digitalpalidictionary.github.io) which is included as a Git submodule in this project. To keep things simple, it is released under the same **CC BY-NC-SA 4.0** license as the *Digital Pāḷi Dictionary* itself.

- __CC__: You are free to __share__ and __adapt__ it
- __BY__: as long as you attribute the source,
- __NC__: don't use it commercially,
- __SA__: and share under the same conditions.

View the full license details on the [Creative Commons website](http://creativecommons.org/licenses/by-nc-sa/4.0/).

<a rel="license" href="http://creativecommons.org/licenses/by-nc-sa/4.0/" target="_blank"><img alt="Creative Commons License" style="border-width:0" src="https://i.creativecommons.org/l/by-nc-sa/4.0/88x31.png" /></a><br/>

---

*Sabbe sattā sukhitā hontu – May all beings be happy* 🙏
