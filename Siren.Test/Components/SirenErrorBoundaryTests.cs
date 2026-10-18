using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shouldly;
using Siren.Components.Shared;
using Siren.Test.Infrastructure;

namespace Siren.Test.Components;

public class SirenErrorBoundaryTests : TestContext
{
    private const string Secret = "the key in /Users/someone/.ssh";

    private readonly RecordingLogger<SirenErrorBoundary> _logger = new();

    public SirenErrorBoundaryTests()
    {
        Services.AddLogging();
        Services.AddSingleton<ILogger<SirenErrorBoundary>>(_logger);
        JSInterop.Mode = JSRuntimeMode.Loose;
        this.AddMudServicesWithPopover();
    }

    /// <summary>
    /// A button that throws when clicked, standing in for any control inside a guarded panel.
    /// </summary>
    private sealed class Exploding : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "button");
            builder.AddAttribute(1, "id", "explode");
            builder.AddAttribute(2, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, Explode));
            builder.AddContent(3, "Explode");
            builder.CloseElement();
        }

        private void Explode() => throw new InvalidOperationException(Secret);
    }

    private IRenderedComponent<SirenErrorBoundary> RenderGuarded() => RenderComponent<SirenErrorBoundary>(p => p
        .Add(x => x.Area, "request panel")
        .AddChildContent<Exploding>());

    private static Task ExplodeAsync(IRenderedComponent<SirenErrorBoundary> cut)
        => cut.Find("#explode").ClickAsync(new MouseEventArgs());

    [Fact(DisplayName = "Content that does not fail is shown as it is")]
    public void Content_IsShown()
    {
        var cut = RenderGuarded();

        cut.FindAll("#explode").Count.ShouldBe(1);
        cut.Instance.ShowsError.ShouldBeFalse();
        cut.Markup.ShouldNotContain("Something went wrong");
    }

    [Fact(DisplayName = "A failure inside the boundary shows the alert in place of the content")]
    public async Task Failure_ShowsAlert()
    {
        var cut = RenderGuarded();

        await ExplodeAsync(cut);

        cut.Markup.ShouldContain("Something went wrong in the request panel.");
        cut.FindAll("#explode").ShouldBeEmpty();
        cut.Instance.ShowsError.ShouldBeTrue();
    }

    [Fact(DisplayName = "The alert never shows the exception's message")]
    public async Task Alert_HidesExceptionMessage()
    {
        var cut = RenderGuarded();

        await ExplodeAsync(cut);

        cut.Markup.ShouldNotContain(Secret);
    }

    [Fact(DisplayName = "A failure is logged as an error with its exception")]
    public async Task Failure_IsLogged()
    {
        var cut = RenderGuarded();

        await ExplodeAsync(cut);

        var entry = _logger.Entries.ShouldHaveSingleItem();
        entry.Level.ShouldBe(LogLevel.Error);
        entry.Exception.ShouldBeOfType<InvalidOperationException>();
        entry.Message.ShouldContain("request panel");
    }

    [Fact(DisplayName = "Try again shows the content again")]
    public async Task TryAgain_ShowsContent()
    {
        var cut = RenderGuarded();
        await ExplodeAsync(cut);

        await cut.FindAll("button").Single(button => button.TextContent.Trim() == "Try again").ClickAsync(new MouseEventArgs());

        cut.FindAll("#explode").Count.ShouldBe(1);
        cut.Instance.ShowsError.ShouldBeFalse();
    }
}
