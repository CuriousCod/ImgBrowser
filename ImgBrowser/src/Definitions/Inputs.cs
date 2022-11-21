using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace ImgBrowser
{
    public static class Inputs
    {
        public enum InputActions
        {
            None,
            MoveImageUp,
            MoveImageDown,
            MoveOrBrowseImageLeft,
            MoveOrBrowseImageRight,
            MoveWindowUp,
            MoveWindowDown,
            MoveWindowLeft,
            MoveWindowRight,
            IncreaseWindowHeight,
            DecreaseWindowHeight,
            IncreaseWindowWidth,
            DecreaseWindowWidth,
            ToggleAlwaysOnTop,
            ToggleTitleBorder,
            OpenCurrentImageLocation,
            RefreshImages,
            RestoreCurrentImage,
            ToggleFullScreen,
            CopyToClipboard,
            PasteFromClipboard,
            RotateImage,
            MirrorImageHorizontally,
            DuplicateImage,
            GetColorAtMousePosition,
            ToggleImageLock,
            SaveImagePng,
            SaveImageJpg,
            ActivateSnippingTool,
            ToggleTransparency,
            CloseApplication,
            DisplayImageName,
            DeleteImage,
            MoveToFirstImage,
            MoveToLastImage,
            CopyImagePathAndDataToClipboard,
            StopWindowHover,
            ZoomIn,
            ZoomOut,
            SaveTemporaryImage,
            LoadTemporaryImage,
            AdjustHoverSpeed,
            Hover,
            AdjustHoverPosition,
            ShowKeyBinds,
            ChangeSortOrder,
        }

        private static readonly Dictionary<string, InputActions> KeyboardBinds = new Dictionary<string, InputActions>()
        {
            {"F1", InputActions.ToggleAlwaysOnTop},
            {"F2", InputActions.ToggleTitleBorder},
            {"F3", InputActions.OpenCurrentImageLocation},
            {"F5", InputActions.RefreshImages},
            {"F6", InputActions.ChangeSortOrder},
            {"F10", InputActions.RestoreCurrentImage},
            {"F11", InputActions.ToggleFullScreen},
            {"F12", InputActions.ShowKeyBinds},

            {"F", InputActions.ToggleFullScreen},
            {"I", InputActions.GetColorAtMousePosition},
            {"L", InputActions.ToggleImageLock},
            {"M", InputActions.MirrorImageHorizontally},
            {"N", InputActions.DisplayImageName},
            {"R", InputActions.RotateImage},
            {"S", InputActions.ActivateSnippingTool},
            {"T", InputActions.ToggleTransparency},
            
            {"Ctrl+C", InputActions.CopyToClipboard},
            {"Ctrl+V", InputActions.PasteFromClipboard},
            {"Ctrl+S", InputActions.SaveImagePng},
            {"Ctrl+D", InputActions.DuplicateImage},
            {"Ctrl+W", InputActions.CloseApplication},
            {"Ctrl+H", InputActions.Hover},
            {"Ctrl+M", InputActions.MirrorImageHorizontally},
            {"Ctrl+R", InputActions.RotateImage},
            {"Shift+H", InputActions.AdjustHoverPosition},
            {"Shift+I", InputActions.GetColorAtMousePosition},
            {"Alt+I", InputActions.GetColorAtMousePosition},
            {"Alt+Return", InputActions.ToggleFullScreen},
            {"Alt+Pause", InputActions.CopyImagePathAndDataToClipboard},
            {"Ctrl+Shift+W", InputActions.CloseApplication},
            {"Ctrl+Shift+S", InputActions.SaveImageJpg},
            
            {"Home", InputActions.MoveToFirstImage},
            {"End", InputActions.MoveToLastImage},
            {"Delete", InputActions.DeleteImage},
            {"Escape", InputActions.StopWindowHover},
            {"Add", InputActions.ZoomIn},
            {"Subtract", InputActions.ZoomOut},
            
            {"Ctrl+Add", InputActions.ZoomIn},
            {"Ctrl+Subtract", InputActions.ZoomOut},

            {"Left", InputActions.MoveOrBrowseImageLeft},
            {"Right", InputActions.MoveOrBrowseImageRight},
            {"Up", InputActions.MoveImageUp},
            {"Down", InputActions.MoveImageDown},
            
            {"Ctrl+Left", InputActions.MoveWindowLeft},
            {"Ctrl+Right", InputActions.MoveWindowRight},
            {"Ctrl+Up", InputActions.MoveWindowUp},
            {"Ctrl+Down", InputActions.MoveWindowDown},
            
            {"Ctrl+Alt+Left", InputActions.MoveWindowLeft},
            {"Ctrl+Alt+Right", InputActions.MoveWindowRight},
            {"Ctrl+Alt+Up", InputActions.MoveWindowUp},
            {"Ctrl+Alt+Down", InputActions.MoveWindowDown},
            
            {"Shift+Left", InputActions.DecreaseWindowWidth},
            {"Shift+Right", InputActions.IncreaseWindowWidth},
            {"Shift+Up", InputActions.DecreaseWindowHeight},
            {"Shift+Down", InputActions.IncreaseWindowHeight},
            
            {"Shift+Alt+Left", InputActions.DecreaseWindowWidth},
            {"Shift+Alt+Right", InputActions.IncreaseWindowWidth},
            {"Shift+Alt+Up", InputActions.DecreaseWindowHeight},
            {"Shift+Alt+Down", InputActions.IncreaseWindowHeight},
            
            {"D1", InputActions.AdjustHoverSpeed},
            {"D2", InputActions.AdjustHoverSpeed},
            {"D3", InputActions.AdjustHoverSpeed},
            {"D4", InputActions.AdjustHoverSpeed},
            {"D5", InputActions.AdjustHoverSpeed},
            {"Ctrl+D0", InputActions.LoadTemporaryImage},
            {"Ctrl+D1", InputActions.LoadTemporaryImage},
            {"Ctrl+D2", InputActions.LoadTemporaryImage},
            {"Ctrl+D3", InputActions.LoadTemporaryImage},
            {"Ctrl+D4", InputActions.LoadTemporaryImage},
            {"Ctrl+D5", InputActions.LoadTemporaryImage},
            {"Ctrl+D6", InputActions.LoadTemporaryImage},
            {"Ctrl+D7", InputActions.LoadTemporaryImage},
            {"Ctrl+D8", InputActions.LoadTemporaryImage},
            {"Ctrl+D9", InputActions.LoadTemporaryImage},
            {"Ctrl+Shift+D0", InputActions.SaveTemporaryImage},
            {"Ctrl+Shift+D1", InputActions.SaveTemporaryImage},
            {"Ctrl+Shift+D2", InputActions.SaveTemporaryImage},
            {"Ctrl+Shift+D3", InputActions.SaveTemporaryImage},
            {"Ctrl+Shift+D4", InputActions.SaveTemporaryImage},
            {"Ctrl+Shift+D5", InputActions.SaveTemporaryImage},
            {"Ctrl+Shift+D6", InputActions.SaveTemporaryImage},
            {"Ctrl+Shift+D7", InputActions.SaveTemporaryImage},
            {"Ctrl+Shift+D8", InputActions.SaveTemporaryImage},
            {"Ctrl+Shift+D9", InputActions.SaveTemporaryImage}
        };

        public class ModifierKeys
        {
            public bool Ctrl { get; set; }
            public bool Shift { get; set; }
            public bool Alt { get; set; }
        }
        
        public static InputActions GetAction(KeyEventArgs e, ModifierKeys m) => GetAction(e.KeyCode.ToString(), m);
        
        private static InputActions GetAction(string key, ModifierKeys modifierKeys)
        {
            var ctrl = modifierKeys.Ctrl ? "Ctrl+" : "";
            var shift = modifierKeys.Shift ? "Shift+" : "";
            var alt = modifierKeys.Alt ? "Alt+" : "";
            var keyString = $"{ctrl}{shift}{alt}{key}";
            
            return KeyboardBinds.ContainsKey(keyString) ? KeyboardBinds[keyString] : InputActions.None;
        }

        public static string[] GetKeyBinds()
        {
            return KeyboardBinds.Select(key => key.Key.PadRight(15) + " = " + string.Concat(key.Value.ToString().Select(x => char.IsUpper(x) ? " " + x : x.ToString())).TrimStart(' ')).ToArray();
        }
    }
}