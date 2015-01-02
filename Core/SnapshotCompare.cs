using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IntelliSync
{
    public class SnapshotCompare
    {
        public Snapshot SnapshotA;
        public Snapshot SnapshotB;

        /// <summary>
        /// What happened to the files in A.
        /// </summary>
        public Dictionary<string, FileObject> ResultUpdates;
        /// <summary>
        /// Files that did not exist in A.
        /// </summary>
        public Dictionary<string, FileStatusInformation> ResultNew;

        /// <summary>
        /// Create a new object for comparing two snapshots.
        /// </summary>
        public SnapshotCompare(Snapshot snapshotA, Snapshot snapshotB)
        {
            SnapshotA = snapshotA;
            SnapshotB = snapshotB;
            ResultUpdates = new Dictionary<string, FileObject>();
            ResultNew = new Dictionary<string, FileStatusInformation>();
        }

        /// <summary>
        /// Compare. B is considered the newer one. For syncing, B should be the source and A should be the target.
        /// </summary>
        public void CompareAgainstSnapshot()
        {
            // Reset the results.
            ResultUpdates = new Dictionary<string, FileObject>();
            ResultNew = new Dictionary<string, FileStatusInformation>();

            // Prepare the result list. Get the file objects from the first snapshot.
            Dictionary<string, FileObject> filesA = SnapshotA.GetFilelist();

            // Get the file list from the second one we compare to the first snapshot.
            Dictionary<string, FileObject> filesB = SnapshotB.GetFilelist();

            // Prepare a list for new files and their status information.
            Dictionary<string, FileStatusInformation> newFiles = new Dictionary<string, FileStatusInformation>();

            // Do it! Go through the new version of a directory and check what happened.
            foreach (string bFile in filesB.Keys)
            {
                // Is there a file with that path?
                if (filesA.ContainsKey(bFile))
                {
                    // Are the modifying dates eqal?
                    if (filesA[bFile].ChangeDate == filesB[bFile].ChangeDate)
                    {
                        // Is the file size equal? We have to open the files at this point. That sucks.
                        if (filesA[bFile].Size == filesB[bFile].Size)
                        {
                            // Those files seem to be equal but as we have the hash anyway, we compare it.
                            if (filesA[bFile].Hash.Equals(filesB[bFile].Hash))
                                filesA[bFile].StatusInformation.Status = FILESTATUS.Unchanged;
                            else
                            {
                                // The content changed, but not the writetime or the file size.
                                // Could happen when files get transferred via an inreliable connection.
                                filesA[bFile].StatusInformation.Status = FILESTATUS.Changed;
                                filesA[bFile].StatusInformation.Information = "Changed without modifying writetime or size";
                            }
                        }
                        else
                        {
                            // Ah, the filesizes differ. Work with the hash.
                            // Hash existing?
                            string oldpath = SnapshotA.GetFilepath(filesB[bFile].Hash);
                            if (oldpath != null)
                            {
                                // We know that file. That means:
                                // A file was replaced by an existing file with equal lastwritetime that was moved here.
                                filesA[bFile].StatusInformation.Status = FILESTATUS.Replaced;
                                filesA[bFile].StatusInformation.Information = oldpath;
                                // It means also a file was moved. Save that.
                                // WARNING: If there is a new / other file at the old path one of the two might get lost.
                                // TODO: Accept multiple statuses for one path to fix that.
                                filesA[oldpath].StatusInformation.Status = FILESTATUS.Moved;
                                filesA[oldpath].StatusInformation.Information = bFile;
                            }
                            else
                            {
                                // Change without modified writetime. Photo batch processing for example.
                                filesA[bFile].StatusInformation.Status = FILESTATUS.Changed;
                                filesA[bFile].StatusInformation.Information = "Changed without modifying writetime";
                            }
                        }
                    }
                    else
                    {
                        // Different modified time
                        // Check the hashes of both files
                        if (filesA[bFile].Hash.Equals(filesB[bFile].Hash))
                        {
                            // Unchanged file with changed modified date.
                            // Happens sometimes when a file is opened and saved but not changed (Access does that all the time)
                            // or if the changes have been reverted meanwhile.
                            filesA[bFile].StatusInformation.Status = FILESTATUS.UnchangedModified;
                        }
                        else
                        {
                            string oldpath = SnapshotA.GetFilepath(filesB[bFile].Hash);
                            if (oldpath != null)
                            {
                                // We know that file. That means:
                                // A file that already existed was overwritten by another file that already existed.
                                filesA[bFile].StatusInformation.Status = FILESTATUS.Replaced;
                                filesA[bFile].StatusInformation.Information = oldpath;
                                // It means also a file was moved. Save that.
                                // WARNING: If there is a new / other file at the old path one of the two might get lost.
                                // TODO: Accept multiple statuses for one path to fix that.
                                filesA[oldpath].StatusInformation.Status = FILESTATUS.Moved;
                                filesA[oldpath].StatusInformation.Information = bFile;
                            }
                            else
                            {
                                // Otherwise, this is a standard change.
                                filesA[bFile].StatusInformation.Status = FILESTATUS.Changed;
                            }
                        }
                    }
                }
                else
                {
                    // File with unknown path. Get Hash!
                    // Hash existing?
                    string oldpath = SnapshotA.GetFilepath(filesB[bFile].Hash);
                    if (oldpath != null)
                    {
                        // We know that one! Is it still there?
                        if (filesB.ContainsKey(oldpath))
                        {
                            // The new file is a copy of a file that existed already.
                            // TODO: More checking here
                            newFiles[bFile] = new FileStatusInformation(FILESTATUS.NewCopy, oldpath);
                        }
                        else
                        {
                            // Move/rename.
                            // WARNING: If there is a new / other file at the old path one of the two might get lost.
                            // TODO: Accept multiple statuses for one path to fix that.
                            filesA[oldpath].StatusInformation.Status = FILESTATUS.Moved;
                            filesA[oldpath].StatusInformation.Information = bFile;
                        }
                    }
                    else
                    {
                        newFiles[bFile] = new FileStatusInformation(FILESTATUS.New, "");
                    }
                }
            }

            // Last step: Check unidentified files in snapshot A (most likely, deleted files)
            foreach (KeyValuePair<string, FileObject> kvp in filesA)
            {
                if (kvp.Value.StatusInformation.Status == FILESTATUS.Unknown)
                {
                    // Let's see if this file's hash is in the second snapshot
                    // We do that all the time in the above procedure but always the other way round
                    string newpath = SnapshotB.GetFilepath(filesA[kvp.Value.Filepath].Hash);
                    if (newpath != null)
                    {
                        // We know that file. That means:
                        // A file that existed before still exists, but we weren't able to identify it yet
                        // Example: A duplicate has been removed, then we only identified the remaining one
                        filesA[kvp.Value.Filepath].StatusInformation.Status = FILESTATUS.Moved;
                        filesA[kvp.Value.Filepath].StatusInformation.Information = newpath;
                    }
                    else
                    {
                        // Now we are sure this file has been deleted. It's hash is gone and there is no longer a file at that path.
                        filesA[kvp.Value.Filepath].StatusInformation.Status = FILESTATUS.Deleted;
                    }
                }
            }

            ResultUpdates = filesA;
            ResultNew = newFiles;
        }
    }
}
