using System.Collections.Specialized;
using System.IO;
using System.Windows;
using ILToCSConverter.Core.Conversion;

namespace ILToCSConverter.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;
        _viewModel.ApplyInitialTheme();
        _viewModel.Logs.CollectionChanged += (_, _) => Scroll(LogList);
        _viewModel.DuzunLogs.CollectionChanged += (_, _) => Scroll(DuzunLogList);
        _viewModel.PackLogs.CollectionChanged += (_, _) => Scroll(PackLogList);
    }

    private static void Scroll(System.Windows.Controls.ListBox list)
    {
        if (list.Items.Count == 0)
            return;
        list.ScrollIntoView(list.Items[^1]);
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            return;
        if (e.Data.GetData(DataFormats.FileDrop) is not string[] paths || paths.Length == 0)
            return;

        string path = paths[0];
        bool asOutput = e.GetPosition(this).Y > 280 && e.GetPosition(this).Y < 380;
        if (asOutput || FileDiscovery.IsSupported(path) || Directory.Exists(path))
            _viewModel.UseDroppedPath(path, asOutput);
    }
}
