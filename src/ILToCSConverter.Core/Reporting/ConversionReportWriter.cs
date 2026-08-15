using System.Text;

namespace ILToCSConverter.Core.Reporting;

public static class ConversionReportWriter
{
    public static string Write(string outputFolder, Conversion.ConversionBatchResult result)
    {
        Directory.CreateDirectory(outputFolder);
        string path = Path.Combine(outputFolder, "conversion-report.log");
        var builder = new StringBuilder();

        builder.AppendLine($"{ProductInfo.Name} {ProductInfo.Version}");
        builder.AppendLine($"Tarih: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        builder.AppendLine($"Süre: {result.Duration:hh\\:mm\\:ss}");
        builder.AppendLine($"Toplam: {result.Total}  Başarılı: {result.Succeeded}  Hatalı: {result.Failed}  Atlanan: {result.Skipped}");
        builder.AppendLine($"Çıktı: {result.OutputPath}");
        builder.AppendLine(new string('-', 72));

        foreach (var file in result.Files)
        {
            builder.AppendLine($"{file.Status,-10} {file.SourcePath}");
            if (!string.IsNullOrWhiteSpace(file.OutputFolder))
                builder.AppendLine($"           → {file.OutputFolder}");
            if (!string.IsNullOrWhiteSpace(file.Message))
                builder.AppendLine($"           {file.Message}");
            if (!string.IsNullOrWhiteSpace(file.Sha256))
                builder.AppendLine($"           SHA256: {file.Sha256}  ({file.SourceLength} bayt)");
            if (!string.IsNullOrWhiteSpace(file.Detail))
            {
                foreach (string line in file.Detail.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                    builder.AppendLine($"           {line}");
            }

            builder.AppendLine();
        }

        File.WriteAllText(path, builder.ToString(), Encoding.UTF8);
        return path;
    }
}
