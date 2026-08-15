namespace ILToCSConverter.Core.Conversion;

public sealed class ConversionQueue
{
    private readonly ManualResetEventSlim _gate = new(initialState: true);

    public bool IsPaused => !_gate.IsSet;

    public void Pause() => _gate.Reset();

    public void Resume() => _gate.Set();

    public void WaitIfPaused(CancellationToken cancellationToken) => _gate.Wait(cancellationToken);
}
