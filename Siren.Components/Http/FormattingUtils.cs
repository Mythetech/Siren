using Siren.Components.Settings;

namespace Siren.Components.Http
{
    public static class FormattingUtils
    {
        public static string FormatSize(long bytes, SizeDisplayOptions displayOption)
        {
            return displayOption switch
            {
                SizeDisplayOptions.Bytes => $"{bytes} B",
                SizeDisplayOptions.Kilobytes => $"{bytes / 1024.0:F2} KB",
                SizeDisplayOptions.Megabytes => $"{bytes / (1024.0 * 1024.0):F2} MB",
                _ => $"{bytes} B"
            };
        }

        public static string FormatTime(TimeSpan duration, TimeDisplayOptions displayOption)
        {
            return displayOption switch
            {
                TimeDisplayOptions.Milliseconds => $"{duration.TotalMilliseconds:F0}ms",
                TimeDisplayOptions.Seconds => $"{duration.TotalSeconds:F2}s",
                _ => $"{duration.TotalMilliseconds:F0}ms"
            };
        }
    }
}

