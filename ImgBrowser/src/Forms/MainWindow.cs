using System;
using System.IO;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Microsoft.VisualBasic.FileIO;
using System.Threading;
using System.Threading.Tasks;
using SearchOption = System.IO.SearchOption;

// TODO Fix slow gif animations
// TODO Button config
// TODO Randomized slideshow?
// TODO Arrow keys to navigate when zoomed in
// TODO Randomize image array order?
// BUG Image can slightly overfill the screen when in autosize + fullscreen mode
// TODO Scale image to screen?
// TODO Remember rotate position for next image
// TODO Z-index adjust
// BUG Window positioning issues when returning to normal window mode from maximized when initiated by a drag event

namespace ImgBrowser
{
    public partial class MainWindow : Form
    {
        private string[] fileEntries = new string[0]{};

        // Locks current image, so image doesn't change on accidental input
        private bool lockImage = false;

        // ScreenCapButton is being held
        private bool screenCapButtonHeld = false;

        // Stores window size during resizeStart
        private Size windowResizeBegin;

        // Frame position
        private int frameLeft = 0;
        private int frameTop = 0;

        // Picturebox zoomed in location
        private Point zoomLocation = new Point(0, 0);

        // If mouse middle button should restore maximized or normal sized window
        private bool windowNormal = false;

        // If border should reappear when draggin window
        private bool showBorder = false;

        // Keeps track of image flipping
        private bool imageFlipped = false;

        // Keeps track of image rotation
        private int imageRotation = 0;

        // Tracks any edits to the bitmap
        private bool imageEdited = false;
        private readonly string randString = Convert.ToBase64String(Guid.NewGuid().ToByteArray()); // A random string to use with the temp image name

        // When text is shown on the screen
        private bool displayingMessage = false;

        // Stores the folder path if drag and drop is used to open a folder
        private string rootImageFolder = string.Empty;

        private readonly Action<string> rootImageFolderChanged;
        
        private Point storedWindowPosition = Point.Empty;

        // Timer for the text display
        private int textTimer;

        private readonly string[] acceptedExtensions = new[] {".jpg", ".png", ".gif", ".bmp", ".tif", ".svg", ".jfif", ".jpeg" };
        
        // Commands for moving window with mouse
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

