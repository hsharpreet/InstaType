namespace InstaType.Models;

/// <summary>Audio input device exposed by <c>IAudioCaptureService.GetAvailableMicrophones()</c>.</summary>
public sealed class MicrophoneDevice
{
    /// <summary>NAudio device index (0 = system default).</summary>
    public int Id { get; set; }

    /// <summary>Human-readable device name reported by Windows.</summary>
    public string Name { get; set; } = string.Empty;
}
