using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImgBrowser
{

    // Used to create a temporary graphics layer when capturing screen
    public partial class CaptureLayer : Form
    {
        private int mouseStartX;
        private int mouseStartY;
        private int offsetX;

        // Start screen capture
        private bool capturing = true;

        public CaptureLayer()
        {
            InitializeComponent();

            mouseStartX = Cursor.Position.X;
            mouseStartY = Cursor.Position.Y;
            offsetX = GetLeftmostScreenStartPoint();

            // Follow mouse actions globally, not needed anymore
            // SetHook();

            ShowDialog();
        }

        // Finds the leftmost screen, screens left of the main screen are in minus coordinates
        // TODO This does not take vertical or some weird screen setups into consideration
        public int GetLeftmostScreenStartPoint()
        {
            int lowestX = 0;

            foreach (Screen screeny in Screen.AllScreens)
            {
                if (screeny.Bounds.Left < lowestX) lowestX = screeny.Bounds.Left;
            }

            return lowestX;
        }


        // Generates a rectangle based on given values
        static public Rectangle GetRectangle(Point p1, Point p2)
        {
            return new Rectangle(Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y),
                Math.Abs(p1.X - p2.X), Math.Abs(p1.Y - p2.Y));
        }

        private void CaptureLayer_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode.ToString())
            {
                // Capture screen from the rectangle drawn by cursor
                // https://stackoverflow.com/questions/13103682/draw-a-bitmap-image-on-the-screen
                case "S":
                    capturing = false;
                    
                    // Clear rectangle drawing
                    captureBox.Refresh();

                    // Create rectangle from current coordinates
                    Rectangle rect = GetRectangle(new Point(mouseStartX, mouseStartY), Cursor.Position);

                    if (rect.Width == 0 || rect.Height == 0) break;

                    Bitmap BM = new Bitmap(rect.Width, rect.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    using (Graphics g = Graphics.FromImage(BM))
                    {
                        g.CopyFromScreen(rect.Left, rect.Top, 0, 0, rect.Size);
                        Clipboard.SetImage(BM);
                    }

                    BM.Dispose();

                    // Stop following mouse globally, not needed anymore
                    //UnHook();

                    // Close form
                    Close();

                    break;

                default:
                    UnHook();
                    Close();
                    break;
            }
        }

        // Does not work when form is transparent, worthless
        private void CaptureLayer_MouseMove(object sender, MouseEventArgs e)
        {/*
            Refresh();
            if (capturing) {
                using (Graphics g = CreateGraphics())
                {
                    Rectangle rect = GetRectangle(new Point(mouseStartX - offsetX, mouseStartY), e.Location);
                    g.DrawRectangle(Pens.Red, rect);
                }
            }
        */
        }

        private void CaptureLayer_Load(object sender, EventArgs e)
        {
            // Fill monitors with the invisible form
            ClientSize = new System.Drawing.Size(SystemInformation.VirtualScreen.Width, SystemInformation.VirtualScreen.Height);
            Location = new System.Drawing.Point(GetLeftmostScreenStartPoint(), 0);

            // TODO This doesn't actually show up properly, since the form is transparent (shows up when mouse is on rectangle -_-)
            Cursor = System.Windows.Forms.Cursors.Cross;
        }



        private void captureBox_MouseMove(object sender, MouseEventArgs e)
        {
            // Refresh picturebox to draw rectangle
            if (capturing) captureBox.Refresh();

            // Previously used captureBox.CreateGraphics()
            // Don't use that as it causes flickering
            // Paint works better
        }

        // Drawing the selection "rubber band" on a picturebox, since the drawing refuses to display on transparent form window. 
        // Works fine on a transparent picturebox though >_>
        // There's also some weird "feature" where the drawing only works on some form background colors, blue is confirmed to work
        private void captureBox_Paint(object sender, PaintEventArgs e)
        {
            if (capturing)
            {
                Graphics g = e.Graphics;

                Rectangle rect = GetRectangle(new Point(mouseStartX - offsetX, mouseStartY), new Point(Cursor.Position.X - offsetX, Cursor.Position.Y));
                g.DrawRectangle(Pens.Red, rect);
            }

        }

        ///
        /// The following madness tracks mouse actions globally, not actually needed anymore
        ///

        // https://stackoverflow.com/questions/17196965/how-do-i-create-a-fully-transparent-winform-in-c-sharp-that-is-interactive

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hInstance, int threadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern bool UnhookWindowsHookEx(int idHook);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern int CallNextHookEx(int idHook, int nCode, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        public class MouseStruct
        {
            public Point pt;
            public int hwnd;
            public int wHitTestCode;
            public int dwExtraInfo;
        }

        public delegate int HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        private int hHook;
        public const int WH_MOUSE_LL = 14;
        public static HookProc hProc;

        public int SetHook()
        {
            hProc = new HookProc(MouseHookProc);
            hHook = SetWindowsHookEx(WH_MOUSE_LL, hProc, IntPtr.Zero, 0);
            return hHook;
        }
        public void UnHook()
        {
            UnhookWindowsHookEx(hHook);
        }
        //callback function, invoked when there is an mouse event
        private int MouseHookProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            var MyMouseStruct = (MouseStruct)Marshal.PtrToStructure(lParam, typeof(MouseStruct));
            if (nCode < 0)
            {
                return CallNextHookEx(hHook, nCode, wParam, lParam);
            }
            else
            {
                // Check for mouse actions
                if (wParam == (IntPtr)513)
                {
                    //click
                }
                else if (wParam == (IntPtr)512)
                {
                    //move

                    // Reduce cpu load
                    Thread.Sleep(10);

                    Refresh();
                    if (capturing)
                    {
                        using (Graphics g = CreateGraphics())
                        {
                            Rectangle rect = GetRectangle(new Point(mouseStartX - offsetX, mouseStartY), new Point(Cursor.Position.X - offsetX, Cursor.Position.Y));
                            g.DrawRectangle(Pens.Red, rect);
                        }
                    }
                }
                else if (wParam == (IntPtr)512)
                {
                    //release
                }

                Cursor.Position = MyMouseStruct.pt;
                //stop the event from passed to other windows.
                return 1;
            }
        }
    }
}
