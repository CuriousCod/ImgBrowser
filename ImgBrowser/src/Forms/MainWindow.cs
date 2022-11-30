using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Microsoft.VisualBasic.FileIO;
using System.Threading;
using System.Threading.Tasks;
using ImgBrowser.Helpers;
using ImgBrowser.Helpers.WebpSupport;
using SearchOption = System.IO.SearchOption;

// TODO Fix slow gif animations -> Kinda fixed?
// TODO Button config
// TODO Randomized slideshow?
// TODO Scale image to screen?
// TODO Remember rotate position for next image
// TODO Z-index adjust
// BUG Fix image rotation and flip issues when playing gif animations
// BUG Window positioning issues when returning to normal window mode from maximized when initiated by a drag event
// BUG Image can slightly overfill the screen when in autosize + fullscreen mode

namespace ImgBrowser
{
    public partial class MainWindow : Form
    {
        private string[] fileEntries = Array.Empty<string>();

        // Locks current image, so image doesn't change on accidental input
        private bool lockImage;

        // ScreenCapButton is being held
        private bool screenCapButtonHeld;

        // Stores window size during resizeStart
        private Size windowResizeBegin;

        // Frame position
        private int frameLeft;
        private int frameTop;

        // Picturebox zoomed in location
        private Point zoomLocation = new Point(0, 0);

        // If mouse middle button should restore maximized or normal sized window
        private bool windowNormal;

        // If border should reappear when draggin window
        private bool showBorder;

        // Keeps track of image flipping
        private bool imageFlipped;

        // Keeps track of image rotation
        private int imageRotation;

        // Tracks any edits to the bitmap
        private bool imageEdited;
        private readonly string randString = Convert.ToBase64String(Guid.NewGuid().ToByteArray()); // A random string to use with the temp image name

        // When text is shown on the screen
        private bool displayingMessage;

        // Stores the folder path if drag and drop is used to open a folder
        private string rootImageFolder = string.Empty;

        private readonly Action<string> rootImageFolderChanged;
        
        private Point storedWindowPosition = Point.Empty;

        // Timer for the text display
        private int textTimer;

        private readonly string[] acceptedExtensions = NativeWebPDecoder.IsDllAvailable()
            ? new[] {".jpg", ".png", ".gif", ".bmp", ".tif", ".svg", ".jfif", ".jpeg", ".webp"}
            : new[] {".jpg", ".png", ".gif", ".bmp", ".tif", ".svg", ".jfif", ".jpeg"};
        
        private Definitions.SortBy sortImagesBy = Definitions.SortBy.NameAscending;

        // Commands for moving window with mouse
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

        private IntPtr thisWindow;
        
        public class WindowHover
        {
            public bool Enabled { get => WindowHoverToken != null; }
            public int AnimSpeed = 1;
            public int Distance { get => Math.Abs(StartX - EndX);}
            public int StartX;
            public int EndX = 100;
            public bool StartSet;
            public bool EndSet;

            public CancellationTokenSource WindowHoverToken;

            public WindowHover()
            {
                EndSet = false;
            }
        }

        public class StoredMousePosition
        {
            public int X;
            public int Y;

            public StoredMousePosition()
            {
                Y = 0;
            }

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
        
        private int launchArgAnimationDelay = -1;

        //-------------------------------------------

        public MainWindow()
        {
            InitializeComponent();
            InitializeMessageBox();

            Application.ApplicationExit += OnApplicationExit;
            rootImageFolderChanged = OnRootImageFolderChange;
            displayingMessage = false;
            imageEdited = false;
            imageRotation = 0;
            imageFlipped = false;
            showBorder = false;
            frameLeft = 0;
            screenCapButtonHeld = false;
        }

        private void InitializeMessageBox()
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
            messageLabelShadowBottom.Location = new Point(11, 8);
            // Offset from origin, not in use, as it makes the font look messy (0,0)
            messageLabelShadowTop.Location = new Point(0, 0);
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

            var screenCapPosX = Cursor.Position.X;
            var screenCapPosY = Cursor.Position.Y;

            Form f = new CaptureLayer();

            if (screenCapPosX != Cursor.Position.X && screenCapPosY != Cursor.Position.Y)
            {
                DisplayMessage("Selection copied to clipboard");
            }

