namespace ImgBrowser
{
    public static class Definitions
    {
        public enum BrowseDirection
        {
            Forward,
            Backward
        }
        
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
    }
}