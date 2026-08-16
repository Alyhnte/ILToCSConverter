# IL → C# Converter

[English](#english) · [Türkçe](#türkçe)

**v1.3.0** · UI: Türkçe · English · Русский · GitHub: [Alyhnte](https://github.com/Alyhnte) · Repo: [Alyhnte/ILToCSConverter](https://github.com/Alyhnte/ILToCSConverter) · Patreon: [Alyhnte](https://www.patreon.com/cw/Alyhnte)

Turns IL, DLL, and EXE files into readable C# projects. **Düzün** disassembles a DLL/EXE to UTF-8 IL. **IL → DLL** assembles your own `.il` files and signs them with **your own** `.snk` key.

### Download / İndir

Hazır Windows paketi (kurulum yok, .NET gömülü): **[son sürümü indir](https://github.com/Alyhnte/ILToCSConverter/releases/latest)**

- `ILToCSConverter-1.3.0-win-x64.zip` — arayüz (`ILToCSConverter.exe`)
- `ILToCS-1.3.0-win-x64.zip` — komut satırı (`ILToCS.exe`)

Ready-to-run Windows x64 (no install, .NET bundled): **[download latest release](https://github.com/Alyhnte/ILToCSConverter/releases/latest)**

---

## English

### What’s new in 1.3.0

- **Settings** tab: language (Turkish / English / Russian), GitHub profile, in-app update check against GitHub Releases
- Strong Name packing: strip signatures, remap extern tokens, optional “replace all tokens”
- CLI `pack` flags: `--strip-signature`, `--replace-all-tokens`, `--token-map`

### UI

```powershell
dotnet run --project src/ILToCSConverter.App
```

Tabs: **Convert to C#**, **Düzün**, **IL → DLL**, **Settings**.

### CLI

```powershell
dotnet run --project src/ILToCSConverter.Cli -- --in "D:\source" --out "D:\output" --recursive --parallel 4
dotnet run --project src/ILToCSConverter.Cli -- duzun --in "D:\mod.dll"
dotnet run --project src/ILToCSConverter.Cli -- pack --in "D:\mine.il" --out "D:\dlls" --snk "D:\mine.snk"
dotnet run --project src/ILToCSConverter.Cli -- pack --in "D:\il" --out "D:\dlls" --generate-snk
dotnet run --project src/ILToCSConverter.Cli -- pack --in "D:\il" --out "D:\dlls" --strip-signature
dotnet run --project src/ILToCSConverter.Cli -- pack --in "D:\il" --out "D:\dlls" --snk "D:\mine.snk" --replace-all-tokens
dotnet run --project src/ILToCSConverter.Cli -- pack --in "D:\il" --out "D:\dlls" --snk "D:\mine.snk" --token-map b77a5c561934e089=aabbccddeeff0011
```

If the Düzün output path is empty, files go next to the DLL in a `{name}_IL_Outputs` folder. `pack --generate-snk` writes `signing.snk` into the output folder. Token maps are `old=new` (repeatable; commas or newlines also work).

### dotnet tool (`il2cs`)

```powershell
dotnet pack src/ILToCSConverter.Cli -c Release -o artifacts/nupkg
dotnet tool install --global --add-source artifacts/nupkg il2cs
il2cs --in .\bin --out .\cs
il2cs duzun --in .\Game.dll
il2cs pack --in .\Game.il --out .\dlls --snk .\mine.snk
```

### Publish (single exe)

Prefer the GitHub Release zip above. To build locally on Windows:

```powershell
pwsh -File scripts/publish-windows.ps1
```

Packages land in `artifacts/dist/`. Tag `v1.3.0` (or run **Actions → Release Windows packages**) publishes the same files to [Releases](https://github.com/Alyhnte/ILToCSConverter/releases/latest).

### Tests

```powershell
dotnet test
```

Roadmap: [ROADMAP.md](ROADMAP.md) · Changelog: [CHANGELOG.md](CHANGELOG.md)

### Support / Donate

- Patreon: [patreon.com/cw/Alyhnte](https://www.patreon.com/cw/Alyhnte)
- GitHub: [Alyhnte](https://github.com/Alyhnte)

### Credits

- [Alyhnte](https://github.com/Alyhnte)

---

## Türkçe

IL, DLL ve EXE dosyalarını okunabilir C# projelerine çevirir. **Düzün** ile DLL/EXE dosyasını UTF-8 IL metnine ayırır. **IL → DLL** ile kendi `.il` dosyanızı **kendi** `.snk` anahtarınızla imzalı DLL’e derler.

### 1.3.0’da yeni

- **Ayarlar** sekmesi: dil (Türkçe / English / Русский), GitHub profili, GitHub Releases üzerinden güncelleme kontrolü
- Strong Name paketleme: imza kaldırma, extern token haritası, isteğe bağlı “tüm token’ları eşitle”
- CLI `pack`: `--strip-signature`, `--replace-all-tokens`, `--token-map`

### Arayüz

```powershell
dotnet run --project src/ILToCSConverter.App
```

Sekmeler: **C#'a çevir**, **Düzün**, **IL → DLL**, **Ayarlar**.

### CLI

```powershell
dotnet run --project src/ILToCSConverter.Cli -- --in "D:\kaynak" --out "D:\cikti" --recursive --parallel 4
dotnet run --project src/ILToCSConverter.Cli -- duzun --in "D:\mod.dll"
dotnet run --project src/ILToCSConverter.Cli -- pack --in "D:\mine.il" --out "D:\dlls" --snk "D:\mine.snk"
dotnet run --project src/ILToCSConverter.Cli -- pack --in "D:\il" --out "D:\dlls" --generate-snk
dotnet run --project src/ILToCSConverter.Cli -- pack --in "D:\il" --out "D:\dlls" --strip-signature
dotnet run --project src/ILToCSConverter.Cli -- pack --in "D:\il" --out "D:\dlls" --snk "D:\mine.snk" --replace-all-tokens
dotnet run --project src/ILToCSConverter.Cli -- pack --in "D:\il" --out "D:\dlls" --snk "D:\mine.snk" --token-map b77a5c561934e089=aabbccddeeff0011
```

Hedef boşsa Düzün, DLL’nin yanında `{ad}_IL_Outputs` klasörü açar. `pack --generate-snk` hedefe `signing.snk` yazar. Token haritası `eski=yeni` (tekrarlanabilir; virgül veya satır da olur).

### dotnet tool (`il2cs`)

```powershell
dotnet pack src/ILToCSConverter.Cli -c Release -o artifacts/nupkg
dotnet tool install --global --add-source artifacts/nupkg il2cs
il2cs --in .\bin --out .\cs
il2cs duzun --in .\Game.dll
il2cs pack --in .\Game.il --out .\dlls --snk .\mine.snk
```

### Yayın (tek exe)

Hazır indirme: [son sürüm](https://github.com/Alyhnte/ILToCSConverter/releases/latest). Windows’ta yerelde paketlemek için:

```powershell
pwsh -File scripts/publish-windows.ps1
```

Dosyalar `artifacts/dist/` altına yazılır. `v1.3.0` etiketi (veya **Actions → Release Windows packages**) aynı paketleri [Releases](https://github.com/Alyhnte/ILToCSConverter/releases/latest) sayfasına yükler.

### Test

```powershell
dotnet test
```

Yol haritası: [ROADMAP.md](ROADMAP.md) · Değişiklikler: [CHANGELOG.md](CHANGELOG.md)

### Destek / Bağış

- Patreon: [patreon.com/cw/Alyhnte](https://www.patreon.com/cw/Alyhnte)
- GitHub: [Alyhnte](https://github.com/Alyhnte)

### Emeği geçenler

- [Alyhnte](https://github.com/Alyhnte)
