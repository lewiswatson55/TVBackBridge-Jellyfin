using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;

namespace Jellyfin.Plugin.VidaaBack;

public sealed class VidaaBackStartupFilter : IStartupFilter
{
    private const string ScriptResource = "Jellyfin.Plugin.VidaaBack.vidaa-back.js";
    private static readonly string ScriptTemplate = LoadScriptTemplate();

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            app.Use(InjectScriptAsync);
            next(app);
        };
    }

    private static async Task InjectScriptAsync(HttpContext context, RequestDelegate next)
    {
        if (!HttpMethods.IsGet(context.Request.Method) || !IsWebIndex(context.Request.Path))
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        var originalBodyFeature = context.Features.Get<IHttpResponseBodyFeature>();
        if (originalBodyFeature is null)
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        var originalAcceptEncoding = RemoveRequestHeader(context, "Accept-Encoding");
        var originalIfNoneMatch = RemoveRequestHeader(context, "If-None-Match");
        var originalIfModifiedSince = RemoveRequestHeader(context, "If-Modified-Since");

        using var buffer = new MemoryStream();
        var bufferedFeature = new StreamResponseBodyFeature(buffer, originalBodyFeature);
        context.Features.Set<IHttpResponseBodyFeature>(bufferedFeature);

        try
        {
            await next(context).ConfigureAwait(false);
        }
        finally
        {
            context.Features.Set(originalBodyFeature);
            RestoreRequestHeader(context, "Accept-Encoding", originalAcceptEncoding);
            RestoreRequestHeader(context, "If-None-Match", originalIfNoneMatch);
            RestoreRequestHeader(context, "If-Modified-Since", originalIfModifiedSince);
        }

        var response = context.Response;
        var bytes = buffer.ToArray();
        if (response.StatusCode == StatusCodes.Status200OK &&
            response.ContentType?.Contains("text/html", StringComparison.OrdinalIgnoreCase) == true)
        {
            var html = Encoding.UTF8.GetString(bytes);
            var closingBody = html.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);
            if (closingBody >= 0 && !html.Contains("data-vidaa-back-plugin", StringComparison.Ordinal))
            {
                html = html.Insert(closingBody, CreateScriptTag());
                bytes = Encoding.UTF8.GetBytes(html);
                response.Headers.Remove("ETag");
                response.Headers.Remove("Last-Modified");
            }
        }

        response.ContentLength = bytes.Length;
        await originalBodyFeature.Stream.WriteAsync(bytes, context.RequestAborted).ConfigureAwait(false);
    }

    private static bool IsWebIndex(PathString path)
    {
        var value = path.Value;
        return value is not null &&
            (value.EndsWith("/web/", StringComparison.OrdinalIgnoreCase) ||
             value.EndsWith("/web/index.html", StringComparison.OrdinalIgnoreCase));
    }

    private static StringValues RemoveRequestHeader(HttpContext context, string name)
    {
        var value = context.Request.Headers[name];
        context.Request.Headers.Remove(name);
        return value;
    }

    private static void RestoreRequestHeader(HttpContext context, string name, StringValues value)
    {
        if (StringValues.IsNullOrEmpty(value))
        {
            context.Request.Headers.Remove(name);
        }
        else
        {
            context.Request.Headers[name] = value;
        }
    }

    private static string CreateScriptTag()
    {
        var configuration = Plugin.Instance?.Configuration ?? new PluginConfiguration();
        var options = JsonSerializer.Serialize(new
        {
            configuration.Enabled,
            configuration.DebugMode,
            configuration.BackKeyCode,
            configuration.BackKeyName,
            configuration.UserAgentKeywords
        });
        return "<script data-vidaa-back-plugin>" +
            ScriptTemplate.Replace("__VIDAA_BACK_OPTIONS__", options, StringComparison.Ordinal) +
            "</script>";
    }

    private static string LoadScriptTemplate()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ScriptResource)
            ?? throw new InvalidOperationException($"Missing embedded resource {ScriptResource}");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
