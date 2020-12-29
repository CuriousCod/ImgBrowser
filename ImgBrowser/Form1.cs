using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace ImgBrowser
{

    public partial class Form1 : Form
    {

        public string[] fileEntries;

        // Mouse position
        public int currentPositionX = 0;
        public int currentPositionY = 0;

        public Form1()
        {
            InitializeComponent();

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
                    browseForward();
                }
                else if (e.Delta < 0)
                {
                    browseBackward();
                }
            }

        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            Console.WriteLine(e.KeyCode);
            // TODO Turn this into case
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
            /*
            else if (e.KeyCode.ToString() == "Up")
            {
                panel1.VerticalScroll.Value += 2;
               // PictureBoxZoom(Image.FromFile(@""), new Size(3, 3));
            }
            else if (e.KeyCode.ToString() == "Down")
            {
                panel1.HorizontalScroll.Value += 2;
                // PictureBoxZoom(Image.FromFile(@""), new Size(1, 1));
            }
            */
            else if (e.KeyCode.ToString() == "Add")
            {
                PictureBoxZoom(pictureBox1.Image, new Size(1, 1));
            }

        }
        private void browseForward()
        {
            if (pictureBox1.ImageLocation != "") {

                //string[] fileEntries = listFiles(Path.GetDirectoryName(pictureBox1.ImageLocation));
                //string[] fileEntries = asd;

                //Console.WriteLine(pictureBox1.ImageLocation);

                int index = Array.IndexOf(fileEntries, pictureBox1.ImageLocation);

                //Console.WriteLine(index);

                if (index + 1 <= fileEntries.Length - 1)
                {
                    pictureBox1.ImageLocation = fileEntries[index + 1];
                }
                else
                {
                    pictureBox1.ImageLocation = fileEntries[0];
                }

                updateFormName();
            }
        }

        private void browseBackward()
        {
            if (pictureBox1.ImageLocation != "")
            {
                //string[] fileEntries = listFiles(Path.GetDirectoryName(pictureBox1.ImageLocation));

                //Console.WriteLine(pictureBox1.ImageLocation);

                int index = Array.IndexOf(fileEntries, pictureBox1.ImageLocation);

                //Console.WriteLine(index);

                if (index - 1 >= 0)
                {
                    pictureBox1.ImageLocation = fileEntries[index - 1];
                }
                else
                {
                    pictureBox1.ImageLocation = fileEntries[fileEntries.Length - 1];
                }

                updateFormName();
            }
        }

        private void updateFormName()
        {
            this.Text = "ImgBrowser - " + Path.GetFileName(pictureBox1.ImageLocation);
        }

        private string[] updateFileList()
        {
            // TODO F5 on empty image crashes app
            if (pictureBox1.ImageLocation != "")
            {
                IEnumerable<string> files = Directory.EnumerateFiles(Path.GetDirectoryName(pictureBox1.ImageLocation), "*.*", SearchOption.TopDirectoryOnly)
                .Where(s => s.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || s.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || s.EndsWith(".gif", StringComparison.OrdinalIgnoreCase));
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
            if (me.Button.ToString() == "Middle")
            {
                if (this.WindowState == FormWindowState.Maximized)
                {
                    if (this.FormBorderStyle == FormBorderStyle.None)
                    {
                        this.FormBorderStyle = FormBorderStyle.Sizable;
                        this.WindowState = FormWindowState.Maximized;
                    }
                    else
                    {
                        this.FormBorderStyle = FormBorderStyle.None;
                    }
                }
                else
                {
                    this.FormBorderStyle = FormBorderStyle.None;
                    this.WindowState = FormWindowState.Maximized;
                }
            }
            else if ((me.Button.ToString() == "Right") && (pictureBox1.ImageLocation != ""))
            {
                if (pictureBox1.SizeMode == PictureBoxSizeMode.AutoSize)
                {
                    panel1.HorizontalScroll.Value = 0;
                    panel1.VerticalScroll.Value = 0;
                    pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
                    pictureBox1.Dock = DockStyle.Fill;
                }
                else if ((pictureBox1.Image.Width > this.Width) || (pictureBox1.Image.Height > this.Height))
                {
                    pictureBox1.SizeMode = PictureBoxSizeMode.AutoSize;
                    pictureBox1.Dock = DockStyle.None;
                }
            }
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
                pictureBox1.ImageLocation = cmdArgs[1];
                updateFormName();
                fileEntries = updateFileList();
            }

        }

        // TODO This barely works atm
        public void PictureBoxZoom(Image img, Size size)
        {
            //Bitmap bm = new Bitmap(img, Convert.ToInt32(img.Width * size.Width), Convert.ToInt32(img.Height * size.Height));
            //Bitmap bm = new Bitmap(img, Convert.ToInt32(img.Width * 1.5), Convert.ToInt32(img.Height * 1.5));

            //https://stackoverflow.com/questions/1922040/how-to-resize-an-image-c-sharp

            // The bitmap and the graphic will both need to be resized
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
            
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            Console.WriteLine(files[0]);

            if (files[0].Contains(".jpg")|| files[0].Contains(".png")|| files[0].Contains(".gif"))
            {
                pictureBox1.ImageLocation = files[0];

                updateFormName();
                fileEntries = updateFileList();
                pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            }
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
                currentPositionX = Cursor.Position.X;
                currentPositionY = Cursor.Position.Y;

                if (pictureBox1.SizeMode == PictureBoxSizeMode.AutoSize)
                {
                    pictureBox1.Cursor = Cursors.Cross;
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
            

        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            //int directionX;
            //int directionY;

            // Add some leeway to mouse movement
            // Also used to make scroll speed dynamic
            double scrollOffsetX = pictureBox1.Width * 0.04;
            double scrollOffsetY = pictureBox1.Height * 0.04;

            if (e.Button.ToString() == "Left")
            {
                if (Cursor.Position.X - scrollOffsetX > currentPositionX)
                {
                    if (panel1.HorizontalScroll.Value + scrollOffsetX * 0.1 <= panel1.HorizontalScroll.Maximum)
                    {
                        panel1.HorizontalScroll.Value += (int)(scrollOffsetX * 0.1);
                    }
                    else
                    {
                        panel1.HorizontalScroll.Value = panel1.HorizontalScroll.Maximum;
                    }

                }
                else if (Cursor.Position.X + scrollOffsetX < currentPositionX)
                {
                    if (panel1.HorizontalScroll.Value - scrollOffsetX * 0.1 >= panel1.HorizontalScroll.Minimum)
                    {
                        panel1.HorizontalScroll.Value -= (int)(scrollOffsetX * 0.1);
                    }
                    else
                    {
                        panel1.HorizontalScroll.Value = panel1.HorizontalScroll.Minimum;
                    }

                }

                if (Cursor.Position.Y - scrollOffsetY > currentPositionY)
                {
                    if (panel1.VerticalScroll.Value + scrollOffsetY * 0.1 <= panel1.VerticalScroll.Maximum)
                    {
                        panel1.VerticalScroll.Value += (int)(scrollOffsetY * 0.1);
                    }
                    else
                    {
                        panel1.VerticalScroll.Value = panel1.VerticalScroll.Maximum;
                    }
                }
                else if (Cursor.Position.Y + scrollOffsetY < currentPositionY)
                {
                    if (panel1.VerticalScroll.Value - scrollOffsetY * 0.1 >= panel1.VerticalScroll.Minimum)
                    {
                        panel1.VerticalScroll.Value -= (int)(scrollOffsetY * 0.1);
                    }
                    else
                    {
                        panel1.VerticalScroll.Value = panel1.VerticalScroll.Minimum;
                    }

                }


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
    }
}
