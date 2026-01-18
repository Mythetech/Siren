using Mythetech.Framework.Infrastructure.Settings;
using Siren.Components.Settings;
using Siren.Components.Theme;

namespace Siren.Components.Http;

/// <summary>
/// Settings for how responses are displayed.
/// </summary>
public class ResponseSettings : SettingsBase
{
    public override string SettingsId => "Response";
    public override string DisplayName => "Display";
    public override string Icon => SirenIcons.Preview;
    public override int Order => 16;

    [Setting(Label = "Time Display",
        Group = "Response",
        Order = 1,
        Description = "How to display response times")]
    public TimeDisplayOptions TimeDisplay { get; set; } = TimeDisplayOptions.Milliseconds;

    [Setting(Label = "Size Display",
        Group = "Response",
        Order = 2,
        Description = "How to display response sizes")]
    public SizeDisplayOptions SizeDisplay { get; set; } = SizeDisplayOptions.Bytes;
}
