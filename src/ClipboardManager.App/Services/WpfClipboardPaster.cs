using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using ClipboardManager.Core.Models;
using ClipboardManager.Core.Services;

namespace ClipboardManager.App.Services;

/// <summary>
/// Places a previously saved clipboard item back onto the active Windows clipboard.
/// </summary>
public class WpfClipboardPaster : IClipboardPaster
{
    public Task SetClipboardAsync(ClipboardItem item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        Application.Current.Dispatcher.Invoke(() =>
        {
            var dataObject = new DataObject();

            if (item.ContentType == ClipboardContentType.Text && item.TextContent != null)
            {
                dataObject.SetData(DataFormats.UnicodeText, item.TextContent);
                Clipboard.SetDataObject(dataObject, copy: true);
            }
            else if (item.ContentType == ClipboardContentType.Image && item.ImageData != null)
            {
                using var stream = new MemoryStream(item.ImageData);
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = stream;
                bitmap.EndInit();
                bitmap.Freeze();

                dataObject.SetData(DataFormats.Bitmap, bitmap);
                Clipboard.SetDataObject(dataObject, copy: true);
            }
        });

        return Task.CompletedTask;
    }
}
