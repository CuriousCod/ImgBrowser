using System.Windows.Forms;

namespace ImgBrowser
{
    partial class CaptureLayer
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.panel1 = new System.Windows.Forms.Panel();
            this.captureBox = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize) (this.captureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(200, 100);
            this.panel1.TabIndex = 1;
            // 
            // captureBox
            // 
            this.captureBox.BackColor = System.Drawing.Color.Blue;
            this.captureBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.captureBox.Location = new System.Drawing.Point(0, 0);
            this.captureBox.Name = "captureBox";
            this.captureBox.Size = new System.Drawing.Size(4940, 1080);
            this.captureBox.TabIndex = 0;
            this.captureBox.TabStop = false;
            this.captureBox.Paint += new System.Windows.Forms.PaintEventHandler(this.captureBox_Paint);
            this.captureBox.MouseMove += new System.Windows.Forms.MouseEventHandler(this.captureBox_MouseMove);
            // 
            // CaptureLayer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Blue;
            this.ClientSize = new System.Drawing.Size(4940, 1080);
            this.Controls.Add(this.captureBox);
            this.Controls.Add(this.panel1);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.KeyPreview = true;
            this.Name = "CaptureLayer";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "CaptureLayer";
            this.TopMost = true;
            this.TransparencyKey = System.Drawing.Color.Blue;
            this.Deactivate += new System.EventHandler(this.CaptureLayer_Deactivate);
            this.Load += new System.EventHandler(this.CaptureLayer_Load);
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.CaptureLayer_KeyUp);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.CaptureLayer_MouseMove);
            ((System.ComponentModel.ISupportInitialize) (this.captureBox)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion

        private Panel panel1;
        private System.Windows.Forms.PictureBox captureBox;
    }

}