            Task.Run(async () =>
            {
                await Task.Delay(100);
                screenCapButtonHeld = false;
            });
        }

        private void SaveCurrentImage(ImageFormat format)
        {
            if (pictureBox1.Image == null)
            {
                return;
            }

            var saveFileDialog = new SaveFileDialog();

            if (Equals(format, ImageFormat.Png))
            {
                saveFileDialog.Filter = "PNG (*.png)|*.png|All files (*.*)|*.*";
            }
            else if (Equals(format, ImageFormat.Jpeg))
            {
                saveFileDialog.Filter = "JPEG (*.jpg)|*.jpg|All files (*.*)|*.*";
            }
            else if (Equals(format, ImageFormat.Bmp))
            {
                saveFileDialog.Filter = "BMP (*.bmp)|*.bmp|All files (*.*)|*.*";
            }
            else if (Equals(format, ImageFormat.Gif))
            {
                saveFileDialog.Filter = "GIF (*.gif)|*.gif|All files (*.*)|*.*";
            }
            else if (Equals(format, ImageFormat.Tiff))
            {
                saveFileDialog.Filter = "TIFF (*.tif)|*.tif|All files (*.*)|*.*";
            }
            else
            {
                saveFileDialog.Filter = "All files (*.*)|*.*";
            }

            saveFileDialog.FilterIndex = 0;
            saveFileDialog.RestoreDirectory = true;
            saveFileDialog.FileName = currentImg.Name == "" ? "image" : currentImg.Name;

            if (saveFileDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }
            
            if (Equals(format, ImageFormat.Jpeg))
            {
                var qualityParamId = Encoder.Quality;
                var encoderParameters = new EncoderParameters(1);
                    
                encoderParameters.Param[0] = new EncoderParameter(qualityParamId, 100L);
                    
                var jpegCodec = ImageCodecInfo.GetImageDecoders().FirstOrDefault(codec => codec.FormatID == format.Guid);

                if (jpegCodec == null)
                {
                    return;
                }
                
                try
                {
                    pictureBox1.Image.Save(saveFileDialog.FileName, jpegCodec, encoderParameters);
                }
                catch (ExternalException)
                {
                    DisplayMessage("Failed to save image");
                }
            }
            else
            {
                try
                {
                    pictureBox1.Image.Save(saveFileDialog.FileName, format);
                }
                catch (ExternalException)
                {
                    DisplayMessage("Failed to save image");
                }
            }
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
            {
                CenterImage(false);
            }
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
            if (!currentImg.IsFile || pictureBox1.Image == null)
            {
                return;
            }
            
            // Get info from the image that is going to be deleted
            var delImgPath = currentImg.Path;
            var delImgName = currentImg.Name;

            lockImage = false;

            // Move to next image, so picturebox won't keep it locked
            // This also keeps the file indexes working, otherwise index will be 0 after deletion
            BrowseForward();

            // Remove image from picturebox, if it is the only image in the folder
            if (currentImg.Name == delImgName)
            {
                var img = pictureBox1.Image;
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
                LoadNewImgFromFile(currentImg);
            }
        }

        private void DisplayMessage<T>(T message) => DisplayMessage(message?.ToString());

