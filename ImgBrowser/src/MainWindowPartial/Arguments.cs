using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ImgBrowser.Helpers;

namespace ImgBrowser
{
    public partial class MainWindow
    {
        private void ProcessLaunchArguments(string[] cmdArgs)
        {
            CheckPreImageLoadArguments(ref cmdArgs);

            var skipImageFileLoad = cmdArgs.Contains(Definitions.LaunchArguments.SkipImageFileLoading);
            
            if (cmdArgs.Length > 1 && !skipImageFileLoad)
            {
                rootImageFolderChanged?.Invoke(string.Empty);
                LoadNewImgFromFile(new ImageObject(cmdArgs[1]));
            }
            else
            {
                LoadNewImgFromClipboard();
            }
            
            CheckPostImageLoadArguments(ref cmdArgs);
        }

        private void CheckPreImageLoadArguments(ref string[] cmdArgs)
        {
            for (var i = 0; i < cmdArgs.Length; i++)
            {
                switch (cmdArgs[i])
                {
                    case Definitions.LaunchArguments.GifDelay:
                        if (int.TryParse(cmdArgs[i + 1], out var delay))
                        {
                            delay = Math.Max(1, Math.Min(1000, delay));
                            launchArgAnimationDelay = delay;
                        }
                        return;
                }
            }
        }
        
        private void CheckPostImageLoadArguments(ref string[] cmdArgs)
        {
            for (var i = 0; i < cmdArgs.Length; i++)
            {
                switch (cmdArgs[i])
                {
                    case Definitions.LaunchArguments.CenterWindowToMouse:
                        Top = Cursor.Position.Y - ClientSize.Height / 2;
                        Left = Cursor.Position.X - ClientSize.Width / 2;
                        break;
                    case Definitions.LaunchArguments.SetWidthAndHeight:
                        var sizeValues = cmdArgs[i + 1].Split(',');

                        if (sizeValues.Length != 2)
                            return;

                        if (!int.TryParse(sizeValues[0], out int formWidth) || !int.TryParse(sizeValues[1], out int formHeight))
                            return;

                        if (formWidth == 0 || formHeight == 0)
                            return;

                        ClientSize = new Size(formWidth, formHeight);
                        break;
                    case Definitions.LaunchArguments.SetWindowPosition:
                        var posValues = cmdArgs[i + 1].Split(',');

                        if (posValues.Length != 2)
                            return;

                        if (!int.TryParse(posValues[0], out int posLeft) || !int.TryParse(posValues[1], out int posTop))
                            return;

                        Top = posTop;
                        Left = posLeft;
                        break;
                    case Definitions.LaunchArguments.HideWindowBackground:
                        TransparencyKey = BackColor;
                        break;
                    case Definitions.LaunchArguments.SetRotation:
                        if (int.TryParse(cmdArgs[i + 1], out var direction))
                        {
                            if (direction > 3 || direction < 0)
                                break;

                            for (var x = 0; x < direction; x++)
                            {
                                RotateImage();
                            }
                        }
                        break;
                    case Definitions.LaunchArguments.FlipX:
                        FlipImageX();
                        break;
                    case Definitions.LaunchArguments.SetBorderless:
                        if (pictureBox1.Image != null)
                            FitImageToWindow();
                        break;
                    case Definitions.LaunchArguments.LockImage:
                        lockImage = true;
                        break;
                    case Definitions.LaunchArguments.SetAlwaysOnTop:
                        TopMost = true;
                        break;
                    default:
                        break;
                }
            }
        }
        
        /// <summary>
        /// Gets the current application settings and converts them into launch arguments
        /// </summary>
        /// <returns>Current arguments as a string</returns>
        private string GetCurrentArgs()
        {
            var args = "";

            if (TransparencyKey == BackColor)
                args += Definitions.LaunchArguments.HideWindowBackground + " ";
            if (lockImage)
                args += Definitions.LaunchArguments.LockImage + " ";
            if (FormBorderStyle == FormBorderStyle.None && WindowState != FormWindowState.Maximized)
                args += Definitions.LaunchArguments.SetBorderless + " ";
            if (TopMost)
                args += Definitions.LaunchArguments.SetAlwaysOnTop + " ";
            if (pictureBox1.ImageXFlipped)
                args += Definitions.LaunchArguments.FlipX + " ";
            if (GifAnimator.CanAnimate(currentImg.Image))
                args += $"{Definitions.LaunchArguments.GifDelay} {GifAnimator.AnimationDelay} ";

            args += $"{Definitions.LaunchArguments.SetRotation} {pictureBox1.ImageRotation} ";
            args += $"{Definitions.LaunchArguments.SetWidthAndHeight} {RestoreBounds.Width},{RestoreBounds.Height} ";

            return args;
        }
    }
    
}