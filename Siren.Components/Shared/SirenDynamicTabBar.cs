using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Siren.Components.Shared
{
    /// <summary>
    /// MudDynamicTabs claims the tab bar header for its add button and replaces any Header passed to it,
    /// so content that sits beside the add button has to be composed into that header here.
    /// </summary>
    public class SirenDynamicTabBar : MudDynamicTabs
    {
        /// <summary>
        /// Content rendered in the tab bar header, to the left of the add button.
        /// </summary>
        [Parameter]
        public RenderFragment? HeaderContent { get; set; }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            var addButton = Header;
            Header = tabs => builder =>
            {
                builder.AddContent(0, HeaderContent);
                builder.AddContent(1, addButton, tabs);
            };
        }
    }
}