        private async void DisplayMessage(string text)
        {
            AdjustTextSize(text);

            messageLabel.Text = text;
            messageLabelShadowBottom.Text = text;
            messageLabelShadowTop.Text = text;

            if (!displayingMessage) 
            {
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
            LoadNewImgFromFile(GetNextImageFilename(Definitions.Direction.Right), false, true);
        }

        private void BrowseBackward()
        {
            LoadNewImgFromFile(GetNextImageFilename(Definitions.Direction.Left), false, true);
        }

        private ImageObject GetNextImageFilename(Definitions.Direction direction)
        {
            if (fileEntries.Length < 2 || currentImg.Path == "" || lockImage)
            {
                return new ImageObject("");
            }

            var file = "";
            int index;

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
            {
                LoadNewImgFromFile(new ImageObject(fileEntries[index]), false, true);
            }
        }

        private void FitImageToWindow()
        {
            if (pictureBox1.Image == null)
            {
                return;
            }
            
            FormBorderStyle = FormBorderStyle.None;

            if (pictureBox1.SizeMode != PictureBoxSizeMode.Zoom)
            {
                return;
            }

            // Image aspect ratio
            var aspectRatio = (double)pictureBox1.Image.Width / (double)pictureBox1.Image.Height;
                    
            // Window frame aspect ratio
            var windowAspectRatio = (double)ClientSize.Width / (double)ClientSize.Height;

            var tempHeight = Height;

            // Adjust frame size when there's a big difference between the image and frame aspect ratios
            // This prevent images from getting too large when readjusting frame size to the image
            if (windowAspectRatio + 2 < aspectRatio)
            {
                while (windowAspectRatio + 2 < aspectRatio)
                {
                    tempHeight = (int)(tempHeight * 0.95f);
                    windowAspectRatio = (double)ClientSize.Width / (double)tempHeight;
                }
                
            }
            else if (aspectRatio + 2 < windowAspectRatio){}
            {
                while (aspectRatio + 2 < windowAspectRatio)
                {
                    tempHeight = (int)(tempHeight * 1.05f);
                    windowAspectRatio = (double)ClientSize.Width / (double)tempHeight;
                }
                
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
            var stringWidth = TextRenderer.MeasureText(text, messageLabel.Font).Width;

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
            var img = pictureBox1.Image;

            img.RotateFlip(RotateFlipType.RotateNoneFlipX);
            pictureBox1.Image = img;

            var imageAutoSizeMode = pictureBox1.SizeMode == PictureBoxSizeMode.AutoSize;

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
            var img = pictureBox1.Image;

            var x = pictureBox1.Location.X;
            var y = pictureBox1.Location.Y;
            Point rotate;

            var height = Height;
            var width = Width;
            
            // Check that the image is larger than the current window size
            var sizeVsFrame = pictureBox1.Image.Width >= ClientSize.Width && pictureBox1.Image.Height >= ClientSize.Height;

            if (CCW) 
            { 
                img.RotateFlip(RotateFlipType.Rotate270FlipNone);
                imageRotation = imageRotation <= 0 ? 3 : imageRotation - 1;

                if (pictureBox1.SizeMode == PictureBoxSizeMode.AutoSize && WindowState != FormWindowState.Maximized && sizeVsFrame) 
                { 
                    ClientSize = new Size(ClientSize.Height, ClientSize.Width);
                    rotate = new Point(0 - Math.Abs(y), -pictureBox1.Image.Height + ClientSize.Height + Math.Abs(x));
                    pictureBox1.Location = rotate;
                }
            }
            else 
            {
                img.RotateFlip(RotateFlipType.Rotate90FlipNone);
                imageRotation = imageRotation >= 3 ? 0 : imageRotation + 1;
                
                Width = height;
                Height = width;

                if (pictureBox1.SizeMode == PictureBoxSizeMode.AutoSize && WindowState != FormWindowState.Maximized && sizeVsFrame)
                {
                    ClientSize = new Size(ClientSize.Height, ClientSize.Width);
                    rotate = new Point(-pictureBox1.Image.Width + ClientSize.Width + Math.Abs(y), 0 - Math.Abs(x));
                    pictureBox1.Location = rotate;
                }
            }

            var isNotMaximizedAndIsZoom = pictureBox1.SizeMode == PictureBoxSizeMode.Zoom && WindowState != FormWindowState.Maximized;
            
            // Resize window
            if (isNotMaximizedAndIsZoom)
            {
                Height = width;
                Width = height;
            }
            
            if(pictureBox1.SizeMode == PictureBoxSizeMode.Zoom || WindowState == FormWindowState.Maximized || !sizeVsFrame) 
            {
                CenterImage();
                zoomLocation = new Point(0, 0);
            }

            pictureBox1.Image = img;
        }

        private void SaveTempImage(string ordinalValue)
        {
            if (pictureBox1.Image == null)
            {
                return;
            }
            
            ordinalValue = NumericInputToNumber(ordinalValue);

            if (ordinalValue == null)
            {
                return;
            }

            DisplayMessage(!SaveImageToTemp(ordinalValue) ? "Unable to save image" : $"Saved to temp {ordinalValue}");
        }

        private void LoadTempImage(string ordinalValue)
        {
            ordinalValue = NumericInputToNumber(ordinalValue);

            if (ordinalValue == null)
            {
                return;
            }
            
            DisplayMessage(!LoadImageFromTemp(ordinalValue) ? "No temp image found" : "Temp image loaded");
        }
        
        private bool SaveImageToTemp(string ordinalValue, bool overrideName = false)
        {
            if (pictureBox1.Image is null)
            {
                return false;
            }
            
            try 
            { 
                var tempPath = Path.GetTempPath();
                string tempName;

                if (!overrideName)
                {
                    tempName = "imgBrowserTemp" + ordinalValue + ".png";
                }
                else
                {
                    tempName = ordinalValue + ".png";
                }

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
            var tempPath = Path.GetTempPath();
            var tempName = "imgBrowserTemp" + ordinalValue + ".png";

            if (!File.Exists(tempPath + "//" + tempName))
            {
                return false;
            }
            
            LoadNewImgFromFile(new ImageObject(tempPath + "//" + tempName), true);
            //lockImage = true;
            return true;
            
        }

        /// <summary>
        /// Convert numeric keyboard inputs to numbers (D0-D9)
        /// </summary>
        /// <param name="ordinalValue"></param>
        /// <returns></returns>
        private static string NumericInputToNumber(string ordinalValue)
        {
            if (ordinalValue.Length != 2 || !ordinalValue.Contains("D"))
            {
                return null;
            }
                    
            return ordinalValue.Replace("D", "0");
        }
        
        private void UpdateWindowTitle()
        {
            var name = currentImg.Name != "" ? $"{currentImg.Name}" : "Image";
            var size = pictureBox1.Image != null ? $"{pictureBox1.Image.Width} x {pictureBox1.Image.Height}" : "";

            var position = $"";

            if (fileEntries.Length > 0) 
            { 
                var index = Array.IndexOf(fileEntries, currentImg.FullFilename);
                position = $" - {index + 1} / {fileEntries.Length}";
            }

            Text = $"ImgBrowser - {name} - {size}{position}";
        }

        private string[] GetImageFiles(string path, bool allDirectories = false)
        {
            if (path == "" || path == Path.GetTempPath() || !Directory.Exists(path))
            {
                return Array.Empty<string>();
            }
            
            var files = Directory.EnumerateFiles(path + "\\", "*.*", allDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                .Where(s => acceptedExtensions.Contains(Path.GetExtension(s).ToLowerInvariant()));
            
            files = SortFiles(files);

            return files.ToArray();
        }
        
        private IEnumerable<string> SortFiles(IEnumerable<string> files)
        {
            switch (sortImagesBy)
            {
                case Definitions.SortBy.NameAscending:
                    files = files.OrderBy(s => s.Length).ThenBy(s => s).ToArray();
                    break;
                case Definitions.SortBy.NameDescending:
                    files = files.OrderByDescending(s => s.Length).ThenByDescending(s => s).ToArray();
                    break;
                case Definitions.SortBy.DateAscending:
                    files = files.OrderBy(File.GetLastWriteTime).ToArray();
                    break;
                case Definitions.SortBy.DateDescending:
                    files = files.OrderByDescending(File.GetLastWriteTime).ToArray();
                    break;
                case Definitions.SortBy.SizeAscending:
                    files = files.OrderBy(s => new FileInfo(s).Length).ToArray();
                    break;
                case Definitions.SortBy.SizeDescending:
                    files = files.OrderByDescending(s => new FileInfo(s).Length).ToArray();
                    break;
                case Definitions.SortBy.Randomized:
                    files = files.OrderBy(s => Guid.NewGuid()).ToArray();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            return files;
        }

        // Used to determine the window quick size adjustment type
        private static Size GetAdjustmentValue()
        {
            if ((ModifierKeys & Keys.Control) == Keys.Control)
            {
                return new Size(1, 0);
            }

            if ((ModifierKeys & Keys.Shift) == Keys.Shift)
            {
                return new Size(0, 1);
            }

            return new Size(1, 1);
        }

        private void HoverWindow()
        {
            if (windowHover.Enabled)
            {
                windowHover.WindowHoverToken.Cancel();
                return;
            }

            thisWindow = NativeMethods.GetActiveWindow();
            //Point originalPos = Location;

            windowHover.WindowHoverToken = new CancellationTokenSource();
            var ct = windowHover.WindowHoverToken.Token;

            Task.Run(() =>
            {
                try
                {
                    for (; ; )
                    {
                        for (var i = 0; i < windowHover.Distance / windowHover.AnimSpeed; i++)
                        {
                            if (i < windowHover.Distance / 2 / windowHover.AnimSpeed)
                            {
                                NativeMethods.MoveWindow(thisWindow, Location.X + 1 * windowHover.AnimSpeed, Location.Y, Width, Height, false);
                            }
                            else
                            {
                                NativeMethods.MoveWindow(thisWindow, Location.X - 1 * windowHover.AnimSpeed, Location.Y, Width, Height, false);
                            }

                            if (ct.IsCancellationRequested)
                            {
                                ct.ThrowIfCancellationRequested();
                            }

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
                    windowHover.WindowHoverToken.Dispose();
                    windowHover.WindowHoverToken = null;
                    windowHover.StartSet = false;
                    windowHover.EndSet = false;
                }

            }, windowHover.WindowHoverToken.Token);
        }
        
        private static Color GetColorAt(Point location)
        {
            var screenPixel = new Bitmap(1, 1, PixelFormat.Format32bppArgb);

            using (var gdest = Graphics.FromImage(screenPixel))
            {
                using (var gsrc = Graphics.FromHwnd(IntPtr.Zero))
                {
                    var hSrcDC = gsrc.GetHdc();
                    var hDC = gdest.GetHdc();
                    var retval = NativeMethods.BitBlt(hDC, 0, 0, 1, 1, hSrcDC, location.X, location.Y, (int)CopyPixelOperation.SourceCopy);
                    gdest.ReleaseHdc();
                    gsrc.ReleaseHdc();
                }
            }

            var grabbedColor = screenPixel.GetPixel(0, 0);
            screenPixel.Dispose();

            return grabbedColor;
        }
        
        private void ResizeImage(double multiplier)
        {
            if (pictureBox1.Image == null)
            {
                return;
            }
            
            // Make a backup of the current image
            if (!imageEdited)
            {
                if (currentImg.Path == "")
                {
                    SaveImageToTemp(randString);
                }
                imageEdited = true;
            }
            
            var zoomOut = multiplier < 1;
            
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

            // https://stackoverflow.com/questions/1922040/how-to-resize-an-image-c-sharp

            // Grab current image
            var img = pictureBox1.Image;
            var resized = new Bitmap(1, 1);

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
                using (var graphics = Graphics.FromImage(resized))
                {
                    graphics.CompositingMode = CompositingMode.SourceCopy;
                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    //grap.SmoothingMode = SmoothingMode.HighQuality;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                    // This draws the new image on top of the bitmap
                    graphics.DrawImage(img, 0, 0, resized.Width, resized.Height);
                }
            }
            
            // Calculate new scroll position 
            var posX = pictureBox1.Location.X * multiplier;
            var posY = pictureBox1.Location.Y * multiplier;
            
            posX = posX > 0 ? 0 : posX;
            posY = posY > 0 ? 0 : posY;

            if (zoomOut)
            {
                // Check that image stays within the borders
                if (posX < -resized.Width + Width)
                {
                    posX = -resized.Width + Width;
                }

                if (posY < -resized.Height + Height)
                {
                    posY = -resized.Height + Height;
                }
            }

            pictureBox1.SizeMode = PictureBoxSizeMode.AutoSize;
            pictureBox1.Dock = DockStyle.None;
            pictureBox1.Image = resized;

            img.Dispose();
            CenterImage();

            // Set scroll if image fills the screen
            if (pictureBox1.Image.Width > Width)
            {
                pictureBox1.Location = new Point((int)posX, pictureBox1.Location.Y);
            }

            if (pictureBox1.Image.Height > Height)
            {
                pictureBox1.Location = new Point(pictureBox1.Location.X, (int)posY);
            }
        }

        // Restore unedited image from file or from the temp folder
        private void RestoreImage(bool showMessage = true)
        {
            if (currentImg.IsFile)
            {
                LoadNewImgFromFile(new ImageObject(currentImg.FullFilename));
            }
            else
            {
                LoadImageFromTemp(randString);
            }

            if (showMessage)
            {
                DisplayMessage("Image restored");
            }
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
            {
                return;
            }

            Image oldImg = null;

            if (pictureBox1.Image != null)
            {
                oldImg = pictureBox1.Image;
            }

            pictureBox1.Image = clipImg;
            currentImg = new ImageObject("");

            pictureBox1.Location = new Point(0, 0);
            SizeModeZoom();

            oldImg?.Dispose();

            rootImageFolderChanged?.Invoke(string.Empty);
            fileEntries = Array.Empty<string>();
            
            UpdateWindowTitle();

            ResetImageModifiers();

        }

        private void LoadNewImgFromFile(ImageObject imgObj, bool removeImagePath = false, bool skipRefresh = false)
        {
            var imageError = false;

            if (imgObj.Name == "" || !imgObj.IsFile)
            {
                return;
            }

            if (imgObj.Image == null) 
            { 
                DisplayImageError();
                imageError = true;
            }
            
            currentImg = imgObj;

            if (pictureBox1.Image != null)
            {
                var oldImg = pictureBox1.Image;

                pictureBox1.Image = null;
                oldImg.Dispose();
            }

            if (!imageError) 
            {
                if (GifAnimator.CanAnimate(imgObj.Image))
                {
                    StartAnimating(imgObj);
                }
                else
                {
                    pictureBox1.Image = imgObj.Image;
                }
            }

            if (removeImagePath) 
            {
                currentImg.FullFilename = "";
            }

            if (!skipRefresh)
            {
                var files = ReloadImageFiles();

                if (files.Length > 0)
                {
                    fileEntries = files;
                }
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

        private void StartAnimating(ImageObject imgObj)
        {
            pictureBox1.Image = imgObj.Image;
            
            if (launchArgAnimationDelay > 0)
            {
                GifAnimator.AnimationDelay = launchArgAnimationDelay;
                launchArgAnimationDelay = -1;
                return;
            }
            
            GifAnimator.AnimationDelay = 25;
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
                var oldImg = pictureBox1.Image;
                pictureBox1.Image = null;
                oldImg.Dispose();
            }

            DisplayMessage("Unable to load image");
        }

        private void CopyCurrentDisplayToClipboard()
        {
            if (pictureBox1.SizeMode != PictureBoxSizeMode.AutoSize)
            {
                return;
            }

            if (pictureBox1.Image.Width < ClientSize.Width || pictureBox1.Image.Height < ClientSize.Height)
            {
                return;
            }

            // Make a backup of the current image
            if (!imageEdited) 
            { 
                SaveImageToTemp(randString);
                imageEdited = true;
            }

            var bm = (Bitmap)pictureBox1.Image;
            bm = bm.Clone(new Rectangle(Math.Abs(pictureBox1.Location.X), Math.Abs(pictureBox1.Location.Y), ClientSize.Width, ClientSize.Height), PixelFormat.Format32bppArgb);

            Clipboard.SetImage(bm);
            bm.Dispose();

            DisplayMessage("Current display copied to clipboard.");
        }
        
        // Extra effort to support image drag to other applications
        private void DragImageFromApp()
        {
            string file;

            if (!currentImg.IsFile)
            {
                SaveImageToTemp("TempImage", true); // Create temp image with clean name
                file = Path.GetTempPath() + "/" + "TempImage.png";
            }
            else
            {
                file = currentImg.FullFilename;
            }

            if (!File.Exists(file))
            {
                return;
            }

            var dataObject = new DataObject();
            var filesInfo = new DragFileInfo(file);

            using (MemoryStream infoStream = GetFileDescriptor(filesInfo), contentStream = GetFileContents(filesInfo))
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
                    {
                        FormBorderStyle = FormBorderStyle.Sizable;
                    }
                    
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
        
        private bool ShouldMovePictureBox()
        {
            if (pictureBox1.Image == null)
            {
                return false;
            }
            
            var autoSizeMode = pictureBox1.SizeMode == PictureBoxSizeMode.AutoSize;

            return autoSizeMode && FormBorderStyle == FormBorderStyle.Sizable || 
                   autoSizeMode && WindowState == FormWindowState.Maximized ||
                   autoSizeMode && (ModifierKeys & Keys.Control) == Keys.Control;
        }
        
        private void SizeModeZoom()
        {
            if (pictureBox1.Image == null)
            {
                return;
            }

            var x = 0;
            var y = 0;

            // Update current zoomed in position
            if (pictureBox1.Image.Width > ClientSize.Width && pictureBox1.Location.X < 0)
            {
                x = pictureBox1.Location.X;
            }

            if (pictureBox1.Image.Height > ClientSize.Height && pictureBox1.Location.Y < 0)
            {
                y = pictureBox1.Location.Y;
            }

            zoomLocation = new Point(x, y);

            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.Dock = DockStyle.Fill;
        }

        private void SizeModeAutoSize()
        {
            currentImg.CopyImageToMemory();
            pictureBox1.SizeMode = PictureBoxSizeMode.AutoSize;
            pictureBox1.Dock = DockStyle.None;

            CenterImage();

            if (zoomLocation.X < -1 && zoomLocation.Y < -1)
            {
                pictureBox1.Location = zoomLocation;
            }
        }
        
        // Set the display message position relative to the picturebox
        private void PositionMessageDisplay() 
        {
            if (pictureBox1.Width >= Width && pictureBox1.Height >= Height)
            {
                messageLabelShadowBottom.Location = new Point(11 + Math.Abs(pictureBox1.Location.X), 3 + Math.Abs(pictureBox1.Location.Y));
            }
            else if (pictureBox1.Width >= Width)
            {
                messageLabelShadowBottom.Location = new Point(11 + Math.Abs(pictureBox1.Location.X), messageLabelShadowBottom.Location.Y);
            }
            else if (pictureBox1.Height >= Height)
            {
                messageLabelShadowBottom.Location = new Point(messageLabelShadowBottom.Location.X, 3 + Math.Abs(pictureBox1.Location.Y));
            }
            else
            {
                messageLabelShadowBottom.Location = new Point(11, 3);
            }
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
        
        // Catches window events for processing
        protected override void WndProc(ref Message m)
        {
            var org = WindowState;
            var location = Location;
            var screen = Screen.FromControl(this);
            
            base.WndProc(ref m);

            if (WindowState == org)
            {
                return;
            }
            
            if (org == FormWindowState.Normal)
            {
                storedWindowPosition = new Point(location.X - screen.Bounds.Left, location.Y - screen.Bounds.Top);
            }
                
            OnFormWindowStateChanged(org);
        }

        // http://ostack.cn/?qa=95752/
        // Get the real image from clipboard (this supports the alpha channel)
        private Image GetAlphaImageFromClipboard()
        {
            if (Clipboard.GetDataObject() == null) return null;

            if (!Clipboard.GetDataObject().GetDataPresent(DataFormats.Dib))
            {
                return Clipboard.ContainsImage() ? Clipboard.GetImage() : null;
            }

            var byteOffset = 52; // Offset of the DIB bits. Different software use different offsets when copying image to clipboard
            
            // Sometimes getting the image data fails and results in a "System.NullReferenceException" error - probably because clipboard handling also can be messy and complex
            byte[] dib;
            try
            {
                dib = ((MemoryStream)Clipboard.GetData(DataFormats.Dib)).ToArray();
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
                bmp = new Bitmap(width, height, width * 4, PixelFormat.Format32bppArgb, ptr);
                bmp.RotateFlip(RotateFlipType.Rotate180FlipX);
                return new Bitmap(bmp);
            }
            finally
            {
                gch.Free();
                bmp?.Dispose();
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

        private static int FindFirstEmptyByteReverse(IReadOnlyList<byte> bytes, int startIndex)
        {
            for (var i = startIndex; i >= 0; i--)
            {
                if (bytes[i] == 0)
                {
                    return i;
                }
            }
            return -1;
        }

        // Check if the image contains any transparency
        private static bool IsImageTransparent(Image image)
        {
            var img = new Bitmap(image);
            for (var y = 0; y < img.Height; ++y)
            {
                for (var x = 0; x < img.Width; ++x)
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