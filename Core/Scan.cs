using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IntelliSync
{
    public class Scan
    {
        /// <summary>
        /// Scan a folder for files and put them with hashes into the database.
        /// </summary>
        /// <param name="path"></param>
        public static List<FileObject> ScanFolder(string path)
        {
            List<FileObject> l = new List<FileObject>();
            System.Security.Cryptography.MD5 md = System.Security.Cryptography.MD5.Create();
            foreach (string s in System.IO.Directory.EnumerateFiles(path, "*", System.IO.SearchOption.AllDirectories))
            {
                FileObject d = FileObject.Instance(s, path, md);
                l.Add(d);
            }
            return l;
        }
    }
}
