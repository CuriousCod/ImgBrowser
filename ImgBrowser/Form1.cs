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

namespace ImgBrowser
{

    public partial class Form1 : Form
    {
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
            if (e.Delta > 0)
            {
                browseForward();
            }
            else if (e.Delta < 0)
            {
                browseBackward();
            }
            
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            //Console.WriteLine(e.KeyCode);

            if (e.KeyCode.ToString() == "Left")
            {
                browseBackward();
            }
            else if (e.KeyCode.ToString() == "Right")
            {
                browseForward();
            }
        }
        private void browseForward()
        {
            if (pictureBox1.ImageLocation != "") { 
                string[] fileEntries = listFiles(Path.GetDirectoryName(pictureBox1.ImageLocation));

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
            }
        }

        private void browseBackward()
        {
            if (pictureBox1.ImageLocation != "")
            {
                string[] fileEntries = listFiles(Path.GetDirectoryName(pictureBox1.ImageLocation));

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
            }
        }

        //TODO This function rechecks all the files whenever moving to next/previous file -> Inefficient
        private string[] listFiles(string dir)
        {
            //string[] fileEntries = Directory.GetFiles(@"G:\MEGA\Pics\new\move\Other\UMP9\");
            var files = Directory.EnumerateFiles(dir, "*.*", SearchOption.TopDirectoryOnly)
                .Where(s => s.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || s.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || s.EndsWith(".gif", StringComparison.OrdinalIgnoreCase));

            string[] fileEntries = files.ToArray();

            return fileEntries;
        }

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
                    //pictureBox1.Dock = DockStyle.Fill;
                    this.FormBorderStyle = FormBorderStyle.None;
                    this.WindowState = FormWindowState.Maximized;
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
            }
            
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            Console.WriteLine(files[0]);

            if (files[0].Contains(".jpg")|| files[0].Contains(".png")|| files[0].Contains(".gif"))
            {
                pictureBox1.ImageLocation = files[0];
            }
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            // Change cursor graphic
            e.Effect = DragDropEffects.Move;
        }
    }
}
