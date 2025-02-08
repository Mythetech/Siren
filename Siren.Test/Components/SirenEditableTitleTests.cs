using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Siren.Components.Shared;
using Xunit;

namespace Siren.Test.Components
{
    public class SirenEditableTitleTests : TestContext
    {
        public SirenEditableTitleTests()
        {
            Services.AddMudServices();
        }

        [Fact(DisplayName = "Can render readonly edit title")]
        public void InitialState_ShouldBeReadOnly()
        {
            // Arrange
            var cut = RenderComponent<SirenEditableTitle>();

            // Act
            var textField = cut.FindComponent<MudTextField<string>>();

            // Assert
            textField.Instance.ReadOnly.Should().BeTrue();
        }
    }
}

