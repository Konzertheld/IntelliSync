using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace GUI
{
    public enum E_ACTION
    {
        cp,
        cpn,
        del,
        mv,
        none
    }

    public class ActionInfo : INotifyPropertyChanged
    {
        private string path;
        public string Path
        {
            get { return path; }
            set
            {
                path = value;
                NotifyPropertyChanged("Path");
                firstLevelSubDir = path.Substring(0, path.IndexOf('\\',1) + 1);
                NotifyPropertyChanged("FirstLevelSubDir");
            }
        }

        private string firstLevelSubDir;
        public string FirstLevelSubDir
        {
            get { return firstLevelSubDir; }
        }


        private string info;
        public string Info
        {
            get { return info; }
            set { info = value; }
        }

        private E_ACTION action;
        public E_ACTION Action
        {
            get { return action; }
            set
            {
                action = value;
                set_actionString(value);
            }
        }

        private string actionString;
        public string ActionString
        {
            get { return actionString; }
        }

        private void set_actionString(E_ACTION value)
        {
            switch (value)
            {
                case E_ACTION.cp:
                    actionString = (changedirection) ? "Im Vergleichsordner ändern" : "In der Quelle aktualisieren";
                    break;
                case E_ACTION.cpn:
                    actionString = (changedirection) ? "Im Vergleichsordner löschen" : "In die Quelle kopieren";
                    break;
                case E_ACTION.del:
                    actionString = (changedirection) ? "In den Vergleichsordner kopieren" : "In der Quelle löschen";
                    break;
                case E_ACTION.mv:
                    actionString = (changedirection) ? "Im Vergleichsordner verschieben" : "In der Quelle verschieben";
                    break;
                case E_ACTION.none:
                    actionString = "Keine Aktion";
                    break;
            }
            NotifyPropertyChanged("ActionString");
        }

        /// <summary>
        /// true = BA, false = AB
        /// </summary>
        private bool changedirection;
        public bool Changedirection
        {
            get { return changedirection; }
            set { changedirection = value; set_actionString(action); NotifyPropertyChanged("Changedirection"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
    }
}
