using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;

namespace GUI
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ObservableCollection<ActionInfo> source;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void lstView_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.X)
            {
                foreach (object i in lstView.SelectedItems)
                {
                    ((ActionInfo)i).Changedirection = !((ActionInfo)i).Changedirection;
                }
            }
        }

        private void Write_CSV(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
            ofd.CheckFileExists = false;
            ofd.AddExtension = true;
            ofd.DefaultExt = "csv";
            ofd.Filter = "CSV|*.csv";
            ofd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            ofd.ShowDialog();
            if (ofd.FileName != "")
            {
                string csvpath = ofd.FileName;
                CSV.Write(source.ToList<ActionInfo>(), csvpath);
            }
        }

        private void Read_CSV(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
            ofd.CheckFileExists = true;
            ofd.DefaultExt = "csv";
            ofd.Filter = "CSV|*.csv";
            ofd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            ofd.ShowDialog();
            if (ofd.FileName != "")
            {
                string csvpath = ofd.FileName;
                List<ActionInfo> actions = CSV.Read(csvpath);
                source = new ObservableCollection<ActionInfo>(actions);
                CollectionView cv = new ListCollectionView(source);
                cv.GroupDescriptions.Add(new PropertyGroupDescription("ActionString"));
                cv.GroupDescriptions.Add(new PropertyGroupDescription("FirstLevelSubDir"));
                lstView.ItemsSource = cv;
            }
        }
    }
}
