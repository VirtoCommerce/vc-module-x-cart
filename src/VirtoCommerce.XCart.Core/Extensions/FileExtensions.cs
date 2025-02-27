namespace VirtoCommerce.XCart.Core.Extensions;

public static class FileExtensions
{
    private const string _attachmentsUrlPrefix = "/api/files/";

    public static string GetFileUrl(string id)
    {
        return $"{_attachmentsUrlPrefix}{id}";
    }

    public static string GetFileId(string url)
    {
        return url != null && url.StartsWith(_attachmentsUrlPrefix)
            ? url[_attachmentsUrlPrefix.Length..]
            : null;
    }
}
