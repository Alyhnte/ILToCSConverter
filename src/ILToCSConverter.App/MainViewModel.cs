using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using ILToCSConverter.App.Localization;
using ILToCSConverter.Core;
using ILToCSConverter.Core.Conversion;
using ILToCSConverter.Core.Decompile;
using ILToCSConverter.Core.Ilasm;
using ILToCSConverter.Core.Ildasm;
using ILToCSConverter.Core.Packing;
using ILToCSConverter.Core.Settings;
using ILToCSConverter.Core.Signing;
using ILToCSConverter.Core.Updates;
using Microsoft.Win32;

namespace ILToCSConverter.App;

public sealed class LogItem
{
    public required string Time { get; init; }
    public required string Level { get; init; }
    public required string Message { get; init; }
}

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly AppSettings _settings;
    private readonly ConversionQueue _queue = new();
    private CancellationTokenSource? _cts;
    private UpdateCheckResult? _lastUpdate;
    private string _inputPath = "";
    private string _outputPath = "";
    private string _duzunInputPath = "";
    private string _duzunOutputPath = "";
    private string _packInputPath = "";
    private string _packOutputPath = "";
    private string _snkPath = "";
    private string _tokenMap = "";
    private string _uiLanguage = "tr";
    private string _title = "";
    private string _themeButtonLabel = "";
    private string _packActionLabel = "";
    private string _installedVersionText = "";
    private string _updateStatus = "";
    private string _languageVersion = "CSharp12";
    private string _statusText = "";
    private string _toolStatus = "";
    private string _previewCode = "";
    private string _pauseLabel = "";
    private bool _recursive = true;
    private bool _verifyBuild;
    private bool _formatOutput;
    private bool _includeGlobal = true;
    private bool _includeGenerated;
    private bool _generateSolution = true;
    private bool _decompileBaml = true;
    private bool _usePdb = true;
    private bool _generateSnkIfMissing = true;
    private bool _stripSignature;
    private bool _replaceAllExternTokens;
    private bool _isRunning;
    private bool _isPaused;
    private bool _isDarkTheme;
    private bool _updateBusy;
    private bool _autoCheckedUpdates;
    private int _selectedTabIndex;
    private int _parallelism;
    private int _progressValue;
    private int _progressMaximum = 1;
    private string? _lastOutputFolder;
    private TypePreviewItem? _selectedPreviewType;

    public MainViewModel()
    {
        _settings = SettingsStore.Load();
        _inputPath = _settings.LastInput ?? "";
        _outputPath = _settings.LastOutput ?? "";
        _duzunInputPath = _settings.LastDuzunInput ?? "";
        _duzunOutputPath = _settings.LastDuzunOutput ?? "";
        _packInputPath = _settings.LastPackInput ?? "";
        _packOutputPath = _settings.LastPackOutput ?? "";
        _snkPath = _settings.LastSnkPath ?? "";
        _tokenMap = _settings.LastTokenMap ?? "";
        _generateSnkIfMissing = _settings.GenerateSnkIfMissing;
        _stripSignature = _settings.StripSignature;
        _replaceAllExternTokens = _settings.ReplaceAllExternTokens;
        _uiLanguage = _settings.UiLanguage is "en" or "ru" ? _settings.UiLanguage : "tr";
        _languageVersion = _settings.LanguageVersion;
        _recursive = _settings.Recursive;
        _verifyBuild = _settings.VerifyBuild;
        _formatOutput = _settings.FormatOutput;
        _includeGlobal = _settings.IncludeGlobalNamespace;
        _includeGenerated = _settings.IncludeCompilerGenerated;
        _generateSolution = _settings.GenerateSolution;
        _decompileBaml = _settings.DecompileBaml;
        _usePdb = _settings.UsePdb;
        _parallelism = _settings.MaxParallelism;
        _isDarkTheme = _settings.DarkTheme;

        foreach (string path in _settings.RecentInputs) RecentInputs.Add(path);
        foreach (string path in _settings.RecentOutputs) RecentOutputs.Add(path);
        foreach (string path in _settings.RecentDuzunInputs) RecentDuzunInputs.Add(path);
        foreach (string path in _settings.RecentDuzunOutputs) RecentDuzunOutputs.Add(path);
        foreach (string path in _settings.RecentPackInputs) RecentPackInputs.Add(path);
        foreach (string path in _settings.RecentPackOutputs) RecentPackOutputs.Add(path);

        BrowseInputCommand = new RelayCommand(BrowseInput, () => !IsRunning);
        BrowseInputFileCommand = new RelayCommand(BrowseInputFile, () => !IsRunning);
        BrowseOutputCommand = new RelayCommand(BrowseOutput, () => !IsRunning);
        BrowseDuzunInputCommand = new RelayCommand(BrowseDuzunInput, () => !IsRunning);
        BrowseDuzunInputFileCommand = new RelayCommand(BrowseDuzunInputFile, () => !IsRunning);
        BrowseDuzunOutputCommand = new RelayCommand(BrowseDuzunOutput, () => !IsRunning);
        BrowsePackInputCommand = new RelayCommand(BrowsePackInput, () => !IsRunning);
        BrowsePackInputFileCommand = new RelayCommand(BrowsePackInputFile, () => !IsRunning);
        BrowsePackOutputCommand = new RelayCommand(BrowsePackOutput, () => !IsRunning);
        BrowseSnkCommand = new RelayCommand(BrowseSnk, () => !IsRunning && PackSigningEnabled);
        GenerateSnkCommand = new RelayCommand(GenerateSnk, () => !IsRunning && PackSigningEnabled);
        StartCommand = new RelayCommand(StartAsync, () => !IsRunning);
        StartDuzunCommand = new RelayCommand(StartDuzunAsync, () => !IsRunning);
        StartPackCommand = new RelayCommand(StartPackAsync, () => !IsRunning);
        CancelCommand = new RelayCommand(Cancel, () => IsRunning);
        PauseCommand = new RelayCommand(TogglePause, () => IsRunning);
        OpenOutputCommand = new RelayCommand(OpenOutput, () => !string.IsNullOrWhiteSpace(_lastOutputFolder));
        ToggleThemeCommand = new RelayCommand(() => IsDarkTheme = !IsDarkTheme);
        LoadPreviewCommand = new RelayCommand(LoadPreviewTypes, () => !IsRunning);
        CheckUpdatesCommand = new RelayCommand(CheckUpdatesAsync, () => !UpdateBusy);
        OpenGitHubProfileCommand = new RelayCommand(() => OpenUrl(ProductInfo.GitHubProfileUrl));
        OpenGitHubRepoCommand = new RelayCommand(() => OpenUrl(ProductInfo.GitHubRepoUrl));
        OpenPatreonCommand = new RelayCommand(() => OpenUrl(ProductInfo.PatreonUrl));
        OpenReleasesCommand = new RelayCommand(() => OpenUrl(_lastUpdate?.ReleaseUrl ?? ProductInfo.GitHubReleasesUrl));
        OpenLatestDownloadCommand = new RelayCommand(() => OpenUrl(ProductInfo.GitHubReleasesLatestUrl));

        ApplyLanguage();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public UiText Ui { get; } = new();
    public IReadOnlyList<LanguageOption> UiLanguages => UiCatalog.Languages;

    public ObservableCollection<string> RecentInputs { get; } = [];
    public ObservableCollection<string> RecentOutputs { get; } = [];
    public ObservableCollection<string> RecentDuzunInputs { get; } = [];
    public ObservableCollection<string> RecentDuzunOutputs { get; } = [];
    public ObservableCollection<string> RecentPackInputs { get; } = [];
    public ObservableCollection<string> RecentPackOutputs { get; } = [];
    public ObservableCollection<LogItem> Logs { get; } = [];
    public ObservableCollection<LogItem> DuzunLogs { get; } = [];
    public ObservableCollection<LogItem> PackLogs { get; } = [];
    public ObservableCollection<TypePreviewItem> PreviewTypes { get; } = [];
    public IReadOnlyList<string> LanguageChoices { get; } = LanguageVersionMapper.Choices;

    public string Title { get => _title; private set => SetField(ref _title, value); }
    public string ThemeButtonLabel { get => _themeButtonLabel; private set => SetField(ref _themeButtonLabel, value); }
    public string PackActionLabel { get => _packActionLabel; private set => SetField(ref _packActionLabel, value); }
    public string InstalledVersionText { get => _installedVersionText; private set => SetField(ref _installedVersionText, value); }
    public string CpuInfo => $"CPU: {Environment.ProcessorCount}  |  {_toolStatus}";
    public bool PackSigningEnabled => !StripSignature && !IsRunning;

    public string GitHubUser => ProductInfo.GitHubUser;
    public string GitHubProfileUrl => ProductInfo.GitHubProfileUrl;
    public string GitHubRepoUrl => ProductInfo.GitHubRepoUrl;
    public string GitHubAvatarUrl => ProductInfo.GitHubAvatarUrl;
    public string PatreonUrl => ProductInfo.PatreonUrl;

    public string UiLanguage
    {
        get => _uiLanguage;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                return;
            string code = value is "en" or "ru" ? value : "tr";
            if (SetField(ref _uiLanguage, code))
            {
                ApplyLanguage();
                _settings.UiLanguage = code;
                SettingsStore.Save(_settings);
            }
        }
    }

    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set
        {
            if (!SetField(ref _selectedTabIndex, value))
                return;

            RefreshToolStatus();
            if (value == 3 && !_autoCheckedUpdates)
            {
                _autoCheckedUpdates = true;
                if (CheckUpdatesCommand.CanExecute(null))
                    CheckUpdatesCommand.Execute(null);
            }
        }
    }

    public string InputPath { get => _inputPath; set => SetField(ref _inputPath, value); }
    public string OutputPath { get => _outputPath; set => SetField(ref _outputPath, value); }
    public string DuzunInputPath { get => _duzunInputPath; set => SetField(ref _duzunInputPath, value); }
    public string DuzunOutputPath { get => _duzunOutputPath; set => SetField(ref _duzunOutputPath, value); }
    public string PackInputPath { get => _packInputPath; set => SetField(ref _packInputPath, value); }
    public string PackOutputPath { get => _packOutputPath; set => SetField(ref _packOutputPath, value); }
    public string SnkPath { get => _snkPath; set => SetField(ref _snkPath, value); }
    public string TokenMap { get => _tokenMap; set => SetField(ref _tokenMap, value); }
    public bool GenerateSnkIfMissing { get => _generateSnkIfMissing; set => SetField(ref _generateSnkIfMissing, value); }
    public bool ReplaceAllExternTokens { get => _replaceAllExternTokens; set => SetField(ref _replaceAllExternTokens, value); }
    public string LanguageVersion { get => _languageVersion; set => SetField(ref _languageVersion, value); }
    public bool Recursive { get => _recursive; set => SetField(ref _recursive, value); }
    public bool VerifyBuild { get => _verifyBuild; set => SetField(ref _verifyBuild, value); }
    public bool FormatOutput { get => _formatOutput; set => SetField(ref _formatOutput, value); }
    public bool IncludeGlobalNamespace { get => _includeGlobal; set => SetField(ref _includeGlobal, value); }
    public bool IncludeCompilerGenerated { get => _includeGenerated; set => SetField(ref _includeGenerated, value); }
    public bool GenerateSolution { get => _generateSolution; set => SetField(ref _generateSolution, value); }
    public bool DecompileBaml { get => _decompileBaml; set => SetField(ref _decompileBaml, value); }
    public bool UsePdb { get => _usePdb; set => SetField(ref _usePdb, value); }
    public int Parallelism { get => _parallelism; set => SetField(ref _parallelism, value); }
    public string StatusText { get => _statusText; set => SetField(ref _statusText, value); }
    public string PreviewCode { get => _previewCode; set => SetField(ref _previewCode, value); }
    public string PauseLabel { get => _pauseLabel; set => SetField(ref _pauseLabel, value); }
    public string UpdateStatus { get => _updateStatus; private set => SetField(ref _updateStatus, value); }

    public bool StripSignature
    {
        get => _stripSignature;
        set
        {
            if (SetField(ref _stripSignature, value))
            {
                OnPropertyChanged(nameof(PackSigningEnabled));
                RefreshPackActionLabel();
                BrowseSnkCommand.RaiseCanExecuteChanged();
                GenerateSnkCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsDarkTheme
    {
        get => _isDarkTheme;
        set
        {
            if (SetField(ref _isDarkTheme, value))
            {
                ThemeManager.Apply(value);
                _settings.DarkTheme = value;
                SettingsStore.Save(_settings);
                RefreshThemeButtonLabel();
            }
        }
    }

    public bool IsPaused
    {
        get => _isPaused;
        set
        {
            if (SetField(ref _isPaused, value))
                PauseLabel = value ? Ui["Resume"] : Ui["Pause"];
        }
    }

    public bool IsRunning
    {
        get => _isRunning;
        set
        {
            if (SetField(ref _isRunning, value))
            {
                OnPropertyChanged(nameof(PackSigningEnabled));
                StartCommand.RaiseCanExecuteChanged();
                StartDuzunCommand.RaiseCanExecuteChanged();
                StartPackCommand.RaiseCanExecuteChanged();
                CancelCommand.RaiseCanExecuteChanged();
                PauseCommand.RaiseCanExecuteChanged();
                BrowseInputCommand.RaiseCanExecuteChanged();
                BrowseInputFileCommand.RaiseCanExecuteChanged();
                BrowseOutputCommand.RaiseCanExecuteChanged();
                BrowseDuzunInputCommand.RaiseCanExecuteChanged();
                BrowseDuzunInputFileCommand.RaiseCanExecuteChanged();
                BrowseDuzunOutputCommand.RaiseCanExecuteChanged();
                BrowsePackInputCommand.RaiseCanExecuteChanged();
                BrowsePackInputFileCommand.RaiseCanExecuteChanged();
                BrowsePackOutputCommand.RaiseCanExecuteChanged();
                BrowseSnkCommand.RaiseCanExecuteChanged();
                GenerateSnkCommand.RaiseCanExecuteChanged();
                LoadPreviewCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool UpdateBusy
    {
        get => _updateBusy;
        private set
        {
            if (SetField(ref _updateBusy, value))
                CheckUpdatesCommand.RaiseCanExecuteChanged();
        }
    }

    public int ProgressValue { get => _progressValue; set => SetField(ref _progressValue, value); }
    public int ProgressMaximum { get => _progressMaximum; set => SetField(ref _progressMaximum, Math.Max(1, value)); }

    public TypePreviewItem? SelectedPreviewType
    {
        get => _selectedPreviewType;
        set
        {
            if (SetField(ref _selectedPreviewType, value) && value is not null)
                ShowPreview(value);
        }
    }

    public RelayCommand BrowseInputCommand { get; }
    public RelayCommand BrowseInputFileCommand { get; }
    public RelayCommand BrowseOutputCommand { get; }
    public RelayCommand BrowseDuzunInputCommand { get; }
    public RelayCommand BrowseDuzunInputFileCommand { get; }
    public RelayCommand BrowseDuzunOutputCommand { get; }
    public RelayCommand BrowsePackInputCommand { get; }
    public RelayCommand BrowsePackInputFileCommand { get; }
    public RelayCommand BrowsePackOutputCommand { get; }
    public RelayCommand BrowseSnkCommand { get; }
    public RelayCommand GenerateSnkCommand { get; }
    public RelayCommand StartCommand { get; }
    public RelayCommand StartDuzunCommand { get; }
    public RelayCommand StartPackCommand { get; }
    public RelayCommand CancelCommand { get; }
    public RelayCommand PauseCommand { get; }
    public RelayCommand OpenOutputCommand { get; }
    public RelayCommand ToggleThemeCommand { get; }
    public RelayCommand LoadPreviewCommand { get; }
    public RelayCommand CheckUpdatesCommand { get; }
    public RelayCommand OpenGitHubProfileCommand { get; }
    public RelayCommand OpenGitHubRepoCommand { get; }
    public RelayCommand OpenPatreonCommand { get; }
    public RelayCommand OpenReleasesCommand { get; }
    public RelayCommand OpenLatestDownloadCommand { get; }

    public void ApplyInitialTheme() => ThemeManager.Apply(IsDarkTheme);

    public void UseDroppedPath(string path, bool asOutput)
    {
        if (SelectedTabIndex == 3)
            return;

        if (SelectedTabIndex == 1)
        {
            if (asOutput)
                DuzunOutputPath = Directory.Exists(path) ? path : System.IO.Path.GetDirectoryName(path) ?? path;
            else
                DuzunInputPath = path;
            return;
        }

        if (SelectedTabIndex == 2)
        {
            if (asOutput)
                PackOutputPath = Directory.Exists(path) ? path : System.IO.Path.GetDirectoryName(path) ?? path;
            else
                PackInputPath = path;
            return;
        }

        if (asOutput)
            OutputPath = Directory.Exists(path) ? path : System.IO.Path.GetDirectoryName(path) ?? path;
        else
            InputPath = path;
    }

    private void ApplyLanguage()
    {
        UiCatalog.SetLanguage(_uiLanguage);
        Ui.Refresh();
        Title = $"{Ui["ProductName"]}  {ProductInfo.Version}";
        InstalledVersionText = UiCatalog.Format("CurrentVersion", ProductInfo.Version);
        RefreshThemeButtonLabel();
        RefreshPackActionLabel();
        StatusText = Ui["Ready"];
        PauseLabel = IsPaused ? Ui["Resume"] : Ui["Pause"];
        if (PreviewTypes.Count == 0)
            PreviewCode = Ui["PreviewPlaceholder"];
        RefreshUpdateStatusText();
        RefreshToolStatus();
    }

    private void RefreshThemeButtonLabel() =>
        ThemeButtonLabel = IsDarkTheme ? Ui["ThemeToLight"] : Ui["ThemeToDark"];

    private void RefreshPackActionLabel() =>
        PackActionLabel = StripSignature ? Ui["PackUnsigned"] : Ui["PackSign"];

    private void RefreshUpdateStatusText()
    {
        if (UpdateBusy)
        {
            UpdateStatus = Ui["UpdateChecking"];
            return;
        }

        if (_lastUpdate is null)
        {
            UpdateStatus = Ui["UpdateIdle"];
            return;
        }

        if (!_lastUpdate.Success)
            UpdateStatus = UiCatalog.Format("UpdateFailed", _lastUpdate.Error ?? "");
        else if (_lastUpdate.UpdateAvailable)
            UpdateStatus = UiCatalog.Format("UpdateAvailable", _lastUpdate.LatestVersion ?? "");
        else if (string.IsNullOrWhiteSpace(_lastUpdate.LatestVersion))
            UpdateStatus = Ui["UpdateNone"];
        else
            UpdateStatus = UiCatalog.Format("UpdateLatest", _lastUpdate.LatestVersion);
    }

    private void RefreshToolStatus()
    {
        if (SelectedTabIndex == 1)
        {
            string? ildasm = IldasmLocator.Find(_settings.IldasmPath);
            _toolStatus = ildasm is null ? Ui["IldasmMissing"] : $"ildasm: {ildasm}";
        }
        else if (SelectedTabIndex == 3)
        {
            _toolStatus = $"{GitHubUser}  |  {ProductInfo.Version}";
        }
        else
        {
            string? ilasm = IlasmLocator.Find(_settings.IlasmPath);
            _toolStatus = ilasm is null
                ? (SelectedTabIndex == 2 ? Ui["IlasmMissingPack"] : Ui["IlasmMissingConvert"])
                : $"ilasm: {ilasm}";
        }

        OnPropertyChanged(nameof(CpuInfo));
    }

    private void BrowseInput() => PickFolder(Ui["BrowseSourceFolder"], v => InputPath = v);
    private void BrowseOutput() => PickFolder(Ui["BrowseTargetFolder"], v => OutputPath = v);
    private void BrowseDuzunOutput() => PickFolder(Ui["BrowseIlOutput"], v => DuzunOutputPath = v);
    private void BrowseDuzunInput() => PickFolder(Ui["BrowseSourceFolder"], v => DuzunInputPath = v);
    private void BrowsePackInput() => PickFolder(Ui["BrowseSourceFolder"], v => PackInputPath = v);
    private void BrowsePackOutput() => PickFolder(Ui["BrowseDllOutput"], v => PackOutputPath = v);

    private void BrowseInputFile() => PickFile(Ui["FilterConvert"], v => InputPath = v);
    private void BrowseDuzunInputFile() => PickFile(Ui["FilterAssembly"], v => DuzunInputPath = v);
    private void BrowsePackInputFile() => PickFile(Ui["FilterIl"], v => PackInputPath = v);
    private void BrowseSnk() => PickFile(Ui["FilterSnk"], v => SnkPath = v);

    private void GenerateSnk()
    {
        var dialog = new SaveFileDialog
        {
            Title = Ui["SaveSnkTitle"],
            Filter = Ui["FilterSnk"],
            FileName = string.IsNullOrWhiteSpace(SnkPath) ? "signing.snk" : System.IO.Path.GetFileName(SnkPath),
            DefaultExt = ".snk",
            OverwritePrompt = true
        };
        if (dialog.ShowDialog() != true)
            return;

        try
        {
            var key = StrongNameKey.Create(dialog.FileName);
            SnkPath = key.SnkPath;
            AddLog(PackLogs, "success", UiCatalog.Format("KeyCreated", key.SnkPath, key.Token));
        }
        catch (Exception ex)
        {
            AddLog(PackLogs, "error", ex.Message);
            MessageBox.Show(ex.Message, Ui["ProductName"], MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static void PickFolder(string title, Action<string> apply)
    {
        var dialog = new OpenFolderDialog { Title = title };
        if (dialog.ShowDialog() == true)
            apply(dialog.FolderName);
    }

    private void PickFile(string filter, Action<string> apply)
    {
        var dialog = new OpenFileDialog { Title = Ui["PickFile"], Filter = filter };
        if (dialog.ShowDialog() == true)
            apply(dialog.FileName);
    }

    private void Cancel() => _cts?.Cancel();

    private void TogglePause()
    {
        if (_queue.IsPaused)
        {
            _queue.Resume();
            IsPaused = false;
            StatusText = Ui["Resuming"];
        }
        else
        {
            _queue.Pause();
            IsPaused = true;
            StatusText = Ui["Paused"];
        }
    }

    private void OpenOutput()
    {
        if (string.IsNullOrWhiteSpace(_lastOutputFolder) || !Directory.Exists(_lastOutputFolder))
            return;
        Process.Start(new ProcessStartInfo { FileName = "explorer.exe", Arguments = $"\"{_lastOutputFolder}\"", UseShellExecute = true });
    }

    private static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, UiCatalog.Get("ProductName"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task CheckUpdatesAsync()
    {
        UpdateBusy = true;
        UpdateStatus = Ui["UpdateChecking"];
        try
        {
            _lastUpdate = await UpdateChecker.CheckAsync();
        }
        catch (Exception ex)
        {
            _lastUpdate = new UpdateCheckResult(
                Success: false,
                UpdateAvailable: false,
                CurrentVersion: ProductInfo.Version,
                LatestVersion: null,
                ReleaseUrl: ProductInfo.GitHubReleasesUrl,
                ReleaseName: null,
                Error: ex.Message);
        }
        finally
        {
            UpdateBusy = false;
            RefreshUpdateStatusText();
        }
    }

    private async Task StartAsync()
    {
        if (string.IsNullOrWhiteSpace(InputPath) || string.IsNullOrWhiteSpace(OutputPath))
        {
            AddLog(Logs, "warn", Ui["NeedConvertPaths"]);
            return;
        }

        var options = new ConversionOptions
        {
            InputPath = InputPath.Trim(),
            OutputPath = OutputPath.Trim(),
            Recursive = Recursive,
            MaxParallelism = Parallelism,
            IlasmPath = _settings.IlasmPath,
            LanguageVersion = LanguageVersion,
            VerifyBuild = VerifyBuild,
            FormatOutput = FormatOutput,
            IncludeGlobalNamespace = IncludeGlobalNamespace,
            IncludeCompilerGenerated = IncludeCompilerGenerated,
            GenerateSolution = GenerateSolution,
            DecompileBaml = DecompileBaml,
            UsePdb = UsePdb
        };

        await RunJobAsync(Logs, token => new ConverterEngine().Convert(options, UiProgress(), UiLogger(Logs), token, _queue), PathMemory.CSharp);
    }

    private async Task StartDuzunAsync()
    {
        if (string.IsNullOrWhiteSpace(DuzunInputPath))
        {
            AddLog(DuzunLogs, "warn", Ui["NeedDuzunInput"]);
            return;
        }

        var options = new DisassembleOptions
        {
            InputPath = DuzunInputPath.Trim(),
            OutputPath = string.IsNullOrWhiteSpace(DuzunOutputPath) ? null : DuzunOutputPath.Trim(),
            Recursive = Recursive,
            MaxParallelism = Parallelism,
            IldasmPath = _settings.IldasmPath
        };

        await RunJobAsync(DuzunLogs, token => new DisassembleEngine().Disassemble(options, UiProgress(), UiLogger(DuzunLogs), token, _queue), PathMemory.Duzun);
    }

    private async Task StartPackAsync()
    {
        if (string.IsNullOrWhiteSpace(PackInputPath) || string.IsNullOrWhiteSpace(PackOutputPath))
        {
            AddLog(PackLogs, "warn", Ui["NeedPackPaths"]);
            return;
        }

        Dictionary<string, string>? tokenMap;
        try
        {
            tokenMap = TokenMapParser.Parse(TokenMap);
        }
        catch (FormatException ex)
        {
            AddLog(PackLogs, "error", ex.Message);
            MessageBox.Show(ex.Message, Ui["ProductName"], MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var options = new PackOptions
        {
            InputPath = PackInputPath.Trim(),
            OutputPath = PackOutputPath.Trim(),
            Recursive = Recursive,
            MaxParallelism = Parallelism,
            IlasmPath = _settings.IlasmPath,
            SnkPath = StripSignature || string.IsNullOrWhiteSpace(SnkPath) ? null : SnkPath.Trim(),
            GenerateSnkIfMissing = !StripSignature && GenerateSnkIfMissing,
            StripSignature = StripSignature,
            ReplaceAllExternTokens = !StripSignature && ReplaceAllExternTokens,
            TokenMap = StripSignature ? null : tokenMap
        };

        await RunJobAsync(PackLogs, token => new PackEngine().Pack(options, UiProgress(), UiLogger(PackLogs), token, _queue), PathMemory.Pack);
    }

    private async Task RunJobAsync(
        ObservableCollection<LogItem> log,
        Func<CancellationToken, ConversionBatchResult> work,
        PathMemory memory)
    {
        IsRunning = true;
        IsPaused = false;
        _queue.Resume();
        log.Clear();
        ProgressValue = 0;
        ProgressMaximum = 1;
        StatusText = Ui["Running"];
        _cts = new CancellationTokenSource();

        try
        {
            var result = await Task.Run(() => work(_cts.Token), _cts.Token);
            _lastOutputFolder = result.OutputPath;
            if (memory == PathMemory.Duzun && string.IsNullOrWhiteSpace(DuzunOutputPath))
                DuzunOutputPath = result.OutputPath;
            if (memory == PathMemory.Pack && !string.IsNullOrWhiteSpace(result.OutputPath))
            {
                string generated = System.IO.Path.Combine(result.OutputPath, "signing.snk");
                if (string.IsNullOrWhiteSpace(SnkPath) && File.Exists(generated))
                    SnkPath = generated;
            }
            OpenOutputCommand.RaiseCanExecuteChanged();
            StatusText = UiCatalog.Format("Done", result.Succeeded, result.Failed);
            RememberPaths(memory);
        }
        catch (OperationCanceledException)
        {
            StatusText = Ui["Cancelled"];
            AddLog(log, "warn", Ui["Stopped"]);
        }
        catch (Exception ex)
        {
            StatusText = Ui["Error"];
            AddLog(log, "error", ex.Message);
            MessageBox.Show(ex.Message, Ui["ProductName"], MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsRunning = false;
            IsPaused = false;
            _queue.Resume();
            _cts.Dispose();
            _cts = null;
        }
    }

    private IProgress<ConversionProgress> UiProgress() => new Progress<ConversionProgress>(p =>
    {
        ProgressMaximum = Math.Max(1, p.Total);
        ProgressValue = p.Completed;
        if (!IsPaused)
            StatusText = $"{p.Completed} / {p.Total}  —  {p.CurrentFile}";
    });

    private CallbackLogger UiLogger(ObservableCollection<LogItem> log) =>
        new((level, message) => Application.Current.Dispatcher.Invoke(() => AddLog(log, level, message)));

    private void LoadPreviewTypes()
    {
        string path = InputPath.Trim();
        if (!File.Exists(path) || !FileDiscovery.IsSupported(path, DisassembleEngine.AssemblyExtensions))
        {
            AddLog(Logs, "warn", Ui["PreviewNeedDll"]);
            return;
        }

        try
        {
            PreviewTypes.Clear();
            foreach (var type in TypePreviewService.ListTypes(path))
                PreviewTypes.Add(type);
            PreviewCode = PreviewTypes.Count == 0
                ? Ui["NoTypes"]
                : UiCatalog.Format("TypesLoaded", PreviewTypes.Count);
        }
        catch (Exception ex)
        {
            PreviewCode = ex.Message;
        }
    }

    private void ShowPreview(TypePreviewItem item)
    {
        string path = InputPath.Trim();
        if (!File.Exists(path))
            return;
        try
        {
            PreviewCode = TypePreviewService.DecompileType(path, item.FullName, LanguageVersion);
        }
        catch (Exception ex)
        {
            PreviewCode = ex.Message;
        }
    }

    private enum PathMemory
    {
        CSharp,
        Duzun,
        Pack
    }

    private void RememberPaths(PathMemory memory)
    {
        _settings.LanguageVersion = LanguageVersion;
        _settings.Recursive = Recursive;
        _settings.VerifyBuild = VerifyBuild;
        _settings.FormatOutput = FormatOutput;
        _settings.IncludeGlobalNamespace = IncludeGlobalNamespace;
        _settings.IncludeCompilerGenerated = IncludeCompilerGenerated;
        _settings.GenerateSolution = GenerateSolution;
        _settings.DecompileBaml = DecompileBaml;
        _settings.UsePdb = UsePdb;
        _settings.MaxParallelism = Parallelism;
        _settings.DarkTheme = IsDarkTheme;
        _settings.UiLanguage = UiLanguage;
        _settings.LastTokenMap = string.IsNullOrWhiteSpace(TokenMap) ? null : TokenMap;
        _settings.GenerateSnkIfMissing = GenerateSnkIfMissing;
        _settings.StripSignature = StripSignature;
        _settings.ReplaceAllExternTokens = ReplaceAllExternTokens;

        if (memory == PathMemory.CSharp)
        {
            _settings.LastInput = InputPath;
            _settings.LastOutput = OutputPath;
            SettingsStore.RememberPath(_settings.RecentInputs, InputPath);
            SettingsStore.RememberPath(_settings.RecentOutputs, OutputPath);
            Replace(RecentInputs, _settings.RecentInputs);
            Replace(RecentOutputs, _settings.RecentOutputs);
        }
        else if (memory == PathMemory.Duzun)
        {
            _settings.LastDuzunInput = DuzunInputPath;
            _settings.LastDuzunOutput = DuzunOutputPath;
            SettingsStore.RememberPath(_settings.RecentDuzunInputs, DuzunInputPath);
            if (!string.IsNullOrWhiteSpace(DuzunOutputPath))
                SettingsStore.RememberPath(_settings.RecentDuzunOutputs, DuzunOutputPath);
            Replace(RecentDuzunInputs, _settings.RecentDuzunInputs);
            Replace(RecentDuzunOutputs, _settings.RecentDuzunOutputs);
        }
        else
        {
            _settings.LastPackInput = PackInputPath;
            _settings.LastPackOutput = PackOutputPath;
            _settings.LastSnkPath = SnkPath;
            SettingsStore.RememberPath(_settings.RecentPackInputs, PackInputPath);
            SettingsStore.RememberPath(_settings.RecentPackOutputs, PackOutputPath);
            Replace(RecentPackInputs, _settings.RecentPackInputs);
            Replace(RecentPackOutputs, _settings.RecentPackOutputs);
        }

        SettingsStore.Save(_settings);
    }

    private static void Replace(ObservableCollection<string> target, IEnumerable<string> source)
    {
        target.Clear();
        foreach (string item in source)
            target.Add(item);
    }

    private static void AddLog(ObservableCollection<LogItem> target, string level, string message)
    {
        target.Add(new LogItem
        {
            Time = DateTime.Now.ToString("HH:mm:ss"),
            Level = level,
            Message = message
        });
    }

    private void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        OnPropertyChanged(name!);
        return true;
    }
}
