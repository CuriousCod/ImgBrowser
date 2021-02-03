using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Runtime.InteropServices;

// TODO Config for window start position
// TODO Color picker?
// TODO Chroma key / Transparency for window?
// TODO Randomized slideshow?
// BUG When zoomed in messages are only shown in top left position
// TODO Verify if other image changing methods require dispose(), copy+paste, rotate, etc

namespace ImgBrowser
{
    public partial class Form1 : Form
    {
        private BackgroundWorker showMessage;
        public string[] fileEntries;

        // Current image information
        public string imgName = "";
        public string imgLocation = "";

        // Mouse position
        public int currentPositionX = 0;
        public int currentPositionY = 0;

        // Frame position
        public int frameLeft = 0;
        public int frameTop = 0;

        // If mouse middle button should restore maximized or normal sized window
        public bool windowNormal = false;

        // If border should reappear when draggin window
        public bool showBorder = false;

        // Commands for moving window with mouse
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [DllImportAttribute("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImportAttribute("user32.dll")]
        public static extern bool ReleaseCapture();
        //-------------------------------------------

        public Form1()
        {
            // Add a worker to remove messages from display after a set duration
            showMessage = new BackgroundWorker();
            showMessage.DoWork += new DoWorkEventHandler(showMessage_DoWork);
            showMessage.RunWorkerCompleted += new RunWorkerCompletedEventHandler(showMessage_RunWorkerCompleted);
            showMessage.WorkerSupportsCancellation = true;

            InitializeComponent();

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

        private void showMessage_DoWork(object sender, DoWorkEventArgs e)
        {

            string value = (string)e.Argument;
            // Sleep 2 seconds to emulate getting data.
            e.Result = value;
            Thread.Sleep(1500);
            //e.Result = "";
        }

        private void showMessage_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            messageLabel.Text = e.Result.ToString();
            messageLabelShadowBottom.Text = e.Result.ToString();
            messageLabelShadowTop.Text = e.Result.ToString();
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

        private void pictureBox1_MouseWheel(object sender, MouseEventArgs e)
        {
            if (pictureBox1.SizeMode != PictureBoxSizeMode.AutoSize)
            {
                if (e.Delta > 0)
                {
                    if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
                    {
                        pictureBoxZoom(pictureBox1.Image, new Size(1, 1));
                    }
                    else
                    {
                        browseForward();
                    }

                }
                else if (e.Delta < 0)
                {
                    if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
                    {
                        pictureBoxRestore();
                    }
                    else
                    {
                        browseBackward();
                    }
                }
            }

        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            Console.WriteLine(e.KeyCode);

            switch (e.KeyCode.ToString())
            {
                case "Left":
                    browseBackward();
                    break;
                case "Right":
                    browseForward();
                    break;
                case "F5":
                    updateFileList();
                    break;
                case "F2":
                    if (FormBorderStyle == FormBorderStyle.None)
                    {
                        FormBorderStyle = FormBorderStyle.Sizable;
                    }
                    else
                    {
                        if (pictureBox1.Image != null)
                        {
                            // Barebones adjust window size to aspect ratio feature
                            FormBorderStyle = FormBorderStyle.None;
                            if (Size.Height > Size.Width)
                            {
                                double aspectRatio = (double)pictureBox1.Image.Height / (double)pictureBox1.Image.Width;
                                Size = new Size(Size.Width, (int)(aspectRatio * Size.Width));
                            }
                            else
                            {
                                double aspectRatio = (double)pictureBox1.Image.Width / (double)pictureBox1.Image.Height;
                                Size = new Size((int)(aspectRatio * Size.Height), Size.Height);
                            }
                        }
                    }
                    break;
                // Open image location
                case "F3":
                    if (imgLocation != "")
                    {
                        System.Diagnostics.Process.Start("explorer.exe", imgLocation);
                    }
                    break;
                // Copy image to clipboard
                case "C":
                    // Check for control key
                    if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
                    {
                        if (pictureBox1.Image != null)
                        {
                            // TODO This makes image file size large
                            displayMessage("Copied to Clipboard");
                            Clipboard.SetImage(pictureBox1.Image);
                        }
                    }
                    break;
                // Display image from clipboard
                case "V":
                    // Check for control key
                    if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
                    {
                        if (Clipboard.GetImage() != null)
                        {
                            Image oldImg = null;
                            if (pictureBox1.Image != null) { oldImg = pictureBox1.Image; }
                            
                            pictureBox1.Image = Clipboard.GetImage();
                            imgLocation = "";
                            imgName = "";

                            panel1.HorizontalScroll.Value = 0;
                            panel1.VerticalScroll.Value = 0;
                            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
                            pictureBox1.Dock = DockStyle.Fill;
                            
                            if (oldImg != null) { oldImg.Dispose(); }
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

                        Image img = pictureBox1.Image;
                        img.RotateFlip(RotateFlipType.Rotate90FlipNone);

                        pictureBox1.Image = img;
                    }
                    break;
                /* TODO WIP
                case "I":
                    if (pictureBox1.Image != null)
                    {
                        float Z = getCurrentPixel();
                        /*
                        // Calculate picture size on screen

                        Size sizey;
                        double aspect = (double)pictureBox1.Image.Width / (double)pictureBox1.Image.Height;

                        if (Size.Height > Size.Width)
                        {
                            double aspectRatio = (double)pictureBox1.Image.Height / (double)pictureBox1.Image.Width;
                            sizey = new Size(Size.Width, (int)(aspectRatio * Size.Width) - 23);
                        }
                        else
                        {
                            double aspectRatio = (double)pictureBox1.Image.Width / (double)pictureBox1.Image.Height;
                            sizey = new Size((int)(aspectRatio * Size.Height), Size.Height - 23);
                        }

                        Console.WriteLine(sizey);


                        if (Cursor.Position.X <= pictureBox1.Right && Cursor.Position.X >= pictureBox1.Left && Cursor.Position.Y <= pictureBox1.Bottom && Cursor.Position.Y >= pictureBox1.Top)
                        {
                            Bitmap grabImg = new Bitmap(pictureBox1.Image);
                            Console.WriteLine(pictureBox1.Image.Width);
                            Console.WriteLine(grabImg.Width);
                            Console.WriteLine(grabImg.Height);
                            Color pixel = grabImg.GetPixel(Cursor.Position.X, Cursor.Position.Y);
                            Console.WriteLine(pixel);
                        }
                        
                    }
                break;*/
                case "F":
                    maxOrNormalizeWindow();
                    break;
                case "F1":
                    // Set always on top

                    // Make sure the string fits the frame
                    int stringWidth = TextRenderer.MeasureText("Stay on Top: False", messageLabel.Font).Width;

                    while (stringWidth + 12 > Width)
                    {
                        messageLabel.Font = new Font(messageLabel.Font.FontFamily, messageLabel.Font.Size - 1, FontStyle.Bold);
                        stringWidth = TextRenderer.MeasureText("Stay on Top: False", messageLabel.Font).Width;
                    }

                    while ((stringWidth - 12) * 2.8 < Width)
                    {
                        if (messageLabel.Font.Size >= 22) { break; }
                        messageLabel.Font = new Font(messageLabel.Font.FontFamily, messageLabel.Font.Size + 1, FontStyle.Bold);
                        stringWidth = TextRenderer.MeasureText("Stay on Top: False", messageLabel.Font).Width;

                    }

                    messageLabelShadowBottom.Font = new Font(messageLabel.Font.FontFamily, messageLabel.Font.Size, FontStyle.Bold);
                    messageLabelShadowTop.Font = new Font(messageLabel.Font.FontFamily, messageLabel.Font.Size, FontStyle.Bold);

                    if (TopMost)
                    {
                        displayMessage("Stay on Top: False");
                        TopMost = false;
                    }
                    else
                    {
                        displayMessage("Stay on Top: True");
                        TopMost = true;
                    }

                    break;
                case "F11":
                    maxOrNormalizeWindow();
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

                case "Add":
                    pictureBoxZoom(pictureBox1.Image, new Size(1, 1));
                    break;
                case "Subtract":
                    pictureBoxRestore();
                    break;
                default:
                    break;
            }

            /*
            if (e.KeyCode.ToString() == "Left")
            {
                browseBackward();
            }
            else if (e.KeyCode.ToString() == "Right")
            {
                browseForward();
            }
            else if (e.KeyCode.ToString() == "F5")
            {
                updateFileList();
            }
            else if (e.KeyCode.ToString() == "H")
            {
                if (FormBorderStyle == FormBorderStyle.None)
                {
                    FormBorderStyle = FormBorderStyle.Sizable;
                }
                else
                {
                    FormBorderStyle = FormBorderStyle.None;
                    double aspectRatio = (double)pictureBox1.Image.Height / (double)pictureBox1.Image.Width;
                    Size = new Size(Size.Width, (int)(aspectRatio * Size.Width));
                }
            }
            else if (e.KeyCode.ToString() == "C")
            {
                // Check for control key
                if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
                {
                    if (pictureBox1.ImageLocation != "")
                    {
                        Clipboard.SetImage(Image.FromFile(pictureBox1.ImageLocation));
                    }
                }

            }
            else if (e.KeyCode.ToString() == "Add")
            {
                pictureBoxZoom(pictureBox1.Image, new Size(1, 1));
            }
            */

        }

        private float getCurrentPixel()
        {

            int imgWidth = pictureBox1.Image.Width;
            int imgHeight = pictureBox1.Image.Height;
            int boxWidth = pictureBox1.Size.Width;
            int boxHeight = pictureBox1.Size.Height;

            //This variable will hold the result
            float X = Cursor.Position.X;
            float Y = Cursor.Position.Y;
            //Comparing the aspect ratio of both the control and the image itself.
            if (imgWidth / imgHeight > boxWidth / boxHeight)
            {
                //If true, that means that the image is stretched through the width of the control.
                //'In other words: the image is limited by the width.

                //The scale of the image in the Picture Box.
                float scale = boxWidth / imgWidth;

                //Since the image is in the middle, this code is used to determinate the empty space in the height
                //'by getting the difference between the box height and the image actual displayed height and dividing it by 2.
                float blankPart = (boxHeight - scale * imgHeight) / 2;

                Y -= blankPart;

                //Scaling the results.
                X /= scale;
                Y /= scale;
            }
            else
            {
                //If true, that means that the image is stretched through the height of the control.
                //'In other words: the image is limited by the height.

                //The scale of the image in the Picture Box.
                float scale = boxHeight / imgHeight;

                //Since the image is in the middle, this code is used to determinate the empty space in the width
                //'by getting the difference between the box width and the image actual displayed width and dividing it by 2.
                float blankPart = (boxWidth - scale * imgWidth) / 2;
                X -= blankPart;

                //Scaling the results.
                X /= scale;
                Y /= scale;
            }
            Console.WriteLine(X);
            Console.WriteLine(Y);
            return X;
        }


        private void displayMessage(string text)
        {
            messageLabel.Text = text;
            messageLabelShadowBottom.Text = text;
            messageLabelShadowTop.Text = text;

            // Clear message
            if (showMessage.IsBusy == false)
            {
                showMessage.RunWorkerAsync("");
            }

        }

        private void browseForward()
        {
            if (imgLocation != "") {

                //string[] fileEntries = listFiles(Path.GetDirectoryName(pictureBox1.ImageLocation));
                //string[] fileEntries = asd;
                //Console.WriteLine(pictureBox1.ImageLocation);

                int index = Array.IndexOf(fileEntries, imgLocation + "\\" + imgName);
                //int index = Array.IndexOf(fileEntries, pictureBox1.ImageLocation);

                //Console.WriteLine(index);
                Image currentImage = pictureBox1.Image;

                if (index + 1 <= fileEntries.Length - 1)
                {
                    pictureBox1.Image = Image.FromFile(fileEntries[index + 1]);
                    //pictureBox1.Image = nextImg;
                    imgLocation = Path.GetDirectoryName(fileEntries[index + 1]);
                    imgName = Path.GetFileName(fileEntries[index + 1]);
                    //prevImg.Dispose();
                }
                else
                {
                    pictureBox1.Image = Image.FromFile(fileEntries[0]);
                    //pictureBox1.Image = nextImg;                    
                    imgLocation = Path.GetDirectoryName(fileEntries[0]);
                    imgName = Path.GetFileName(fileEntries[0]);
                    //prevImg.Dispose();
                }

                currentImage.Dispose();
                updateFormName();
            }
        }

        private void browseBackward()
        {
            if (imgLocation != "")
            {
                //string[] fileEntries = listFiles(Path.GetDirectoryName(pictureBox1.ImageLocation));

                int index = Array.IndexOf(fileEntries, imgLocation + "\\" + imgName);
                //int index = Array.IndexOf(fileEntries, pictureBox1.ImageLocation);

                //Console.WriteLine(index);
                Image currentImage = pictureBox1.Image;

                if (index - 1 >= 0)
                {
                    pictureBox1.Image = Image.FromFile(fileEntries[index - 1]);
                    //pictureBox1.Image = prevImg;
                    imgLocation = Path.GetDirectoryName(fileEntries[index - 1]);
                    imgName = Path.GetFileName(fileEntries[index - 1]);
                    //nextImg.Dispose();
                }
                else
                {
                    pictureBox1.Image = Image.FromFile(fileEntries[fileEntries.Length - 1]);
                    //pictureBox1.Image = prevImg;
                    imgLocation = Path.GetDirectoryName(fileEntries[fileEntries.Length - 1]);
                    imgName = Path.GetFileName(fileEntries[fileEntries.Length - 1]);
                    //nextImg.Dispose();
                }

                currentImage.Dispose();
                updateFormName();
            }
        }

        private void updateFormName()
        {
            Text = "ImgBrowser - " + imgName;
        }

        private string[] updateFileList()
        {
            
            if (imgLocation != "")
            {
                IEnumerable<string> files = Directory.EnumerateFiles(imgLocation, "*.*", SearchOption.TopDirectoryOnly)
                .Where(s => s.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || s.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                s.EndsWith(".gif", StringComparison.OrdinalIgnoreCase) || s.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase) ||
                s.EndsWith(".tif", StringComparison.OrdinalIgnoreCase) || s.EndsWith(".svg", StringComparison.OrdinalIgnoreCase) ||
                s.EndsWith(".jfif", StringComparison.OrdinalIgnoreCase));
                return files.ToArray();
            }
            
            else
            {
                string[] files = new string[0];
                return files;
            }
            
        }

        /*
        private string[] listFiles(string dir)
        {
            //string[] fileEntries = Directory.GetFiles(@"");
            var files = Directory.EnumerateFiles(dir, "*.*", SearchOption.TopDirectoryOnly)
                .Where(s => s.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || s.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || s.EndsWith(".gif", StringComparison.OrdinalIgnoreCase));

            string[] fileEntries = files.ToArray();

            return fileEntries;
        }
        */

        private void pictureBox1_Click(object sender, EventArgs e)
        {

            MouseEventArgs me = (MouseEventArgs)e;

            
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            var cmdArgs = Environment.GetCommandLineArgs();

            foreach (string i in cmdArgs)
            {
                Console.WriteLine(i);
            }

            if (cmdArgs.Length > 1)
            {
                loadNewImg(cmdArgs[1]);
            }
            else if (Clipboard.GetImage() != null)
            {
                pictureBox1.Image = Clipboard.GetImage();
                imgName = "";
                imgLocation = "";
            }

        }

        public void pictureBoxZoom(Image img, Size size)
        {
            if (pictureBox1.Image != null)
            {
                //Bitmap bm = new Bitmap(img, Convert.ToInt32(img.Width * size.Width), Convert.ToInt32(img.Height * size.Height));
                //Bitmap bm = new Bitmap(img, Convert.ToInt32(img.Width * 1.5), Convert.ToInt32(img.Height * 1.5));

                //https://stackoverflow.com/questions/1922040/how-to-resize-an-image-c-sharp

                // The bitmap and the graphic will both need to be resized
                Image currentImg = pictureBox1.Image;
                Bitmap resized = new Bitmap(Convert.ToInt32(img.Width * 1.5), Convert.ToInt32(img.Height * 1.5));

                using (Graphics grap = Graphics.FromImage(resized))
                {
                    grap.CompositingMode = CompositingMode.SourceCopy;
                    grap.CompositingQuality = CompositingQuality.HighQuality;
                    grap.InterpolationMode = InterpolationMode.Bicubic;
                    //grap.SmoothingMode = SmoothingMode.HighQuality;
                    grap.PixelOffsetMode = PixelOffsetMode.HighQuality;

                    grap.DrawImage(img, 0, 0, Convert.ToInt32(img.Width * 1.5), Convert.ToInt32(img.Height * 1.5));
                }

                pictureBox1.SizeMode = PictureBoxSizeMode.AutoSize;
                pictureBox1.Dock = DockStyle.None;
                pictureBox1.Image = resized;
                currentImg.Dispose();

                centerImage();
            }



        }

        // Reset Zoom, if original image can be accessed
        public void pictureBoxRestore()
        {
            if (imgLocation != "")
            {
                Image currentImg = pictureBox1.Image;
                pictureBox1.Image = Image.FromFile(imgLocation + "\\" + imgName);

                panel1.HorizontalScroll.Value = 0;
                panel1.VerticalScroll.Value = 0;
                pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
                pictureBox1.Dock = DockStyle.Fill;
                currentImg.Dispose();

                centerImage();
            }
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            //Console.WriteLine(files[0]);

            if (files != null)
            {
                string lowerCase = files[0].ToLower();

                if (lowerCase.EndsWith(".jpg") || lowerCase.EndsWith(".png") || lowerCase.EndsWith(".gif") || lowerCase.EndsWith(".bmp") || lowerCase.EndsWith(".tif") || lowerCase.EndsWith(".svg") || lowerCase.EndsWith(".jfif"))
                {
                    loadNewImg(files[0]);
                    pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
                }
            }


        }

        private void loadNewImg(string file)
        {
            imgName = Path.GetFileName(file);
            imgLocation = Path.GetDirectoryName(file);
            Image img = Image.FromFile(imgLocation + "\\" + imgName);

            pictureBox1.Image = img;

            updateFormName();
            fileEntries = updateFileList();
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            // Change cursor graphic
            e.Effect = DragDropEffects.Move;

        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {

            if (e.Button.ToString() == "Left")

            // Get mouse position for image scroll
            currentPositionX = Cursor.Position.X;
            currentPositionY = Cursor.Position.Y;

            // For moving the window with mouse without ReleaseCapture();
            //frameTop = Top;
            //frameLeft = Left;

            // Maximize or normalize window
            if (e.Button.ToString() == "Left" && e.Clicks == 2)
                {
                    maxOrNormalizeWindow();            
                }
            else
            {
                // Scroll
                if (pictureBox1.SizeMode == PictureBoxSizeMode.AutoSize)
                {
                    pictureBox1.Cursor = Cursors.SizeAll;
                }
                // Drag
                else
                {
                        // Raw commands for moving window with mouse
                        ReleaseCapture();
                        SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
                }
            }
                


            /*
            //Console.WriteLine(pictureBox1.Width);
            //Console.WriteLine(pictureBox1.Height);
            //Console.WriteLine(e.X);
            double valueX = (double)e.X / (double)pictureBox1.Width * panel1.Width;
            double valueY = (double)e.Y / (double)pictureBox1.Height * panel1.Height;
            Console.WriteLine(valueX);
            Console.WriteLine(valueY);
            //Console.WriteLine(e.Y / pictureBox1.Height * 100);
            Console.WriteLine(panel1.VerticalScroll.Maximum);
            panel1.HorizontalScroll.Value = (int)valueX;
            panel1.VerticalScroll.Value = (int)valueY;
            */
        }
            
        private void maxOrNormalizeWindow()
        {
            if (this.WindowState == FormWindowState.Maximized)
            {
                if (this.FormBorderStyle == FormBorderStyle.None)
                {
                    this.FormBorderStyle = FormBorderStyle.Sizable;
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
            //int directionX;
            //int directionY;

            // Add some leeway to mouse movement
            // TODO Should be dynamic
            int minMov = 150;

            // Make scroll speed dynamic
            // TODO This should be simplified
            double scrollOffsetX = pictureBox1.Width * 0.04;
            double scrollOffsetY = pictureBox1.Height * 0.04;
            double scrollOffset = (double)((double)scrollOffsetX + (double)scrollOffsetY) / 2 * 0.1;

            if (e.Button.ToString() == "Left")
            {
                if (pictureBox1.SizeMode == PictureBoxSizeMode.AutoSize)
                {

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
                    else if (Cursor.Position.X + minMov < currentPositionX)
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
                    else if (Cursor.Position.Y + minMov < currentPositionY)
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

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            pictureBox1.Cursor = Cursors.Arrow;
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
                        // Barebones adjust window size to aspect ratio feature
                        FormBorderStyle = FormBorderStyle.None;
                        if (Size.Height > Size.Width)
                        {
                            double aspectRatio = (double)pictureBox1.Image.Height / (double)pictureBox1.Image.Width;
                            Size = new Size(Size.Width, (int)(aspectRatio * Size.Width));
                        }
                        else
                        {
                            double aspectRatio = (double)pictureBox1.Image.Width / (double)pictureBox1.Image.Height;
                            Size = new Size((int)(aspectRatio * Size.Height), Size.Height);
                        }
                    }
                }
                
            }
            else if ((e.Button.ToString() == "Right"))
            {
                if (pictureBox1.Image != null)
                {
                    // Disable scrolling
                    if (pictureBox1.SizeMode == PictureBoxSizeMode.AutoSize)
                    {
                        panel1.HorizontalScroll.Value = 0;
                        panel1.VerticalScroll.Value = 0;
                        pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
                        pictureBox1.Dock = DockStyle.Fill;
                    }

                    // Scrolling for large images
                    else if ((pictureBox1.Image.Width > Width) || (pictureBox1.Image.Height > Height))
                    {
                        pictureBox1.SizeMode = PictureBoxSizeMode.AutoSize;
                        pictureBox1.Dock = DockStyle.None;

                        centerImage();
                    }
                }
                // Paste image from clipboard, if picturebox is empty
                else if (Clipboard.GetImage() != null)
                {
                    pictureBox1.Image = Clipboard.GetImage();
                    imgName = "";
                    imgLocation = "";
                }
            }
        }

        private void centerImage()
        {
            if (pictureBox1.Image != null)
            {
                // Calculate padding to center image
                if (Width > pictureBox1.Image.Width)
                {
                    pictureBox1.Left = (Width - pictureBox1.Image.Width) / 2;
                    pictureBox1.Top = 0;
                }
                else if (Height > pictureBox1.Image.Height)
                {
                    pictureBox1.Top = (Height - pictureBox1.Image.Height) / 2;
                    pictureBox1.Left = 0;
                }
                else
                {
                    pictureBox1.Top = 0;
                    pictureBox1.Left = 0;
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
    }
}