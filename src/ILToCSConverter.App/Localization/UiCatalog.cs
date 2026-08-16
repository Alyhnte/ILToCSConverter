namespace ILToCSConverter.App.Localization;

public static class UiCatalog
{
    public static string Language { get; private set; } = "tr";

    public static IReadOnlyList<LanguageOption> Languages { get; } =
    [
        new("tr", "Türkçe"),
        new("en", "English"),
        new("ru", "Русский")
    ];

    public static void SetLanguage(string? code) =>
        Language = code is "en" or "ru" ? code : "tr";

    public static string Get(string key)
    {
        if (Tables.TryGetValue(Language, out var table) && table.TryGetValue(key, out string? value))
            return value;
        if (Tables.TryGetValue("tr", out var fallback) && fallback.TryGetValue(key, out value))
            return value;
        return key;
    }

    public static string Format(string key, params object?[] args) =>
        string.Format(Get(key), args);

    private static readonly Dictionary<string, Dictionary<string, string>> Tables = Build();

    private static Dictionary<string, Dictionary<string, string>> Build()
    {
        var tr = new Dictionary<string, string>(StringComparer.Ordinal);
        var en = new Dictionary<string, string>(StringComparer.Ordinal);
        var ru = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var (key, turkish, english, russian) in Entries)
        {
            tr[key] = turkish;
            en[key] = english;
            ru[key] = russian;
        }

