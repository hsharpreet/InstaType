using InstaType.Services;
using System.IO;
using System.Net.Http;
using System.Text;
using Whisper; // needed for createContext() extension method on iModel

namespace InstaType.Infrastructure.Whisper;

/// <summary>
/// Implements <see cref="ITranscriptionService"/> via WhisperNet (Const-me COM wrapper).
/// Loads a GGML model from %LOCALAPPDATA%\InstaType\Models\, downloading it if absent.
/// Audio is written to a temporary WAV file and loaded via iMediaFoundation.loadAudioFile,
/// which resamples to 16 kHz mono automatically.
/// </summary>
internal sealed class TranscriptionService : ITranscriptionService
{
    public bool IsModelLoaded { get; private set; }
    public string? LoadedModelName { get; private set; }

    private global::Whisper.iModel? _model;
    private global::Whisper.Context? _context;
    private global::Whisper.iMediaFoundation? _mf;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private const string DownloadBaseUrl =
        "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/";

    private static string ModelsDir => System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "InstaType", "Models");

    // ── Public API ───────────────────────────────────────────────────────────

    public async Task LoadModelAsync(string modelFileName, CancellationToken ct = default)
    {
        System.IO.Directory.CreateDirectory(ModelsDir);
        string modelPath = System.IO.Path.Combine(ModelsDir, modelFileName);

        if (!System.IO.File.Exists(modelPath))
            await DownloadModelAsync(modelFileName, modelPath, ct);

        // Try DirectML GPU first; fall back to CPU reference on any failure.
        try
        {
            _model = await global::Whisper.Library.loadModelAsync(modelPath, ct,
                         global::Whisper.eGpuModelFlags.None, null, null,
                         global::Whisper.eModelImplementation.GPU);
        }
        catch
        {
            System.Diagnostics.Debug.WriteLine("[Transcription] GPU load failed — falling back to CPU");
            _model = await global::Whisper.Library.loadModelAsync(modelPath, ct,
                         global::Whisper.eGpuModelFlags.None, null, null,
                         global::Whisper.eModelImplementation.Reference);
        }
        _context = _model.createContext();
        _mf      = global::Whisper.Library.initMediaFoundation();

        IsModelLoaded   = true;
        LoadedModelName = modelFileName;
    }

    public async Task<string> TranscribeAsync(float[] audio, string language = "en",
        CancellationToken ct = default)
    {
        if (_context is null || _mf is null || audio.Length == 0)
            return string.Empty;

        await _lock.WaitAsync(ct);
        try
        {
            // Set language; languageFromCode returns nullable — fall back to English
            var lang = global::Whisper.Library.languageFromCode(language)
                       ?? global::Whisper.eLanguage.English;
            _context.parameters.language = lang;

            // Write PCM float[] → temporary WAV, then transcribe via Media Foundation
            string tmp = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(), $"instatype_{Guid.NewGuid():N}.wav");
            try
            {
                WritePcmWav(tmp, audio);
                using var audioBuf = _mf.loadAudioFile(tmp, stereo: false);
                _context.runFull(audioBuf, new NoopCallbacks());

                var result = _context.results(global::Whisper.eResultFlags.None);
                var sb     = new StringBuilder();
                foreach (ref readonly var seg in result.segments)
                    sb.Append(seg.text);
                return sb.ToString().Trim();
            }
            finally
            {
                try { System.IO.File.Delete(tmp); } catch { }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[Transcription] FAILED {ex.GetType().Name}: {ex.Message}");
            if (ex.InnerException is not null)
                System.Diagnostics.Debug.WriteLine($"[Transcription] Inner: {ex.InnerException.Message}");
            return string.Empty;
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Dispose()
    {
        // COM objects freed by GC via ComLight; dispose semaphore only
        _lock.Dispose();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static void WritePcmWav(string path, float[] samples)
    {
        const int sampleRate    = 16000;
        const int channels      = 1;
        const int bitsPerSample = 16;
        int       dataBytes     = samples.Length * 2;

        using var fs = System.IO.File.Create(path);
        using var bw = new System.IO.BinaryWriter(fs);

        bw.Write(0x46464952); // "RIFF"
        bw.Write(36 + dataBytes);
        bw.Write(0x45564157); // "WAVE"
        bw.Write(0x20746D66); // "fmt "
        bw.Write(16);
        bw.Write((short)1);                                  // PCM
        bw.Write((short)channels);
        bw.Write(sampleRate);
        bw.Write(sampleRate * channels * bitsPerSample / 8); // byte rate
        bw.Write((short)(channels * bitsPerSample / 8));     // block align
        bw.Write((short)bitsPerSample);
        bw.Write(0x61746164); // "data"
        bw.Write(dataBytes);

        foreach (float s in samples)
        {
            short i16 = (short)Math.Clamp(s * 32767f, short.MinValue, short.MaxValue);
            bw.Write(i16);
        }
    }

    private static async Task DownloadModelAsync(string fileName, string destPath,
        CancellationToken ct)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromHours(1) };
        http.DefaultRequestHeaders.Add("User-Agent", "InstaType/1.0");

        using var response = await http.GetAsync(
            DownloadBaseUrl + fileName, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        using var src  = await response.Content.ReadAsStreamAsync(ct);
        using var dest = System.IO.File.Create(destPath);
        await src.CopyToAsync(dest, ct);
    }

    // Noop callbacks — required by runFull; return true to continue processing
    private sealed class NoopCallbacks : global::Whisper.Callbacks
    {
        protected override bool onEncoderBegin(global::Whisper.Context ctx) => true;
        protected override void onNewSegment(global::Whisper.Context ctx, int n) { }
    }
}
