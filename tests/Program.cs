using System.Text;
using Jellyfin.Plugin.VidaaBack;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

const string sampleHtml = "<!doctype html><html><body><main>Jellyfin</main></body></html>";
var settingsResource = typeof(Plugin).Assembly.GetManifestResourceStream("Jellyfin.Plugin.VidaaBack.Configuration.configPage.html")
    ?? throw new Exception("Settings page resource missing");
using (var settingsReader = new StreamReader(settingsResource))
{
    var settingsPage = await settingsReader.ReadToEndAsync();
    if (!settingsPage.Contains("VidaaBackUserAgentKeywords", StringComparison.Ordinal) ||
        !settingsPage.Contains("VidaaBackReset", StringComparison.Ordinal))
    {
        throw new Exception("New settings controls are missing");
    }
}
var sampleFile = Path.GetTempFileName();
await File.WriteAllTextAsync(sampleFile, sampleHtml);

try
{
    var filter = new VidaaBackStartupFilter();
    var builder = new ApplicationBuilder(new ServiceCollection().BuildServiceProvider());
    filter.Configure(app => app.Run(async context =>
    {
        context.Response.ContentType = "text/html";
        context.Response.ContentLength = Encoding.UTF8.GetByteCount(sampleHtml);
        await context.Response.SendFileAsync(sampleFile);
    }))(builder);
    var pipeline = builder.Build();

    var indexContext = new DefaultHttpContext();
    indexContext.Request.Method = "GET";
    indexContext.Request.Path = "/web/index.html";
    indexContext.Request.Headers.IfNoneMatch = "old-etag";
    using var indexBody = new MemoryStream();
    indexContext.Response.Body = indexBody;
    await pipeline(indexContext);
    var indexHtml = Encoding.UTF8.GetString(indexBody.ToArray());
    if (!indexHtml.Contains("data-vidaa-back-plugin", StringComparison.Ordinal) ||
        !indexHtml.Contains("Key: ", StringComparison.Ordinal) ||
        !indexHtml.Contains("UserAgentKeywords", StringComparison.Ordinal) ||
        indexContext.Response.ContentLength != indexBody.Length ||
        indexContext.Request.Headers.IfNoneMatch != "old-etag")
    {
        throw new Exception("Web index transformation failed");
    }

    var otherContext = new DefaultHttpContext();
    otherContext.Request.Method = "GET";
    otherContext.Request.Path = "/other-app/index.html";
    using var otherBody = new MemoryStream();
    otherContext.Response.Body = otherBody;
    await pipeline(otherContext);
    var otherHtml = Encoding.UTF8.GetString(otherBody.ToArray());
    if (otherHtml != sampleHtml)
    {
        throw new Exception("Non-Jellyfin response was changed");
    }

    Console.WriteLine("Smoke test passed: Jellyfin index transformed; other path unchanged.");
}
finally
{
    File.Delete(sampleFile);
}
