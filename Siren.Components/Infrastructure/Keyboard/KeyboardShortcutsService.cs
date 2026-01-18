using Microsoft.FluentUI.AspNetCore.Components;

namespace Siren.Components.Infrastructure.Keyboard
{
    public static class KeyboardShortcutsService
    {
        public static IReadOnlyList<KeyboardShortcut> GetAllShortcuts()
        {
            return new List<KeyboardShortcut>
            {
                new KeyboardShortcut(
                    Description: "Run active request",
                    Key: KeyCode.Enter,
                    RequiresCtrl: true
                ),
                new KeyboardShortcut(
                    Description: "New request tab",
                    Key: KeyCode.KeyT,
                    RequiresCtrl: true
                ),
                new KeyboardShortcut(
                    Description: "Focus URI input",
                    Key: KeyCode.KeyU,
                    RequiresCtrl: true
                ),
                new KeyboardShortcut(
                    Description: "Open settings",
                    Key: KeyCode.KeyS,
                    RequiresCtrl: true
                ),
                new KeyboardShortcut(
                    Description: "Focus HTTP method",
                    Key: KeyCode.KeyM,
                    RequiresCtrl: true
                )
            }.AsReadOnly();
        }
    }
}

