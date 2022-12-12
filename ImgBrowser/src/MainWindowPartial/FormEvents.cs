using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ImgBrowser.Helpers;

namespace ImgBrowser
{
    public partial class MainWindow
    {
        private void MainWindow_MouseWheel(object sender, MouseEventArgs e)
        {
            var scrollingUp = e.Delta > 0;
            
            if ((ModifierKeys & Keys.Alt) == Keys.Alt)
            {
                // Increase window size
                if (WindowState != FormWindowState.Normal)
                {
                    return;
                }
                
                Size = scrollingUp ? Size.Add(Size, GetAdjustmentValue()) : Size.Subtract(Size, GetAdjustmentValue());
                
                if (pictureBox1.SizeMode != PictureBoxSizeMode.AutoSize && FormBorderStyle == FormBorderStyle.None)
                {
                    FitImageToWindow();
                }
                
                return;
            }

            if (pictureBox1.SizeMode != PictureBoxSizeMode.AutoSize)
            {
                if ((ModifierKeys & Keys.Control) == Keys.Control)
                {
                    ResizeImage(scrollingUp ? 1.5 : 0.75);
                }
                else
                {
                    if (scrollingUp)
                    {
                        BrowseForward();    
                    }
                    else
                    {
                        BrowseBackward();
                    }
                }
                
                return;
            }

            if (pictureBox1.SizeMode == PictureBoxSizeMode.AutoSize)
            {
                MovePictureBox(Definitions.MovementType.MouseScroll, scrollingUp ? Definitions.Direction.Up : Definitions.Direction.Down);
                return;
            }
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            Console.WriteLine(e.KeyCode);

            var mk = new Inputs.ModifierKeys
            {
                Ctrl = (ModifierKeys & Keys.Control) == Keys.Control,
                Shift = (ModifierKeys & Keys.Shift) == Keys.Shift,
                Alt = (ModifierKeys & Keys.Alt) == Keys.Alt
            };

            var action = Inputs.GetAction(e, mk);
            
            switch(action)
            {
                case Inputs.InputActions.None:
                    break;
                case Inputs.InputActions.MoveImageUp:
                    ArrowKeyMoveImageOrBrowseImages(Definitions.Direction.Up);
                    break;
                case Inputs.InputActions.MoveImageDown:
                    ArrowKeyMoveImageOrBrowseImages(Definitions.Direction.Down);
                    break;
                case Inputs.InputActions.MoveOrBrowseImageLeft:
                    ArrowKeyMoveImageOrBrowseImages(Definitions.Direction.Left);
                    break;
                case Inputs.InputActions.MoveOrBrowseImageRight:
                    ArrowKeyMoveImageOrBrowseImages(Definitions.Direction.Right);
                    break;
                case Inputs.InputActions.MoveWindowUp:
                    ArrowKeyMoveWindow(Definitions.Direction.Up, mk);
                    break;
                case Inputs.InputActions.MoveWindowDown:
                    ArrowKeyMoveWindow(Definitions.Direction.Down, mk);
                    break;
                case Inputs.InputActions.MoveWindowLeft:
                    ArrowKeyMoveWindow(Definitions.Direction.Left, mk);
                    break;
                case Inputs.InputActions.MoveWindowRight:
                    ArrowKeyMoveWindow(Definitions.Direction.Right, mk);
                    break;
                case Inputs.InputActions.IncreaseWindowHeight:
                    ArrowKeyAdjustWindowSize(Definitions.Direction.Down, mk);
                    break;
                case Inputs.InputActions.DecreaseWindowHeight:
                    ArrowKeyAdjustWindowSize(Definitions.Direction.Up, mk);
                    break;
                case Inputs.InputActions.IncreaseWindowWidth:
                    ArrowKeyAdjustWindowSize(Definitions.Direction.Right, mk);
                    break;
                case Inputs.InputActions.DecreaseWindowWidth:
                    ArrowKeyAdjustWindowSize(Definitions.Direction.Left, mk);
                    break;
                case Inputs.InputActions.IncreaseGifSpeed:
                    if (!GifAnimator.CanAnimate(currentImg.Image))
                    {
                        break;
                    }
                    
                    GifAnimator.AnimationDelay = Math.Max(1,  GifAnimator.AnimationDelay - 10);
                    DisplayMessage("Delay: " + GifAnimator.AnimationDelay + "ms");
                    
                    break;
                case Inputs.InputActions.DecreaseGifSpeed:
                    if (!GifAnimator.CanAnimate(currentImg.Image))
                    {
                        break;
                    }

                    GifAnimator.AnimationDelay = GifAnimator.AnimationDelay > 2 ? Math.Min(1000, GifAnimator.AnimationDelay + 10) : 10;
                    DisplayMessage("Delay: " + GifAnimator.AnimationDelay + "ms");   
                    
                    break;
                case Inputs.InputActions.Hover:
                    // TODO Borderline experimental
                    HoverWindow();
                    break;
                case Inputs.InputActions.AdjustHoverPosition:
                    if (!windowHover.StartSet)
                    {
                        windowHover.StartX = Cursor.Position.X;
                        windowHover.StartSet = true;
                        DisplayMessage("Hover start position set " + windowHover.StartX);
                        break;
                    }
                    if (!windowHover.EndSet)
                    {
                        windowHover.EndX = Cursor.Position.X;
                        windowHover.EndSet = true;
                        DisplayMessage("Hover end position set " + windowHover.EndX);
                    }
                    break;
                case Inputs.InputActions.ToggleAlwaysOnTop:
                    ToggleAlwaysOnTop();
                    break;
                case Inputs.InputActions.ToggleTitleBorder:
                    if (FormBorderStyle == FormBorderStyle.None)
                    {
                        FormBorderStyle = FormBorderStyle.Sizable;
                    }
                    else
                    {
                        FitImageToWindow();
                    }
                    break;
                case Inputs.InputActions.OpenCurrentImageLocation:
                    if (currentImg.IsFile)
                    {
                        Process.Start("explorer.exe", $"/select,\"{currentImg.Path}\\{currentImg.Name}\"");
                    }
                    break;
                case Inputs.InputActions.RefreshImages:
                    ReloadImages();
                    break;
                case Inputs.InputActions.RestoreCurrentImage:
                    if (imageEdited)
                    {
                        RestoreImage();
                    }
                    break;
                case Inputs.InputActions.ToggleFullScreen:
                    MaxOrNormalizeWindow();
                    
                    if (mk.Alt)
                    {
                        // Suppress angry Windows noises
                        // e.SuppressKeyPress = true;                        
                    }
                    
                    break;
                case Inputs.InputActions.ChangeSortOrder:
                    var index = (int)sortImagesBy;
                    index++;

                    if (index >= Enum.GetNames(typeof(Definitions.SortBy)).Length)
                    {
                        index = 0;
                    }
                    
                    sortImagesBy = (Definitions.SortBy)index;
                    
                    var sortName = sortImagesBy.ToString();
                    
                    // add space before capital letters
                    sortName = System.Text.RegularExpressions.Regex.Replace(sortName, "(\\B[A-Z])", " $1");

                    DisplayMessage("Sort order changed to " + sortName);
                        
                    ReloadImages(true);
                    
                    break;
                case Inputs.InputActions.CopyToClipboard:
                    if (!mk.Ctrl)
                    {
                        break;
                    }

                    if (pictureBox1.Image == null)
                    {
                        break;
                    }
                
                    if (mk.Shift) 
                    {
                        CopyCurrentDisplayToClipboard();
                    }
                    else
                    {
                        // TODO This makes image file size large
                        DisplayMessage("Copied to Clipboard");
                        Clipboard.SetImage(pictureBox1.Image);
                    }
                    break;
                case Inputs.InputActions.PasteFromClipboard:
                    if (mk.Ctrl)
                    {
                        LoadNewImgFromClipboard();
                    }
                    break;
                case Inputs.InputActions.RotateImage:
                    RotateImage(mk.Ctrl);
                    break;
                case Inputs.InputActions.MirrorImageHorizontally:
                    FlipImageX(mk.Ctrl);
                    break;
                case Inputs.InputActions.DuplicateImage:
                    var exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                    
                    if (!File.Exists(exePath))
                    {
                        break;
                    }

                    var args = GetCurrentArgs();

                    if (currentImg.IsFile)
                    {
                        Process.Start(exePath, $"\"{currentImg.FullFilename}\" {args} {Definitions.LaunchArguments.CenterWindowToMouse}");
                    }
                    else if (pictureBox1.Image != null) 
                    {
                        Clipboard.SetImage(pictureBox1.Image);
                        Process.Start(exePath, $"{Definitions.LaunchArguments.SkipImageFileLoading} {args} {Definitions.LaunchArguments.CenterWindowToMouse}");
                    }
                    break;
                case Inputs.InputActions.GetColorAtMousePosition:
                    var currentColor = GetColorAt(Cursor.Position);

                    BackColor = currentColor;

                    var colorHex = ColorTranslator.ToHtml(Color.FromArgb(currentColor.ToArgb()));

                    DisplayMessage(mk.Shift ? $"{currentColor.R}, {currentColor.G}, {currentColor.B}" : colorHex);
                    break;
                case Inputs.InputActions.ToggleImageLock:
                    if (pictureBox1.Image == null)
                    {
                        break;
                    }
                
                    if (lockImage)
                    {
                        lockImage = false;
                        DisplayMessage("Image unlocked");
                    }
                    else
                    {
                        if (fileEntries != null && fileEntries.Length > 0)
                        {
                            lockImage = true;
                            DisplayMessage("Image locked");
                        }
                    }
                    break;
                case Inputs.InputActions.SaveImagePng:
                    SaveCurrentImage(ImageFormat.Png);
                    break;
                case Inputs.InputActions.SaveImageJpg:
                    SaveCurrentImage(ImageFormat.Jpeg);
                    break;
                case Inputs.InputActions.ActivateSnippingTool:
                    ActivateSnippingTool();
                    break;
                case Inputs.InputActions.ToggleTransparency:
                    ToggleTransparentBackground();
                    break;
                case Inputs.InputActions.CloseApplication:
                    Application.Exit();
                    break;
                case Inputs.InputActions.DisplayImageName:
                    if (currentImg.IsFile)
                        DisplayMessage(currentImg.Name);
                    break;
                case Inputs.InputActions.DeleteImage:
                    DeleteImage();
                    break;
                case Inputs.InputActions.MoveToFirstImage:
                    JumpToImage(0);
                    break;
                case Inputs.InputActions.MoveToLastImage:
                    if (fileEntries != null)
                    {
                        JumpToImage(fileEntries.Length - 1);
                    }
                    break;
                case Inputs.InputActions.CopyImagePathAndDataToClipboard:
                    if (!currentImg.IsFile)
                    {
                        return;
                    }

                    DisplayMessage("Image path and window size added to clipboard");
                    Clipboard.SetText($"{currentImg.Path}\\{currentImg.Name} {Top},{Left},{Height},{Width}");
                    break;
                case Inputs.InputActions.StopWindowHover:
                    if (windowHover.Enabled)
                    {
                        windowHover.WindowHoverToken.Cancel();
                    }
                    break;
                case Inputs.InputActions.ZoomIn:
                    ResizeImage(mk.Ctrl ? 1.2 : 1.5);
                    break;
                case Inputs.InputActions.ZoomOut:
                    ResizeImage(mk.Ctrl ? 0.9 : 0.75);
                    break;
                case Inputs.InputActions.SaveTemporaryImage:
                    SaveTempImage(e.KeyCode.ToString());
                    break;
                case Inputs.InputActions.LoadTemporaryImage:
                    LoadTempImage(e.KeyCode.ToString());
                    break;
                // TODO Experimental
                case Inputs.InputActions.AdjustHoverSpeed:
                    var digit = e.KeyCode.ToString();

                    if (digit.Length != 2 || !digit.Contains("D"))
                    {
                        break;
                    }
                    
                    digit = digit.Replace("D", "");
                    
                    var acceptedValues = new[] { "1", "2", "3", "4", "5" };

                    if (!acceptedValues.Contains(digit))
                    {
                        break;
                    }

                    var value = int.Parse(digit);

                    if (windowHover.Enabled)
                    {
                        windowHover.AnimSpeed = value;
                        DisplayMessage($"Hover speed set to {value}");
                    }

                    break;
                case Inputs.InputActions.ShowKeyBinds:
                    var keyBinds = Inputs.GetKeyBinds();
                    
                    var tempFile = Path.GetTempFileName().Replace("tmp", "txt");
                    File.WriteAllLines(tempFile, keyBinds);

                    var process = Process.Start(tempFile);

                    if (process == null)
                    {
                        break;
                    }
                    
                    process.EnableRaisingEvents = true;
                    
                    process.Exited += (s, a) =>
                    {
                        File.Delete(tempFile);
                    };

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void ReloadImages(bool suppressMessage = false)
        {
            fileEntries = ReloadImageFiles();
            UpdateWindowTitle();

            if (fileEntries.Length > 0 && !suppressMessage)
            {
                DisplayMessage("Images reloaded");
            }

            if (currentImg.IsFile)
            {
                LoadNewImgFromFile(new ImageObject(currentImg.FullFilename), false, true);
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            // MouseEventArgs me = (MouseEventArgs)e;
        }

        private void MainWindow_Load(object sender, EventArgs e)
        {
            var cmdArgs = Environment.GetCommandLineArgs();
            
            ProcessLaunchArguments(cmdArgs);
        }
        
        private void MainWindow_DragDrop(object sender, DragEventArgs e)
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);

            if (files == null) return;
            
            var lowerCase = files[0].ToLower();

            if (acceptedExtensions.Contains(Path.GetExtension(lowerCase)))
            {
                rootImageFolderChanged?.Invoke(string.Empty);
                LoadNewImgFromFile(new ImageObject(files[0]));
                return;
            }

            rootImageFolderChanged?.Invoke(files[0]);
            
            if (rootImageFolder == string.Empty)
                return;
            
            currentImg = new ImageObject(files[0] + " \\");
            JumpToImage(0);

        }
        
        private void MainWindow_DragEnter(object sender, DragEventArgs e)
        {
            // Change cursor graphic
            e.Effect = DragDropEffects.Move;
        }
        
        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            var mouseLeftDown = e.Button.ToString() == "Left";
            
            if (mouseLeftDown)
            {
                if ((ModifierKeys & Keys.Alt) == Keys.Alt)
                {
                    if (pictureBox1.Image != null)
                    {
                        DragImageFromApp();
                        return;
                    }
                }

                // Get mouse position for image scroll
                storedMousePosition.Position = Cursor.Position;
            }

            if (WindowState != FormWindowState.Maximized)
            {
                // For moving the window with mouse without ReleaseCapture();
                frameTop = Top;
                frameLeft = Left;
            }

            // Maximize or normalize window
            if (mouseLeftDown && e.Clicks == 2 && (ModifierKeys & Keys.Control) != Keys.Control)
            {
                MaxOrNormalizeWindow();
            }
            else
            {
                // Check if image scrolling should be activated
                // Activates when picturebox is autosized and window is bordered and also when autosized and window is borderless and maximized
                if (pictureBox1.SizeMode == PictureBoxSizeMode.AutoSize && FormBorderStyle == FormBorderStyle.Sizable
                    || pictureBox1.SizeMode == PictureBoxSizeMode.AutoSize && WindowState == FormWindowState.Maximized)
                {
                    // Activate image scrolling
                    // Exits this function and starts the MouseMove function
                    pictureBox1.Cursor = Cursors.SizeAll;
                }
            }
        }
        
        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button.ToString() != "Left")
                return;
            
