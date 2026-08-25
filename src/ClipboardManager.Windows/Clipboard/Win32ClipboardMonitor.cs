using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;
using ClipboardManager.Core.Models;
using ClipboardManager.Core.Services;
using ClipboardManager.Windows.Native;

namespace ClipboardManager.Windows.Clipboard;

/// <summary>
/// Listens to Windows clipboard changes via AddClipboardFormatListener / WM_CLIPBOARDUPDATE.
/// Automatically filters out password manager entries (ExcludeClipboardContentFromMonitorProcessing).
/// </summary>
public sealed class Win32ClipboardMonitor : IClipboardMonitor
{
    private readonly uint _excludeFromMonitorFormat;
    private readonly uint _canIncludeInHistoryFormat;
    private readonly uint _clipboardViewerIgnoreFormat;

    private MessageWindow? _messageWindow;
    private bool _isMonitoring;
    private bool _isPaused;
    private bool _disposed;
    private string? _lastCapturedHash;

    public event EventHandler<ClipboardItem>? ClipboardContentCaptured;

    public bool IsMonitoring => _isMonitoring && !_isPaused;

    public Win32ClipboardMonitor()
    {
        // Register standard Windows format names used by password managers to opt out of clipboard history
        _excludeFromMonitorFormat = NativeMethods.RegisterClipboardFormat("ExcludeClipboardContentFromMonitorProcessing");
        _canIncludeInHistoryFormat = NativeMethods.RegisterClipboardFormat("CanIncludeInClipboardHistory");
        _clipboardViewerIgnoreFormat = NativeMethods.RegisterClipboardFormat("Clipboard Viewer Ignore");
    }

    public void Start()
    {
        if (_isMonitoring || _disposed) return;

        _messageWindow = new MessageWindow(WndProc);
        if (_messageWindow.Handle != IntPtr.Zero)
        {
            NativeMethods.AddClipboardFormatListener(_messageWindow.Handle);
            _isMonitoring = true;
        }
    }

    public void Stop()
    {
        if (!_isMonitoring) return;

        if (_messageWindow != null && _messageWindow.Handle != IntPtr.Zero)
        {
            NativeMethods.RemoveClipboardFormatListener(_messageWindow.Handle);
            _messageWindow.Dispose();
            _messageWindow = null;
        }

        _isMonitoring = false;
    }

    public void Pause()
    {
        _isPaused = true;
    }

    public void Resume()
    {
        _isPaused = false;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_CLIPBOARDUPDATE)
        {
            if (!_isPaused && _isMonitoring)
            {
                ProcessClipboardChange();
            }
            handled = true;
        }

        return IntPtr.Zero;
    }

    private void ProcessClipboardChange()
    {
        // 1. Respect privacy exclusion headers from password managers (Bitwarden, 1Password, KeePass)
        if (IsExcludedContent())
        {
            return;
        }

        // 2. Read clipboard with retry (clipboard can be momentarily locked by copying app)
        ClipboardItem? capturedItem = ReadCurrentClipboardWithRetry();
        if (capturedItem == null)
        {
            return;
        }

        // Avoid consecutive duplicate triggers for the exact same hash
        if (capturedItem.ContentHash == _lastCapturedHash)
        {
            return;
        }

        _lastCapturedHash = capturedItem.ContentHash;
        ClipboardContentCaptured?.Invoke(this, capturedItem);
    }

    private bool IsExcludedContent()
    {
        if (_excludeFromMonitorFormat != 0 && NativeMethods.IsClipboardFormatAvailable(_excludeFromMonitorFormat))
        {
            return true;
        }

        if (_canIncludeInHistoryFormat != 0 && NativeMethods.IsClipboardFormatAvailable(_canIncludeInHistoryFormat))
        {
            return true;
        }

        if (_clipboardViewerIgnoreFormat != 0 && NativeMethods.IsClipboardFormatAvailable(_clipboardViewerIgnoreFormat))
        {
            return true;
        }

        return false;
    }

    private static ClipboardItem? ReadCurrentClipboardWithRetry(int maxAttempts = 5, int delayMs = 30)
    {
        for (int i = 0; i < maxAttempts; i++)
        {
            try
            {
                // Check for text first
                if (System.Windows.Clipboard.ContainsText())
                {
                    string text = System.Windows.Clipboard.GetText();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        return ClipboardItem.CreateText(text);
                    }
                }

                // Check for image
                if (System.Windows.Clipboard.ContainsImage())
                {
                    var imageSource = System.Windows.Clipboard.GetImage();
                    if (imageSource != null)
                    {
                        byte[] pngBytes = EncodeBitmapToPng(imageSource);
                        if (pngBytes.Length > 0)
                        {
                            return ClipboardItem.CreateImage(pngBytes);
                        }
                    }
                }

                return null;
            }
            catch (COMException)
            {
                // CLIPBRD_E_CANT_OPEN: Another process is holding the clipboard open, retry after delay
                Thread.Sleep(delayMs);
            }
            catch (Exception)
            {
                return null;
            }
        }

        return null;
    }

    private static byte[] EncodeBitmapToPng(BitmapSource bitmapSource)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmapSource));

        using var memoryStream = new MemoryStream();
        encoder.Save(memoryStream);
        return memoryStream.ToArray();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
    }
}
