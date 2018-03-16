using System;
using System.IO;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using Newtonsoft.Json;
using System.Xml.Linq;
using Expando;
using System.Dynamic;
using System.Collections.Generic;

namespace ModTest
{
    class Program
    {
        static void Main(string[] args)
        {
            if(args[0] == "CheckSharedBytes")
            {
                decimal size = Logic.getHardlinkedSize(args[1], args[2]);
                Console.WriteLine(size);
            }
            

        }
    }
    public class Logic
    {
        public static bool compareFileIndex(string file1, string file2)
        {
            return Kernel32.GetFileIndex(file1) == Kernel32.GetFileIndex(file2);
        }
        public static List<iFile> generateIndexList(string path)
        {
            List<iFile> indexList = new List<iFile>();

            string[] folder = Directory.GetFiles(path,"*",SearchOption.AllDirectories);
            foreach(string file in folder)
            {
                FileInfo fi = new FileInfo(file);
                if (fi.Attributes.HasFlag(FileAttributes.ReparsePoint))
                    continue;
                indexList.Add(new iFile {
                    fInfo = fi,
                    index = Kernel32.GetFileIndex(file)});
            }
            return indexList;
        }
        public static decimal getHardlinkedSize(string folder1, string folder2)
        {
            List<iFile> list1 = generateIndexList(folder1);
            List<iFile> list2 = generateIndexList(folder2);

            decimal size = 0;

            foreach(iFile f2 in list2)
            {
                foreach (iFile f in list1)
                {
                    if(f.index == f2.index)
                    {
                        size = size + f.fInfo.Length;
                    }
                }
            }
            return size;
        }


    }
    public class iFile
    {
        public FileInfo fInfo { get; set; }
        public UInt64 index { get; set; }
    }
    public static class Kernel32
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct BY_HANDLE_FILE_INFORMATION
        {
            public uint FileAttributes;
            public FILETIME CreationTime;
            public FILETIME LastAccessTime;
            public FILETIME LastWriteTime;
            public uint VolumeSerialNumber;
            public uint FileSizeHigh;
            public uint FileSizeLow;
            public uint NumberOfLinks;
            public uint FileIndexHigh;
            public uint FileIndexLow;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetFileInformationByHandle(
            Microsoft.Win32.SafeHandles.SafeFileHandle hFile,
            out BY_HANDLE_FILE_INFORMATION lpFileInformation
        );
        public static void GetFileInformation(string path, out BY_HANDLE_FILE_INFORMATION info)
        {
            using (FileStream file = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                if (!GetFileInformationByHandle(file.SafeFileHandle, out info))
                {
                    throw new Win32Exception();
                }
            }
        }
        public static UInt64 GetFileIndex(string path)
        {
            BY_HANDLE_FILE_INFORMATION info;
            GetFileInformation(path, out info);
            return info.FileIndexLow | ((UInt64)info.FileIndexHigh << 32);
        }
        public static Tuple<uint, ulong> GetFileIdentifier(string path)
        {
            BY_HANDLE_FILE_INFORMATION info;
            GetFileInformation(path, out info);
            return new Tuple<uint, ulong>(info.VolumeSerialNumber,
                                                info.FileIndexLow | ((UInt64)info.FileIndexHigh << 32));
        }
        public static uint GetNumHardLinks(string path)
        {
            BY_HANDLE_FILE_INFORMATION info;
            GetFileInformation(path, out info);
            return info.NumberOfLinks;
        }
    }
}

