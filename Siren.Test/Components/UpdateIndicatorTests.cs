using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using Mythetech.Framework.Desktop.Updates;
using Mythetech.Framework.Desktop.Updates.Components;
using Mythetech.Framework.Desktop.Updates.Events;
using Mythetech.Framework.Infrastructure.MessageBus;
using NSubstitute;
using Siren.Components.Shared;
using Siren.Components.Shared.Dialogs.Commands;
using Siren.Test.Infrastructure;

namespace Siren.Test.Components
{
    public class UpdateIndicatorTests : TestContext
    {
        private readonly IUpdateService _updateService = Substitute.For<IUpdateService>();
        private readonly IMessageBus _messageBus = Substitute.For<IMessageBus>();

        public UpdateIndicatorTests()
        {
            Services.AddSingleton(_updateService);
            Services.AddSingleton(_messageBus);
            this.AddMudServicesWithPopover();
        }

        private static UpdateInfo Update(string version) => new() { TargetVersion = Version.Parse(version) };

        [Fact(DisplayName = "Shows nothing when no update is available")]
        public void NoUpdate_RendersNoButton()
        {
            _updateService.AvailableUpdate.Returns((UpdateInfo?)null);

            var cut = RenderComponent<UpdateIndicator>();

            cut.FindComponents<MudIconButton>().Should().BeEmpty();
        }

        [Fact(DisplayName = "Shows the indicator when an update was already found")]
        public void UpdateAlreadyAvailable_RendersButton()
        {
            _updateService.AvailableUpdate.Returns(Update("1.2.3"));

            var cut = RenderComponent<UpdateIndicator>();

            cut.FindComponents<MudIconButton>().Should().ContainSingle();
        }

        [Fact(DisplayName = "Shows the indicator when an update is found after startup")]
        public async Task UpdateAvailableMessage_RendersButton()
        {
            _updateService.AvailableUpdate.Returns((UpdateInfo?)null);
            var cut = RenderComponent<UpdateIndicator>();

            var update = Update("1.2.3");
            _updateService.AvailableUpdate.Returns(update);
            await cut.Instance.Consume(new UpdateAvailable(update));

            cut.FindComponents<MudIconButton>().Should().ContainSingle();
        }

        [Fact(DisplayName = "Clicking the indicator opens the update dialog")]
        public async Task Click_OpensUpdateDialog()
        {
            _updateService.AvailableUpdate.Returns(Update("1.2.3"));
            var cut = RenderComponent<UpdateIndicator>();

            await cut.Find("button").ClickAsync(new());

            await _messageBus.Received(1).PublishAsync(
                Arg.Is<ShowDialog>(d => d.Dialog == typeof(UpdateProgressDialog)));
        }
    }
}
