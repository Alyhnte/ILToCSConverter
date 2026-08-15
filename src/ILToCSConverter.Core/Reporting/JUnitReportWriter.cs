using System.Globalization;
using System.Text;
using System.Xml.Linq;
using ILToCSConverter.Core.Conversion;

namespace ILToCSConverter.Core.Reporting;

public static class JUnitReportWriter
{
    public static string Write(string outputFolder, ConversionBatchResult result)
    {
        Directory.CreateDirectory(outputFolder);
        string path = Path.Combine(outputFolder, "conversion-junit.xml");
        double seconds = result.Duration.TotalSeconds;

        var suite = new XElement("testsuite",
            new XAttribute("name", ProductInfo.Name),
            new XAttribute("tests", result.Total),
            new XAttribute("failures", result.Failed),
            new XAttribute("skipped", result.Skipped),
            new XAttribute("time", seconds.ToString("0.###", CultureInfo.InvariantCulture)));

        foreach (var file in result.Files)
        {
            var test = new XElement("testcase",
                new XAttribute("classname", "ILToCSConverter"),
                new XAttribute("name", Path.GetFileName(file.SourcePath)));

            if (file.Status == ConversionStatus.Failed)
            {
                test.Add(new XElement("failure",
                    new XAttribute("message", file.Message ?? "Hata"),
                    new XCData(file.Detail ?? file.Message ?? "")));
            }
            else if (file.Status is ConversionStatus.Skipped or ConversionStatus.Cancelled)
            {
                test.Add(new XElement("skipped", new XAttribute("message", file.Message ?? file.Status.ToString())));
            }

            suite.Add(test);
        }

        var document = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), suite);
        using var writer = new StreamWriter(path, false, new UTF8Encoding(false));
        document.Save(writer);
        return path;
    }
}
