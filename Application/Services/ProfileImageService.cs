using datopus.Core.Services.Imaging;
using SkiaSharp;

namespace datopus.Application.Services;

public class ProfileImageService
{
    private readonly IImageConverter _imageConverter;
    private readonly AzureBlobService _azureBlobService;

    public ProfileImageService(IImageConverter imagingConverter, AzureBlobService azureBlobService)
    {
        _imageConverter = imagingConverter;
        _azureBlobService = azureBlobService;
    }

    public async Task<string> ProcessAndUploadProfileImage(
        IFormFile file,
        string userId,
        int thumbnailSize
    )
    {
        var imageBytesTask = Task.Run(() =>
        {
            using var convertedImage = _imageConverter.ConvertFormFileToBitMap(file);
            using var resizedImage = MakeThumb(
                convertedImage,
                new SKSizeI(thumbnailSize, thumbnailSize)
            );
            using var quantizedImage = _imageConverter.QuantizeImage(resizedImage);
            return _imageConverter.ConvertToBytes(quantizedImage);
        });

        var imageBytes = await imageBytesTask;
        var blobUrl = await _azureBlobService.UploadImageAsync(imageBytes, userId, "profile");

        return blobUrl;
    }

    private SKBitmap MakeThumb(SKBitmap bitmap, SKSizeI thumbSize, bool preserveAspectRatio = true)
    {
        if (bitmap.Width <= thumbSize.Width && bitmap.Height <= thumbSize.Height)
            return bitmap;

        return _imageConverter.ResizeImage(bitmap, thumbSize, preserveAspectRatio);
    }
}
