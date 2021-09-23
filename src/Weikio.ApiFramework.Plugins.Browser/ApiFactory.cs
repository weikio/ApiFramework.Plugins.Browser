using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;
using Weikio.TypeGenerator;

namespace Weikio.ApiFramework.Plugins.Browser
{
    public class ApiFactory
    {
        private readonly ILogger<ApiFactory> _logger;

        public ApiFactory(ILogger<ApiFactory> logger)
        {
            _logger = logger;
        }

        public async Task<List<Type>> Create(BrowserOptions configuration)
        {
            var executablePath = configuration?.ExecutablePath;
            var downloadBrowser = string.IsNullOrWhiteSpace(executablePath) && string.IsNullOrWhiteSpace(configuration?.BrowserWSEndpoint);

            if (downloadBrowser)
            {
                var path = GetBrowserPath();
                _logger.LogDebug("Downloading browser to path {Path}", path);
                
                var browserConfigurationFile = Path.Combine(path, ".browser.config");

                if (File.Exists(browserConfigurationFile))
                {
                    executablePath = await File.ReadAllTextAsync(browserConfigurationFile, Encoding.UTF8);

                    if (!File.Exists(executablePath))
                    {
                        File.Delete(browserConfigurationFile);
                        Directory.Delete(path, true);

                        executablePath = string.Empty;
                        await Task.Delay(TimeSpan.FromMilliseconds(300));
                    }
                }

                if (!File.Exists(browserConfigurationFile))
                {
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }

                    var browserFetcherOptions = new BrowserFetcherOptions { Path = path };
                    var browserFetcher = new BrowserFetcher(browserFetcherOptions);
                    await browserFetcher.DownloadAsync(BrowserFetcher.DefaultRevision).ConfigureAwait(false);

                    executablePath = browserFetcher.GetExecutablePath(BrowserFetcher.DefaultRevision);

                    await File.WriteAllTextAsync(browserConfigurationFile, executablePath, Encoding.UTF8);

                    await Task.Delay(TimeSpan.FromMilliseconds(300));
                }

                executablePath = await File.ReadAllTextAsync(browserConfigurationFile, Encoding.UTF8);
            }

            var code = string.Empty;

            if (!string.IsNullOrWhiteSpace(executablePath))
            {
                if (!File.Exists(executablePath))
                {
                    throw new ArgumentException($"Can not use executable as the browser. File {executablePath} not found.");
                }

                _logger.LogDebug("Using locally installed Chromium based browser");

                var sourceWriter = new StringBuilder();
                sourceWriter.UsingNamespace("System.Threading.Tasks");
                sourceWriter.UsingNamespace("PuppeteerSharp");

                sourceWriter.Namespace("Weikio.ApiFramework.Plugins.Browser");

                sourceWriter.StartClass($"ExecutableBrowser : WebBrowser");

                sourceWriter.WriteLine($"private readonly string _executablePath = @\"{executablePath}\";");

                sourceWriter.Write(
                    "protected override async Task<PuppeteerSharp.Browser> GetBrowser() { var launchOptions = new LaunchOptions() { Headless = true, ExecutablePath = _executablePath };var result = await Puppeteer.LaunchAsync(launchOptions); return result; }");
                sourceWriter.FinishBlock(); // Finish the class
                sourceWriter.FinishBlock(); // Finish the namespace

                code = sourceWriter.ToString();
            }
            else
            {
                _logger.LogDebug("Using remote browser");
                var sourceWriter = new StringBuilder();

                sourceWriter.UsingNamespace("System.Threading.Tasks");
                sourceWriter.UsingNamespace("PuppeteerSharp");

                sourceWriter.Namespace("Weikio.ApiFramework.Plugins.Browser");

                sourceWriter.StartClass($"RemoteBrowser : WebBrowser");

                sourceWriter.WriteLine($"private readonly string _browserWSEndpoint = \"{configuration?.BrowserWSEndpoint}\";");

                sourceWriter.Write(
                    "protected override async Task<PuppeteerSharp.Browser> GetBrowser() { var connectOptions = new ConnectOptions() { BrowserWSEndpoint = _browserWSEndpoint };var result = await Puppeteer.ConnectAsync(connectOptions); return result; }");
                sourceWriter.FinishBlock(); // Finish the class
                sourceWriter.FinishBlock(); // Finish the namespace

                code = sourceWriter.ToString();
            }

            var generator = new CodeToAssemblyGenerator();
            generator.ReferenceAssemblyContainingType<WebBrowser>();
            generator.ReferenceAssemblyContainingType<PuppeteerSharp.Browser>();

            var assembly = generator.GenerateAssembly(code);

            var result = assembly.GetExportedTypes()
                .ToList();

            return result;
        }

        public static string GetBrowserPath()
        {
            return Path.Combine(Path.GetTempPath(), "ApiFramework_Browser");
        }
    }
}
