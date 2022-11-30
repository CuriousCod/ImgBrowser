using System;
using System.Drawing;
using System.Windows.Forms;

namespace ImgBrowser
{
    public partial class MainWindow
    {
        // Scroll the image in the window
        private void MovePictureBox(Definitions.MovementType movementType, Definitions.Direction direction)
        {
            if (pictureBox1.Image == null)
            {
                return;
            }

            // Hide any currently displayed message
            DisplayMessage("");
            
            // Only allow adjustments if the image is larger than the screen resolution
            if (pictureBox1.Image.Width > Width)
            {
                int borderMin = 0;
                int borderMax = -pictureBox1.Image.Width + ClientRectangle.Width;

                switch (movementType)
                {
                    case Definitions.MovementType.MouseDrag:
                        if (CheckMinimumMouseDragDistance(Definitions.Axis.X))
                        {
                            pictureBox1.Location =
                                NewPictureBoxLocationByMouseCoordinates(Definitions.Axis.X, Definitions.MovementType.MouseDrag);
                    
                            // Reset mouse position variable to stop infinite scrolling
                            storedMousePosition.X = Cursor.Position.X;

                        }
                        break;
                    case Definitions.MovementType.Keyboard:
                        int newPos;
                        var multiplier = (int)(pictureBox1.Image.Width * 0.01);
                        
                        switch (direction)
                        {
                            case Definitions.Direction.Right:
                                newPos = VerifyBorders(pictureBox1.Location.X - 1 * multiplier, borderMin, borderMax);
                                pictureBox1.Location = new Point(newPos, pictureBox1.Location.Y);
                                break;
                            case Definitions.Direction.Left:
                                newPos = VerifyBorders(pictureBox1.Location.X + 1 * multiplier, borderMin, borderMax);
                                pictureBox1.Location = new Point(newPos, pictureBox1.Location.Y);
                                break;
                        }
                        break;
                }

                zoomLocation = pictureBox1.Location;
            }
            
            if (pictureBox1.Image.Height > Height)
            {
                const int borderMin = 0;
                var borderMax = -pictureBox1.Image.Height + ClientRectangle.Height;
                
                int newPos;
                int multiplier;
                
                switch (movementType)
                {
                    case Definitions.MovementType.MouseDrag:
                        if (CheckMinimumMouseDragDistance(Definitions.Axis.Y))
                        {
                            pictureBox1.Location =
                                NewPictureBoxLocationByMouseCoordinates(Definitions.Axis.Y, Definitions.MovementType.MouseDrag);
                    
                            // Reset mouse position variable to stop infinite scrolling
                            storedMousePosition.Y = Cursor.Position.Y;

                        }
                        break;
                    
                    case Definitions.MovementType.MouseScroll:
                        multiplier = (int)(pictureBox1.Image.Height * 0.02);
                        
                        switch (direction)
                        {
                            case Definitions.Direction.Up:
                                newPos = VerifyBorders(pictureBox1.Location.Y + 1 * multiplier, borderMin, borderMax);
                                pictureBox1.Location = new Point(pictureBox1.Location.X, newPos);
                                break;
                            
                            case Definitions.Direction.Down:
                                newPos = VerifyBorders(pictureBox1.Location.Y - 1 * multiplier, borderMin, borderMax);
                                pictureBox1.Location = new Point(pictureBox1.Location.X, newPos);
                                break;
                        }
                        break;
                    case Definitions.MovementType.Keyboard:
                        multiplier = (int)(pictureBox1.Image.Height * 0.01);
                        
                        switch (direction)
                        {
                            case Definitions.Direction.Up:
                                newPos = VerifyBorders(pictureBox1.Location.Y + 1 * multiplier, borderMin, borderMax);
                                pictureBox1.Location = new Point(pictureBox1.Location.X, newPos);
                                break;
                            case Definitions.Direction.Down:
                                newPos = VerifyBorders(pictureBox1.Location.Y - 1 * multiplier, borderMin, borderMax);
                                pictureBox1.Location = new Point(pictureBox1.Location.X, newPos);
                                break;
                        }
                        break;
                }

                zoomLocation = pictureBox1.Location;
            }
        }

        private Point NewPictureBoxLocationByMouseCoordinates(Definitions.Axis axis, Definitions.MovementType movementType)
        {
            const int borderMin = 0;
            int borderMax;
            int newPos;

            switch (axis)
            {
                case Definitions.Axis.X:
                    borderMax = -pictureBox1.Image.Width + ClientRectangle.Width;
                    switch (movementType)
                    {
                        case Definitions.MovementType.MouseDrag:
                            newPos = VerifyBorders(pictureBox1.Location.X + Cursor.Position.X - storedMousePosition.X, borderMin, borderMax);
                            return new Point(newPos, pictureBox1.Location.Y);
                    }
                    break;
                case Definitions.Axis.Y:
                    borderMax = -pictureBox1.Image.Height + ClientRectangle.Height;
                    switch (movementType)
                    {
                        case Definitions.MovementType.MouseDrag:
                            newPos = VerifyBorders(pictureBox1.Location.Y + Cursor.Position.Y - storedMousePosition.Y, borderMin, borderMax);
                            return new Point(pictureBox1.Location.X, newPos);
                    }
                    break;
            }

            return new Point(pictureBox1.Location.X, pictureBox1.Location.Y);
        }
        
        private bool CheckMinimumMouseDragDistance(Definitions.Axis axis)
        {
            var minDistance = (int)((Width + Height) * 0.01);

            if (axis == Definitions.Axis.X)
            {
                return Math.Abs(Math.Abs(Cursor.Position.X) - Math.Abs(storedMousePosition.X)) > minDistance;
            }
            
            return Math.Abs(Math.Abs(Cursor.Position.Y) - Math.Abs(storedMousePosition.Y)) > minDistance;
        }
        
        private static int VerifyBorders(int newPos, int borderMin, int borderMax)
        {
            if (newPos > borderMin)
            {
                newPos = borderMin;
            }

            if (newPos < borderMax)
            {
                newPos = borderMax;
            }
            
            return newPos;
        }

        private void CenterImage(bool updateZoom = true)
        {
            if (pictureBox1.Image == null)
            {
                return;
            }

            // Return to zoom mode, if image is smaller than the frame
            if (Width > pictureBox1.Image.Width && Height > pictureBox1.Image.Height) 
            {
                SizeModeZoom();
                return;
            }
                
            // Calculate padding to center image
            if (ClientSize.Width > pictureBox1.Image.Width)
            {
                pictureBox1.Left = (Width - pictureBox1.Image.Width) / 2;
                // Update zoom location to center image
                if (!updateZoom)
                {
                    return;
                }
                
                zoomLocation = new Point(pictureBox1.Left, zoomLocation.Y);
                pictureBox1.Top = 0;
                
                return;
            }

            if (ClientSize.Height > pictureBox1.Image.Height)
            {
                pictureBox1.Top = (Height - pictureBox1.Image.Height) / 2;

                // Update zoom location to center image
                if (!updateZoom)
                {
                    return;
                }
                
                zoomLocation = new Point(zoomLocation.X, pictureBox1.Top);
                pictureBox1.Left = 0;
                
                return;
            }

            if (!updateZoom)
            {
                return;
            }
                
            pictureBox1.Top = 0;
            pictureBox1.Left = 0;
            
        }
        
    }
}