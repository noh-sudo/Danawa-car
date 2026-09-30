using System.IO;
using System.Windows.Media.Imaging;
using Danawa.Shared.Contracts;

namespace Danawa.Client.Services;

/// <summary>image_get으로 이미지를 받아 BitmapImage로 변환, 카드 뷰모델들이 공유</summary>
public static class ImageLoader
{
    public static async Task<BitmapImage?> LoadAsync(ServerConnection connection, string? imageUrl)
    {
        if (string.IsNullOrEmpty(imageUrl))
        {
            return null;
        }

        try
        {
            var (resp, blob) = await connection.SendForBlobAsync<ImageGetResponse>(
                new ImageGetRequest { Type = RequestType.ImageGet, ImageUrl = imageUrl });

            if (resp.Result != "OK" || blob is not { Length: > 0 })
            {
                return null;
            }

            var bitmap = new BitmapImage();
            using var stream = new MemoryStream(blob);
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = stream;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            // 이미지 로딩 실패해도 카드 표시는 그대로, 자리표시(빈 이미지)만
            return null;
        }
    }
}
