namespace ILToCSConverter.Core.Plugins;

public interface IConversionPlugin
{
    string Name { get; }
    string Description { get; }
}

public interface IInputPreprocessor : IConversionPlugin
{
    bool CanProcess(string filePath);
    string Process(string filePath, string workDirectory, CancellationToken cancellationToken);
}

public interface IOutputPostprocessor : IConversionPlugin
{
    void Process(Conversion.FileConversionResult result, Conversion.ConversionOptions options, CancellationToken cancellationToken);
}
