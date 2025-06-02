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

namespace Siren.Test.Components
{
    public class HistoryPanelTests : TestContext
    {
        private SirenAppState _state;
        private IHistoryService _historyService;

        public HistoryPanelTests()
        {
            _state = new();
            _historyService = Substitute.For<IHistoryService>();
            Services.AddSingleton(_state);
            Services.AddSingleton(_historyService);
            Services.AddMudServices();
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
    }
}

