using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Siren.Test.Integration.HttpSendPath;

public sealed class EchoHostFixture : IAsyncLifetime
{
    private static readonly string[] SupportedVerbs =
    {
        "GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS", "HEAD", "QUERY"
    };

    private WebApplication? _app;

    public string BaseUrl { get; private set; } = "";

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        _app = builder.Build();
        _app.MapMethods("/echo", SupportedVerbs, EchoHandlerAsync);

        await _app.StartAsync();

        var addresses = _app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()
            ?? throw new InvalidOperationException("IServerAddressesFeature not available on test host");

        BaseUrl = addresses.Addresses.First();
    }

    public async Task DisposeAsync()
    {
        if (_app is null)
        {
            return;
        }

        await _app.StopAsync();
        await _app.DisposeAsync();
    }

    private static async Task EchoHandlerAsync(HttpContext ctx)
    {
        ctx.Response.Headers["X-Echo-Method"] = ctx.Request.Method;

        if (HttpMethods.IsHead(ctx.Request.Method))
        {
            ctx.Response.ContentType = "application/json";
            return;
        }

        var body = "";
        if (ctx.Request.ContentLength is > 0)
        {
            using var reader = new StreamReader(ctx.Request.Body);
            body = await reader.ReadToEndAsync();
        }

        var headers = ctx.Request.Headers
            .ToDictionary(h => h.Key, h => h.Value.ToString());

        var payload = new
        {
            method = ctx.Request.Method,
            path = ctx.Request.Path.Value,
            headers,
            body,
            contentType = ctx.Request.ContentType
        };

        await ctx.Response.WriteAsJsonAsync(payload);
    }
}
