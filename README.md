# IL → C# Converter

[English](#english) · [Türkçe](#türkçe)

---

## English

Turns IL, DLL, and EXE files into readable C# projects. **Düzün** disassembles a DLL/EXE to UTF-8 IL. **IL → DLL** assembles your own `.il` files into a DLL signed with your own `.snk` key.

### UI

```powershell
dotnet run --project src/ILToCSConverter.App
```

Tabs: **Convert to C#**, **Düzün**, and **IL → DLL**.

### CLI

```powershell
dotnet run --project src/ILToCSConverter.Cli -- --in "D:\source" --out "D:\output" --recursive --parallel 4
dotnet run --project src/ILToCSConverter.Cli -- duzun --in "D:\mod.dll"
dotnet run --project src/ILToCSConverter.Cli -- duzun --in "D:\dlls" --out "D:\il"
dotnet run --project src/ILToCSConverter.Cli -- pack --in "D:\mine.il" --out "D:\dlls" --snk "D:\mine.snk"
dotnet run --project src/ILToCSConverter.Cli -- pack --in "D:\il" --out "D:\dlls" --generate-snk
```

If the output path is empty, Düzün writes next to the DLL in a `{name}_IL_Outputs` folder. `pack --generate-snk` writes `signing.snk` into the output folder.

### dotnet tool (`il2cs`)

```powershell
dotnet pack src/ILToCSConverter.Cli -c Release -o artifacts/nupkg
dotnet tool install --global --add-source artifacts/nupkg il2cs
il2cs --in .\bin --out .\cs
il2cs duzun --in .\Game.dll
il2cs pack --in .\Game.il --out .\dlls --snk .\mine.snk
```

### Publish (single exe)

```powershell
dotnet publish src/ILToCSConverter.App -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o artifacts/app
dotnet publish src/ILToCSConverter.Cli -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o artifacts/cli
```

### Tests

```powershell
dotnet test
```

Roadmap: [ROADMAP.md](ROADMAP.md)

### Credits

- [Alyhnte](https://github.com/Alyhnte)

---

## Türkçe

IL, DLL ve EXE dosyalarını okunabilir C# projelerine çevirir. **Düzün** ile DLL/EXE dosyasını UTF-8 IL metnine ayırır. **IL → DLL** ile kendi `.il` dosyanızı kendi `.snk` anahtarınızla imzalı DLL'e derler.

### Arayüz

```powershell
dotnet run --project src/ILToCSConverter.App
```

Sekmeler: **C#'a çevir**, **Düzün** ve **IL → DLL**.

### CLI

```powershell
dotnet run --project src/ILToCSConverter.Cli -- --in "D:\kaynak" --out "D:\cikti" --recursive --parallel 4
dotnet run --project src/ILToCSConverter.Cli -- duzun --in "D:\mod.dll"
dotnet run --project src/ILToCSConverter.Cli -- duzun --in "D:\dlls" --out "D:\il"
dotnet run --project src/ILToCSConverter.Cli -- pack --in "D:\mine.il" --out "D:\dlls" --snk "D:\mine.snk"
dotnet run --project src/ILToCSConverter.Cli -- pack --in "D:\il" --out "D:\dlls" --generate-snk
```

Hedef boşsa Düzün, DLL'nin yanında `{ad}_IL_Outputs` klasörü açar. `pack --generate-snk` hedefe `signing.snk` yazar.

### dotnet tool (`il2cs`)

```powershell
dotnet pack src/ILToCSConverter.Cli -c Release -o artifacts/nupkg
dotnet tool install --global --add-source artifacts/nupkg il2cs
il2cs --in .\bin --out .\cs
il2cs duzun --in .\Game.dll
il2cs pack --in .\Game.il --out .\dlls --snk .\mine.snk
```

### Yayın (tek exe)

```powershell
dotnet publish src/ILToCSConverter.App -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o artifacts/app
dotnet publish src/ILToCSConverter.Cli -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o artifacts/cli
```

### Test

```powershell
dotnet test
```

Ayrıntılı plan: [ROADMAP.md](ROADMAP.md)

### Emeği geçenler

- [Alyhnte](https://github.com/Alyhnte)
