using Microsoft.FluentUI.AspNetCore.Components;

namespace Siren.Components.Infrastructure.Keyboard
{
    public record KeyboardShortcut(
        string Description,
        KeyCode Key,
        bool RequiresCtrl = true,
        bool RequiresShift = false,
        bool RequiresAlt = false,
        bool RequiresMeta = false
    )
    {
        public string GetDisplayString()
        {
            var parts = new List<string>();
            
            if (RequiresCtrl)
            {
                parts.Add("Ctrl / ⌘");
            }
            else if (RequiresMeta)
            {
                parts.Add("⌘");
            }
            
            if (RequiresShift)
            {
                parts.Add("Shift");
            }
            
            if (RequiresAlt)
            {
                parts.Add("Alt");
            }
            
            parts.Add(GetKeyDisplay(Key));
            
            return string.Join(" + ", parts);
        }

        private static string GetKeyDisplay(KeyCode key)
        {
            return key switch
            {
                KeyCode.Enter => "Enter",
                KeyCode.KeyT => "T",
                KeyCode.KeyU => "U",
                KeyCode.KeyS => "S",
                _ => key.ToString().Replace("Key", "")
            };
        }
    }
}

