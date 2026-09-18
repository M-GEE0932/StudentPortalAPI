namespace StudentPortalAPI.Helpers;

public static class FileHelper
{
    private static readonly string[] AllowedImageTypes = { "image/jpeg", "image/png", "image/gif", "image/webp" };
    private static readonly string[] AllowedDocumentTypes = { "application/pdf", "application/msword", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" };

    public static bool IsValidImageType(string contentType) => AllowedImageTypes.Contains(contentType);
    public static bool IsValidDocumentType(string contentType) => AllowedDocumentTypes.Contains(contentType);

    public static async Task<string> SaveFileAsync(IFormFile file, string subFolder, string wwwRootPath)
    {
        var uploadsFolder = Path.Combine(wwwRootPath, "uploads", subFolder);
        Directory.CreateDirectory(uploadsFolder);

        var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        return $"/uploads/{subFolder}/{uniqueFileName}";
    }

    public static void DeleteFile(string filePath, string wwwRootPath)
    {
        var fullPath = Path.Combine(wwwRootPath, filePath.TrimStart('/'));
        if (File.Exists(fullPath))
            File.Delete(fullPath);
    }
}
