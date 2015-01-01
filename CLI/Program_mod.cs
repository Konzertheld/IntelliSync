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
            //System.Reflection.Assembly file = System.Reflection.Assembly.ReflectionOnlyLoadFrom(@"C:\Users\Christian\Desktop\intellisync\bin\System.Data.SQLite.dll");
            //System.Reflection.AssemblyName fn = file.GetName();

            //System.Reflection.Assembly lib = System.Reflection.Assembly.GetAssembly(typeof(Snapshot));
            //foreach (System.Reflection.AssemblyName an in lib.GetReferencedAssemblies())
            //{
            //    Console.WriteLine(an.FullName);
            //    Console.WriteLine(an.FullName == fn.FullName);
            //}
            //Console.ReadKey();
            if (args.Count() > 0)
            {
                if (args[0].ToLower() == "-a")
                {
                    if (Directory.Exists(args[1]))
                    {
                        if (Directory.Exists(Path.GetDirectoryName(args[2])))
                        {
                            if (File.Exists(args[2]))
                                File.Delete(args[2]);
                            new Snapshot(args[2]).WriteFiles(Scan.ScanFolder(args[1]));
                            Console.WriteLine(@"Ordner analysiert und Ergebnis gespeichert.");
                        }
                        else
                            Console.WriteLine("Das zweite Argument nach -a muss der Pfad zu einer Datei in einem existierenden Verzeichnis sein! (Die Datei wird bei Bedarf angelegt.)");
                    }
                    else
                        Console.WriteLine("Das erste Argument nach -a muss ein gültiges Verzeichnis sein!");
                }
                else if (args[0].ToLower() == "-c")
                {
                    // Get stuff from command line, if applicable
                    if (args.Count() == 4)
                    {
                        if (File.Exists(args[1]) && Directory.Exists(args[2]) && Directory.Exists(Path.GetDirectoryName(args[3])))
                            Compare(args[1], args[2], args[3]);
                        else if (File.Exists(args[2]) && Directory.Exists(args[1]) && Directory.Exists(Path.GetDirectoryName(args[3])))
                            Compare(args[2], args[1], args[3]);
                        else
                            Console.WriteLine("Fehler beim Parsen der Kommandozeilenargumente: Nach dem Parameter \"c\" müssen der Pfad zu einer Abbilddatei und zu einem zu vergleichenden Ordner folgen. Das war nicht der Fall, oder einer der beiden Pfade existierte nicht.");
                    }
                    else
                        Console.WriteLine("Verwendung: -c <Analysedatei> <Vergleichspfad> <Zielpfad>");
                }
                else if (args[0].ToLower() == "-sc")
                {
                    if (args.Count() == 4)
                    {
                        if (File.Exists(args[1]) && File.Exists(args[2]))
                        {
                            SnapshotCompare(args[1], args[2], args[3]);
                        }
                    }
                }
                else if (args[0].ToLower() == "-s" || args[0].ToLower() == "-sb")
                {
                    if (args.Count() == 4 && File.Exists(args[1]) && Directory.Exists(args[2]) && Directory.Exists(args[3]))
                    {
                        Sync(args[1], args[2], args[3], args[0].ToLower() == "-s");
                        Console.WriteLine("Synchronisation erfolgreich");
                    }
                    else
                    {
                        Console.WriteLine("Verwendung: -s <CompareResults> <LeftFolder> <RightFolder>");
                        Console.WriteLine("Die Verzeichnisse müssen bereits angelegt sein.");
                    }
                }
            }
            else
            {
                Console.WriteLine("Es muss einer der Parameter -a -c -s übergeben werden!");
                Console.WriteLine("intellisync.exe -a <Analysepfad> <Zielpfad>");
                Console.WriteLine("intellisync.exe -c <Analysedatei> <Vergleichspfad> <Zielpfad>");
                Console.WriteLine("intellisync.exe -s[b] <CompareResults> <LeftFolder> <RightFolder>");
            }

            Console.WriteLine("Eine beliebige Taste drücken zum Beenden.");
            Console.ReadKey();
        }

        private static void Compare(string snapshot, string folder, string destination)
        {
            FolderCompare fc = new FolderCompare(new Snapshot(snapshot), folder);
            fc.CompareAgainstFolder();

            // Process results
            using (StreamWriter sw = new StreamWriter(new FileStream(destination, FileMode.Create)))
            {
                foreach (string file in fc.ResultUpdates.Keys)
                {
                    string code = "";
                    switch (fc.ResultUpdates[file].StatusInformation.Status)
                    {
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
                foreach (string file in fc.ResultNew.Keys)
                {
                    sw.WriteLine(file + ";" + fc.ResultNew[file].Status.ToString() + ";" + fc.ResultNew[file].Information + ";" + "cpn" + ";AB");
                }
                sw.Close();
            }
        }

        private static void SnapshotCompare(string snapshotA, string snapshotB, string destination)
        {
            SnapshotCompare sc = new SnapshotCompare(new Snapshot(snapshotA), new Snapshot(snapshotB));
            sc.CompareAgainstSnapshot();

            // Process results
            using (StreamWriter sw = new StreamWriter(new FileStream(destination, FileMode.Create)))
            {
                foreach (string file in sc.ResultUpdates.Keys)
                {
                    string code = "";
                    switch (sc.ResultUpdates[file].StatusInformation.Status)
                    {
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
                    sw.WriteLine(file + ";" + sc.ResultUpdates[file].StatusInformation.Status.ToString() + ";" + sc.ResultUpdates[file].StatusInformation.Information + ";" + code + ";AB");
                }
                foreach (string file in sc.ResultNew.Keys)
                {
                    sw.WriteLine(file + ";" + sc.ResultNew[file].Status.ToString() + ";" + sc.ResultNew[file].Information + ";" + "cpn" + ";AB");
                }
                sw.Close();
            }
        }

        private static void Sync(string compareResults, string folderA, string folderB, bool keepbackup)
        {
            using (StreamReader sr = new StreamReader(new FileStream(compareResults, FileMode.Open)))
            {
                string backupA = System.IO.Path.GetDirectoryName(folderA + "\\_intellsyncbackup");
                string backupB = System.IO.Path.GetDirectoryName(folderB + "\\_intellsyncbackup");
                if (keepbackup)
                {
                    System.IO.Directory.CreateDirectory(backupA);
                    System.IO.Directory.CreateDirectory(backupB);
                }
                while (!sr.EndOfStream)
                {
                    string[] line = sr.ReadLine().Split(';');
                    if (line.Count() != 5 || line[4].Trim() == "" || line[3].Trim() == "" || (line[4].Trim() != "AB" && line[4].Trim() != "BA"))
                        continue;

                    if (line[3].Trim() == "cp")
                    {
                        // File has been changed
                        // Copy in the given direction. Delete the destination file before if necessary (for replaced files).
                        if (line[4].Trim() == "AB")
                        {
                            if (File.Exists(folderB + line[0]))
                                Del(folderB + line[0], keepbackup, backupB);
                            File.Copy(folderA + line[0], folderB + line[0]);
                        }
                        else
                        {
                            if (File.Exists(folderA + line[0]))
                                Del(folderA + line[0], keepbackup, backupA);
                            File.Copy(folderB + line[0], folderA + line[0]);
                        }
                    }
                    else if (line[3].Trim() == "cpn")
                    {
                        // File is new
                        // Copy from A to B for AB or delete in A for BA
                        if (line[4].Trim() == "AB")
                        {
                            if (File.Exists(folderB + line[0]))
                                Del(folderB + line[0], keepbackup, backupB);
                            MakeSureDirExists(folderB + line[0]);
                            File.Copy(folderA + line[0], folderB + line[0]);
                        }
                        else
                        {
                            if (File.Exists(folderA + line[0]))
                                Del(folderA + line[0], keepbackup, backupA);
                        }
                    }
                    else if (line[3].Trim() == "del")
                    {
                        // File has been deleted
                        // Kill in right folder for AB or revert killing for BA (copy file back)
                        if (line[4].Trim() == "AB")
                        {
                            if (File.Exists(folderB + line[0]))
                                Del(folderB + line[0], keepbackup, backupB);
                        }
                        else
                        {
                            if (File.Exists(folderA + line[0]))
                                Del(folderA + line[0], keepbackup, backupA);
                            MakeSureDirExists(folderA + line[0]);
                            File.Copy(folderB + line[0], folderA + line[0]);
                        }
                    }
                    else if (line[3].Trim() == "mv")
                    {
                        // File has been moved
                        // Move in B for AB or move in A for BA
                        // If necessary, delete destination file before (for moved files that replaced other files)
                        if (line[4].Trim() == "AB")
                        {
                            if (File.Exists(folderB + line[2]))
                                Del(folderB + line[2], keepbackup, backupB);
                            MakeSureDirExists(folderB + line[2]);
                            File.Move(folderB + line[0], folderB + line[2]);
                        }
                        else
                        {
                            if (File.Exists(folderA + line[0]))
                                Del(folderA + line[0], keepbackup, backupA);
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

        static void Del(string path, bool keepbackup, string backuppath)
        {
            if (keepbackup)
                File.Move(path, backuppath);
            else
                File.Delete(path);
        }
    }
}
