using Mythetech.Framework.Infrastructure.Settings;
using Siren.Components.Settings;
using Siren.Components.Theme;

namespace Siren.Components.History;

/// <summary>
/// Settings for history and storage behavior.
/// </summary>
public class HistorySettings : SettingsBase
{
    public override string SettingsId => "History";
    public override string DisplayName => "Storage";
    public override string Icon => SirenIcons.AppData;
    public override int Order => 30;

    /// <summary>
    /// Adds the delete app data button after the settings.
    /// </summary>
    public override Type? EndingContent => typeof(StorageEndingContent);

    [Setting(Label = "Save HTTP Response Content",
        Order = 1,
        Description = "Allows requests to be restored from history, but takes more space on disk")]
    public bool SaveHttpContent { get; set; } = false;
}
