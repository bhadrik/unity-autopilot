# com.utilities.encoder.wav

[![Discord](https://img.shields.io/discord/855294214065487932.svg?label=&logo=discord&logoColor=ffffff&color=7389D8&labelColor=6A7EC2)](https://discord.gg/xQgMW9ufN4) [![openupm](https://img.shields.io/npm/v/com.utilities.encoder.wav?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.utilities.encoder.wav/) [![openupm](https://img.shields.io/badge/dynamic/json?color=brightgreen&label=downloads&query=%24.downloads&suffix=%2Fmonth&url=https%3A%2F%2Fpackage.openupm.com%2Fdownloads%2Fpoint%2Flast-month%2Fcom.utilities.encoder.wav)](https://openupm.com/packages/com.utilities.encoder.wav/)

Simple library for WAV encoding support in the [Unity](https://unity.com/) Game Engine.

## Installing

Requires Unity 2021.3 LTS or higher.

The recommended installation method is though the unity package manager and [OpenUPM](https://openupm.com/packages/com.utilities.encoder.wav).

### Via Unity Package Manager and OpenUPM

#### Terminal

```terminal
openupm add com.utilities.encoder.wav
```

#### Manual

- Open your Unity project settings
- Select the `Package Manager`
![scoped-registries](images/package-manager-scopes.png)
- Add the OpenUPM package registry:
  - Name: `OpenUPM`
  - URL: `https://package.openupm.com`
  - Scope(s):
    - `com.utilities`
- Open the Unity Package Manager window
- Change the Registry from Unity to `My Registries`
- Add the `Utilities.Encoder.Wav` package

### Via Unity Package Manager and Git url

> [!WARNING]
> This repo has dependencies on other repositories! You are responsible for adding these on your own.

- Open your Unity Package Manager
- Add package from git url: `https://github.com/RageAgainstThePixel/com.utilities.encoder.wav.git#upm`
  - [com.utilities.async](https://github.com/RageAgainstThePixel/com.utilities.async)
  - [com.utilities.audio](https://github.com/RageAgainstThePixel/com.utilities.audio)

---

## Documentation

### Table of Contents

- [Recording Behaviour](#recording-behaviour)
- [Audio Clip Extensions](#audio-clip-extensions)
  - [Encode WAV](#encode-wav)
- [Related Packages](#related-packages)

## Recording Behaviour

Simply add the `WavRecorderBehaviour` to any GameObject to enable recording.

> This will stream the recording directly to disk as it is recorded.

## Audio Clip Extensions

Provides extensions to encode `AudioClip`s to WAV encoded bytes.
Supports 8, 16, 24, and 32 bit sample sizes.

### Encode WAV

```csharp
var bytes = audioClip.EncodeToWav();
var bytes = await audioClip.EncodeToWavAsync();
```

## Related Packages

- [Ogg Encoder](https://github.com/RageAgainstThePixel/com.utilities.encoder.ogg)
