using InstaType.Services;

namespace InstaType.Infrastructure.Win32;

/// <summary>
/// Implements <see cref="ITextInjectionService"/> using SendInput with KEYEVENTF_UNICODE.
/// Handles BMP characters (2 events each) and surrogate pairs (4 events each).
/// Verifies modifier key state via GetAsyncKeyState before injecting.
/// Returns false instead of throwing when UIPI blocks injection.
/// </summary>
internal sealed class TextInjectionService : ITextInjectionService
{
    // TODO (F-04): Implement CaptureTargetWindow (GetForegroundWindow) and
    // Inject (SendInput loop over Unicode code points including surrogate pairs).

    public nint CaptureTargetWindow() => throw new NotImplementedException();
    public bool Inject(nint targetWindowHandle, string text) => throw new NotImplementedException();
}
