using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ImgBrowser.Helpers;

namespace ImgBrowser
{
    public class ImageObject
    {
        public string FullFilename;
        public Bitmap Image => _image ?? (_image = VerifyImg(FullFilename));
        public string Name => FullFilename == "" ? "" : System.IO.Path.GetFileName(FullFilename);
        public string Path => FullFilename == "" ? "" : System.IO.Path.GetDirectoryName(FullFilename)?.TrimEnd('\\');
        public bool IsFile => File.Exists(FullFilename);

        private Bitmap _image;
        private bool _imageValidated;
        private IntPtr _imagePtr;

        public Bitmap[] Frames = { };

        public int FrameIndex = 0;

        public ImageObject(string file)
        {
            FullFilename = file;
        }

        private Bitmap VerifyImg(string file)
        {
            try
            {
                return (Bitmap) GdiApi.GetImage(file, ref _imagePtr);
            }
            catch (OutOfMemoryException ex)
            {
                Console.WriteLine(ex);
                return null;
            }
            // This actually doesn't catch these errors, since it also requires HandleProcessCorruptedStateExceptions 
            // Got this one while trying to access corrupt image
            // https://social.msdn.microsoft.com/Forums/vstudio/en-US/4de25cc0-9235-4e40-9cd7-d7c934d78cc6/sehexception-is-not-caught-in-managed-code-windows-just-kills-the-process?forum=clr
            catch (SEHException ex)
            {
                Console.WriteLine(ex);
                return null;
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine(ex);
                return null;
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(ex);
                return null;
            }
        }

        /// <summary>
        /// Copies current image to RAM
        /// Will speed up image rendering (redraws faster) in picture box autosize mode
        /// </summary>
        public void CopyImageToMemory()
        {
            if (_imageValidated)
            {
                return;
            }
            
            if (_imagePtr == IntPtr.Zero)
            {
                return;
            }
            
            GdiApi.ValidateImage(_imagePtr);
            
            _imageValidated = true;
        }
        
        public bool IsAnimated()
        {
            return Name.EndsWith(".gif");
        }
        
        public Task<bool> GetFrames(CancellationToken token)
        {
            if (_imagePtr == IntPtr.Zero)
            {
                return Task.FromResult(false);
            }
            
            GdiApi.CloneImage(_imagePtr, out var imagePtr);
            var clone = (Bitmap) GdiApi.CreateImageObject(imagePtr);
            
            return Task.Run(() =>
            {
                if (clone == null)
                {
                    return false;
                }
            
                var numberOfFrames = clone.GetFrameCount(FrameDimension.Time);
                var frames = new Bitmap[numberOfFrames];

                for (var i = 0; i < numberOfFrames; i++)
                {
                    if (token.IsCancellationRequested)
                    {
                        return false;
                    }
                    
                    clone.SelectActiveFrame(FrameDimension.Time, i);

                    GdiApi.CloneImage(imagePtr, out var clonePtr);
                    frames[i] = new Bitmap(GdiApi.CreateImageObject(clonePtr));
                }

                Frames = frames;
                return true;

            }, token);
        }

        public void IncrementFrame()
        {
            if (Frames.Length == 0)
            {
                return;
            }
            
            FrameIndex++;
            
            if (FrameIndex >= Frames.Length)
            {
                FrameIndex = 0;
            }
        }
        
        /// <summary>
        /// Gets the delay between frames in a gif
        /// </summary>
        /// <returns> Delay in milliseconds </returns>
        public int GetFrameDelay()
        {
            var frameDelay = Image.GetPropertyItem (0x5100);
            return (frameDelay.Value [0] + frameDelay.Value [1] * 256) * 10;
        }
    }
}
