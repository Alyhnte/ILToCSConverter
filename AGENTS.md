# AGENTS.md

## Cursor Cloud specific instructions

Bu depo bir .NET 9 çözümüdür (`ILToCSConverter.sln`). IL/DLL/EXE dosyalarını okunabilir C# projelerine çeviren bir araçtır. Aşağıdaki notlar gelecekteki Cloud agent'ları içindir (güncelleme scripti çalıştırıldıktan sonraki ortam varsayılır).

### Projeler / servisler

| Proje | Tür | Linux'ta durum |
| --- | --- | --- |
| `src/ILToCSConverter.Core` | Sınıf kütüphanesi (`net9.0`) | Derlenir |
| `src/ILToCSConverter.Cli` | Konsol uygulaması `il2cs` / `ILToCS` (`net9.0`) | Derlenir ve çalışır — Linux'ta çalıştırılabilir birincil uygulama |
| `tests/ILToCSConverter.Tests` | xUnit testleri (`net9.0`) | Çalışır |
| `src/ILToCSConverter.App` | WPF masaüstü (`net9.0-windows`) | **Linux'ta derlenmez** (yalnızca Windows) |

### Önemli / bariz olmayan notlar

- **`.NET SDK` PATH'te değil olabilir.** SDK `~/.dotnet` altına kurulur. Etkileşimli kabuklar için `~/.bashrc` ortam kurulumu sırasında güncellendi (`DOTNET_ROOT`, `PATH`). Yeni bir kabukta `dotnet` bulunmazsa: `export DOTNET_ROOT="$HOME/.dotnet"` ve `export PATH="$HOME/.dotnet:$PATH"` çalıştırın veya doğrudan `~/.dotnet/dotnet` kullanın.
- **Tüm çözümü (`dotnet build ILToCSConverter.sln`) Linux'ta derlemeyin.** WPF `ILToCSConverter.App` projesi `NETSDK1100` hatasıyla başarısız olur. Bunun yerine projeleri tek tek hedefleyin (Core / Cli / Tests). Bu üçlü, Windows'a özgü App projesine bağımlı değildir.
- **`duzun` (DLL→IL) ve `pack` (IL→DLL) alt komutları** harici `ildasm.exe` / `ilasm.exe` araçlarını arar; bunlar bu Linux ortamında yoktur, dolayısıyla bu iki komut çalışmaz (CLI zarifçe uyarı verir). `--in/--out` ile **C#'a çevirme (decompile)** yolu tamamen yönetimli koddur ve harici araç gerektirmeden çalışır.
- İlgili `ilasm` gerektiren test (`ConverterEngineTests`) `ilasm` bulunamazsa sessizce erken döner (atlanır), bu yüzden testler Linux'ta yeşil kalır.
- Standart komutlar `README.md` içinde belgelidir (build/run/test/publish). Burada tekrarlanmaz.

### Sık kullanılan komutlar (Linux'ta çalışan alt küme)

- Derle: `dotnet build src/ILToCSConverter.Cli/ILToCSConverter.Cli.csproj`
- Test: `dotnet test tests/ILToCSConverter.Tests/ILToCSConverter.Tests.csproj`
- Lint/biçim kontrolü: `dotnet format <proje> --verify-no-changes` (yalnızca `info` düzeyi öneriler; build zaten 0 uyarı verir)
- Çalıştır (hello-world / decompile): `dotnet run --project src/ILToCSConverter.Cli -- --in <klasör-veya-dll> --out <çıktı> --no-baml`
