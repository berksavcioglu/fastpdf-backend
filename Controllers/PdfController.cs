using Microsoft.AspNetCore.Mvc;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PdfSharp.Drawing;

namespace FastPdf.Api.Controllers;

[ApiController]
[Route("api/pdf")]
public class PdfController : ControllerBase
{
    [HttpPost("merge")]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<IActionResult> Merge([FromForm] List<IFormFile> files)
    {
        if (files == null || files.Count < 2)
            return BadRequest("En az 2 PDF dosyası yüklemelisin.");

        var outputDocument = new PdfDocument();

        foreach (var file in files)
        {
            if (file.Length == 0)
                continue;

            if (!file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Sadece PDF dosyaları kabul edilir.");

            using var inputStream = file.OpenReadStream();
            using var tempStream = new MemoryStream();
            await inputStream.CopyToAsync(tempStream);
            tempStream.Position = 0;

            using var inputDocument = PdfReader.Open(tempStream, PdfDocumentOpenMode.Import);

            for (int i = 0; i < inputDocument.PageCount; i++)
            {
                outputDocument.AddPage(inputDocument.Pages[i]);
            }
        }

        using var outputStream = new MemoryStream();
        outputDocument.Save(outputStream, false);

        return File(outputStream.ToArray(), "application/pdf", "merged.pdf");
    
    }
    [HttpPost("images-to-pdf")]
[RequestSizeLimit(50 * 1024 * 1024)]
public async Task<IActionResult> ImagesToPdf([FromForm] List<IFormFile> files)
{
    if (files == null || files.Count == 0)
        return BadRequest("En az 1 görsel yüklemelisin.");

    var document = new PdfDocument();

    foreach (var file in files)
    {
        if (file.Length == 0)
            continue;

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (ext != ".jpg" && ext != ".jpeg" && ext != ".png" && ext != ".bmp")
            return BadRequest("Sadece JPG, JPEG, PNG veya BMP kabul edilir.");

        var tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}{ext}");

        await using (var fs = new FileStream(tempFilePath, FileMode.Create))
        {
            await file.CopyToAsync(fs);
        }

        try
        {
            var page = document.AddPage();

            using var image = XImage.FromFile(tempFilePath);

            page.Width = image.PointWidth;
            page.Height = image.PointHeight;

            using var gfx = XGraphics.FromPdfPage(page);
            gfx.DrawImage(image, 0, 0, image.PointWidth, image.PointHeight);
        }
        finally
        {
            if (System.IO.File.Exists(tempFilePath))
                System.IO.File.Delete(tempFilePath);
        }
    }

    using var outputStream = new MemoryStream();
    document.Save(outputStream, false);

    return File(outputStream.ToArray(), "application/pdf", "images.pdf");
}
}