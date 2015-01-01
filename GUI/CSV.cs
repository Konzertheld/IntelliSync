using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GUI
{
    public static class CSV
    {
        public static List<ActionInfo> Read(string path)
        {
            List<ActionInfo> actions = new List<ActionInfo>();
            if (File.Exists(path))
            {
                using (StreamReader sr = new StreamReader(new FileStream(path, FileMode.Open)))
                {
                    while (!sr.EndOfStream)
                    {
                        string[] line = sr.ReadLine().Split(';');
                        ActionInfo a = new ActionInfo();
                        a.Path = line[0];
                        a.Info = line[2];
                        a.Action = (line[3].Trim() == "") ? E_ACTION.none : (E_ACTION)Enum.Parse(typeof(E_ACTION), line[3]);
                        a.Changedirection = (line[4].Trim() == "BA");
                        actions.Add(a);
                    }
                    sr.Close();
                }
            }
            return actions;
        }

        public static bool Write(List<ActionInfo> elements, string path)
        {
            using (StreamWriter sw = new StreamWriter(new FileStream(path, FileMode.Create)))
            {
                foreach (ActionInfo i in elements)
                {
                    sw.WriteLine(string.Format("{0};{1};{2};{3};{4}", i.Path, i.ActionString, i.Info, i.Action, (i.Changedirection) ? "BA" : "AB"));
                }
                sw.Close();
            }
            return true;
        }
    }
}
