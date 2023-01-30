using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ImgBrowser.Helpers;
using ImgBrowser.Helpers.WebpSupport;

namespace ImgBrowser
{
    public class ImageObject
    {
        public string FullFilename;
        public Bitmap Image => image ?? (image = LoadFileAsBitmap(FullFilename));
        public string Name => FullFilename == "" ? "" : System.IO.Path.GetFileName(FullFilename);
        public string Path => FullFilename == "" ? "" : System.IO.Path.GetDirectoryName(FullFilename)?.TrimEnd('\\');
        public bool IsFile => File.Exists(FullFilename);

        private Bitmap image;
        private bool imageValidated;
        private IntPtr imagePtr;

        public ImageObject(string file)
        {
            FullFilename = file;
        }

        private Bitmap LoadFileAsBitmap(string file)
        {
            try
            {
                if (!file.EndsWith(".webp"))
                {
                    return (Bitmap) GdiApi.GetImageWithoutLock(file, ref imagePtr);
                }

                return !NativeWebPDecoder.IsDllAvailable() ? null : WebPDecoder.DecodeBGRA(file);
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
            if (imageValidated)
            {
                return;
            }
            
            if (imagePtr == IntPtr.Zero)
            {
                return;
            }
            
            GdiApi.ValidateImage(imagePtr);
            
            imageValidated = true;
        }
    }
}
