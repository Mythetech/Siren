using Bunit;
using FluentAssertions;
using MudBlazor.Services;
using Siren.Components.Shared;

namespace Siren.Test.Components
{
    public class SirenDynamicTabBarTests : TestContext
    {
        private const string HeaderMarkup = "<span id=\"header-content\"></span>";

        public SirenDynamicTabBarTests()
        {
            Services.AddMudServices();
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        [Fact(DisplayName = "Header content renders to the left of the add button")]
        public void HeaderContent_RendersBeforeAddButton()
        {
            var cut = RenderComponent<SirenDynamicTabBar>(p => p.Add(x => x.HeaderContent, HeaderMarkup));

            var header = cut.Find(".mud-tabs-header-after");

            header.FirstElementChild!.Id.Should().Be("header-content");
            header.QuerySelectorAll("button").Should().ContainSingle();
        }

        [Fact(DisplayName = "Header content and the add button survive a re-render")]
        public void HeaderContent_SurvivesRerender()
        {
            var cut = RenderComponent<SirenDynamicTabBar>(p => p.Add(x => x.HeaderContent, HeaderMarkup));

            cut.SetParametersAndRender(p => p.Add(x => x.HeaderContent, HeaderMarkup));

            var header = cut.Find(".mud-tabs-header-after");
            header.FirstElementChild!.Id.Should().Be("header-content");
            header.QuerySelectorAll("button").Should().ContainSingle();
        }

        [Fact(DisplayName = "The add button renders alone when there is no header content")]
        public void NoHeaderContent_RendersOnlyAddButton()
        {
            var cut = RenderComponent<SirenDynamicTabBar>();

            var header = cut.Find(".mud-tabs-header-after");

            header.QuerySelector("#header-content").Should().BeNull();
            header.QuerySelectorAll("button").Should().ContainSingle();
        }
    }
}
