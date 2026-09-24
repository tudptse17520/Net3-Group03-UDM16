using System.Media;

namespace CaroClient;

public sealed class MoveSound : IDisposable
{
    private readonly Stream? _resource = typeof(MoveSound).Assembly.GetManifestResourceStream("Caro.MoveSound");
    private SoundPlayer? _player;
    public int TriggerCount { get; private set; }
    public MoveSound()
    {
        try { if (_resource != null) { _player = new SoundPlayer(_resource); _player.Load(); } }
        catch (Exception) { _player?.Dispose(); _player = null; }
    }
    public void PlayAcceptedMove()
    {
        TriggerCount++;
        try { _player?.Play(); } catch (Exception) { /* Audio is optional; never fail gameplay. */ }
    }
    public void Dispose() { _player?.Dispose(); _resource?.Dispose(); }
}
