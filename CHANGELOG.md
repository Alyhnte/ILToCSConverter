# Changelog

## 1.3.0 — 2026-08-16

### English

- Settings tab: Turkish / English / Russian UI language
- GitHub profile ([Alyhnte](https://github.com/Alyhnte)) and repository links in Settings
- In-app update check against GitHub Releases
- Strong Name: detect private vs public SNK, strip signatures, token map, replace-all extern tokens
- CLI `pack`: `--strip-signature`, `--replace-all-tokens`, `--token-map old=new`

### Türkçe

- **Ayarlar** sekmesi: Türkçe / English / Русский arayüz dili
- GitHub profili ([Alyhnte](https://github.com/Alyhnte)) ve depo bağlantıları
- GitHub Releases üzerinden uygulama içi güncelleme kontrolü
- Strong Name: özel/genel anahtar ayrımı, imza kaldırma, token haritası, tüm extern token’ları eşitleme
- CLI `pack`: `--strip-signature`, `--replace-all-tokens`, `--token-map eski=yeni`

## 1.2.0 — 2026-08-15

- PDB satır eşlemesi ve yerel değişken adları (portable / gömülü PDB)
- Kaynak assembly’den isabetli `TargetFramework`
- Toplu dönüşümde `.sln` üretimi
- Duraklat / devam et kuyruğu
- Koyu tema
- BAML → XAML
- Tip önizleme paneli
- JUnit raporu
- `il2cs` dotnet tool
- **Düzün** sekmesi: DLL/EXE → IL (`ildasm /utf8 /nobar`)
- **IL → DLL**: `.il` dosyasını kendi `.snk` anahtarınızla derleyip imzalama (`ilasm /key=`)

## 1.0.0 — 2026-08-15

İlk ürün sürümü: ILSpy proje decompile, CLI ve WPF arayüz.
