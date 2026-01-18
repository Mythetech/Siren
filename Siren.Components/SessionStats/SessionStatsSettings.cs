using Mythetech.Framework.Infrastructure.Settings;
using Siren.Components.Theme;

namespace Siren.Components.SessionStats;

/// <summary>
/// Settings for the Session Stats panel.
/// </summary>
public class SessionStatsSettings : SettingsBase
{
    public override string SettingsId => "SessionStats";
    public override string DisplayName => "Session Stats";
    public override string Icon => SirenIcons.Stats;
    public override int Order => 19;

    [Setting(Label = "Show Session Stats",
        Group = "Session Stats",
        Order = 1,
        Description = "Show the Session Stats panel in the sidebar")]
    public bool ShowSessionStats { get; set; } = true;
}