        [DllImportAttribute("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImportAttribute("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        public static extern bool MoveWindow(IntPtr hWnd, int x, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll")]
        static extern IntPtr GetActiveWindow();

        [DllImport("gdi32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
        public static extern int BitBlt(IntPtr hDC, int x, int y, int nWidth, int nHeight, IntPtr hSrcDC, int xSrc, int ySrc, int dwRop);

        IntPtr thisWindow;
        
        public class WindowHover
        {
            public bool Enabled { get => Token != null; }
            public int AnimSpeed = 1;
            public int Distance { get => Math.Abs(Math.Abs(StartX) - Math.Abs(EndX));}
            public int StartX = 0;
            public int EndX = 100;
            public bool StartSet = false;

            public CancellationTokenSource Token;
        }

        public class StoredMousePosition
        {
            public int X = 0;
            public int Y = 0;
            public Point Position { get => new Point(X, Y);
                set
                {
                    X = value.X; 
                    Y = value.Y; 
                }
                
            }
        }

        private ImageObject currentImg = new ImageObject("");
        private readonly WindowHover windowHover = new WindowHover(); 

        private StoredMousePosition storedMousePosition = new StoredMousePosition();

        //-------------------------------------------

        public MainWindow()
        {
            InitializeComponent();
            InitializeMessageBox();

            Application.ApplicationExit += new EventHandler(OnApplicationExit);
            rootImageFolderChanged = new Action<string>(OnRootImageFolderChange);
        }

        void InitializeMessageBox()
        {
            // Adjust message label
            messageLabel.Text = "";
            messageLabelShadowBottom.Text = "";
            messageLabelShadowTop.Text = "";

            // Get rid of message label background and attach labels to each other
            messageLabel.Parent = messageLabelShadowTop;
            messageLabelShadowBottom.Parent = pictureBox1;
            messageLabelShadowTop.Parent = messageLabelShadowBottom;

            // Offset from offset
            messageLabel.Location = new Point(-3, -2);
            // Origin
            // Origin
            messageLabelShadowBottom.Location = new Point(11, 8);
            // Offset from origin, not in use, as it makes the font look messy (0,0)
            messageLabelShadowTop.Location = new Point(0, 0);
        }

        private void MainWindow_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Down:
                case Keys.Up:
                    // Enable arrow keys
                    e.IsInputKey = true;
                    break;
            }
        }

        private void MainWindow_MouseWheel(object sender, MouseEventArgs e)
        {
            if (e.Delta > 0)
            {
                if ((ModifierKeys & Keys.Alt) == Keys.Alt)
                {
                    // Increase window size
                    if (WindowState != FormWindowState.Normal) 
                        return;
                    
                    Size = Size.Add(Size, GetAdjustmentValue());
                    if ((pictureBox1.SizeMode != PictureBoxSizeMode.AutoSize) && (FormBorderStyle == FormBorderStyle.None))
                        FitImageToWindow();
                }
                else if (pictureBox1.SizeMode != PictureBoxSizeMode.AutoSize)
                {
                    if ((ModifierKeys & Keys.Control) == Keys.Control)
                        ResizeImage(1.5);
                    else
                        BrowseForward();
                }
                else if (pictureBox1.SizeMode == PictureBoxSizeMode.AutoSize)
                    MovePictureBox(Definitions.MovementType.MouseScroll, Definitions.Direction.Up);

            }
            else if (e.Delta < 0)
            {
                if ((ModifierKeys & Keys.Alt) == Keys.Alt)
                {
                    // Decrease window size
                    if (WindowState != FormWindowState.Normal) 
                        return;
                    
                    Size = Size.Subtract(Size, GetAdjustmentValue());
                    if ((pictureBox1.SizeMode != PictureBoxSizeMode.AutoSize) && (FormBorderStyle == FormBorderStyle.None))
                        FitImageToWindow();
                }
                else if (pictureBox1.SizeMode != PictureBoxSizeMode.AutoSize)
                {
                    if ((ModifierKeys & Keys.Control) == Keys.Control)
                        ResizeImage(0.75);
                    else
                        BrowseBackward();
                }
                else if (pictureBox1.SizeMode == PictureBoxSizeMode.AutoSize)
                    MovePictureBox(Definitions.MovementType.MouseScroll, Definitions.Direction.Down);
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
                case Inputs.InputActions.Hover:
                    // TODO Borderline experimental
                    HoverWindow();
                    break;
                case Inputs.InputActions.AdjustHoverPosition:
                    if (!windowHover.StartSet)
                    {
                        windowHover.StartX = Location.X;
                        windowHover.StartSet = true;
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
                    if (currentImg.HasFile)
                    {
                        Process.Start("explorer.exe", $"/select,\"{currentImg.Path}\\{currentImg.Name}\"");
                    }
                    break;
                case Inputs.InputActions.RefreshImages:
                    fileEntries = ReloadImageFiles();
                    UpdateWindowTitle();
                    
                    if (fileEntries.Length > 0)
                        DisplayMessage("Images reloaded");
                    
                    if (currentImg.HasFile)
                        LoadNewImg(currentImg, false, true);
                    break;
                case Inputs.InputActions.RestoreCurrentImage:
                    if (imageEdited)
                        RestoreImage(true);
                    break;
                case Inputs.InputActions.ToggleFullScreen:
                    MaxOrNormalizeWindow();
                    
                    if (mk.Alt)
                    {
                        // Suppress angry Windows noises
                        // e.SuppressKeyPress = true;                        
                    }
                    
                    break;
                case Inputs.InputActions.CopyToClipboard:
                    if (!mk.Ctrl)
                        break;
                    
                    if (pictureBox1.Image == null)
                        break;
                
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
                        LoadNewImgFromClipboard();
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
                        return;

                    string args = GetCurrentArgs();

                    if (currentImg.HasFile)
                    {
                        Process.Start(exePath, $"\"{currentImg.FullFilename}\" {args} -center");
                    }
                    else if (pictureBox1.Image != null) 
                    {
                        Clipboard.SetImage(pictureBox1.Image);
                        Process.Start(exePath, $"-noImage {args} -center");
                    }
                    break;
                case Inputs.InputActions.GetColorAtMousePosition:
                    Color currentColor = GetColorAt(Cursor.Position);
                    string colorHex;

                    BackColor = currentColor;

                    colorHex = ColorTranslator.ToHtml(Color.FromArgb(currentColor.ToArgb()));

                    DisplayMessage(mk.Shift ? $"{currentColor.R}, {currentColor.G}, {currentColor.B}" : colorHex);
                    break;
                case Inputs.InputActions.ToggleImageLock:
                    if (pictureBox1.Image == null)
                        break;
                
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
                case Inputs.InputActions.SaveImage:
                    SaveCurrentImage();
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
                    if (currentImg.HasFile)
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
                    if (!currentImg.HasFile)
                        return;

                    DisplayMessage("Image path and window size added to clipboard");
                    Clipboard.SetText($"{currentImg.Path}\\{currentImg.Name} {Top},{Left},{Height},{Width}");
                    break;
                case Inputs.InputActions.StopWindowHover:
                    if (windowHover.Enabled)
                        windowHover.Token.Cancel();
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
                        break;
                    
                    digit = digit.Replace("D", "");
                    
                    if (digit != "1" || digit != "2" || digit != "3" || digit != "4" || digit != "5")
                        break;

                    var value = int.Parse(digit);
                    
                    if (windowHover.Enabled)
                        windowHover.AnimSpeed = value;
                    break;
                case Inputs.InputActions.ShowKeyBinds:
                    var keyBinds = Inputs.GetKeyBinds();
                    
                    var tempFile = Path.GetTempFileName().Replace("tmp", "txt");
                    File.WriteAllLines(tempFile, keyBinds);

                    var process = Process.Start(tempFile);
                    
                    if (process == null)
                        break;
                    
                    process.Exited += (s, a) =>
                    {
                        File.Delete(tempFile);
                    };

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private string[] ReloadImageFiles()
        {
            return rootImageFolder != string.Empty ? GetImageFiles(rootImageFolder, true) : GetImageFiles(currentImg.Path);
        }

        private void ActivateSnippingTool()
        {
            if (screenCapButtonHeld)
                return;

            screenCapButtonHeld = true;

            int screenCapPosX = Cursor.Position.X;
            int screenCapPosY = Cursor.Position.Y;
            //pictureBox1.Cursor = Cursors.Cross;

            Form f = new CaptureLayer();
            
            if (screenCapPosX != Cursor.Position.X && screenCapPosY != Cursor.Position.Y)
                DisplayMessage("Selection copied to clipboard");

            Task.Run(async () =>
            {
                await Task.Delay(100);
                screenCapButtonHeld = false;
            });
        }

        private void SaveCurrentImage()
        {
            if (pictureBox1.Image == null) 
                return;
            
            SaveFileDialog saveDialog = new SaveFileDialog
            {
                Filter = "PNG (*.png)|*.png|All files (*.*)|*.*",
                FilterIndex = 0,
                RestoreDirectory = true,
                FileName = currentImg.Name == "" ? "image" : currentImg.Name
            };

            if (saveDialog.ShowDialog() == DialogResult.OK)
                pictureBox1.Image.Save(saveDialog.FileName, ImageFormat.Png);
        }

        // TODO This can make the image transparent as well if the color matches the form's bg
        private void ToggleTransparentBackground()
        {
            if (TransparencyKey != BackColor)
            {
                DisplayMessage("Background hidden");
                TransparencyKey = BackColor;
            }
            else
            {
                DisplayMessage("Background visible");
                TransparencyKey = Control.DefaultBackColor;
            }
        }

        private void ArrowKeyMoveWindow(Definitions.Direction direction, Inputs.ModifierKeys mk)
        {
            var distance = mk.Alt ? 5 : 1;

            switch (direction)
            {
                case Definitions.Direction.Up:
                    Location = Point.Add(Location, new Size(0, -distance));
                    break;
                case Definitions.Direction.Down:
                    Location = Point.Add(Location, new Size(0, distance));
                    break;
                case Definitions.Direction.Left:
                    Location = Point.Add(Location, new Size(-distance, 0));
                    break;
                case Definitions.Direction.Right:
                    Location = Point.Add(Location, new Size(distance, 0));
                    break;
            }
            
        }
        
        private void ArrowKeyAdjustWindowSize(Definitions.Direction direction, Inputs.ModifierKeys mk)
        {
            var modifier = (ModifierKeys & Keys.Alt) == Keys.Alt ? 5 : 1;
                
            switch (direction)
            {
                case Definitions.Direction.Up:
                    Size = Size.Add(Size, new Size(0, -modifier));
                    break;
                case Definitions.Direction.Down:
                    Size = Size.Add(Size, new Size(0, modifier));
                    break;
                case Definitions.Direction.Left:
                    Size = Size.Add(Size, new Size(-modifier, 0));
                    break;
                case Definitions.Direction.Right:
                    Size = Size.Add(Size, new Size(modifier, 0));
                    break;
            }
                
            if (pictureBox1.SizeMode == PictureBoxSizeMode.AutoSize)
                CenterImage(false);
        }
        
        private void ArrowKeyMoveImageOrBrowseImages(Definitions.Direction direction)
        {
            if (pictureBox1.SizeMode == PictureBoxSizeMode.AutoSize)
            {
                MovePictureBox(Definitions.MovementType.Keyboard, direction);
                return;
            }

            switch (direction)
            {
                case Definitions.Direction.Left:
                    BrowseBackward();
                    break;
                case Definitions.Direction.Right:
                    BrowseForward();
                    break;
                case Definitions.Direction.Up:
                    break;
                case Definitions.Direction.Down:
                    break;
                case Definitions.Direction.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
        }

        private void DeleteImage()
        {
            if (!currentImg.HasFile || pictureBox1.Image == null) return;
            
            // Get info from the image that is going to be deleted
            string delImgPath = currentImg.Path;
            string delImgName = currentImg.Name;

            lockImage = false;

            // Move to next image, so picturebox won't keep it locked
            // This also keeps the file indexes working, otherwise index will be 0 after deletion
            BrowseForward();

            // Remove image from picturebox, if it is the only image in the folder
            if (currentImg.Name == delImgName)
            {
                Image img = pictureBox1.Image;
                pictureBox1.Image = null;
                img.Dispose();
                currentImg = new ImageObject("");
            }

            try
            {
                FileSystem.DeleteFile(delImgPath + "\\" + delImgName, UIOption.AllDialogs, RecycleOption.SendToRecycleBin);
                fileEntries = ReloadImageFiles();
                DisplayMessage("Image moved to recycle bin");
            }
            catch (OperationCanceledException)
            {
                LoadNewImg(currentImg);
            }
        }

        private void DisplayMessage<T>(T message) => DisplayMessage(message.ToString());

        private async void DisplayMessage(string text)
        {
            AdjustTextSize(text);

            messageLabel.Text = text;
            messageLabelShadowBottom.Text = text;
            messageLabelShadowTop.Text = text;

            if (!displayingMessage) {
                displayingMessage = true;
                while (textTimer < 3)
                {
                    await Task.Delay(500);
                    textTimer += 1;
                }

                messageLabel.Text = "";
                messageLabelShadowBottom.Text = "";
                messageLabelShadowTop.Text = "";
                
                displayingMessage = false;
                textTimer = 0;

                return;

            }
            else
            {
                textTimer = 0;
                return;
            }
        }

        private void BrowseForward()
        {
            LoadNewImg(GetNextImageFilename(Definitions.Direction.Right), false, true);
        }

        private void BrowseBackward()
        {
            LoadNewImg(GetNextImageFilename(Definitions.Direction.Left), false, true);
        }

        ImageObject GetNextImageFilename(Definitions.Direction direction)
        {
            if (fileEntries.Length < 2 || currentImg.Path == "" || lockImage)
                return new ImageObject("");

            string file = "";
            int index = 0;

            switch (direction)
            {
                case Definitions.Direction.Right:
                    index = Array.IndexOf(fileEntries, currentImg.FullFilename);

                    file = index + 1 < fileEntries.Length ? fileEntries[index + 1] : fileEntries[0];
                    break;
                
                case Definitions.Direction.Left:
                    index = Array.IndexOf(fileEntries, currentImg.FullFilename);

                    file = index - 1 >= 0 ? fileEntries[index - 1] : fileEntries[fileEntries.Length - 1];
                    break;
            }

            return new ImageObject(file);

        }
        private void JumpToImage(int index)
        {
            if (!lockImage && currentImg.Path != "")
                LoadNewImg(new ImageObject(fileEntries[index]), false, true);
        }

        private void FitImageToWindow()
        {
            if (pictureBox1.Image == null)
                return;
            
            FormBorderStyle = FormBorderStyle.None;
            
            if (pictureBox1.SizeMode != PictureBoxSizeMode.Zoom) 
                return;

            // Image aspect ratio
            double aspectRatio = (double)pictureBox1.Image.Width / (double)pictureBox1.Image.Height;
                    
            // Window frame aspect ratio
            double windowAspectRatio = (double)ClientSize.Width / (double)ClientSize.Height;

            int tempHeight = Height;

            // Adjust frame size when there's a big difference between the image and frame aspect ratios
            // This prevent images from getting too large when readjusting frame size to the image
            if (windowAspectRatio + 2 < aspectRatio)
                while (windowAspectRatio + 2 < aspectRatio)
                {
                    tempHeight = (int)(tempHeight * 0.95f);
                    windowAspectRatio = (double)ClientSize.Width / (double)tempHeight;
                }
            else if (aspectRatio + 2 < windowAspectRatio)
                while (aspectRatio + 2 < windowAspectRatio)
                {
                    tempHeight = (int)(tempHeight * 1.05f);
                    windowAspectRatio = (double)ClientSize.Width / (double)tempHeight;
                }

            Height = tempHeight;

            // Set frame size to match the image aspect ratio
            Size = new Size((int)(aspectRatio * Size.Height), Size.Height);

        }

        private void ToggleAlwaysOnTop()
        {
            DisplayMessage($"Stay on Top: {!TopMost}");
            TopMost = !TopMost;
        }

        private void AdjustTextSize(string text)
        {
            // Make sure the string fits the frame
            int stringWidth = TextRenderer.MeasureText(text, messageLabel.Font).Width;

            while (stringWidth + 12 > ClientSize.Width)
            {
                if (messageLabel.Font.Size - 1 <= 0) { break; }
                messageLabel.Font = new Font(messageLabel.Font.FontFamily, messageLabel.Font.Size - 1, FontStyle.Bold);
                stringWidth = TextRenderer.MeasureText(text, messageLabel.Font).Width;
            }

            while ((stringWidth - 12) * 2.8 < ClientSize.Width)
            {
                if (messageLabel.Font.Size >= 22) { break; }
                messageLabel.Font = new Font(messageLabel.Font.FontFamily, messageLabel.Font.Size + 1, FontStyle.Bold);
                stringWidth = TextRenderer.MeasureText(text, messageLabel.Font).Width;
            }

            messageLabelShadowBottom.Font = new Font(messageLabel.Font.FontFamily, messageLabel.Font.Size, FontStyle.Bold);
            messageLabelShadowTop.Font = new Font(messageLabel.Font.FontFamily, messageLabel.Font.Size, FontStyle.Bold);
        }

        private void FlipImageX(bool ignoreCurrentViewport = false)
        {
            Image img = pictureBox1.Image;

            img.RotateFlip(RotateFlipType.RotateNoneFlipX);
            pictureBox1.Image = img;

            bool imageAutoSizeMode = pictureBox1.SizeMode == PictureBoxSizeMode.AutoSize;

            if (!imageFlipped)
            {
                // Flip image only based on current viewport, unless ctrl is held
                if (imageAutoSizeMode && !ignoreCurrentViewport && WindowState != FormWindowState.Maximized && pictureBox1.Image.Width >= ClientSize.Width)
                {
                    pictureBox1.Location = new Point(-pictureBox1.Image.Width + ClientSize.Width + Math.Abs(pictureBox1.Location.X), pictureBox1.Location.Y);
                    PositionMessageDisplay();
                }
                imageFlipped = true;
            }
            else
            {
                if (imageAutoSizeMode && !ignoreCurrentViewport && WindowState != FormWindowState.Maximized && pictureBox1.Image.Width >= ClientSize.Width)
                {
                    pictureBox1.Location = new Point(0 - pictureBox1.Image.Width - pictureBox1.Location.X + ClientSize.Width, pictureBox1.Location.Y);
                    PositionMessageDisplay();
                }
                imageFlipped = false;
            }
        }

        private void RotateImage(bool CCW = false)
        {
            Image img = pictureBox1.Image;

            int x = pictureBox1.Location.X;
            int y = pictureBox1.Location.Y;
            Point rotate;

            // Check that the image is larger than the current window size
            bool sizeVsFrame = pictureBox1.Image.Width >= ClientSize.Width && pictureBox1.Image.Height >= ClientSize.Height;

            if (CCW) { 
                img.RotateFlip(RotateFlipType.Rotate270FlipNone);
                imageRotation = imageRotation <= 0 ? 3 : imageRotation - 1;

                if (pictureBox1.SizeMode == PictureBoxSizeMode.AutoSize && WindowState != FormWindowState.Maximized && sizeVsFrame) { 
                    ClientSize = new Size(ClientSize.Height, ClientSize.Width);
                    rotate = new Point(0 - Math.Abs(y), -pictureBox1.Image.Height + ClientSize.Height + Math.Abs(x));
                    pictureBox1.Location = rotate;
                }
            }
            else { 
                img.RotateFlip(RotateFlipType.Rotate90FlipNone);
                imageRotation = imageRotation >= 3 ? 0 : imageRotation + 1;

                if (pictureBox1.SizeMode == PictureBoxSizeMode.AutoSize && WindowState != FormWindowState.Maximized && sizeVsFrame)
                {
                    ClientSize = new Size(ClientSize.Height, ClientSize.Width);
                    rotate = new Point(-pictureBox1.Image.Width + ClientSize.Width + Math.Abs(y), 0 - Math.Abs(x));
                    pictureBox1.Location = rotate;
                }
            }

            if(pictureBox1.SizeMode == PictureBoxSizeMode.Zoom || WindowState == FormWindowState.Maximized || !sizeVsFrame) {
                CenterImage();
                zoomLocation = new Point(0, 0);
            }

            pictureBox1.Image = img;
        }

        private void SaveTempImage(string ordinalValue)
        {
            if (pictureBox1.Image == null) 
                return;
            
            ordinalValue = NumericInputToNumber(ordinalValue);
            
            if (ordinalValue == null)
                return;

            DisplayMessage(!SaveImageToTemp(ordinalValue) ? "Unable to save image" : $"Saved to temp {ordinalValue}");
        }

        private void LoadTempImage(string ordinalValue)
        {
            ordinalValue = NumericInputToNumber(ordinalValue);
            
            if (ordinalValue == null)
                return;
            
            DisplayMessage(!LoadImageFromTemp(ordinalValue) ? "No temp image found" : "Temp image loaded");
        }
        
        private bool SaveImageToTemp(string ordinalValue, bool overrideName = false)
        {
            if (pictureBox1.Image is null)
                return false;
            
            try { 
                string tempPath = Path.GetTempPath();
                string tempName;

                if (!overrideName)
                    tempName = "imgBrowserTemp" + ordinalValue + ".png";
                else
                    tempName = ordinalValue + ".png";

                pictureBox1.Image.Save(tempPath + "\\" + tempName, ImageFormat.Png);

                return true;
            }
            // This occurs when trying to rewrite a currently opened image
            catch (ExternalException)
            {  
                return false;
            }

        }
        
        private bool LoadImageFromTemp(string ordinalValue)
        {
            string tempPath = Path.GetTempPath();
            string tempName = "imgBrowserTemp" + ordinalValue + ".png";

            if (!File.Exists(tempPath + "//" + tempName))
                return false;
            
            LoadNewImg(new ImageObject(tempPath + "//" + tempName), true);
            //lockImage = true;
            return true;
            
        }

        /// <summary>
        /// Convert numeric keyboard inputs to numbers (D0-D9)
        /// </summary>
        /// <param name="ordinalValue"></param>
        /// <returns></returns>
        private string NumericInputToNumber(string ordinalValue)
        {
            if (ordinalValue.Length != 2 || !ordinalValue.Contains("D"))
                return null;
                    
            return ordinalValue.Replace("D", "0");
        }
        
        private void UpdateWindowTitle()
        {
            string name = currentImg.Name != "" ? $"{currentImg.Name}" : "Image";
            string size = pictureBox1.Image != null ? $"{pictureBox1.Image.Width} x {pictureBox1.Image.Height}" : "";

            string position = $"";

            if (fileEntries.Length > 0) { 
                int index = Array.IndexOf(fileEntries, currentImg.FullFilename);
                position = $" - {index + 1} / {fileEntries.Length}";
            }

            Text = $"ImgBrowser - {name} - {size}{position}";
        }

        private string[] GetImageFiles(string path, bool allDirectories = false)
        {
            if (path == "" || path == Path.GetTempPath() || !Directory.Exists(path)) 
                return Array.Empty<string>();
            
            IEnumerable<string> files = Directory.EnumerateFiles(path + "\\", "*.*", allDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                .Where(s => acceptedExtensions.Contains(Path.GetExtension(s).ToLowerInvariant()));

            files = files.OrderBy(s => s.Length).ThenBy(s => s);

            return files.ToArray();

        }

        // Used to determine the window quick size adjustment type
        private Size GetAdjustmentValue()
        {
            Size adjustmentValue;

            if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
                adjustmentValue = new Size(1, 0);
            else if ((Control.ModifierKeys & Keys.Shift) == Keys.Shift)
                adjustmentValue = new Size(0, 1);
            else
                adjustmentValue = new Size(1, 1);

            return adjustmentValue;
        }

        void HoverWindow()
        {
            if (windowHover.Enabled)
            {
                windowHover.Token.Cancel();
                return;
            }

            thisWindow = GetActiveWindow();
            //Point originalPos = Location;

            windowHover.Token = new CancellationTokenSource();
            CancellationToken ct = windowHover.Token.Token;

            Task.Run(() =>
            {
                try
                {
                    for (; ; )
                    {
                        for (int i = 0; i < windowHover.Distance / windowHover.AnimSpeed; i++)
                        {
                            if (i < windowHover.Distance / 2 / windowHover.AnimSpeed)
                                MoveWindow(thisWindow, Location.X + (1 * windowHover.AnimSpeed), Location.Y, Width, Height, false);
                            else
                                MoveWindow(thisWindow, Location.X - (1 * windowHover.AnimSpeed), Location.Y, Width, Height, false);

                            if (ct.IsCancellationRequested)
                                ct.ThrowIfCancellationRequested();

                            Thread.Sleep(2);
                        }
                    }

                    // token.Dispose();
                    // token = null;
                }
                catch (OperationCanceledException)
                {
                    //MoveWindow(thisWindow, originalPos.X, originalPos.Y, Width, Height, false);
                }
                finally
                {
                    windowHover.Token.Dispose();
                    windowHover.Token = null;
                }

            }, windowHover.Token.Token);
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            // MouseEventArgs me = (MouseEventArgs)e;
        }

        private void MainWindow_Load(object sender, EventArgs e)
        {
            var cmdArgs = Environment.GetCommandLineArgs();

            foreach (string i in cmdArgs)
                Console.WriteLine(i);

            int argsLength = cmdArgs.Length;

            if (argsLength > 1 && cmdArgs[1] != "-noImage")
            {
                rootImageFolderChanged?.Invoke(string.Empty);
                LoadNewImg(new ImageObject(cmdArgs[1]));
            }
            else
                LoadNewImgFromClipboard();
            
            // Process other arguments
            for (int i = 0; i < cmdArgs.Length; i++)
            {
                switch (cmdArgs[i])
                {
                    // Center to mouse
                    case "-center":
                        Top = Cursor.Position.Y - ClientSize.Height / 2;
                        Left = Cursor.Position.X - ClientSize.Width / 2;
                        break;
                    // Window width and height
                    case "-size":
                        string[] sizeValues = cmdArgs[i + 1].Split(',');

                        if (sizeValues.Length != 2)
                            return;

                        if (!int.TryParse(sizeValues[0], out int formWidth) || !int.TryParse(sizeValues[1], out int formHeight))
                            return;

                        if (formWidth == 0 || formHeight == 0)
                            return;

                        ClientSize = new Size(formWidth, formHeight);
                        break;
                    // Window position
                    case "-position":
                        string[] posValues = cmdArgs[i + 1].Split(',');

                        if (posValues.Length != 2)
                            return;

                        if (!int.TryParse(posValues[0], out int posLeft) || !int.TryParse(posValues[1], out int posTop))
                            return;

                        Top = posTop;
                        Left = posLeft;
                        break;
                    // Enable transparency
                    case "-transparent":
                        TransparencyKey = BackColor;
                        break;
                    // Rotation
                    case "-rotate":
                        if (int.TryParse(cmdArgs[i + 1], out int direction)){
                            if (direction > 3)
                                break;
                        
                            for (int x = 0; x < direction; x++)
                            {
                                RotateImage();
                            }
                        }
                        break;
                    // FlipX
                    case "-flip":
                        FlipImageX();
                        break;
                    //Setting for fitting the image to window
                    case "-borderless":
                        if (pictureBox1.Image != null)
                            FitImageToWindow();
                        break;
                    // Lock image
                    case "-lock":
                        lockImage = true;
                        break;
                    // Always on top
                    case "-topmost":
                        TopMost = true;
                        break;
                    default:
                        break;
                }
            }
        }

        // Gets the current application settings and converts them into launch arguments
        private string GetCurrentArgs()
        {
            string args = "";

            if (TransparencyKey == BackColor)
                args += "-transparent ";
            if (lockImage)
                args += "-lock ";
            if (FormBorderStyle == FormBorderStyle.None)
                args += "-borderless ";
            if (TopMost)
                args += "-topmost ";
            if (imageFlipped)
                args += $"-flip ";

            args += $"-rotate {imageRotation} ";
            args += $"-size {ClientSize.Width},{ClientSize.Height} ";

            return args;
        }

        private Color GetColorAt(Point location)
        {

            Bitmap screenPixel = new Bitmap(1, 1, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            using (Graphics gdest = Graphics.FromImage(screenPixel))
            {
                using (Graphics gsrc = Graphics.FromHwnd(IntPtr.Zero))
                {
                    IntPtr hSrcDC = gsrc.GetHdc();
                    IntPtr hDC = gdest.GetHdc();
                    int retval = BitBlt(hDC, 0, 0, 1, 1, hSrcDC, location.X, location.Y, (int)CopyPixelOperation.SourceCopy);
                    gdest.ReleaseHdc();
                    gsrc.ReleaseHdc();
                }
            }

            Color grabbedColor = screenPixel.GetPixel(0, 0);
            screenPixel.Dispose();

            return grabbedColor;
        }

        void ResizeImage(double multiplier)
        {
            if (pictureBox1.Image == null) 
                return;
            
            // Make a backup of the current image
            if (!imageEdited)
            {
                if (currentImg.Path == "")
                    SaveImageToTemp(randString);
                imageEdited = true;
            }
            
            bool zoomOut = multiplier < 1;
            
            // Perform a rough image size check to avoid memory issues
            if (pictureBox1.Image.Width * multiplier + pictureBox1.Image.Height * multiplier > 40000)
            {
                DisplayMessage("Image too large to resize");
                return;
            }
            
            // Do not zoom out if it makes image smaller than screen
            if (zoomOut && pictureBox1.Image.Width * multiplier < Width && pictureBox1.Image.Height * multiplier < Height)
            {
                RestoreImage(false);
                return;
            }

            //https://stackoverflow.com/questions/1922040/how-to-resize-an-image-c-sharp

            // Grab current image
            Image img = pictureBox1.Image;
            Bitmap resized = new Bitmap(1, 1);

            // Creating a new resized bitmap
            try
            {
                resized = new Bitmap(img, Convert.ToInt32(img.Width * multiplier), Convert.ToInt32(img.Height * multiplier));
            }
            // Catch out of memory exceptions
            // TODO This doesn't actually free up memory correctly, so it will eventually cause issues
            catch (ArgumentException)
            {
                pictureBox1.Image = null;
                img.Dispose();
                resized.Dispose();
                pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
                pictureBox1.Dock = DockStyle.Fill;
                return;
            }
            catch (OutOfMemoryException)
            {
                pictureBox1.Image = null;
                img.Dispose();
                resized.Dispose();
                pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
                pictureBox1.Dock = DockStyle.Fill;
                return;
            }
            // This will rescale the image. Optional, but makes it look better
            // Do not rescale images that are over 10 000 pixels, as it will cause memory and performance issues
            if (img.Width + img.Height < 10000)
            {
                using (Graphics grap = Graphics.FromImage(resized))
                {
                    grap.CompositingMode = CompositingMode.SourceCopy;
                    grap.CompositingQuality = CompositingQuality.HighQuality;
                    grap.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    //grap.SmoothingMode = SmoothingMode.HighQuality;
                    grap.PixelOffsetMode = PixelOffsetMode.HighQuality;

                    // This draws the new image on top of the bitmap
                    grap.DrawImage(img, 0, 0, Convert.ToInt32(img.Width * multiplier), Convert.ToInt32(img.Height * multiplier));
                }
            }
            
            // Calculate new scroll position 
            double posX = (double)(pictureBox1.Location.X * multiplier);
            double posY = (double)(pictureBox1.Location.Y * multiplier);
            if (posX > 0) posX = 0;
            if (posY > 0) posY = 0;
            
            if (zoomOut){
                // Check that image stays within the borders
                if (posX < -resized.Width + Width) posX = -resized.Width + Width;
                if (posY < -resized.Height + Height) posY = -resized.Height + Height;
            }

            pictureBox1.SizeMode = PictureBoxSizeMode.AutoSize;
            pictureBox1.Dock = DockStyle.None;
            pictureBox1.Image = resized;

            img.Dispose();
            CenterImage();

            // Set scroll if image fills the screen
            if (pictureBox1.Image.Width > Width) pictureBox1.Location = new Point((int)posX, pictureBox1.Location.Y);
            if (pictureBox1.Image.Height > Height) pictureBox1.Location = new Point(pictureBox1.Location.X, (int)posY);
        }

        // Restore unedited image from file or from the temp folder
        private void RestoreImage(bool showMessage = true)
        {
            if (currentImg.HasFile)
                LoadNewImg(new ImageObject(currentImg.FullFilename));
            else
                LoadImageFromTemp(randString);

            if (showMessage)
                DisplayMessage("Image restored");
        }

        private void MainWindow_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            if (files == null) return;
            
            string lowerCase = files[0].ToLower();

            if (acceptedExtensions.Contains(Path.GetExtension(lowerCase)))
            {
                rootImageFolderChanged?.Invoke(string.Empty);
                LoadNewImg(new ImageObject(files[0]));
                return;
            }

            rootImageFolderChanged?.Invoke(files[0]);
            
            if (rootImageFolder == string.Empty)
                return;
            
            currentImg = new ImageObject(files[0] + " \\");
            JumpToImage(0);

        }

        private void LoadNewImgFromClipboard()
        {
            Image clipImg;

            try
            {
                clipImg = GetAlphaImageFromClipboard();
            }
            catch (ExternalException e)
            {
                DisplayMessage("Could not load image from clipboard");
                Console.WriteLine(e);
                return;
            }

            if (clipImg == null)
                return;

            Image oldImg = null;

            if (pictureBox1.Image != null)
                oldImg = pictureBox1.Image;

            pictureBox1.Image = clipImg;
            currentImg = new ImageObject("");

            pictureBox1.Location = new Point(0, 0);
            SizeModeZoom();

            if (oldImg != null) { oldImg.Dispose(); }

            rootImageFolderChanged?.Invoke(string.Empty);
            fileEntries = Array.Empty<string>();
            
            UpdateWindowTitle();

            ResetImageModifiers();

        }
        
        private void LoadNewImg(ImageObject imgObj, bool removeImagePath = false, bool skipRefresh = false)
        {
            bool imageError = false;

            if (imgObj.Name == "" || !imgObj.HasFile)
                return;

            if (imgObj.ImageData == null) { 
                DisplayImageError();
                imageError = true;
            }

            currentImg = imgObj;

            if (pictureBox1.Image != null)
            {
                Image oldImg = pictureBox1.Image;
                pictureBox1.Image = null;
                oldImg.Dispose();
            }

            if (!imageError) { 
                // Separate handling for gif files to make the animation work
                if (!currentImg.Name.EndsWith(".gif"))
                {
                    // This way files won't be locked to the application
                    pictureBox1.Image = imgObj.ImageData;
                }
                else {
                    imgObj.ImageData.Dispose();
                    pictureBox1.Image = new Bitmap(currentImg.FullFilename); // This locks the image to the application
                }
            }

            if (removeImagePath) {
                currentImg.FullFilename = "";
            }

            if (!skipRefresh)
            {
                var files = ReloadImageFiles();

                if (files.Length > 0)
                    fileEntries = files;
            }

            UpdateWindowTitle();

            // Reset zoomed in position
            pictureBox1.Location = new Point(0, 0);
            lockImage = false;

            ResetImageModifiers();

            CenterImage();

            SizeModeZoom();

            PositionMessageDisplay();

            GC.Collect();
        }

        private void ResetImageModifiers()
        {
            // Reset image modifiers
            imageFlipped = false;
            imageEdited = false;
        }

        private void DisplayImageError()
        {
            if (pictureBox1.Image != null)
            {
                Image oldImg = pictureBox1.Image;
                pictureBox1.Image = null;
                oldImg.Dispose();
            }

            DisplayMessage("Unable to load image");
        }

        private void CopyCurrentDisplayToClipboard()
        {
            if (pictureBox1.SizeMode != PictureBoxSizeMode.AutoSize)
                return;

            if (pictureBox1.Image.Width < ClientSize.Width || pictureBox1.Image.Height < ClientSize.Height)
                return;

            // Make a backup of the current image
            if (!imageEdited) { 
                SaveImageToTemp(randString);
                imageEdited = true;
            }

            Bitmap bm = (Bitmap)pictureBox1.Image;
            bm = bm.Clone(new Rectangle(Math.Abs(pictureBox1.Location.X), Math.Abs(pictureBox1.Location.Y), ClientSize.Width, ClientSize.Height), System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            Clipboard.SetImage((Image)bm);
            bm.Dispose();

            DisplayMessage("Current display copied to clipboard.");
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
                    if (pictureBox1.Image != null)
                    {
                        DragImageFromApp();
                        return;
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
                if ((pictureBox1.SizeMode == PictureBoxSizeMode.AutoSize) && (FormBorderStyle == FormBorderStyle.Sizable)
                    || (pictureBox1.SizeMode == PictureBoxSizeMode.AutoSize) && (WindowState == FormWindowState.Maximized))
                {
                    // Activate image scrolling
                    // Exits this function and starts the MouseMove function
                    pictureBox1.Cursor = Cursors.SizeAll;
                }
                
                else
                {
                }
            }

        }

        // Extra effort to support image drag to other applications
        void DragImageFromApp()
        {
            string file;

            if (!currentImg.HasFile)
            {
                SaveImageToTemp("TempImage", true); // Create temp image with clean name
                file = Path.GetTempPath() + "/" + "TempImage.png";
            }
            else
                file = currentImg.FullFilename;

            if (!File.Exists(file))
                return;

            DataObject dataObject = new DataObject();
            DragFileInfo filesInfo = new DragFileInfo(file);

            using (MemoryStream infoStream = GetFileDescriptor(filesInfo),
                                contentStream = GetFileContents(filesInfo))
            {
                dataObject.SetData(CFSTR_FILEDESCRIPTORW, infoStream);
                dataObject.SetData(CFSTR_FILECONTENTS, contentStream);
                dataObject.SetData(CFSTR_PERFORMEDDROPEFFECT, null);

                DoDragDrop(dataObject, DragDropEffects.All);
            }
        }

        private void MaxOrNormalizeWindow()
        {

            if (WindowState == FormWindowState.Maximized)
            {
                if (FormBorderStyle == FormBorderStyle.None)
                {
                    if (showBorder)
                        FormBorderStyle = FormBorderStyle.Sizable;
                    
                    // Reset picturebox style, when returning from full screen
                    pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
                    pictureBox1.Dock = DockStyle.Fill;
                    CenterImage();

                    WindowState = windowNormal ? FormWindowState.Normal : FormWindowState.Maximized;

                }
                else
                {
                    FormBorderStyle = FormBorderStyle.None;
                    windowNormal = false;
                    
                    // Restore border if window is dragged from full screen
                    showBorder = true;
                }
            }
            else
            {
                pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
                pictureBox1.Dock = DockStyle.Fill;

                FormBorderStyle = FormBorderStyle.None;
                WindowState = FormWindowState.Maximized;
                windowNormal = true;
                // Restore border if window is dragged from full screen
                showBorder = true;
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
                var useModernWindowDrag =
                    TransparencyKey != BackColor && (ModifierKeys & Keys.Control) != Keys.Control;
                
                if (WindowState == FormWindowState.Maximized)
                {
                    // Check if mouse has moved far enough to activate window drag
                    var resolution = Screen.FromControl(this).Bounds.Size;

                    var x = Math.Abs(Math.Abs(Cursor.Position.X) - Math.Abs(storedMousePosition.X)) > resolution.Width * 0.01;
                    var y = Math.Abs(Math.Abs(Cursor.Position.Y) - Math.Abs(storedMousePosition.Y)) > resolution.Height * 0.01;
                    
                    if (!x && !y)
                        return;
                    
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
                    ReleaseCapture();
                    SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
                    return;
                }
                
                // Classic style window drag anywhere to move feature
                // Useful when you don't want window to snap to screen edges
                if (TransparencyKey != BackColor) 
                    return;
                
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
                        break;
                    
                    if (WindowState == FormWindowState.Maximized && FormBorderStyle == FormBorderStyle.Sizable)
                    {
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

        private bool ShouldMovePictureBox()
        {
            if(pictureBox1.Image == null)
                return false;
            
            var autoSizeMode = pictureBox1.SizeMode == PictureBoxSizeMode.AutoSize;

            return autoSizeMode && FormBorderStyle == FormBorderStyle.Sizable || 
                   autoSizeMode && WindowState == FormWindowState.Maximized ||
                   autoSizeMode && (ModifierKeys & Keys.Control) == Keys.Control;
        }
        
        private void SizeModeZoom()
        {
            if (pictureBox1.Image == null)
                return;

            int x = 0;
            int y = 0;

            // Update current zoomed in position
            if (pictureBox1.Image.Width > ClientSize.Width && pictureBox1.Location.X < 0)
                x = pictureBox1.Location.X;

            if (pictureBox1.Image.Height > ClientSize.Height && pictureBox1.Location.Y < 0)
                y = pictureBox1.Location.Y;

            zoomLocation = new Point(x, y);

            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.Dock = DockStyle.Fill;

        }

        private void SizeModeAutoSize()
        {
            pictureBox1.SizeMode = PictureBoxSizeMode.AutoSize;
            pictureBox1.Dock = DockStyle.None;

            CenterImage();

            if (zoomLocation.X < -1 && zoomLocation.Y < -1)
                pictureBox1.Location = zoomLocation;

        }

        private void MainWindow_Move(object sender, EventArgs e)
        {
            if (FormBorderStyle == FormBorderStyle.None && showBorder)
                FormBorderStyle = FormBorderStyle.Sizable;

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
                    if (windowHover.StartSet) { 
                        windowHover.EndX = Location.X;
                        windowHover.StartSet = false;
                    }
                    break;
                
                case Inputs.InputActions.GetColorAtMousePosition:
                    var currentColor = GetColorAt(Cursor.Position);
                    var colorHex = ColorTranslator.ToHtml(Color.FromArgb(currentColor.ToArgb()));

                    // TODO This is probably pointless
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
                default:
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
            if ((pictureBox1.Image != null) && (pictureBox1.SizeMode == PictureBoxSizeMode.AutoSize) && (windowResizeBegin != Size))
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

        // Set the display message position relative to the picturebox
        private void PositionMessageDisplay() { 
            if (pictureBox1.Width >= Width && pictureBox1.Height >= Height)
                messageLabelShadowBottom.Location = new Point(11 + Math.Abs(pictureBox1.Location.X), 3 + Math.Abs(pictureBox1.Location.Y));
            else if (pictureBox1.Width >= Width)
                messageLabelShadowBottom.Location = new Point(11 + Math.Abs(pictureBox1.Location.X), messageLabelShadowBottom.Location.Y);
            else if (pictureBox1.Height >= Height)
                messageLabelShadowBottom.Location = new Point(messageLabelShadowBottom.Location.X, 3 + Math.Abs(pictureBox1.Location.Y));
            else
                messageLabelShadowBottom.Location = new Point(11, 3);
        }

        private void OnRootImageFolderChange(string folderName)
        {
            rootImageFolder = string.Empty;
            
            if (string.IsNullOrEmpty(folderName))
            {
                return;
            }
            
            if (!Directory.Exists(folderName))
            {
                return;
            }
            
            rootImageFolder = folderName;
            var files = ReloadImageFiles();
            
            if (files.Length <= 0)
            {
                DisplayMessage("No images found");
                rootImageFolder = string.Empty;
                return;
            }
            
            fileEntries = files;
        }
        
        private void OnApplicationExit(object sender, EventArgs e)
        {
            string tempFile = Path.GetTempPath() + "/" + "imgBrowserTemp" + randString + ".png";
            string tempFile2 = Path.GetTempPath() + "/" + "TempImage.png";

            // Attempt to delete the temporary files
            try 
            { 
                if (File.Exists(tempFile))
                    File.Delete(tempFile);

                if (File.Exists(tempFile2))
                    File.Delete(tempFile2);
            }
            catch (IOException) {
            }
        }

        // Catches window events for processing
        protected override void WndProc(ref Message m)
        {
            FormWindowState org = WindowState;
            var location = Location;
            var screen = Screen.FromControl(this);

            base.WndProc(ref m);

            if (WindowState != org)
            {
                if (org == FormWindowState.Normal)
                {
                    storedWindowPosition = new Point(location.X - screen.Bounds.Left,
                        location.Y - screen.Bounds.Top);
                }
                
                OnFormWindowStateChanged(org);
            }
        }

        private void OnFormWindowStateChanged(FormWindowState previousState)
        {
            if (WindowState == FormWindowState.Normal && storedWindowPosition != Point.Empty)
            {
                var screen = Screen.FromControl(this);
                
                var location = new Point(storedWindowPosition.X + screen.Bounds.Left, storedWindowPosition.Y + screen.Bounds.Top);

                Location = location;
            }

            if(pictureBox1.SizeMode == PictureBoxSizeMode.Zoom)
                return;
            
            CenterImage();
        }

        // http://ostack.cn/?qa=95752/
        // Get the real image from clipboard (this supports the alpha channel)
        private Image GetAlphaImageFromClipboard()
        {
            if (Clipboard.GetDataObject() == null) return null;
            
            if (!Clipboard.GetDataObject().GetDataPresent(DataFormats.Dib))
                return Clipboard.ContainsImage() ? Clipboard.GetImage() : null;

            int byteOffset = 52; // Offset of the DIB bits. Different software use different offsets when copying image to clipboard
            
            // Sometimes getting the image data fails and results in a "System.NullReferenceException" error - probably because clipboard handling also can be messy and complex
            byte[] dib;
            try
            {
                dib = ((System.IO.MemoryStream)Clipboard.GetData(DataFormats.Dib)).ToArray();
                byteOffset = VerifyByteOffset(dib, byteOffset);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return Clipboard.ContainsImage() ? Clipboard.GetImage() : null;
            }

            var width = BitConverter.ToInt32(dib, 4);
            var height = BitConverter.ToInt32(dib, 8);
            var bpp = BitConverter.ToInt16(dib, 14);
            
            if (bpp != 32) return Clipboard.ContainsImage() ? Clipboard.GetImage() : null;
            
            var gch = GCHandle.Alloc(dib, GCHandleType.Pinned);
            Bitmap bmp = null;
            try
            {
                var ptr = new IntPtr((long)gch.AddrOfPinnedObject() + byteOffset);
                bmp = new Bitmap(width, height, width * 4, System.Drawing.Imaging.PixelFormat.Format32bppArgb, ptr);
                bmp.RotateFlip(RotateFlipType.Rotate180FlipX);
                return new Bitmap(bmp);
            }
            finally
            {
                gch.Free();
                if (bmp != null) bmp.Dispose();
            }
            return Clipboard.ContainsImage() ? Clipboard.GetImage() : null;
        }

        private int VerifyByteOffset(byte[] dib, int byteOffset)
        {
            // Check if the last empty byte is in its usual place
            if (dib[byteOffset - 2] == 0 && dib[byteOffset] != 0) 
                return byteOffset;
            
            return 40;
            
            byteOffset = FindFirstEmptyByteReverse(dib, 50) + 1;

            if (byteOffset == -1)
                byteOffset = 52;

            return byteOffset;
        }

        int FindFirstEmptyByteReverse(Byte[] bytes, int startIndex)
        {
            for (int i = startIndex; i >= 0; i--)
            {
                if (bytes[i] == 0)
                    return i;
            }
            return -1;
        }

        // Check if the image contains any transparency
        private static bool IsImageTransparent(Image image)
        {
            Bitmap img = new Bitmap(image);
            for (int y = 0; y < img.Height; ++y)
            {
                for (int x = 0; x < img.Width; ++x)
                {
                    if (img.GetPixel(x, y).A != 255)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}