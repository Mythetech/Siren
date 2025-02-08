using System;
using MudBlazor;

namespace Siren.Components.Theme
{
    public class SirenTheme : MudTheme
    {
        public SirenTheme()
        {
            PaletteLight = new PaletteLight()
            {
                Primary = SirenColors.Primary,
                Secondary = SirenColors.Darken,
                Tertiary = SirenColors.Tertiary,
                HoverOpacity = 0.5,
                Background = SirenColors.Light,
                AppbarBackground = SirenColors.Primary,
                Black = SirenColors.Black,
                Surface = "FFFFFF",
                ActionDefault = SirenColors.Primary,
                ActionDisabled = SirenColors.ActionDisabled,
                TableHover = SirenColors.Tertiary,

            };
            PaletteDark = new PaletteDark()
            {
                Primary = SirenColors.Primary,
                Secondary = SirenColors.Light,
                Tertiary = SirenColors.Dark.AppBarBackground,
                HoverOpacity = 0.5,
                TableHover = SirenColors.Dark.AppBarBackground,
                AppbarBackground = SirenColors.Dark.AppBarBackground,
                Background = SirenColors.Black,
                Black = SirenColors.Black,
                Surface = SirenColors.Black,
                ActionDefault = SirenColors.Primary,
                GrayDarker = "#222"
            };
            Typography = new Typography()
            {
                Default = new Default()
                {
                    FontFamily = new[] { "Tahoma", "Geneva", "Verdana", },
                    TextTransform = "none",
                    FontSize = "0.75",
                },

                Button = new Button()
                {
                    FontFamily = new[] { "Tahoma", "Geneva", "Verdana", },
                    TextTransform = "none",
                    FontSize = "1em",
                }
            };
            LayoutProperties = new LayoutProperties()
            {
                DefaultBorderRadius = "0.5em",
                AppbarHeight = "3rem",
                DrawerWidthLeft = "3.25em",
            };
        }
    }
}

