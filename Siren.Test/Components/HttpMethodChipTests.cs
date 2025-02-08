using System;
using Bunit;
using FluentAssertions;
using FluentAssertions.Common;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Siren.Components;
using Siren.Components.Http;
using Siren.Test.Infrastructure;

namespace Siren.Test.Components
{
    public class HttpMethodChipTests : TestContext
    {
        private SirenAppState _state;

        public HttpMethodChipTests()
        {
            _state = new();
            Services.AddSingleton(_state);
            this.AddMudServicesWithPopover();
        }

        [Theory]
        [InlineData("GET", "#4caf50", "rgba(76, 175, 80, 0.1)")]
        [InlineData("get", "#4caf50", "rgba(76, 175, 80, 0.1)")]
        [InlineData("POST", "#2196f3", "rgba(33, 150, 243, 0.1)")]
        [InlineData("poSt", "#2196f3", "rgba(33, 150, 243, 0.1)")]
        [InlineData("PUT", "#ff9800", "rgba(255, 152, 0, 0.1)")]
        [InlineData("puT", "#ff9800", "rgba(255, 152, 0, 0.1)")]
        [InlineData("DELETE", "#f44336", "rgba(244, 67, 54, 0.1)")]
        [InlineData("deLEte", "#f44336", "rgba(244, 67, 54, 0.1)")]
        [InlineData("PATCH", "#9c27b0", "rgba(156, 39, 176, 0.1)")]
        [InlineData("patCh", "#9c27b0", "rgba(156, 39, 176, 0.1)")]
        [InlineData("OPTIONS", "#3f51b5", "rgba(63, 81, 181, 0.1)")]
        [InlineData("OptIONS", "#3f51b5", "rgba(63, 81, 181, 0.1)")]
        [InlineData("HEAD", "#009688", "rgba(0, 150, 136, 0.1)")]
        [InlineData("HeaD", "#009688", "rgba(0, 150, 136, 0.1)")]
        [InlineData("UNKNOWN", "rgb(66, 66, 66)", "rgba(255, 255, 255, 0.1)")]
        public void HttpMethodChip_Should_Have_Correct_Color_And_BackgroundColor(string method, string expectedColor, string expectedBackgroundColor)
        {
            // Arrange
            var component = RenderComponent<HttpMethodChip>(parameters => parameters
                .Add(p => p.Method, new HttpMethod(method)));

            // Act
            var chip = component.Find("div.mud-chip");

            // Assert
            chip.GetAttribute("style").Should().Contain($"color:{expectedColor}");
            chip.GetAttribute("style").Should().Contain($"background-color:{expectedBackgroundColor}");
        }
    }
}

