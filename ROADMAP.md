# IL → C# Dönüştürücü — Yol haritası

Bu belge, uygulamanın nerede durduğunu ve sıradaki adımları gösterir.

## 1.0.0 (tamamlandı)

- Tam proje decompile, CLI, WPF, paralellik, eklenti API, testler

## 1.1 (tamamlandı)

- PDB varsa satır eşlemesi (`IDebugInfoProvider` / portable PDB)
- Üretilen `.csproj` hedef çerçevesini kaynak assembly’den seçme
- Çok dosyalı `.sln` üretimi
- Dönüştürme kuyruğu: duraklat / devam et
- Koyu tema

## 1.2 (tamamlandı)

- WPF BAML → XAML (`ICSharpCode.BamlDecompiler`)
- Önizleme paneli (seçilen tipin C# çıktısı)
- CI için JUnit raporu (`conversion-junit.xml`)
- `dotnet tool` olarak yayın (`il2cs`)
- **Düzün:** DLL/EXE → IL (`ildasm`, UTF-8)

## Sonra

- Eski Windows PDB (DIA) desteği
- Çoklu dosya kuyruğunu kaydet / geri yükle
- `dotnet tool` nuget.org yayını
