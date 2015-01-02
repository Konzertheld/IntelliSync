using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IntelliSync
{
	public class FolderCompare
	{
		public Snapshot BaseSnapshot { get; set; }

		public string Folder { get; set; }

		/// <summary>
		/// What happened to the old files.
		/// </summary>
		public Dictionary<string, FileObject> ResultUpdates;
		/// <summary>
		/// Files that did not exist at that path before.
		/// </summary>
		public Dictionary<string, FileStatusInformation> ResultNew;

		// Options go here

		/// <summary>
		/// Create a new object for comparing two folders.
		/// </summary>
		/// <param name="left">Old path</param>
		/// <param name="right">New path</param>
		public FolderCompare(Snapshot snapshot, string folder)
		{
			BaseSnapshot = snapshot;
			Folder = folder;
			ResultUpdates = new Dictionary<string, FileObject>();
			ResultNew = new Dictionary<string, FileStatusInformation>();
		}

		/// <summary>
		/// Compare. B is considered the newer one. For syncing, B should be the source and A should be the target.
		/// </summary>
		public void Compare()
		{
			// Reset the results.
			ResultUpdates = new Dictionary<string, FileObject>();
			ResultNew = new Dictionary<string, FileStatusInformation>();

			// Prepare the result list. Get the file objects from the snapshot.
			Dictionary<string, FileObject> filesSnapshot = BaseSnapshot.GetFilelist();

			// Get the file list from the folder we compare to the snapshot.
			List<string> filesFolder = new List<string>(Directory.EnumerateFiles(Folder, "*", SearchOption.AllDirectories));

			// Prepare a list for new files and their status information.
			Dictionary<string, FileStatusInformation> newFiles = new Dictionary<string, FileStatusInformation>();

			// Do it! Go through the new version of a directory and check what happened.
			foreach (string f in filesFolder) {
				// Trim basepath
				string file = f.Substring(Folder.Length);

				// Should we ignore this file?
				string filename = Path.GetFileName(file);
				if (filename.StartsWith(".") || filename.EndsWith(".db") || filename.EndsWith(".ini"))
					continue;

				// Is there a file with that path in the snapshot?
				if (filesSnapshot.ContainsKey(file)) {
					// Are the modifying dates equal (and we consider them equal, when they differ less than a millisecond)?
					if (Math.Abs(filesSnapshot[file].ChangeDate - File.GetLastWriteTime(Folder + file).Ticks) < 10000) {
						// Is the file size equal? We have to open the files at this point. That sucks.
						long size = -1;
						using (FileStream fsb = File.OpenRead(Folder + file)) {
							size = fsb.Length;
							fsb.Close();
						}
						if (filesSnapshot[file].Size == size) {
							// Paranoid user stuff goes here (for detecting 1. files that have equal size,
							// creation time and last write time but different content and are new and 2. files like that
							// that have been moved.
							// Otherwise, those files seem to be equal.
							filesSnapshot[file].StatusInformation.Status = FILESTATUS.Unchanged;
						} else {
							// Ah, the filesizes differ. Get the hash.
							FileObject d = FileObject.Instance(Folder + file);
							// Hash existing?
							string oldpath = BaseSnapshot.GetFilepath(d.Hash);
							if (oldpath != null) {
								// We know that file. That means:
								// A file was replaced by an existing file with equal lastwritetime that was moved here.
								filesSnapshot[file].StatusInformation.Status = FILESTATUS.Replaced;
								filesSnapshot[file].StatusInformation.Information = oldpath;
								// It means also a file was moved. Save that.
								// WARNING: If there is a new / other file at the old path one of the two might get lost.
								// TODO: Accept multiple statuses for one path to fix that.
								// TODO: Maybe swapping the infos (old path is now in info) fixed that already.
								filesSnapshot[oldpath].StatusInformation.Status = FILESTATUS.Moved;
								filesSnapshot[oldpath].StatusInformation.Information = file;
							} else {
								// Change without modified writetime. Photo batch processing for example.
								filesSnapshot[file].StatusInformation.Status = FILESTATUS.Changed;
								filesSnapshot[file].StatusInformation.Information = "Changed without modifying writetime";
							}
						}
					} else {
						// Different modified time
						// Get the hashes of both files
						FileObject b = FileObject.Instance(Folder + file);
						if (filesSnapshot[file].Hash.Equals(b.Hash)) {
							// Unchanged file with changed modified date.
							// Happens sometimes when a file is opened and saved but not changed (Access does that all the time)
							// or if the changes have been reverted meanwhile.
							filesSnapshot[file].StatusInformation.Status = FILESTATUS.UnchangedModified;
						} else {
							string oldpath = BaseSnapshot.GetFilepath(b.Hash);
							if (oldpath != null) {
								// We know that file. That means:
								// A file that already existed was overwritten by another file that already existed.
								filesSnapshot[file].StatusInformation.Status = FILESTATUS.Replaced;
								filesSnapshot[file].StatusInformation.Information = oldpath;
								// It means also a file was moved. Save that.
								// WARNING: If there is a new / other file at the old path one of the two might get lost.
								// TODO: Accept multiple statuses for one path to fix that.
								filesSnapshot[oldpath].StatusInformation.Status = FILESTATUS.Moved;
								filesSnapshot[oldpath].StatusInformation.Information = file;
							} else {
								// Otherwise, this is a standard change.
								filesSnapshot[file].StatusInformation.Status = FILESTATUS.Changed;
							}
						}
					}
				} else {
					// File with unknown path. Get Hash!
					FileObject d = FileObject.Instance(Folder + file);
					// Hash existing?
					string oldpath = BaseSnapshot.GetFilepath(d.Hash);
					if (oldpath != null) {
						// We know that one! Is it still there?
						if (File.Exists(Folder + oldpath)) {
							// The new file is a copy of a file that existed already.
							// TODO: More checking here
							newFiles[file] = new FileStatusInformation(FILESTATUS.NewCopy, oldpath);
						} else {
							// Move/rename.
							// WARNING: If there is a new / other file at the old path one of the two might get lost.
							// TODO: Accept multiple statuses for one path to fix that.
							filesSnapshot[oldpath].StatusInformation.Status = FILESTATUS.Moved;
							filesSnapshot[oldpath].StatusInformation.Information = file;
						}
					} else {
						newFiles[file] = new FileStatusInformation(FILESTATUS.New, "");
					}
				}
			}

			// Last step: Check unidentified files in the snapshot (most likely, deleted files)
			foreach (KeyValuePair<string, FileObject> kvp in filesSnapshot) {
				if (kvp.Value.StatusInformation.Status == FILESTATUS.Unknown) {
					// TODO: 
				}
			}

			ResultUpdates = filesSnapshot;
			ResultNew = newFiles;
		}
	}
}
