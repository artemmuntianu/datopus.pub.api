using SkiaSharp;

public static class SizeExtensions
{
    public static SKSize Aspectize(
        this SKSize targetSize,
        SKSize originalSize,
        bool preserveAspectRatio
    )
    {
        if (!preserveAspectRatio)
        {
            return targetSize;
        }

        float ratioWidth = (float)targetSize.Width / originalSize.Width;
        float ratioHeight = (float)targetSize.Height / originalSize.Height;
        float scale = Math.Min(ratioWidth, ratioHeight);

        float adjustedWidth = originalSize.Width * scale;
        float adjustedHeight = originalSize.Height * scale;

        return new SKSize(adjustedWidth, adjustedHeight);
    }

    public static SKSizeI Aspectize(
        this SKSizeI targetSize,
        SKSizeI originalSize,
        bool preserveAspectRatio
    )
    {
        if (!preserveAspectRatio)
        {
            return targetSize;
        }

        float ratioWidth = (float)targetSize.Width / originalSize.Width;
        float ratioHeight = (float)targetSize.Height / originalSize.Height;
        float scale = Math.Min(ratioWidth, ratioHeight);

        int adjustedWidth = (int)(originalSize.Width * scale);
        int adjustedHeight = (int)(originalSize.Height * scale);

        return new SKSizeI(adjustedWidth, adjustedHeight);
    }
}
