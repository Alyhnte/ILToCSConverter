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

## 1.3 (tamamlandı)

- Arayüz dilleri: Türkçe, English, Русский
- Ayarlar: GitHub hesabı ([Alyhnte](https://github.com/Alyhnte)) ve güncelleme kontrolü
- Strong Name paketleme: imza kaldırma, token haritası, tüm extern token eşitleme
- CLI `pack` seçenekleri (`--strip-signature`, `--replace-all-tokens`, `--token-map`)
- Patreon: [patreon.com/cw/Alyhnte](https://www.patreon.com/cw/Alyhnte)
- GitHub Release: Windows x64 GUI/CLI zip ve exe ([indir](https://github.com/Alyhnte/ILToCSConverter/releases/latest))

## Sonra

- Eski Windows PDB (DIA) desteği
- Çoklu dosya kuyruğunu kaydet / geri yükle
- `dotnet tool` nuget.org yayını