        return new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal)
        {
            ["tr"] = tr,
            ["en"] = en,
            ["ru"] = ru
        };
    }

    private static readonly (string Key, string Tr, string En, string Ru)[] Entries =
    [
        ("ProductName", "IL → C# Dönüştürücü", "IL → C# Converter", "IL → C# Конвертер"),
        ("Subtitle", "C#'a çevir, Düzün (DLL → IL) veya kendi .snk anahtarınızla IL → DLL. Sürükle-bırak desteklenir.", "Convert to C#, disassemble (DLL → IL), or assemble IL → DLL with your own .snk. Drag and drop is supported.", "Конвертация в C#, разборка (DLL → IL) или сборка IL → DLL с вашим .snk. Поддерживается перетаскивание."),
        ("ThemeToDark", "Koyu tema", "Dark theme", "Тёмная тема"),
        ("ThemeToLight", "Açık tema", "Light theme", "Светлая тема"),
        ("TabConvert", "C#'a çevir", "Convert to C#", "В C#"),
        ("TabDuzun", "Düzün", "Disassemble", "Разборка"),
        ("TabPack", "IL → DLL", "IL → DLL", "IL → DLL"),
        ("TabSettings", "Ayarlar", "Settings", "Настройки"),
        ("Source", "Kaynak", "Source", "Источник"),
        ("Target", "Hedef", "Target", "Назначение"),
        ("Folder", "Klasör", "Folder", "Папка"),
        ("File", "Dosya", "File", "Файл"),
        ("Options", "Seçenekler", "Options", "Параметры"),
        ("OptRecursive", "Alt klasörler", "Subfolders", "Вложенные папки"),
        ("OptVerify", "Derlemeyi doğrula", "Verify build", "Проверить сборку"),
        ("OptFormat", "Format", "Format", "Форматировать"),
        ("OptGlobal", "Global isim alanı", "Global namespace", "Глобальное пространство имён"),
        ("OptCompiler", "Derleyici tipleri", "Compiler types", "Типы компилятора"),
        ("OptSolution", "Çözüm (.sln)", "Solution (.sln)", "Решение (.sln)"),
        ("OptBaml", "BAML → XAML", "BAML → XAML", "BAML → XAML"),
        ("OptPdb", "PDB satırları", "PDB lines", "Строки PDB"),
        ("CSharpVersion", "C# sürümü", "C# version", "Версия C#"),
        ("Parallelism", "Paralellik (0 = CPU)", "Parallelism (0 = CPU)", "Параллелизм (0 = CPU)"),
        ("Start", "Başlat", "Start", "Старт"),
        ("Cancel", "İptal", "Cancel", "Отмена"),
        ("OpenOutput", "Çıktıyı aç", "Open output", "Открыть результат"),
        ("Log", "Günlük", "Log", "Журнал"),
        ("Preview", "Önizleme", "Preview", "Просмотр"),
        ("LoadTypes", "Tipleri yükle", "Load types", "Загрузить типы"),
        ("DuzunTitle", "Düzün — DLL / EXE → IL", "Disassemble — DLL / EXE → IL", "Разборка — DLL / EXE → IL"),
        ("DuzunHint", "ildasm ile assembly'yi UTF-8 IL metnine ayırır. Her DLL için SHA256 hesaplanır; .il başlığına, .sha256 dosyasına ve checksums.sha256 listesine yazılır. Hedef boşsa _IL_Outputs klasörü açılır.", "Uses ildasm to emit UTF-8 IL. SHA256 is computed for each DLL and written to the .il header, a .sha256 file, and checksums.sha256. If the target is empty, an _IL_Outputs folder is created.", "Разбирает сборку в UTF-8 IL с помощью ildasm. Для каждой DLL вычисляется SHA256 и записывается в заголовок .il, файл .sha256 и список checksums.sha256. Если папка назначения пуста, создаётся _IL_Outputs."),
        ("TargetOptional", "Hedef (isteğe bağlı)", "Target (optional)", "Назначение (необязательно)"),
        ("DuzunStart", "Düzün", "Disassemble", "Разобрать"),
        ("PackTitle", "IL → DLL — kendi anahtarınız", "IL → DLL — your own key", "IL → DLL — ваш ключ"),
        ("PackHint", "Kendi .il dosyanızı ilasm ile DLL'e derler. Kendi .snk dosyanızı seçin veya yeni üretin; IL içindeki publickey bu anahtara hizalanır ve /key= ile imzalanır. Başkasının yayıncı imzasını taklit etmek için kullanmayın.", "Assembles your .il with ilasm. Select your .snk or generate a new one; the publickey in the IL is aligned to that key and signed with /key=. Do not use this to impersonate another publisher's signature.", "Собирает ваш .il в DLL через ilasm. Выберите свой .snk или создайте новый; publickey в IL выравнивается под этот ключ и подписывается через /key=. Не используйте для подделки чужой издательской подписи."),
        ("PackSourceIl", "Kaynak (.il)", "Source (.il)", "Источник (.il)"),
        ("PackTargetDll", "Hedef (DLL klasörü)", "Target (DLL folder)", "Назначение (папка DLL)"),
        ("PackSnk", "Kendi .snk anahtarınız", "Your .snk key", "Ваш ключ .snk"),
        ("Select", "Seç", "Select", "Выбрать"),
        ("GenerateKey", "Yeni anahtar üret", "Generate key", "Создать ключ"),
        ("OptGenerateSnk", "Anahtar yoksa hedef klasöre yeni signing.snk yaz", "If no key is set, write a new signing.snk into the target folder", "Если ключа нет, записать новый signing.snk в папку назначения"),
        ("OptStrip", "Strong Name imzasını kaldır", "Strip Strong Name signature", "Удалить подпись Strong Name"),
        ("OptReplaceAll", "Tüm .assembly extern token'larını yeni anahtara eşitle", "Replace all .assembly extern tokens with the new key", "Заменить все токены .assembly extern новым ключом"),
        ("TokenMap", "Token haritası", "Token map", "Карта токенов"),
        ("TokenMapHint", "Eski=yeni publickeytoken eşlemesi. Örnek: b77a5c561934e089=aabbccddeeff0011 (virgülle ayırın).", "old=new publickeytoken mappings. Example: b77a5c561934e089=aabbccddeeff0011 (comma-separated).", "Соответствие старый=новый publickeytoken. Пример: b77a5c561934e089=aabbccddeeff0011 (через запятую)."),
        ("PackUnsigned", "İmzasız derle", "Assemble unsigned", "Собрать без подписи"),
        ("PackSign", "Derle ve imzala", "Assemble and sign", "Собрать и подписать"),
        ("SettingsTitle", "Ayarlar", "Settings", "Настройки"),
        ("SettingsHint", "Arayüz dili, görünüm, GitHub ve sürüm güncellemeleri.", "Language, appearance, GitHub, and updates.", "Язык интерфейса, оформление, GitHub и обновления."),
        ("SettingsLanguage", "Dil", "Language", "Язык"),
        ("SettingsAppearance", "Görünüm", "Appearance", "Оформление"),
        ("GitHubTitle", "GitHub", "GitHub", "GitHub"),
        ("GitHubHint", "Kaynak kod, sürümler, geliştirici profili ve Patreon destek.", "Source code, releases, author profile, and Patreon support.", "Исходный код, релизы, профиль автора и поддержка на Patreon."),
        ("OpenProfile", "Profili aç", "Open profile", "Открыть профиль"),
        ("OpenRepo", "Depoyu aç", "Open repository", "Открыть репозиторий"),
        ("OpenPatreon", "Patreon'da destekle", "Support on Patreon", "Поддержать на Patreon"),
        ("UpdateTitle", "Güncelleme", "Updates", "Обновления"),
        ("UpdateHint", "GitHub Releases üzerinden yeni sürümleri kontrol edin. Hazır Windows paketini (zip / exe) son sürüm sayfasından indirin.", "Check GitHub Releases for newer versions. Download the ready Windows package (zip / exe) from the latest release page.", "Проверяйте новые версии через GitHub Releases. Готовый пакет Windows (zip / exe) скачивается со страницы последнего релиза."),
        ("CheckUpdates", "Güncellemeleri kontrol et", "Check for updates", "Проверить обновления"),
        ("OpenReleases", "Sürümleri aç", "Open releases", "Открыть релизы"),
        ("DownloadLatest", "Paketi indir", "Download package", "Скачать пакет"),
        ("UpdateIdle", "Henüz kontrol edilmedi.", "Not checked yet.", "Ещё не проверялось."),
        ("UpdateChecking", "Kontrol ediliyor…", "Checking…", "Проверка…"),
        ("UpdateLatest", "Güncel sürümü kullanıyorsunuz ({0}).", "You are on the latest version ({0}).", "Установлена актуальная версия ({0})."),
        ("UpdateAvailable", "Yeni sürüm var: {0}", "Update available: {0}", "Доступна новая версия: {0}"),
        ("UpdateNone", "Yayınlanmış sürüm bulunamadı.", "No published releases found.", "Опубликованных релизов нет."),
        ("UpdateFailed", "Kontrol başarısız: {0}", "Update check failed: {0}", "Не удалось проверить: {0}"),
        ("CurrentVersion", "Mevcut sürüm: {0}", "Current version: {0}", "Текущая версия: {0}"),
        ("Author", "Geliştirici", "Author", "Автор"),
        ("BrowseSourceFolder", "Kaynak klasörü seçin", "Select source folder", "Выберите исходную папку"),
        ("BrowseTargetFolder", "Hedef klasörü seçin", "Select target folder", "Выберите папку назначения"),
        ("BrowseIlOutput", "IL çıktı klasörü", "IL output folder", "Папка вывода IL"),
        ("BrowseDllOutput", "DLL çıktı klasörü", "DLL output folder", "Папка вывода DLL"),
        ("PickFile", "Dosya seçin", "Select a file", "Выберите файл"),
        ("FilterConvert", "Dönüştürülebilir|*.il;*.dll;*.exe;*.netmodule|Tüm dosyalar|*.*", "Convertible|*.il;*.dll;*.exe;*.netmodule|All files|*.*", "Поддерживаемые|*.il;*.dll;*.exe;*.netmodule|Все файлы|*.*"),
        ("FilterAssembly", "Assembly|*.dll;*.exe;*.netmodule|Tüm dosyalar|*.*", "Assembly|*.dll;*.exe;*.netmodule|All files|*.*", "Сборка|*.dll;*.exe;*.netmodule|Все файлы|*.*"),
        ("FilterIl", "IL dosyası|*.il|Tüm dosyalar|*.*", "IL file|*.il|All files|*.*", "Файл IL|*.il|Все файлы|*.*"),
        ("FilterSnk", "Strong name anahtarı|*.snk|Tüm dosyalar|*.*", "Strong name key|*.snk|All files|*.*", "Ключ strong name|*.snk|Все файлы|*.*"),
        ("SaveSnkTitle", "Yeni .snk kaydet", "Save new .snk", "Сохранить новый .snk"),
        ("KeyCreated", "Yeni anahtar yazıldı: {0}  (token {1})", "New key written: {0}  (token {1})", "Новый ключ записан: {0}  (token {1})"),
        ("NeedConvertPaths", "Kaynak ve hedef yollarını girin.", "Enter source and target paths.", "Укажите пути источника и назначения."),
        ("NeedDuzunInput", "Kaynak DLL/EXE yolunu girin.", "Enter the source DLL/EXE path.", "Укажите путь к исходному DLL/EXE."),
        ("NeedPackPaths", "Kaynak IL ve hedef klasör yollarını girin.", "Enter source IL and target folder paths.", "Укажите пути исходного IL и папки назначения."),
        ("PreviewNeedDll", "Önizleme için bir DLL/EXE seçin.", "Select a DLL/EXE for preview.", "Для просмотра выберите DLL/EXE."),
        ("NoTypes", "Tip bulunamadı.", "No types found.", "Типы не найдены."),
        ("TypesLoaded", "{0} tip yüklendi. Soldan birini seçin.", "{0} types loaded. Select one on the left.", "Загружено типов: {0}. Выберите слева."),
        ("Ready", "Hazır", "Ready", "Готово"),
        ("Running", "Çalışıyor…", "Running…", "Выполняется…"),
        ("Paused", "Duraklatıldı", "Paused", "Приостановлено"),
        ("Resuming", "Devam ediyor…", "Resuming…", "Продолжение…"),
        ("Cancelled", "İptal edildi", "Cancelled", "Отменено"),
        ("Stopped", "İşlem durduruldu.", "Operation stopped.", "Операция остановлена."),
        ("Error", "Hata", "Error", "Ошибка"),
        ("Done", "Bitti: {0} başarılı, {1} hatalı", "Done: {0} succeeded, {1} failed", "Готово: {0} успешно, {1} с ошибкой"),
        ("IldasmMissing", "ildasm.exe bulunamadı — Windows SDK / NETFX Tools gerekli", "ildasm.exe not found — Windows SDK / NETFX Tools required", "ildasm.exe не найден — нужен Windows SDK / NETFX Tools"),
        ("IlasmMissingPack", "ilasm.exe bulunamadı — IL → DLL için Windows SDK / NETFX Tools gerekli", "ilasm.exe not found — Windows SDK / NETFX Tools required for IL → DLL", "ilasm.exe не найден — для IL → DLL нужен Windows SDK / NETFX Tools"),
        ("IlasmMissingConvert", "ilasm.exe bulunamadı — yalnızca DLL/EXE dönüştürülebilir", "ilasm.exe not found — only DLL/EXE can be converted", "ilasm.exe не найден — можно конвертировать только DLL/EXE"),
        ("Pause", "Duraklat", "Pause", "Пауза"),
        ("Resume", "Devam", "Resume", "Продолжить"),
        ("PreviewPlaceholder", "Bir assembly seçin, soldan tip seçerek C# önizleyin.", "Select an assembly, then pick a type on the left to preview C#.", "Выберите сборку и тип слева для просмотра C#.")
    ];
}
