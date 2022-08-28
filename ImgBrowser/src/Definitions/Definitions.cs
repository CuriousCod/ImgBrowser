namespace ImgBrowser
{
    public static class Definitions
    {
        public enum Axis
        {
            X,
            Y
        }
        
        public enum MovementType
        {
            MouseDrag,
            MouseScroll,
            Keyboard
        }
        
        public enum Direction
        {
            Up,
            Down,
            Left,
            Right,
            None
        }

        public static class LaunchArguments
        {
            public const string CenterWindowToMouse = "-center";
            public const string SetWidthAndHeight = "-size";
            public const string SetWindowPosition = "-position";
            public const string HideWindowBackground = "-transparent";
            public const string SetAlwaysOnTop = "-topmost";
            public const string SetBorderless = "-borderless";
            public const string SetRotation = "-rotate";
            public const string FlipX = "-flip";
            public const string LockImage = "-lock";
            public const string SkipImageFileLoading = "-noImage";

        }
    }
}