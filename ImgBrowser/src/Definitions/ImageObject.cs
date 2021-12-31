using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ImgBrowser
{
    public class ImageObject
    {
        public string FullFilename;
        public Bitmap ImageData { get => VerifyImg(FullFilename); }
        public string Name { get => FullFilename == "" ? "" : System.IO.Path.GetFileName(FullFilename); }
        public string Path { get => FullFilename == "" ? "" : System.IO.Path.GetDirectoryName(FullFilename)?.TrimEnd('\\'); }
        public bool Valid { get => File.Exists(FullFilename); }

        public ImageObject(string file)
        {
            FullFilename = file;
        }

        private Bitmap VerifyImg(string file)
        {
            try
            {
                // Check if image can be loaded
                using (var temp = new Bitmap(file))
                    return new Bitmap(temp);
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
    }
}
