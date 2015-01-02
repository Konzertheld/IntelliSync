using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Data.Sqlite;

namespace IntelliSync
{
	public class Snapshot
	{
		private static SqliteConnection connection;

		public Snapshot()
			: this(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\" + Core.AppName + "\\tempsnapshot.db")
		{
		}

		public Snapshot(string path)
		{
			System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
			if (!System.IO.File.Exists(path))
				System.IO.File.Create(path).Close();
			connection = new SqliteConnection("Data Source=" + path);
			connection.Open();
			MakeSureTableExists();
		}

		private void MakeSureTableExists()
		{
			using (SqliteCommand sqc = new SqliteCommand(connection)) {
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
			using (SqliteTransaction sqt = connection.BeginTransaction()) {
				using (SqliteCommand sqc = new SqliteCommand(connection)) {
					// Alle vorhandenen Einträge löschen, da diese in jedem Fall überschrieben werden sollten
					sqc.CommandText = "DELETE FROM Files WHERE path=:path";
					sqc.Parameters.Add(new SqliteParameter("path"));
					foreach (FileObject d in liste) {
						sqc.Parameters["path"].Value = d.Filepath;
						sqc.ExecuteNonQuery();
					}
					//sqc.Parameters.Clear(); // Nötig?

					// Dateien eintragen
					sqc.CommandText = "INSERT INTO Files (path, hash, size, changedate) VALUES (:path, :hash, :size, :changedate)";
					sqc.Parameters.Add(new SqliteParameter("path"));
					sqc.Parameters.Add(new SqliteParameter("hash"));
					sqc.Parameters.Add(new SqliteParameter("size"));
					sqc.Parameters.Add(new SqliteParameter("changedate"));
					foreach (FileObject d in liste) {
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
			using (SqliteCommand sqc = new SqliteCommand(connection)) {
				sqc.CommandText = "SELECT path, hash, size, changedate FROM Files";
				using (SqliteDataReader sqr = sqc.ExecuteReader()) {
					Dictionary<string, FileObject> filelist = new Dictionary<string, FileObject>();
					while (sqr.Read()) {
						filelist.Add(sqr.GetString(0), new FileObject(sqr.GetString(0), sqr.GetString(1), sqr.GetInt64(2), sqr.GetInt64(3)));
					}
					return filelist;
				}
			}
		}

		public bool CheckHash(string hash)
		{
			using (SqliteCommand sqc = new SqliteCommand(connection)) {
				sqc.CommandText = "SELECT id FROM Files WHERE hash=:hash";
				sqc.Parameters.Add(new SqliteParameter("hash", hash));
				return sqc.ExecuteReader().HasRows;
			}
		}

		public string GetFilepath(string hash)
		{
			using (SqliteCommand sqc = new SqliteCommand(connection)) {
				sqc.CommandText = "SELECT path FROM Files WHERE hash=:hash";
				sqc.Parameters.Add(new SqliteParameter("hash", hash));
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
