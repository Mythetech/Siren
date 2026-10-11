using System;
using Bunit;
using FluentAssertions;
using FluentAssertions.Common;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;
using Siren.Components;
using Siren.Components.History;
using Siren.Components.Http;
using Siren.Components.MockServer;
using Siren.Components.Http.Models;
using Siren.Components.Shared;
using Siren.Test.Infrastructure;

namespace Siren.Test.Components
{
    public class HistoryPanelTests : TestContext
    {
        private IHistoryService _historyService;

        public HistoryPanelTests()
        {
            _historyService = Substitute.For<IHistoryService>();
            this.AddRequestTabs();
            Services.AddSingleton(_historyService);
            Services.AddSingleton(Substitute.For<IMockServerService>());
            Services.AddSingleton<MockServerState>();
            Services.AddMudServices();
            Services.AddLogging();
            JSInterop.Mode = JSRuntimeMode.Loose;
            JSInterop.SetupVoid("mudPopover.initialize", _ => true);
            JSInterop.Setup<int>("mudpopoverHelper.countProviders");
            JSInterop.SetupVoid("mudPopover.connect", _ => true);
            JSInterop.SetupVoid("mudKeyInterceptor.connect", _ => true);
            JSInterop.SetupVoid("mudElementRef.addOnBlurEvent", _ => true);
            JSInterop.SetupVoid("mudElementRef.addOnBlurEvent", _ => true);

            var popover = RenderComponent<MudPopoverProvider>();
        }

        [Fact(DisplayName = "Determines when a history record has data")]
        public void HasHistoryData_ReturnsTrue_WhenHistoryRecordHasData()
        {
            // Arrange
            var historyRecord = new HistoryRecord
            {
                RequestUri = "http://example.com",
                Request = new HttpRequest(),
                Response = new RequestResult()
            };


            _historyService.GetHistory().Returns(new List<HistoryRecord> { historyRecord });

            var cut = RenderComponent<HistoryPanel>();

            // Act
            var result = cut.Instance.HasHistoryData(historyRecord);

            // Assert
            result.Should().BeTrue();
        }

        [Fact(DisplayName = "Determines when a history record has no data")]
        public void HasHistoryData_ReturnsFalse_WhenHistoryRecordHasNoData()
        {
            // Arrange
            var historyRecord = new HistoryRecord
            {
                RequestUri = null,
                Request = null,
                Response = null
            };

            _historyService.GetHistory().Returns(new List<HistoryRecord> { historyRecord });

            var cut = RenderComponent<HistoryPanel>();

            // Act
            var result = cut.Instance.HasHistoryData(historyRecord);

            // Assert
            result.Should().BeFalse();
        }

        [Fact(DisplayName = "Deleting history from the panel deletes it exactly once")]
        public async Task DeleteHistory_Confirmed_DeletesOnce()
        {
            _historyService.GetHistory().Returns(new List<HistoryRecord>());
            var dialogs = RenderComponent<MudDialogProvider>();
            var cut = RenderComponent<HistoryPanel>();
            cut.WaitForAssertion(() => cut.FindComponents<SirenIconButton>().Should().NotBeEmpty());

            var deleteButton = cut.FindComponents<SirenIconButton>().Single(b => b.Instance.Tooltip == "Delete History");
            var prompt = deleteButton.Find("button").ClickAsync(new());
            dialogs.WaitForAssertion(() => dialogs.HasOpenDialog().Should().BeTrue());
            await dialogs.ClickButtonAsync("Delete");
            await prompt;

            _historyService.Received(1).DeleteAllHistoryRecords();
            Services.GetRequiredService<ISnackbar>().ShownSnackbars.Should().ContainSingle();
        }
    }
}

