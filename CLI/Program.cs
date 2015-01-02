using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IntelliSync;

namespace IntelliSync.Analyzer
{
	class Program
	{
		static void Main(string[] args)
		{
			if (args[0].ToLower() == "-a") {
				if (Directory.Exists(args[1])) {
					if (Directory.Exists(Path.GetDirectoryName(args[2]))) {
						if (File.Exists(args[2]))
							File.Delete(args[2]);
						new Snapshot(args[2]).WriteFiles(Scan.ScanFolder(args[1]));
						Console.WriteLine("Ordner analysiert und Ergebnis gespeichert in " + args[2]);
					} else
						Console.WriteLine("Das zweite Argument nach -a muss der Pfad zu einer Datei in einem existierenden Verzeichnis sein! (Die Datei wird bei Bedarf angelegt.)");
				} else
					Console.WriteLine("Das erste Argument nach -a muss ein gültiges existierendes Verzeichnis sein!");
			} else if (args[0].ToLower() == "-c") {
				// Get stuff from command line
				// TODO: Safety checks (is snapshot a db...)
				if (args.Count() == 4) {
					if (File.Exists(args[1]) && Directory.Exists(args[2]) && Directory.Exists(Path.GetDirectoryName(args[3]))) {
						Compare(args[1], args[2], args[3]);
						Console.WriteLine("Vergleichen erfolgreich, Ergebnis gespeichert in " + args[3]);
					} else
						Console.WriteLine("Verwendung: -c <FileFromAnalysis> <FolderToCompare> <OutputFile>");
				}
			} else if (args[0].ToLower() == "-s") {
				if (args.Count() == 4 && File.Exists(args[1]) && Directory.Exists(args[2]) && Directory.Exists(args[3])) {
					Sync(args[1], args[2], args[3]);
					Console.WriteLine("Synchronisation erfolgreich");
				} else {
					Console.WriteLine("Verwendung: -s <CompareResults> <LeftFolder> <RightFolder>");
					Console.WriteLine("Die Verzeichnisse müssen bereits angelegt sein.");
				}
			} else {
				Console.WriteLine("Es muss einer der Parameter -a -c -s übergeben werden!");
				Console.WriteLine("-a <Folder> <OutputFile>");
				Console.WriteLine("-c <FileFromAnalysis> <FolderToCompare> <OutputFile>");
				Console.WriteLine("-s <CompareResults> <ComparedFolder> <AnalyzedFolder>");
			}

			Console.WriteLine("Eine beliebige Taste drücken zum Beenden.");
			Console.ReadKey();
		}

		private static void Compare(string snapshot, string folder, string outputpath)
		{
			FolderCompare fc = new FolderCompare(new Snapshot(snapshot), folder);
			fc.CompareAgainstFolder();

			// Process results
			using (StreamWriter sw = new StreamWriter(new FileStream(outputpath, FileMode.Create))) {
				foreach (string file in fc.ResultUpdates.Keys) {
					string code = "";
					switch (fc.ResultUpdates[file].StatusInformation.Status) {
						case FILESTATUS.Changed:
							code = "cp";
							break;
						case FILESTATUS.Replaced:
							code = "rcp";
							break;
						case FILESTATUS.Moved:
							code = "mv";
							break;
						case FILESTATUS.Deleted:
						case FILESTATUS.Unknown:
							code = "del";
							break;
					}
					sw.WriteLine(file + ";" + fc.ResultUpdates[file].StatusInformation.Status.ToString() + ";" + fc.ResultUpdates[file].StatusInformation.Information + ";" + code + ";AB");
				}
				foreach (string file in fc.ResultNew.Keys) {
					sw.WriteLine(file + ";" + fc.ResultNew[file].Status.ToString() + ";" + fc.ResultNew[file].Information + ";" + "cpn" + ";AB");
				}
				sw.Close();
			}
		}

		private static void Sync(string compareResults, string folderA, string folderB)
		{
			using (StreamReader sr = new StreamReader(new FileStream(compareResults, FileMode.Open))) {
				while (!sr.EndOfStream) {
					string[] line = sr.ReadLine().Split(';');
					if (line.Count() != 5 || line[4].Trim() == "" || line[3].Trim() == "" || (line[4].Trim() != "AB" && line[4].Trim() != "BA"))
						continue;

					if (line[3].Trim() == "cp") {
						// File has been changed
						// Copy in the given direction. Delete the destination file before if necessary (for replaced files).
						if (line[4].Trim() == "AB") {
							if (File.Exists(folderB + line[0]))
								File.Delete(folderB + line[0]);
							File.Copy(folderA + line[0], folderB + line[0]);
						} else if (line[4].Trim() == "BA") {
							if (File.Exists(folderA + line[0]))
								File.Delete(folderA + line[0]);
							File.Copy(folderB + line[0], folderA + line[0]);
						}
					} else if (line[3].Trim() == "cpn") {
						// File is new
						// Copy from A to B for AB or delete in A for BA
						if (line[4].Trim() == "AB") {
							if (File.Exists(folderB + line[0]))
								File.Delete(folderB + line[0]);
							MakeSureDirExists(folderB + line[0]);
							File.Copy(folderA + line[0], folderB + line[0]);
						} else if (line[4].Trim() == "BA") {
							if (File.Exists(folderA + line[0]))
								File.Delete(folderA + line[0]);
						}
					} else if (line[3].Trim() == "del") {
						// File has been deleted
						// Kill in right folder for AB or revert killing for BA (copy file back)
						if (line[4].Trim() == "AB") {
							if (File.Exists(folderB + line[0]))
								File.Delete(folderB + line[0]);
						} else if (line[4].Trim() == "BA") {
							if (File.Exists(folderA + line[0]))
								File.Delete(folderA + line[0]);
							MakeSureDirExists(folderA + line[0]);
							File.Copy(folderB + line[0], folderA + line[0]);
						}
					} else if (line[3].Trim() == "mv") {
						// File has been moved
						// Move in B for AB or move in A for BA
						// If necessary, delete destination file before (for moved files that replaced other files)
						if (line[4].Trim() == "AB") {
							if (File.Exists(folderB + line[2]))
								File.Delete(folderB + line[2]);
							MakeSureDirExists(folderB + line[2]);
							File.Move(folderB + line[0], folderB + line[2]);
						} else if (line[4].Trim() == "BA") {
							if (File.Exists(folderA + line[0]))
								File.Delete(folderA + line[0]);
							MakeSureDirExists(folderA + line[0]);
							File.Move(folderA + line[2], folderA + line[0]);
						}
					}
				}
				sr.Close();
			}
		}

		private static void MakeSureDirExists(string path)
		{
			if (!Directory.Exists(Path.GetDirectoryName(path)))
				Directory.CreateDirectory(Path.GetDirectoryName(path));
		}
	}
}
