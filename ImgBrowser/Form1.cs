using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Threading;
using System.Runtime.InteropServices;
using Microsoft.VisualBasic.FileIO;
using System.Threading.Tasks;
using SearchOption = System.IO.SearchOption;


// TODO Config for window start position
// TODO Button config
// TODO Randomized slideshow? <-----------
// TODO Verify if other image changing methods require dispose(), copy+paste, rotate, etc
// TODO Arrow keys to navigate when zoomed in
// TODO Tabs?
// TODO Folder image count
// TODO Open folder in app and os.walk an image array
// TODO Randomize image array order?
// BUG Image can slighty overfill the screen when in autosize + fullscreen mode
// BUG Dragging a full screen image into a small window will make the window slightly smaller every time
// BUG Tempfile deletion does not work when closing the application
// TODO Scale image to screen?
// TODO Remember rotate position for next image
// TODO Z-index adjust

namespace ImgBrowser
{
    public partial class Form1 : Form
    {
        private string[] fileEntries = new string[0]{};

        // Current image information
        private string imgName = "";
        private string imgLocation = "";

        // Locks current image, so browsing doesn't work
        private bool lockImage = false;

        // Mouse position
        private int currentPositionX = 0;
        private int currentPositionY = 0;

        // ScreenCapButton is being held
        private bool screenCapButtonHeld = false;

        // Screenshot start position
        private int screenCapPosX;
        private int screenCapPosY;

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

        // Timer for the text display
        private int textTimer;

        // Commands for moving window with mouse
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

