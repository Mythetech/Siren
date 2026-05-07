using MudBlazor.Utilities;

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
                    Key: JsKey.Enter,
                    RequiresCtrl: true
                ),
                new KeyboardShortcut(
                    Description: "New request tab",
                    Key: JsKey.KeyT,
                    RequiresCtrl: true
                ),
                new KeyboardShortcut(
                    Description: "Focus URI input",
                    Key: JsKey.KeyU,
                    RequiresCtrl: true
                ),
                new KeyboardShortcut(
                    Description: "Open settings",
                    Key: JsKey.KeyS,
                    RequiresCtrl: true
                ),
                new KeyboardShortcut(
                    Description: "Focus HTTP method",
                    Key: JsKey.KeyM,
                    RequiresCtrl: true
                ),
                new KeyboardShortcut(
                    Description: "Open history panel",
                    Key: JsKey.KeyH,
                    RequiresCtrl: true,
                    RequiresShift: true
                ),
                new KeyboardShortcut(
                    Description: "Open collections panel",
                    Key: JsKey.KeyC,
                    RequiresCtrl: true,
                    RequiresShift: true
                ),
                new KeyboardShortcut(
                    Description: "Open variables panel",
                    Key: JsKey.KeyV,
                    RequiresCtrl: true,
                    RequiresShift: true
                ),
                new KeyboardShortcut(
                    Description: "Go to requests page",
                    Key: JsKey.KeyR,
                    RequiresCtrl: true,
                    RequiresShift: true
                ),
                new KeyboardShortcut(
                    Description: "Go to mock server page",
                    Key: JsKey.KeyM,
                    RequiresCtrl: true,
                    RequiresShift: true
                )
            }.AsReadOnly();
        }
    }
}
