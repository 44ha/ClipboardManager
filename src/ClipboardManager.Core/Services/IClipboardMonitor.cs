using ClipboardManager.Core.Models;

namespace ClipboardManager.Core.Services;

/// <summary>
/// Service that listens to system clipboard updates and notifies subscribers of newly copied content.
/// </summary>
public interface IClipboardMonitor : IDisposable
{
    /// <summary>
    /// Raised whenever valid non-excluded clipboard content is copied.
    /// </summary>
    event EventHandler<ClipboardItem>? ClipboardContentCaptured;

    /// <summary>
    /// Indicates whether the monitor is actively listening to clipboard update messages.
    /// </summary>
    bool IsMonitoring { get; }

    /// <summary>
    /// Starts the clipboard listener.
    /// </summary>
    void Start();

    /// <summary>
    /// Stops the clipboard listener.
    /// </summary>
    void Stop();

    /// <summary>
    /// Pauses capturing without tearing down the underlying listener handle.
    /// </summary>
    void Pause();

    /// <summary>
    /// Resumes capturing content.
    /// </summary>
    void Resume();
}