        [DllImportAttribute("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImportAttribute("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("gdi32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
        public static extern int BitBlt(IntPtr hDC, int x, int y, int nWidth, int nHeight, IntPtr hSrcDC, int xSrc, int ySrc, int dwRop);
        //-------------------------------------------

        public Form1()
        {
            InitializeComponent();
            InitializeMessageBox();

            Application.ApplicationExit += new EventHandler(OnApplicationExit);
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
            messageLabelShadowBottom.Location = new Point(11, 8);
            // Offset from origin, not in use, as it makes the font look messy (0,0)
            messageLabelShadowTop.Location = new Point(0, 0);
        }

        private void Form1_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
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

        private void Form1_MouseWheel(object sender, MouseEventArgs e)
        {
                if (e.Delta > 0)
                {
                    if ((Control.ModifierKeys & Keys.Alt) == Keys.Alt)
                    {
                        // Increase window size
                        if (WindowState == FormWindowState.Normal)
                        {
                            Size = Size.Add(Size, GetAdjustmentValue());
                            if ((pictureBox1.SizeMode != PictureBoxSizeMode.AutoSize) && (FormBorderStyle == FormBorderStyle.None))
                                FitImageToWindow();
                        }
                    }
                    else if (pictureBox1.SizeMode != PictureBoxSizeMode.AutoSize)
                    {
                        if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
                            pictureBoxZoom(1.5);
                        else
                            browseForward();
                    }

                }
                else if (e.Delta < 0)
                {
                    if ((Control.ModifierKeys & Keys.Alt) == Keys.Alt)
                    {
                        // Decrease window size
                        if (WindowState == FormWindowState.Normal)
                        {
                        Size = Size.Subtract(Size, GetAdjustmentValue());
                        if ((pictureBox1.SizeMode != PictureBoxSizeMode.AutoSize) && (FormBorderStyle == FormBorderStyle.None))
                            FitImageToWindow();
                        }
                    }
                    else if (pictureBox1.SizeMode != PictureBoxSizeMode.AutoSize)
                    {
                        if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
                            pictureBoxUnZoom(1.5);
                        else
                            BrowseBackward();
                    }

            }

        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            Console.WriteLine(e.KeyCode);

            switch (e.KeyCode.ToString())
            {
                case "Left":
                    // Pixel movement
                    if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
                    {
                        int modifier = ((Control.ModifierKeys & Keys.Shift) == Keys.Shift) ? 5 : 1;
                        Location = Point.Subtract(Location, new Size(modifier, 0));
                    }
                    // Size adjust
                    else if ((Control.ModifierKeys & Keys.Alt) == Keys.Alt)
                    {
                        int modifier = ((Control.ModifierKeys & Keys.Shift) == Keys.Shift) ? 5 : 1;
                        Size = Size.Subtract(Size, new Size(modifier, 0));
                        if (pictureBox1.SizeMode == PictureBoxSizeMode.AutoSize) {
                            CenterImage(false);
                            pictureBox1.Location = new Point(pictureBox1.Location.X - modifier, pictureBox1.Location.Y);
                        }
                    }
                    else
                        BrowseBackward();
                    break;
                case "Right":
                    if ((Control.ModifierKeys & Keys.Control) == Keys.Control) {
                        int modifier = ((Control.ModifierKeys & Keys.Shift) == Keys.Shift) ? 5 : 1;
                        Location = Point.Add(Location, new Size(modifier, 0));
                    }
                    else if ((Control.ModifierKeys & Keys.Alt) == Keys.Alt)
                    {
                        int modifier = ((Control.ModifierKeys & Keys.Shift) == Keys.Shift) ? 5 : 1;
                        Size = Size.Add(Size, new Size(modifier, 0));
                        if (pictureBox1.SizeMode == PictureBoxSizeMode.AutoSize) {
                            CenterImage(false);
                            pictureBox1.Location = new Point(pictureBox1.Location.X + modifier, pictureBox1.Location.Y);
                        }
                    }
                    else
                        browseForward();
                    break;
                case "Up":
                    if ((Control.ModifierKeys & Keys.Control) == Keys.Control) {
                        int modifier = ((Control.ModifierKeys & Keys.Shift) == Keys.Shift) ? 3 : 1;
                        Location = Point.Subtract(Location, new Size(0, modifier));
                    }
                    else if ((Control.ModifierKeys & Keys.Alt) == Keys.Alt)
                    {
                        int modifier = ((Control.ModifierKeys & Keys.Shift) == Keys.Shift) ? 3 : 1;
                        Size = Size.Subtract(Size, new Size(0, modifier));
                        if (pictureBox1.SizeMode == PictureBoxSizeMode.AutoSize)
                        {
                            CenterImage(false);
                            //pictureBox1.Location = new Point(pictureBox1.Location.X, pictureBox1.Location.Y - modifier);
                        }

                    }
                    break;
                case "Down":
                    if ((Control.ModifierKeys & Keys.Control) == Keys.Control) {
                        int modifier = ((Control.ModifierKeys & Keys.Shift) == Keys.Shift) ? 3 : 1;
                        Location = Point.Add(Location, new Size(0, modifier));
                    }
                    else if ((Control.ModifierKeys & Keys.Alt) == Keys.Alt)
                    {
                        int modifier = ((Control.ModifierKeys & Keys.Shift) == Keys.Shift) ? 3 : 1;
                        Size = Size.Add(Size, new Size(0, modifier));
                        if (pictureBox1.SizeMode == PictureBoxSizeMode.AutoSize)
                        {
                            CenterImage(false);
                            //pictureBox1.Location = new Point(pictureBox1.Location.X, pictureBox1.Location.Y + modifier);
                        }
                    }
                    break;
                case "Y":
                    for (int x = 0; x < 20; x++) { 
                        for (int i = 0; i < 200; i++) { 
                        if (i < 100)
                            Location = Point.Add(Location, new Size(1,0));
                        else
                            Location = Point.Subtract(Location, new Size(1, 0));
                        }
                    }
                    break;

                case "F1":
                    ToggleAlwaysOnTop();
                    break;
                case "F2":
                    if (FormBorderStyle == FormBorderStyle.None)
                    {
                        FormBorderStyle = FormBorderStyle.Sizable;
                    }
                    else
                    {
                        FitImageToWindow();
                    }
                    break;
                // Open image location
                case "F3":
                    if (imgLocation != "")
                    {
                        System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{imgLocation}\\{imgName}\"");
                    }
                    break;
                case "F5":
                    fileEntries = UpdateFileList();
                    break;
                // Restore unedited image
                case "F10":
                    if (imageEdited)
                        RestoreImage();
                    break;
                case "F11":
                    maxOrNormalizeWindow();
                    break;
                // Copy image to clipboard
                case "C":
                    // Check for control key
                    if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
                    {
                        if (pictureBox1.Image != null) {
                            if ((Control.ModifierKeys & Keys.Shift) == Keys.Shift) {
                                CopyCurrentDisplaytoClipboard();
                            }
                            else
                            {
                                // TODO This makes image file size large

                                DisplayMessage("Copied to Clipboard");
                                Clipboard.SetImage(pictureBox1.Image);
                            }
                        }
                    }
                    break;
                // Display image from clipboard
                case "V":
                    // Check for control key
                    if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
                    {
                        Image clipImg = Clipboard.GetImage();
                        if (clipImg != null)
                        {
                            Image oldImg = null;
                            if (pictureBox1.Image != null) {
                                oldImg = pictureBox1.Image; }

                            pictureBox1.Image = clipImg;
                            imgLocation = "";
                            imgName = "";

                            pictureBox1.Location = new Point(0, 0);
                            SizeModeZoom();

                            if (oldImg != null) { oldImg.Dispose(); }

                            UpdateFormName();

                            ResetImageModifiers();
                        }

                    }
                    break;
                // Rotate
                case "R":
                    if (pictureBox1.Image != null)
                    {
                        /*
                        Bitmap img = new Bitmap(pictureBox1.Image);
                        using (Graphics grap = Graphics.FromImage(img))
                        {
                            grap.TranslateTransform(img.Width / 2, img.Height / 2);

                            grap.RotateTransform(90);

                            grap.TranslateTransform(-img.Width / 2, -img.Height / 2);

                            grap.DrawImage(img, 0, 0, pictureBox1.Image.Width, pictureBox1.Image.Height);
                        }
                        */

                        if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
                            RotateImage(true);
                        else
                            RotateImage(false);

                    }
                    break;
                case "M":
                    if (pictureBox1.Image != null)
                    {
                        FlipImageX(((Control.ModifierKeys & Keys.Control) == Keys.Control));
                    }
                    break;
                // Duplicate current image into a new window
                case "D":
                    if ((Control.ModifierKeys & Keys.Control) == Keys.Control) {
                        string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                        if (!File.Exists(exePath))
                            return;

                        string args = GetCurrentArgs();

                        if (imgLocation != "")
                        {
                           System.Diagnostics.Process.Start(exePath, $"\"{imgLocation}/{imgName}\" {args} -center");
                        }
                        else if (pictureBox1.Image != null) {
                           Clipboard.SetImage(pictureBox1.Image);
                           System.Diagnostics.Process.Start(exePath, $"noImg {args} -center");
                        }
                    }
                    break;
                case "F":
                    maxOrNormalizeWindow();
                    break;
                // Color picker
                case "I":
                    Color currentColor = GetColorAt(Cursor.Position);
                    string colorHex;

                    BackColor = currentColor;

                    colorHex = ColorTranslator.ToHtml(Color.FromArgb(currentColor.ToArgb()));
                    if ((Control.ModifierKeys & Keys.Shift) == Keys.Shift)
                        DisplayMessage($"{currentColor.R}, {currentColor.G}, {currentColor.B}");
                    else
                        DisplayMessage(colorHex);

                    break;
                case "L":
                    if (pictureBox1.Image != null)
                    {
                        if ((lockImage))
                        {
                            lockImage = false;
                            DisplayMessage("Image unlocked");
                        }
                        else
                        {
                            if ((fileEntries != null) && (fileEntries.Length > 0))
                            {
                                lockImage = true;
                                DisplayMessage("Image locked");
                            }
                        }
                    }
                    break;
                // Snipping tool, captured when button is released
                case "S":
                    if (screenCapButtonHeld == false)
                    {
                        screenCapButtonHeld = true;
                        screenCapPosX = Cursor.Position.X;
                        screenCapPosY = Cursor.Position.Y;
                        //pictureBox1.Cursor = Cursors.Cross;

                        Form f = new CaptureLayer();
                        DisplayMessage("Selection copied to clipboard");
                        screenCapButtonHeld = false;

                    }
                    break;
                // TODO This can make the image transparent as well if the color matches the form's bg
                case "T":
                    if ((Control.ModifierKeys & Keys.Control) == Keys.Control) { 
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
                    break;
                // Close app
                case "W":
                    if ((Control.ModifierKeys & Keys.Control) == Keys.Control && (Control.ModifierKeys & Keys.Shift) == Keys.Shift)
                    {
                        Application.Exit();
                    }
                    break;
                // Display image name
                case "N":
                    if (!string.IsNullOrEmpty(imgName))
                        DisplayMessage(imgName);
                    break;
                // Move image to recycle bin
                case "Delete":
                    if ((imgLocation != "") && (imgName != "") && (pictureBox1.Image != null))
                    {
                        // Get info from the image that is going to be deleted
                        string delImgLocation = imgLocation;
                        string delImgName = imgName;

                        lockImage = false;

                        // Move to next image, so picturebox won't keep it locked
                        // This also keeps the file indexes working, otherwise index will be 0 after deletion
                        browseForward();

                        // Remove image from picturebox, if it is the only image in the folder
                        if (imgName == delImgName)
                        {
                            Image img = pictureBox1.Image;
                            pictureBox1.Image = null;
                            img.Dispose();
                        }
                        try
                        {
                            FileSystem.DeleteFile(delImgLocation + "\\" + delImgName, UIOption.AllDialogs, RecycleOption.SendToRecycleBin);
                            fileEntries = UpdateFileList();
                            DisplayMessage("Image moved to recycle bin");
                        }
                        catch (OperationCanceledException)
                        {
                            pictureBox1.Image = Image.FromFile(imgLocation + "\\" + imgName);
                        }

                    }
                    break;
                case "Home":
                    JumpToImage(0);
                    break;
                case "End":
                    if (fileEntries != null)
                    {
                        JumpToImage(fileEntries.Length - 1);
                    }
                    break;
                case "Return":
                    if ((Control.ModifierKeys & Keys.Alt) == Keys.Alt)
                    {
                        maxOrNormalizeWindow();
                        //e.Handled = true;
                        // Suppress Windows noises
                        e.SuppressKeyPress = true;
                    }
                    break;
                // Copy image fullname and window coordinates to clipboard
                case "Pause":
                    if ((Control.ModifierKeys & Keys.Alt) == Keys.Alt) {
                        DisplayMessage("Image path and window size added to clipboard");
                        Clipboard.SetText($"{imgLocation}\\{imgName} {Top},{Left},{Height},{Width}");
                    }
                    break;
                case "Add":
                    // Hold ctrl for smaller zoom value
                    if ((Control.ModifierKeys & Keys.Control) == Keys.Control) pictureBoxZoom(1.2);
                    else pictureBoxZoom(1.5);
                    break;
                case "Subtract":
                    // Hold ctrl for smaller zoom value
                    if ((Control.ModifierKeys & Keys.Control) == Keys.Control) pictureBoxUnZoom(1.2);
                    else pictureBoxUnZoom(1.5);
                    break;
                case "D1":
                    TempImageHandling("01");
                    break;
                case "D2":
                    TempImageHandling("02");
                    break;
                case "D3":
                    TempImageHandling("03");
                    break;
                case "D4":
                    TempImageHandling("04");
                    break;
                case "D5":
                    TempImageHandling("05");
                    break;
                case "D6":
                    TempImageHandling("06");
                    break;
                case "D7":
                    TempImageHandling("07");
                    break;
                case "D8":
                    TempImageHandling("08");
                    break;
                case "D9":
                    TempImageHandling("09");
                    break;
                case "D0":
                    TempImageHandling("00");
                    break;
                default:
                    break;
            }
        }

        async void DisplayMessage(string text)
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

        private void browseForward()
        {
            if ((imgLocation != "") && lockImage == false)
            {
                int index = Array.IndexOf(fileEntries, imgLocation + "\\" + imgName);
                string file;

                if (index + 1 <= fileEntries.Length - 1)
                    file = fileEntries[index + 1];
                else
                    file = fileEntries[0];

                LoadNewImg(file, false, true);
            }
        }

        private void BrowseBackward()
        {
            if ((imgLocation != "") && lockImage == false)
            {
                int index = Array.IndexOf(fileEntries, imgLocation + "\\" + imgName);
                string file;

                if (index - 1 >= 0)
                    file = fileEntries[index - 1];
                else
                    file = fileEntries[fileEntries.Length - 1];

                LoadNewImg(file, false, true);
            }
        }

        private void JumpToImage(int index)
        {
            if ((!lockImage) && ((imgLocation != ""))) 
                LoadNewImg(fileEntries[index], false, true);
        }

        private void FitImageToWindow()
        {
            if (pictureBox1.Image != null)
            {
                // Barebones adjust window size to aspect ratio feature
                FormBorderStyle = FormBorderStyle.None;
                if (pictureBox1.SizeMode == PictureBoxSizeMode.Zoom)
                {
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
            }
        }

        private void ToggleAlwaysOnTop()
        {
            if (TopMost)
            {
                DisplayMessage("Stay on Top: False");
                TopMost = false;
            }
            else
            {
                DisplayMessage("Stay on Top: True");
                TopMost = true;
            }
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

        private void FlipImageX(bool ctrl = false)
        {
            Image img = pictureBox1.Image;

            img.RotateFlip(RotateFlipType.RotateNoneFlipX);
            pictureBox1.Image = img;

            bool imageAutoSizeMode = pictureBox1.SizeMode == PictureBoxSizeMode.AutoSize;

            if (!imageFlipped)
            {
                // Flip image only based on current viewport, unless ctrl is held
                if (imageAutoSizeMode && !ctrl && WindowState != FormWindowState.Maximized && pictureBox1.Image.Width >= ClientSize.Width)
                {
                    pictureBox1.Location = new Point(-pictureBox1.Image.Width + ClientSize.Width + Math.Abs(pictureBox1.Location.X), pictureBox1.Location.Y);
                    PositionMessageDisplay();
                }
                imageFlipped = true;
            }
            else
            {
                if (imageAutoSizeMode && !ctrl && WindowState != FormWindowState.Maximized && pictureBox1.Image.Width >= ClientSize.Width)
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

        private void TempImageHandling(string ordinalValue)
        {            
            if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
                if ((Control.ModifierKeys & Keys.Shift) == Keys.Shift) { 
                    if (pictureBox1.Image != null) { 
                        if (!SaveImageToTemp(ordinalValue)) { 
                            DisplayMessage("Unable to save image");
                        }
                        else
                            DisplayMessage($"Saved to temp {ordinalValue}");
                        
                    }
                }
                else
                    if (LoadImageFromTemp(ordinalValue))
                        DisplayMessage("Temp image loaded");
        }

        private bool SaveImageToTemp(string ordinalValue)
        {
            if (pictureBox1.Image != null)
            {
                try { 
                    string tempPath = Path.GetTempPath();
                    string tempName = "imgBrowserTemp" + ordinalValue + ".png";
                    pictureBox1.Image.Save(tempPath + "\\" + tempName, ImageFormat.Png);
                    return true;
                }
                // This occurs when trying to rewrite a currently opened image
                catch (ExternalException)
                {  
                    return false;
                }
            }

            return false;
        }

        private bool LoadImageFromTemp(string ordinalValue)
        {
            string tempPath = Path.GetTempPath();
            string tempName = "imgBrowserTemp" + ordinalValue + ".png";

            if (File.Exists(tempPath + "//" + tempName))
            {
                LoadNewImg(tempPath + "//" + tempName, true);
                lockImage = true;
                return true;
            }

            return false;
        }

        private void UpdateFormName()
        {
            string name = imgName != "" ? $"{imgName}" : "Image";
            string size = pictureBox1.Image != null ? $"{pictureBox1.Image.Width} x {pictureBox1.Image.Height}" : "";

            string position = $"";

            if (fileEntries.Length > 0) { 
                int index = Array.IndexOf(fileEntries, imgLocation + "\\" + imgName);
                position = $" - {index + 1} / {fileEntries.Length}";
            }

            Text = $"ImgBrowser - {name} - {size}{position}";
        }

        private string[] UpdateFileList(bool allDirectories = false)
        {

            if (imgLocation != "")
            {
                IEnumerable<string> files = Directory.EnumerateFiles(imgLocation + "\\", "*.*", allDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                .Where(s => s.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || s.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                s.EndsWith(".gif", StringComparison.OrdinalIgnoreCase) || s.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase) ||
                s.EndsWith(".tif", StringComparison.OrdinalIgnoreCase) || s.EndsWith(".svg", StringComparison.OrdinalIgnoreCase) ||
                s.EndsWith(".jfif", StringComparison.OrdinalIgnoreCase) || s.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase));

                files = files.OrderBy(s => s.Length).ThenBy(s => s);

                return files.ToArray();
            }

            // Return empty array
            else
            {
                string[] files = new string[0];
                return files;
            }

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

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            // MouseEventArgs me = (MouseEventArgs)e;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            var cmdArgs = Environment.GetCommandLineArgs();

            foreach (string i in cmdArgs)
            {
                Console.WriteLine(i);
            }

            int argsLength = cmdArgs.Length;

            if (argsLength > 1 && cmdArgs[1] != "noImg")
            {
                LoadNewImg(cmdArgs[1]); 
            }
            else
            {
                Image clipImg = Clipboard.GetImage();

                if (clipImg != null)
                {
                    pictureBox1.Image = clipImg;
                    imgName = "";
                    imgLocation = "";
                    UpdateFormName();
                }
            }

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
                        if (int.TryParse(cmdArgs[i + 1], out int direction))
                            if (direction > 3)
                                break;
                            for (int x = 0; x < direction; x++)
                            {
                                RotateImage();
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

        public Color GetColorAt(Point location)
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

        public void pictureBoxZoom(double multiplier)
        {
            if (pictureBox1.Image != null)
            {
                // Make a backup of the current image
                if (!imageEdited)
                {
                    if (imgLocation == "")
                        SaveImageToTemp(randString);
                    imageEdited = true;
                }

                //Bitmap bm = new Bitmap(img, Convert.ToInt32(img.Width * size.Width), Convert.ToInt32(img.Height * size.Height));
                //Bitmap bm = new Bitmap(img, Convert.ToInt32(img.Width * 1.5), Convert.ToInt32(img.Height * 1.5));

                // Perform a rough image size check to avoid memory issues
                if (pictureBox1.Image.Width * multiplier + pictureBox1.Image.Height * multiplier > 40000)
                {
                    DisplayMessage("Image too large to resize");
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
                        grap.InterpolationMode = InterpolationMode.Bicubic;
                        //grap.SmoothingMode = SmoothingMode.HighQuality;
                        grap.PixelOffsetMode = PixelOffsetMode.HighQuality;

                        // This draws the new image on top of the bitmap
                        grap.DrawImage(img, 0, 0, Convert.ToInt32(img.Width * multiplier), Convert.ToInt32(img.Height * multiplier));
                    }
                }

                // Calculate the current scroll position as a percentage
                //double horPos;
                //double verPos;
                //horPos = (double)panel1.HorizontalScroll.Value / (double)panel1.HorizontalScroll.Maximum;
                //verPos = (double)panel1.VerticalScroll.Value / (double)panel1.VerticalScroll.Maximum;

                // Reset scroll position to keep the picturebox in proper position
                //panel1.HorizontalScroll.Value = 0;
                //panel1.VerticalScroll.Value = 0;

                // Calculate new scroll position 
                double posX = (double)(pictureBox1.Location.X * multiplier);
                double posY = (double)(pictureBox1.Location.Y * multiplier);
                if (posX > 0) posX = 0;
                if (posY > 0) posY = 0;

                pictureBox1.SizeMode = PictureBoxSizeMode.AutoSize;
                pictureBox1.Dock = DockStyle.None;
                pictureBox1.Image = resized;

                img.Dispose();
                CenterImage();

                // Set scroll if image fills the screen
                if (pictureBox1.Image.Width > Width) pictureBox1.Location = new Point((int)posX, pictureBox1.Location.Y);
                if (pictureBox1.Image.Height > Height) pictureBox1.Location = new Point(pictureBox1.Location.X, (int)posY);

                // Set the scroll position to match the position before zooming
                //panel1.HorizontalScroll.Value = (int)(panel1.HorizontalScroll.Maximum * horPos);
                //panel1.VerticalScroll.Value = (int)(panel1.VerticalScroll.Maximum * verPos);
            }
        }

        public void pictureBoxUnZoom(double multiplier)
        {
            if (pictureBox1.Image != null)
            {

                // Make a backup of the current image
                if (!imageEdited)
                {
                    if (imgLocation == "")
                        SaveImageToTemp(randString);
                    imageEdited = true;
                }

                // Perform a rough image size check to avoid memory issues
                if (pictureBox1.Image.Width / multiplier + pictureBox1.Image.Height / multiplier > 40000)
                {
                    DisplayMessage("Image too large to resize");
                    return;
                }

                // Do not zoom out if it makes image smaller than screen
                if ((pictureBox1.Image.Width / multiplier < Width) && (pictureBox1.Image.Height / multiplier < Height))
                {
                    SizeModeZoom();
                    return;
                }

                //https://stackoverflow.com/questions/1922040/how-to-resize-an-image-c-sharp

                // Grab current image
                Image img = pictureBox1.Image;
                Bitmap resized = new Bitmap(1, 1);

                // Creating a new resized bitmap
                try
                {
                    resized = new Bitmap(img, Convert.ToInt32(img.Width / multiplier), Convert.ToInt32(img.Height / multiplier));
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
                        grap.DrawImage(img, 0, 0, Convert.ToInt32(img.Width / multiplier), Convert.ToInt32(img.Height / multiplier));
                    }
                }

                // Calculate new scroll position 
                double posX = (double)(pictureBox1.Location.X / multiplier);
                double posY = (double)(pictureBox1.Location.Y / multiplier);

                // Check that image stays within the borders
                if (posX > 0) posX = 0;
                if (posY > 0) posY = 0;
                if (posX < -resized.Width + Width) posX = -resized.Width + Width;
                if (posY < -resized.Height + Height) posY = -resized.Height + Height;

                pictureBox1.SizeMode = PictureBoxSizeMode.AutoSize;
                pictureBox1.Dock = DockStyle.None;
                pictureBox1.Image = resized;

                img.Dispose();
                CenterImage();

                // Set scroll if image fills the screen
                if (pictureBox1.Image.Width > Width) pictureBox1.Location = new Point((int)posX, pictureBox1.Location.Y);
                if (pictureBox1.Image.Height > Height) pictureBox1.Location = new Point(pictureBox1.Location.X, (int)posY);

            }
        }

        // Restore unedited image from file or from the temp folder
        public void RestoreImage()
        {
            if (!string.IsNullOrEmpty(imgName) && !string.IsNullOrEmpty(imgLocation))
                LoadNewImg(imgLocation + "\\" + imgName);
            else
                LoadImageFromTemp(randString);

            DisplayMessage("Image restored");
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            //Console.WriteLine(files[0]);

            if (files != null)
            {
                string lowerCase = files[0].ToLower();

                if (lowerCase.EndsWith(".jpg") || lowerCase.EndsWith(".png") || lowerCase.EndsWith(".gif") || lowerCase.EndsWith(".bmp") || lowerCase.EndsWith(".tif") || lowerCase.EndsWith(".svg") || lowerCase.EndsWith(".jfif") || lowerCase.EndsWith(".jpeg"))
                {
                    LoadNewImg(files[0]);
                }
                else if (Directory.Exists(files[0]))
                {
                    imgLocation = files[0];
                    fileEntries = UpdateFileList(true);

                    if (fileEntries.Length > 0)
                        browseForward();
                    else
                        imgLocation = "";
                }
            }


        }

        private void LoadNewImg(string file, bool removeImagePath = false, bool skipRefresh = false)
        {
            imgName = Path.GetFileName(file);
            imgLocation = Path.GetDirectoryName(file).TrimEnd('\\');

            if (verifyImg(imgLocation + "\\" + imgName))
            {
                if (pictureBox1.Image != null)
                {
                    Image oldImg = pictureBox1.Image;
                    pictureBox1.Image = null;
                    oldImg.Dispose();
                }

                // Separate handling for gif files to make the animation work
                if (!imgName.EndsWith(".gif")) { 
                    Bitmap newImg = new Bitmap(Image.FromFile(imgLocation + "\\" + imgName)); // This way files won't be locked to the application
                    pictureBox1.Image = newImg;
                }
                else
                    pictureBox1.Image = Image.FromFile(imgLocation + "\\" + imgName);
            }

            if (removeImagePath) { 
                imgLocation = "";
                imgName = "";
            }

            UpdateFormName();

            if (!skipRefresh)
                fileEntries = UpdateFileList();

            // Reset zoomed in position
            pictureBox1.Location = new Point(0, 0);
            lockImage = false;

            ResetImageModifiers();

            CenterImage();

            SizeModeZoom();

            PositionMessageDisplay();

            GC.Collect();
        }

        private bool verifyImg(string file)
        {
            try
            {
                // Check if image can be loaded
                Image img = Image.FromFile(file);
                // using (img)
                // { }
                img.Dispose();
                return true;
            }
            catch (OutOfMemoryException ex)
            {
                Console.WriteLine(ex);
                pictureBox1.Image = imageError();
                return false;
            }
            // This actually doesn't catch these errors, since it also requires HandleProcessCorruptedStateExceptions 
            // Got this one while trying to access corrupt image
            // https://social.msdn.microsoft.com/Forums/vstudio/en-US/4de25cc0-9235-4e40-9cd7-d7c934d78cc6/sehexception-is-not-caught-in-managed-code-windows-just-kills-the-process?forum=clr
            catch (SEHException ex)
            {
                Console.WriteLine(ex);
                pictureBox1.Image = imageError();
                return false;
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine(ex);
                pictureBox1.Image = imageError();
                return false;
            }
        }

        private void ResetImageModifiers()
        {
            // Reset image modifiers
            imageFlipped = false;
            imageEdited = false;
        }

        private Image imageError()
        {
            DisplayMessage("Unable to load image");
            return null;
        }

        private void CopyCurrentDisplaytoClipboard()
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

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            // Change cursor graphic
            e.Effect = DragDropEffects.Move;
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {

            if (e.Button.ToString() == "Left")
            {
                // Get mouse position for image scroll
                currentPositionX = Cursor.Position.X;
                currentPositionY = Cursor.Position.Y;
            }

            if (WindowState != FormWindowState.Maximized)
            {
                // For moving the window with mouse without ReleaseCapture();
                frameTop = Top;
                frameLeft = Left;
            }

            // Maximize or normalize window
            if (e.Button.ToString() == "Left" && e.Clicks == 2)
            {
                maxOrNormalizeWindow();
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
                // Activate window drag
                else
                {
                    // Ignore if form is transparent, so the image can be moved without snapping to screen edges
                    if ((TransparencyKey != BackColor) && ((Control.ModifierKeys & Keys.Control) != Keys.Control)) {
                        
                        /* This triggers with too early when just clicking the image
                        // Center the window on the mouse if maximized
                        if (WindowState == FormWindowState.Maximized)
                        {
                            WindowState = FormWindowState.Normal;
                            Left = Cursor.Position.X - ClientSize.Width / 2;
                            Top = Cursor.Position.Y - ClientSize.Height / 2;
                        }*/

                        // Raw commands for moving window with mouse// Raw commands for moving window with mouse
                        ReleaseCapture();
                        SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);

                    }
                }
            }

        }

        private void maxOrNormalizeWindow()
        {
            if (this.WindowState == FormWindowState.Maximized)
            {
                if (this.FormBorderStyle == FormBorderStyle.None)
                {
                    if (showBorder == true)
                        this.FormBorderStyle = FormBorderStyle.Sizable;
                    // Reset picturebox style, when returning from full screen
                    pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
                    pictureBox1.Dock = DockStyle.Fill;
                    CenterImage();

                    if (windowNormal)
                    {
                        this.WindowState = FormWindowState.Normal;
                    }
                    else
                    {
                        this.WindowState = FormWindowState.Maximized;
                    }

                }
                else
                {
                    this.FormBorderStyle = FormBorderStyle.None;
                    windowNormal = false;
                    // Restore border if window is dragged from full screen
                    showBorder = true;
                }
            }
            else
            {
                panel1.HorizontalScroll.Value = 0;
                panel1.VerticalScroll.Value = 0;
                pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
                pictureBox1.Dock = DockStyle.Fill;

                this.FormBorderStyle = FormBorderStyle.None;
                this.WindowState = FormWindowState.Maximized;
                windowNormal = true;
                // Restore border if window is dragged from full screen
                showBorder = true;
            }
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (pictureBox1.Image != null)
            {

                if (e.Button.ToString() == "Left")
                {

                    if ((pictureBox1.SizeMode == PictureBoxSizeMode.AutoSize) && (FormBorderStyle == FormBorderStyle.Sizable) || 
                        (pictureBox1.SizeMode == PictureBoxSizeMode.AutoSize) && (WindowState == FormWindowState.Maximized) ||
                        (pictureBox1.SizeMode == PictureBoxSizeMode.AutoSize) && ((Control.ModifierKeys & Keys.Control) == Keys.Control))
                    {
                        MovePictureBox();
                    }
                    else
                    {
                        // Classic style window drag anywhere to move feature
                        // Useful when you don't want window to snap to screen edges
                        if (TransparencyKey == BackColor) 
                        {
                            if ((Cursor.Position.X != currentPositionX) && (Cursor.Position.Y != currentPositionY) && (WindowState == FormWindowState.Maximized))
                            {
                                WindowState = FormWindowState.Normal;

                                // Center window on mouse
                                Location = Cursor.Position;
                                frameTop = Top - (int)(Height / 2);
                                frameLeft = Left - (int)(Width / 2);
                            }
                            
                            // Keep border hidden when restoring window
                            showBorder = false;
                            Location = new Point(Cursor.Position.X - currentPositionX + frameLeft, Cursor.Position.Y - currentPositionY + frameTop);
                        }
                    }

                    // Old code scrolling using scrollbars with panel autoscroll
                    /*
                    if (Cursor.Position.X - minMov > currentPositionX)
                    {
                        if (panel1.HorizontalScroll.Value - scrollOffset * 2 >= panel1.HorizontalScroll.Minimum)
                        {
                            panel1.HorizontalScroll.Value -= (int)(scrollOffset) * 2;
                        }
                        else
                        {
                            panel1.HorizontalScroll.Value = panel1.HorizontalScroll.Minimum;
                        }

                    }
                    if (Cursor.Position.X + minMov < currentPositionX)
                    {
                        if (panel1.HorizontalScroll.Value + scrollOffset * 2 <= panel1.HorizontalScroll.Maximum)
                        {
                            panel1.HorizontalScroll.Value += (int)(scrollOffset) * 2;
                        }
                        else
                        {
                            panel1.HorizontalScroll.Value = panel1.HorizontalScroll.Maximum;
                        }
                    }

                    if (Cursor.Position.Y - minMov > currentPositionY)
                    {
                        if (panel1.VerticalScroll.Value - scrollOffset >= panel1.VerticalScroll.Minimum)
                        {
                            panel1.VerticalScroll.Value -= (int)(scrollOffset);
                        }
                        else
                        {
                            panel1.VerticalScroll.Value = panel1.VerticalScroll.Minimum;
                        }
                    }
                    if (Cursor.Position.Y + minMov < currentPositionY)
                    {
                        if (panel1.VerticalScroll.Value + scrollOffset <= panel1.VerticalScroll.Maximum)
                        {
                            panel1.VerticalScroll.Value += (int)(scrollOffset);
                        }
                        else
                        {
                            panel1.VerticalScroll.Value = panel1.VerticalScroll.Maximum;
                        }

                    }
                }
                */

                    // Changed to use the Windows function ReleaseCapture();
                    // Move frame with mouse
                    /*
                    else
                    {
                        Location = new Point(Cursor.Position.X - currentPositionX + frameLeft, Cursor.Position.Y - currentPositionY + frameTop);
                    }
                    */

                    // Old image move code
                    /*
                    // Grab mouse X direction
                    double deltaDirection = currentPositionX - e.X;
                    directionX = deltaDirection > 0 ? -1 : 1;
                    currentPositionX = e.X;
                    Console.WriteLine("X: " + directionX);

                    // Move image based on direction
                    if ((directionX == 1) && (panel1.HorizontalScroll.Value + 5 <= panel1.HorizontalScroll.Maximum))
                    {
                        panel1.HorizontalScroll.Value += 5;
                    }
                    else if (panel1.HorizontalScroll.Value - 5 >= panel1.HorizontalScroll.Minimum)
                    {
                        panel1.HorizontalScroll.Value -= 5;
                    }

                    // Grab mouse Y direction
                    deltaDirection = currentPositionY - e.Y;
                    directionY = deltaDirection > 0 ? -1 : 1;
                    currentPositionX = e.Y;
                    Console.WriteLine("Y: " + directionY);

                    // Move image based on direction
                    if ((directionY == 1) && (panel1.VerticalScroll.Value + 5 <= panel1.VerticalScroll.Maximum))
                    {
                        panel1.VerticalScroll.Value += 5;
                    }
                    else if (panel1.VerticalScroll.Value - 5 >= panel1.VerticalScroll.Minimum)
                    {
                        panel1.VerticalScroll.Value -= 5;
                    }
                    */
                }
                else
                {
                    //currentPositionX = e.X;
                    //currentPositionY = e.Y;
                }
            }

        }

        // Moves the picturebox image when dragging the mouse on the image
        private void MovePictureBox()
        {

            //int directionX;
            //int directionY;

            // Add some leeway to mouse movement
            // TODO Should be dynamic
            //int minMov = (int)((double)((Width + Height) * 0.04));

            // Make scroll speed dynamic
            // TODO This should be simplified
            //double scrollOffsetX = pictureBox1.Width * 0.04 * 0.1;
            //double scrollOffsetY = pictureBox1.Height * 0.04 / 2 * 0.1;
            ///double scrollOffset = (double)((double)scrollOffsetX + (double)scrollOffsetY / 2.2) / 2 * 0.1;

            /*
            double scrollOffsetX;
            double scrollOffsetY;

            if (pictureBox1.Image.Width + pictureBox1.Image.Height > 6000)
            {
                scrollOffsetX = 45;
                scrollOffsetY = 45;
            }
            else
            {
                scrollOffsetX = 35;
                scrollOffsetY = 35;
            }
            */

            int range;
            int minMov = (int)((double)((Width + Height) * 0.01));

            // Hide any currently displayed message
            DisplayMessage("");

            //pictureBox1.Refresh();
            // Only allow adjustments if the image is larger than the screen resolution
            if (pictureBox1.Image.Width > Width)
            {
                if (Cursor.Position.X - minMov > currentPositionX)
                {
                    // Prevent picturebox from going over the left border
                    if (pictureBox1.Location.X + Cursor.Position.X - currentPositionX > 0)
                        pictureBox1.Location = new Point(0, pictureBox1.Location.Y);
                    else if (pictureBox1.Location.X <= 0)
                        pictureBox1.Location = new Point(pictureBox1.Location.X + Cursor.Position.X - currentPositionX, pictureBox1.Location.Y);

                    // Reset mouse position variable to stop infinite scrolling
                    currentPositionX = Cursor.Position.X;
                }
                if (Cursor.Position.X + minMov < currentPositionX)
                {
                    range = -pictureBox1.Image.Width + ClientRectangle.Width;

                    // Prevent picturebox from going over the right border
                    if (pictureBox1.Location.X - currentPositionX + Cursor.Position.X < range)
                        pictureBox1.Location = new Point(range, pictureBox1.Location.Y);
                    else if (pictureBox1.Location.X >= range)
                        pictureBox1.Location = new Point(pictureBox1.Location.X - currentPositionX + Cursor.Position.X, pictureBox1.Location.Y);

                    currentPositionX = Cursor.Position.X;
                }
            }
            if (pictureBox1.Image.Height > Height)
            {
                if (Cursor.Position.Y - minMov > currentPositionY)
                {
                    // Prevent picturebox from going over the top border
                    if (pictureBox1.Location.Y + Cursor.Position.Y - currentPositionY > 0)
                        pictureBox1.Location = new Point(pictureBox1.Location.X, 0);
                    else if (pictureBox1.Location.Y <= 0)
                        pictureBox1.Location = new Point(pictureBox1.Location.X, pictureBox1.Location.Y + Cursor.Position.Y - currentPositionY);

                    currentPositionY = Cursor.Position.Y;
                }
                if (Cursor.Position.Y + minMov < currentPositionY)
                {
                    range = -pictureBox1.Image.Height + ClientRectangle.Height;

                    // Prevent picturebox from going over the bottom border
                    if (pictureBox1.Location.Y - currentPositionY + Cursor.Position.Y < range)
                        pictureBox1.Location = new Point(pictureBox1.Location.X, range);
                    else if (pictureBox1.Location.Y >= range)
                        pictureBox1.Location = new Point(pictureBox1.Location.X, pictureBox1.Location.Y - currentPositionY + Cursor.Position.Y);

                    currentPositionY = Cursor.Position.Y;
                }
            }
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            pictureBox1.Cursor = Cursors.Arrow;
            PositionMessageDisplay();
        }

        private void pictureBox1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button.ToString() == "Middle")
            {
                if (FormBorderStyle == FormBorderStyle.None)
                {
                    FormBorderStyle = FormBorderStyle.Sizable;
                }
                else
                {
                    if (pictureBox1.Image != null)
                    {
                        FitImageToWindow();
                    }
                }

            }
            else if ((e.Button.ToString() == "Right"))
            {
                if (pictureBox1.Image != null)
                {
                    // Return to autofit image mode
                    if (pictureBox1.SizeMode == PictureBoxSizeMode.AutoSize)
                    {
                        SizeModeZoom();
                    }

                    // Scrolling for large images
                    else if ((pictureBox1.Image.Width > Width) || (pictureBox1.Image.Height > Height))
                    {
                        SizeModeAutoSize();
                    }
                }
                // Paste image from clipboard, if picturebox is empty
                // This does not dispose the previous image, but this also only works when the picturebox image is null
                else { 
                    Image clipImg = Clipboard.GetImage();

                    if (clipImg != null)
                    {
                        pictureBox1.Image = clipImg;
                        imgName = "";
                        imgLocation = "";
                    }
                }
            }
        }

        private void SizeModeZoom()
        {

            // Update current zoomed in position
            zoomLocation = pictureBox1.Location;

            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.Dock = DockStyle.Fill;

        }

        private void SizeModeAutoSize()
        {
            pictureBox1.SizeMode = PictureBoxSizeMode.AutoSize;
            pictureBox1.Dock = DockStyle.None;

            CenterImage();

            pictureBox1.Location = zoomLocation;

        }

        private void CenterImage(bool updateZoom = true)
        {

            if (pictureBox1.Image != null)
            {
                // Return to zoom mode, if image is smaller than the frame
                if (Width > pictureBox1.Image.Width && Height > pictureBox1.Image.Height) {
                    SizeModeZoom();
                    return;
                }
                
                // Calculate padding to center image
                if (ClientSize.Width > pictureBox1.Image.Width)
                {
                    pictureBox1.Left = (Width - pictureBox1.Image.Width) / 2;
                    // Update zoom location to center image
                    if (updateZoom) { 
                        zoomLocation = new Point(pictureBox1.Left, zoomLocation.Y);
                        pictureBox1.Top = 0;
                    }
                }
                else if (ClientSize.Height > pictureBox1.Image.Height)
                {
                    pictureBox1.Top = (Height - pictureBox1.Image.Height) / 2;

                    // Update zoom location to center image
                    if (updateZoom) { 
                        zoomLocation = new Point(zoomLocation.X, pictureBox1.Top);
                        pictureBox1.Left = 0;
                    }
                }
                else
                {
                    if (updateZoom) { 
                        pictureBox1.Top = 0;
                        pictureBox1.Left = 0;
                    }
                }
            }
        }

        private void Form1_Move(object sender, EventArgs e)
        {
            if (FormBorderStyle == FormBorderStyle.None && showBorder == true)
            {
                FormBorderStyle = FormBorderStyle.Sizable;
                showBorder = false;
            }
            else
            {
                showBorder = false;
            }
        }

        // TODO This doesn't work
        private void pictureBox1_LocationChanged(object sender, EventArgs e)
        {
            // PositionMessageDisplay();
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode.ToString())
            {

                // Moved to separate form window


                // Capture screen from the rectangle drawn by cursor
                // https://stackoverflow.com/questions/13103682/draw-a-bitmap-image-on-the-screen
                case "S":
                    /*
                    screenCapButtonHeld = false;
                    pictureBox1.Cursor = Cursors.Default;

                    // Get start and end coordinates
                    int p1 = Math.Min(screenCapPosX, Cursor.Position.X);
                    int p2 = Math.Min(screenCapPosY, Cursor.Position.Y);
                    int s1 = Math.Max(screenCapPosX, Cursor.Position.X);
                    int s2 = Math.Max(screenCapPosY, Cursor.Position.Y);

                    // Create rectangle from coordinates
                    Rectangle rect = new Rectangle(new Point(p1, p2), new Size(s1 - p1, s2 - p2));

                    if (rect.Width == 0 || rect.Height == 0) break;

                    Bitmap BM = new Bitmap(rect.Width, rect.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    using (Graphics g = Graphics.FromImage(BM))
                    {
                        g.CopyFromScreen(rect.Left, rect.Top, 0, 0, rect.Size);
                        Clipboard.SetImage(BM);
                    }

                    BM.Dispose();
                    displayMessage("Selection copied to clipboard");
                    */

                    break;
                case "I":
                    Color currentColor = GetColorAt(Cursor.Position);
                    string colorHex;
                    colorHex = ColorTranslator.ToHtml(Color.FromArgb(currentColor.ToArgb()));

                    // TODO This is probably pointless
                    // Set chroma key if alt is held
                    if ((Control.ModifierKeys & Keys.Alt) == Keys.Alt)
                    { 
                        TransparencyKey = BackColor;
                        DisplayMessage("Chroma key set");
                    }
                    // Get color RGB when shift is being held
                    else if ((Control.ModifierKeys & Keys.Shift) == Keys.Shift)
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

        private void Form1_ResizeEnd(object sender, EventArgs e)
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

        private void Form1_ResizeBegin(object sender, EventArgs e)
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

        private void OnApplicationExit(object sender, EventArgs e)
        {
            string tempFile = Path.GetTempPath() + "/" + "imgBrowserTemp" + randString + ".png";

            // Attempt to delete the temporary file
            try 
            { 
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
            catch (IOException) {
            }
        }
    }
}