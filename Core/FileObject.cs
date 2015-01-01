using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace IntelliSync
{
    public enum FILESTATUS
    {
        New,
        NewCopy,
        NewReplaced,
        Deleted,
        Moved,
        Replaced,
        Changed,
        Unchanged,
        UnchangedModified,
        Unknown,
        Error
    }

    public class FileObject
    {
        private string _filepath;
        public string Filepath
        {
            get { return _filepath; }
            private set { _filepath = value; Subpath = Path.GetDirectoryName(_filepath); }
        }
        public string Hash { get; set; }
        public string Subpath { get; private set; }
        public long Size { get; set; }
        public long ChangeDate { get; set; }
        public FileStatusInformation StatusInformation { get; set; }
        
        private FileObject()
        {
            StatusInformation=new FileStatusInformation(FILESTATUS.Unknown,"");
        }

        public FileObject(string filepath, FILESTATUS status)
            : this()
        {
            Filepath = filepath;
            StatusInformation.Status = status;
        }
        public FileObject(string filepath, string hash, long size, long changedate)
            : this()
        {
            Filepath = filepath;
            Hash = hash;
            Size = size;
            ChangeDate = changedate;
        }

        public static FileObject Instance(string abspath)
        {
            return Instance(abspath, "");
        }

        public static FileObject Instance(string abspath, string basepath)
        {
            return Instance(abspath, basepath, MD5.Create());
        }
        /// <summary>
        /// Aus einer existierenden Datei einen Eintrag erstellen, sprich den Hash generieren.
        /// </summary>
        /// <param name="AbsPath"></param>
        /// <returns></returns>
        public static FileObject Instance(string abspath, string basepath, MD5 md)
        {
            // Hashen
            using (FileStream fs = File.OpenRead(abspath))
            {
                long size = fs.Length;
                byte[] hash = md.ComputeHash(fs);
                fs.Close();
                return new FileObject(abspath.Substring(basepath.Length), System.Text.ASCIIEncoding.ASCII.GetString(hash), size, File.GetLastWriteTime(abspath).Ticks);
            }
        }
    }
}
