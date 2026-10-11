using Bunit;
using MudBlazor;
using MudBlazor.Services;
using Microsoft.Extensions.DependencyInjection;
using Mythetech.Framework.Infrastructure.Guards;
using NSubstitute;

namespace Siren.Test.Infrastructure
{
    public static class TestContextExtensions
    {
        public static IRenderedComponent<MudPopoverProvider> AddMudServicesWithPopover(this TestContext context)
        {
            context.Services.AddMudServices();
            context.JSInterop.SetupVoid("mudPopover.initialize", _ => true);
            context.JSInterop.Setup<int>("mudpopoverHelper.countProviders");
            context.JSInterop.SetupVoid("mudPopover.connect", _ => true);
            context.JSInterop.SetupVoid("mudKeyInterceptor.connect", _ => true);

            return context.RenderComponent<MudPopoverProvider>();
        }

        /// <summary>
        /// Makes every JsGuard render its content at once, as it does in the app once the script has loaded.
        /// Call it before AddMudServicesWithPopover, which locks service registration.
        /// </summary>
        public static void AddReadyJsGuards(this TestContext context)
        {
            var guards = Substitute.For<IJsGuardService>();
            guards.IsReady(Arg.Any<string>()).Returns(true);
            context.Services.AddSingleton(guards);
        }
    }
}

