using SkiaSharp;

namespace datopus.Core.Services.Imaging;

public interface IImageConverter
{
    SKBitmap ResizeImage(SKBitmap image, SKSizeI thumbSize, bool preserveAspectRatio);
    SKBitmap QuantizeImage(SKBitmap image);

    byte[] ConvertToBytes(SKBitmap bitmap);

    SKBitmap ConvertFormFileToBitMap(IFormFile file);
}
