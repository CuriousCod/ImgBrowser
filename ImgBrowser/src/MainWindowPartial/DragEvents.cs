﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ImgBrowser
{
    // Support for dragging images from the application to other software
    // https://stackoverflow.com/questions/1845654/how-to-use-filegroupdescriptor-to-drag-file-to-explorer-c-sharp

    public partial class MainWindow
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        struct FILEDESCRIPTOR
        {
            public UInt32 dwFlags;
            public Guid clsid;
            public System.Drawing.Size sizel;
            public System.Drawing.Point pointl;
            public UInt32 dwFileAttributes;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
            public UInt32 nFileSizeHigh;
            public UInt32 nFileSizeLow;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public String cFileName;
        }

        public const string CFSTR_PREFERREDDROPEFFECT = "Preferred DropEffect";
        public const string CFSTR_PERFORMEDDROPEFFECT = "Performed DropEffect";
        public const string CFSTR_FILEDESCRIPTORW = "FileGroupDescriptorW";
        public const string CFSTR_FILECONTENTS = "FileContents";

        public const Int32 FD_WRITESTIME = 0x00000020;
        public const Int32 FD_FILESIZE = 0x00000040;
        public const Int32 FD_PROGRESSUI = 0x00004000;

        public struct DragFileInfo
        {
            public string FileName;
            public string SourceFileName;
            public DateTime WriteTime;
            public Int64 FileSize;

            public DragFileInfo(string fileName)
            {
                FileName = Path.GetFileName(fileName);
                SourceFileName = fileName;
                WriteTime = DateTime.Now;
                FileSize = (new FileInfo(fileName)).Length;
            }
        }

        private MemoryStream GetFileDescriptor(DragFileInfo fileInfo)
        {
            MemoryStream stream = new MemoryStream();
            stream.Write(BitConverter.GetBytes(1), 0, sizeof(UInt32));

            FILEDESCRIPTOR fileDescriptor = new FILEDESCRIPTOR();

            fileDescriptor.cFileName = fileInfo.FileName;
            Int64 fileWriteTimeUtc = fileInfo.WriteTime.ToFileTimeUtc();
            fileDescriptor.ftLastWriteTime.dwHighDateTime = (Int32)(fileWriteTimeUtc >> 32);
            fileDescriptor.ftLastWriteTime.dwLowDateTime = (Int32)(fileWriteTimeUtc & 0xFFFFFFFF);
            fileDescriptor.nFileSizeHigh = (UInt32)(fileInfo.FileSize >> 32);
            fileDescriptor.nFileSizeLow = (UInt32)(fileInfo.FileSize & 0xFFFFFFFF);
            fileDescriptor.dwFlags = FD_WRITESTIME | FD_FILESIZE | FD_PROGRESSUI;

            Int32 fileDescriptorSize = Marshal.SizeOf(fileDescriptor);
            IntPtr fileDescriptorPointer = Marshal.AllocHGlobal(fileDescriptorSize);
            Byte[] fileDescriptorByteArray = new Byte[fileDescriptorSize];

            try
            {
                Marshal.StructureToPtr(fileDescriptor, fileDescriptorPointer, true);
                Marshal.Copy(fileDescriptorPointer, fileDescriptorByteArray, 0, fileDescriptorSize);
            }
            finally
            {
                Marshal.FreeHGlobal(fileDescriptorPointer);
            }
            stream.Write(fileDescriptorByteArray, 0, fileDescriptorByteArray.Length);
            return stream;
        }

        private MemoryStream GetFileContents(DragFileInfo fileInfo)
        {
            MemoryStream stream = new MemoryStream();
            using (BinaryReader reader = new BinaryReader(File.OpenRead(fileInfo.SourceFileName)))
            {
                Byte[] buffer = new Byte[fileInfo.FileSize];
                reader.Read(buffer, 0, (Int32)fileInfo.FileSize);
                if (buffer.Length == 0) buffer = new Byte[1];
                stream.Write(buffer, 0, buffer.Length);
            }
            return stream;
        }
    }
}
