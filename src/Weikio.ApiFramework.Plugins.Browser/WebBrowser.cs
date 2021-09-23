using System.IO;
using System.Threading.Tasks;
using PuppeteerSharp;

namespace Weikio.ApiFramework.Plugins.Browser
{
    public abstract class WebBrowser
    {
        protected abstract Task<PuppeteerSharp.Browser> GetBrowser();

        public async Task<FileInfo> Pdf(string url)
        {
            using (var browser = await GetBrowser())
            {
                using (var page = await browser.NewPageAsync())
                {
                    await page.GoToAsync(url);
                    var bytes = await page.PdfDataAsync(new PdfOptions { MarginOptions = new PuppeteerSharp.Media.MarginOptions { Top = "1cm", Bottom = "1cm" } });

                    var browserPath = ApiFactory.GetBrowserPath();
                    var pdfPath = Path.Combine(browserPath, "pdf");

                    if (!Directory.Exists(pdfPath))
                    {
                        Directory.CreateDirectory(pdfPath);
                    }

                    var pdfFilePath = Path.Combine(pdfPath, Path.GetFileNameWithoutExtension(Path.GetTempFileName()) + ".pdf");

                    await File.WriteAllBytesAsync(pdfFilePath, bytes);
                    var fileInfo = new FileInfo(pdfFilePath);

                    return fileInfo;
                }
            }
        }
        
        public async Task<string> Content(string url)
        {
            using (var browser = await GetBrowser())
            {
                using (var page = await browser.NewPageAsync())
                {
                    await page.GoToAsync(url);
                    var result = await page.GetContentAsync();

                    return result;
                }
            }
        }

        public async Task<byte[]> Screenshot(string url)
        {
            using (var browser = await GetBrowser())
            {
                using (var page = await browser.NewPageAsync())
                {
                    await page.GoToAsync(url);
                    var result = await page.ScreenshotDataAsync();

                    return result;
                }
            }
        }
    }
}