            if (e.Clicks == 2)
                return;
            
            if (ShouldMovePictureBox())
            {
                MovePictureBox(Definitions.MovementType.MouseDrag, Definitions.Direction.None);
            }
            // Activate window drag
            else
            {
                var useModernWindowDrag = TransparencyKey != BackColor && (ModifierKeys & Keys.Control) != Keys.Control;
                
                if (WindowState == FormWindowState.Maximized)
                {
                    // Check if mouse has moved far enough to activate window drag
                    var resolution = Screen.FromControl(this).Bounds.Size;

                    var x = Math.Abs(Math.Abs(Cursor.Position.X) - Math.Abs(storedMousePosition.X)) > resolution.Width * 0.01;
                    var y = Math.Abs(Math.Abs(Cursor.Position.Y) - Math.Abs(storedMousePosition.Y)) > resolution.Height * 0.01;

                    if (!x && !y)
                    {
                        return;
                    }
                    
                    // TODO This reverts the window position to the position before the window was maximized
                    WindowState = FormWindowState.Normal;

                    // Center window on mouse
                    Location = Cursor.Position;

                    frameTop = Top - ClientSize.Height / 2;
                    frameLeft = Left - ClientSize.Width / 2;
                    
                    Top = frameTop;
                    Left = frameLeft;
                }
                
                if (useModernWindowDrag)
                {
                    // Raw commands for moving window with mouse
                    NativeMethods.ReleaseCapture();
                    NativeMethods.SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
                    return;
                }
                
                // Classic style window drag anywhere to move feature
                // Useful when you don't want window to snap to screen edges
                if (TransparencyKey != BackColor)
                {
                    return;
                }
                
                // Keep border hidden when restoring window
                showBorder = false;
                Location = new Point(Cursor.Position.X - storedMousePosition.X + frameLeft, Cursor.Position.Y - storedMousePosition.Y + frameTop);
            }
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            pictureBox1.Cursor = Cursors.Arrow;
            PositionMessageDisplay();
        }
        
        private void pictureBox1_MouseClick(object sender, MouseEventArgs e)
        {
            switch (e.Button.ToString())
            {
                case "Middle" when FormBorderStyle == FormBorderStyle.None:
                    FormBorderStyle = FormBorderStyle.Sizable;
                    break;
                case "Middle":
                {
                    if (pictureBox1.Image == null)
                    {
                        break;
                    }
                    
                    if (WindowState == FormWindowState.Maximized && FormBorderStyle == FormBorderStyle.Sizable)
                    {
                        // TODO This will overflow the window to other monitors
                        FormBorderStyle = FormBorderStyle.None;
                        showBorder = true;
                        break;
                    }

                    FitImageToWindow();
                    
                    break;
                }
                case "Right" when pictureBox1.Image != null:
                {
                    // Return to autofit image mode
                    if (pictureBox1.SizeMode == PictureBoxSizeMode.AutoSize)
                    {
                        SizeModeZoom();
                    }

                    // Scrolling for large images
                    else if (pictureBox1.Image.Width > Width || pictureBox1.Image.Height > Height)
                    {
                        SizeModeAutoSize();
                    }

                    break;
                }
                // Paste image from clipboard, if picturebox is empty
                case "Right":
                    LoadNewImgFromClipboard();
                    break;
            }
        }
        
        private void MainWindow_Move(object sender, EventArgs e)
        {
            if (FormBorderStyle == FormBorderStyle.None && showBorder)
            {
                FormBorderStyle = FormBorderStyle.Sizable;
            }

            showBorder = false;
        }
        
        private void MainWindow_KeyUp(object sender, KeyEventArgs e)
        {
            var mk = new Inputs.ModifierKeys
            {
                Ctrl = (ModifierKeys & Keys.Control) == Keys.Control,
                Shift = (ModifierKeys & Keys.Shift) == Keys.Shift,
                Alt = (ModifierKeys & Keys.Alt) == Keys.Alt
            };

            var action = Inputs.GetAction(e, mk);
            
            switch (action)
            {
                case Inputs.InputActions.Hover:
                    if (windowHover.StartSet) 
                    { 
                        windowHover.EndX = Location.X;
                        windowHover.StartSet = false;
                    }
                    break;
                
                case Inputs.InputActions.GetColorAtMousePosition:
                    var currentColor = GetColorAt(Cursor.Position);
                    var colorHex = ColorTranslator.ToHtml(Color.FromArgb(currentColor.ToArgb()));
                    
                    // Set chroma key if alt is held
                    if (mk.Alt)
                    { 
                        TransparencyKey = BackColor;
                        DisplayMessage("Chroma key set");
                    }
                    // Get color RGB when shift is being held
                    else if (mk.Shift)
                    {
                        BackColor = Color.FromArgb(28, 28, 28);
                        Clipboard.SetText($"{currentColor.R}, {currentColor.G}, {currentColor.B}");
                        DisplayMessage("Color RGB copied to clipboard");
                    }
                    // Get color hex otherwise
                    else
                    {
                        BackColor = Color.FromArgb(28, 28, 28);
                        Clipboard.SetText(colorHex);
                        DisplayMessage("Color copied to clipboard");
                    }
                    break;
            }
        }


        
        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            /*
            Point loc = new Point(-pictureBox1.Location.X, -pictureBox1.Location.Y);
            TextRenderer.DrawText(e.Graphics, "Test", new Font(this.Font.FontFamily, 60, 0f, GraphicsUnit.Point), Point.Subtract(loc, new Size(-2, -1)), SystemColors.WindowText);
            TextRenderer.DrawText(e.Graphics, "Test", new Font(this.Font.FontFamily, 60, 0f, GraphicsUnit.Point), Point.Subtract(loc, new Size(2,1)), SystemColors.WindowText);
            TextRenderer.DrawText(e.Graphics, "Test", new Font(this.Font.FontFamily, 60, 0f, GraphicsUnit.Point), loc, SystemColors.Window);
            */
        }

        private void MainWindow_ResizeEnd(object sender, EventArgs e)
        {
            // Recenter image when window is being resized
            if (pictureBox1.Image != null && pictureBox1.SizeMode == PictureBoxSizeMode.AutoSize && windowResizeBegin != Size)
            {
                // Only recenter, if empty border is shown
                // TODO This is still buggy, added offset 20 and 40 to prevent image from needlessly centering when scrolled to the corners
                if (pictureBox1.Location.X > 0)
                {
                    CenterImage();
                }
                else if (pictureBox1.Location.X < -pictureBox1.Image.Width + ClientRectangle.Width - 20)
                {
                    CenterImage();
                }
                else if (pictureBox1.Location.Y > 0)
                {
                    CenterImage();
                }
                else if (pictureBox1.Location.Y < -pictureBox1.Image.Height + ClientRectangle.Height - 40)
                {
                    CenterImage();
                }
            }

            PositionMessageDisplay();
            AdjustTextSize("This is a test string");

        }

        private void MainWindow_ResizeBegin(object sender, EventArgs e)
        {
            windowResizeBegin = Size;
        }
        
        private void OnApplicationExit(object sender, EventArgs e)
        {
            var tempFile = Path.GetTempPath() + "/" + "imgBrowserTemp" + randString + ".png";
            var tempFile2 = Path.GetTempPath() + "/" + "TempImage.png";

            // Attempt to delete the temporary files
            try 
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }

                if (File.Exists(tempFile2))
                {
                    File.Delete(tempFile2);
                }
            }
            catch (IOException) 
            { }
        }
        
        private void OnFormWindowStateChanged(FormWindowState previousState)
        {
            if (WindowState == FormWindowState.Normal && storedWindowPosition != Point.Empty)
            {
                var screen = Screen.FromControl(this);
                
                var location = new Point(storedWindowPosition.X + screen.Bounds.Left, storedWindowPosition.Y + screen.Bounds.Top);

                Location = location;
            }

            if (pictureBox1.SizeMode == PictureBoxSizeMode.Zoom)
            {
                return;
            }
            
            CenterImage();
        }
    }
}