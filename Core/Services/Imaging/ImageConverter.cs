using ImageMagick;
using SkiaSharp;

namespace datopus.Core.Services.Imaging
{
    public class ImageConvertor : IImageConverter
    {
        public SKBitmap QuantizeImage(SKBitmap bitmap)
        {
            using (var memoryStream = new MemoryStream())
            {
                bitmap.Encode(memoryStream, SKEncodedImageFormat.Png, 100);
                memoryStream.Position = 0;

                using (var magickImage = new MagickImage(memoryStream))
                {
                    var quantizeSettings = new QuantizeSettings
                    {
                        Colors = 256,
                        DitherMethod = DitherMethod.No,
                    };

                    magickImage.Quantize(quantizeSettings);

                    using (var resultStream = new MemoryStream())
                    {
                        magickImage.Write(resultStream);
                        resultStream.Position = 0;

                        return SKBitmap.Decode(resultStream);
                    }
                }
            }
        }

        public SKBitmap ResizeImage(SKBitmap bitmap, SKSizeI thumbSize, bool preserveAspectRatio)
        {
            var aspectedSize = thumbSize.Aspectize(
                new SKSizeI(bitmap.Width, bitmap.Height),
                preserveAspectRatio
            );

            var result = new SKBitmap(aspectedSize.Width, aspectedSize.Height);

            using (var canvas = new SKCanvas(result))
            {
                var samplingOptions = new SKSamplingOptions(
                    SKFilterMode.Linear,
                    SKMipmapMode.Nearest
                );
                canvas.DrawImage(
                    SKImage.FromBitmap(bitmap),
                    new SKRect(0, 0, aspectedSize.Width, aspectedSize.Height),
                    samplingOptions
                );
            }

            return result;
        }

        public byte[] ConvertToBytes(SKBitmap bitmap)
        {
            using var memoryStream = new MemoryStream();
            bitmap.Encode(memoryStream, SKEncodedImageFormat.Png, 100);
            return memoryStream.ToArray();
        }

        public SKBitmap ConvertFormFileToBitMap(IFormFile file)
        {
            using (var stream = file.OpenReadStream())
            {
                var skBitmap = SKBitmap.Decode(stream);
                return skBitmap;
            }
        }
    }
}
