using Mythetech.Framework.Infrastructure.Settings;
using Siren.Components.Settings.Editors;
using Siren.Components.Theme;

namespace Siren.Components.Settings;

/// <summary>
/// Environment-related settings for controlling startup behavior.
/// </summary>
public class EnvironmentSettings : SettingsBase
{
    public override string SettingsId => "Environment";
    public override string DisplayName => "Environment";
    public override string Icon => SirenIcons.Environment;
    public override int Order => 17;

    /// <summary>
    /// The default environment to restore on startup.
    /// Uses custom editor for dynamic environment list.
    /// </summary>
    [Setting(Label = "Default Environment",
        Description = "Environment restored on startup",
        CustomEditor = typeof(EnvironmentSelectEditor))]
    public string? DefaultEnvironment { get; set; }
}
