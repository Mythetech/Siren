using Bunit;
using MudBlazor;
using MudBlazor.Services;

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
    }
}

