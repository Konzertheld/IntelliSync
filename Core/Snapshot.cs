using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SQLite;

namespace IntelliSync
{
    public class Snapshot
    {
        private static SQLiteConnection connection;

        public Snapshot()
            : this(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\" + Core.AppName + "\\tempsnapshot.db")
        {
        }

        public Snapshot(string path)
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            if (!System.IO.File.Exists(path))
                System.IO.File.Create(path).Close();
            connection = new SQLiteConnection("Data Source=" + path);
            connection.Open();
            MakeSureTableExists();
        }

        private void MakeSureTableExists()
        {
            using (SQLiteCommand sqc = new SQLiteCommand(connection))
            {
                sqc.CommandText = "CREATE TABLE IF NOT EXISTS `Files` (`id` INTEGER NOT NULL PRIMARY KEY, `path` TEXT NOT NULL, `changedate` INTEGER NOT NULL, `size` INTEGER NOT NULL, `hash` TEXT NOT NULL)";
                sqc.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Write or update files and hashes.
        /// </summary>
        /// <param name="liste"></param>
        public void WriteFiles(List<FileObject> liste)
        {
            using (SQLiteTransaction sqt = connection.BeginTransaction())
            {
                using (SQLiteCommand sqc = new SQLiteCommand(connection))
                {
                    // Alle vorhandenen Einträge löschen, da diese in jedem Fall überschrieben werden sollten
                    sqc.CommandText = "DELETE FROM Files WHERE path=:path";
                    sqc.Parameters.Add(new SQLiteParameter("path"));
                    foreach (FileObject d in liste)
                    {
                        sqc.Parameters["path"].Value = d.Filepath;
                        sqc.ExecuteNonQuery();
                    }
                    //sqc.Parameters.Clear(); // Nötig?

                    // Dateien eintragen
                    sqc.CommandText = "INSERT INTO Files (path, hash, size, changedate) VALUES (:path, :hash, :size, :changedate)";
                    sqc.Parameters.Add(new SQLiteParameter("path"));
                    sqc.Parameters.Add(new SQLiteParameter("hash"));
                    sqc.Parameters.Add(new SQLiteParameter("size"));
                    sqc.Parameters.Add(new SQLiteParameter("changedate"));
                    foreach (FileObject d in liste)
                    {
                        sqc.Parameters["path"].Value = d.Filepath;
                        sqc.Parameters["hash"].Value = d.Hash;
                        sqc.Parameters["size"].Value = d.Size;
                        sqc.Parameters["changedate"].Value = d.ChangeDate;
                        sqc.ExecuteNonQuery();
                    }
                }
                sqt.Commit();
            }
        }

        public Dictionary<string, FileObject> GetFilelist()
        {
            using (SQLiteCommand sqc = new SQLiteCommand(connection))
            {
                sqc.CommandText = "SELECT path, hash, size, changedate FROM Files";
                using (SQLiteDataReader sqr = sqc.ExecuteReader())
                {
                    Dictionary<string, FileObject> filelist = new Dictionary<string, FileObject>();
                    while (sqr.Read())
                    {
                        filelist.Add(sqr.GetString(0), new FileObject(sqr.GetString(0), sqr.GetString(1), sqr.GetInt64(2), sqr.GetInt64(3)));
                    }
                    return filelist;
                }
            }
        }

        public bool CheckHash(string hash)
        {
            using (SQLiteCommand sqc = new SQLiteCommand(connection))
            {
                sqc.CommandText = "SELECT id FROM Files WHERE hash=:hash";
                sqc.Parameters.Add(new SQLiteParameter("hash", hash));
                return sqc.ExecuteReader().HasRows;
            }
        }

        public string GetFilepath(string hash)
        {
            using (SQLiteCommand sqc = new SQLiteCommand(connection))
            {
                sqc.CommandText = "SELECT path FROM Files WHERE hash=:hash";
                sqc.Parameters.Add(new SQLiteParameter("hash", hash));
                object result = sqc.ExecuteScalar();
                return (result == null) ? null : (string)result;
            }
        }

        public void CloseDB()
        {
            connection.Close();
        }

        // Destructor and dispose stuff go here
    }
}
