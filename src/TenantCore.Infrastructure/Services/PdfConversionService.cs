using TenantCore.Application.Services;

namespace TenantCore.Infrastructure.Services;

public class PdfConversionService : IPdfConversionService
{
    private static readonly HashSet<string> ImageExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tiff" };

    public Task<byte[]> ConvertToPdfAsync(byte[] fileBytes, string originalExtension, CancellationToken ct = default)
    {
        var ext = originalExtension.StartsWith('.') ? originalExtension : $".{originalExtension}";

        if (ext.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(fileBytes);

        if (ImageExtensions.Contains(ext))
            return Task.FromResult(WrapImageInPdf(fileBytes));

        return Task.FromResult(fileBytes);
    }

    private static byte[] WrapImageInPdf(byte[] imageBytes)
    {
        // Minimal PDF wrapping an embedded image using raw PDF syntax.
        // For production use, replace with a proper PDF library (e.g., iText, PdfPig).
        var imageBase64 = Convert.ToBase64String(imageBytes);
        var pdfContent = $@"%PDF-1.4
1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj
2 0 obj<</Type/Pages/Kids[3 0 R]/Count 1>>endobj
3 0 obj<</Type/Page/Parent 2 0 R/MediaBox[0 0 612 792]/Contents 4 0 R/Resources<</XObject<</Im0 5 0 R>>>>>>endobj
4 0 obj<</Length 32>>stream
q 612 0 0 792 0 0 cm /Im0 Do Q
endstream endobj
5 0 obj<</Type/XObject/Subtype/Image/Width 612/Height 792/ColorSpace/DeviceRGB/BitsPerComponent 8/Filter/DCTDecode/Length {imageBytes.Length}>>stream
";
        var header = System.Text.Encoding.ASCII.GetBytes(pdfContent);
        var footer = System.Text.Encoding.ASCII.GetBytes("\nendstream endobj\nxref\ntrailer<</Root 1 0 R/Size 6>>\n%%EOF");
        var result = new byte[header.Length + imageBytes.Length + footer.Length];
        header.CopyTo(result, 0);
        imageBytes.CopyTo(result, header.Length);
        footer.CopyTo(result, header.Length + imageBytes.Length);
        return result;
    }
}
