using Microsoft.Win32;

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace KSAGrinder.Pages
{
    /// <summary>
    /// Interaction logic for FileInput.xaml
    /// </summary>
    public partial class FileInput : Page
    {
        private readonly MainWindow _main;

        public FileInput(MainWindow main)
        {
            InitializeComponent();

            _main = main;
        }

        private void BtnSelect_Click(object sender, RoutedEventArgs e)
        {
            if (TrySelectFile(out DataSet result))
            {
                _main.Main.Navigate(new MainPage());
            }
            else
            {
                MessageBox.Show("Failed to load the file!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool TrySelectFile(out DataSet result)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Please select a provided file.";
            ofd.Filter = "ZIP files (*.zip)|*.zip";
            result = null;
            if (ofd.ShowDialog() == true)
            {
                ZipArchive arch = ZipFile.OpenRead(ofd.FileName);
                if (arch.Entries.Count != 2) return false;
                var f = (from entry in arch.Entries select entry.Name).ToArray();
                if (!(f[0].EndsWith(".xml", StringComparison.OrdinalIgnoreCase) && f[1].EndsWith(".xsd", StringComparison.OrdinalIgnoreCase) ||
                    f[1].EndsWith(".xml", StringComparison.OrdinalIgnoreCase) && f[0].EndsWith(".xsd", StringComparison.OrdinalIgnoreCase)))
                    return false;
                result = new DataSet();
                int xmlIdx = Array.FindIndex(f, (s) => s.EndsWith(".xml", StringComparison.OrdinalIgnoreCase));
                int xsdIdx = 1 - xmlIdx;
                using (StreamReader sr = new StreamReader(arch.Entries[xsdIdx].Open()))
                    result.ReadXmlSchema(sr);
                using (StreamReader sr = new StreamReader(arch.Entries[xmlIdx].Open()))
                    result.ReadXml(sr);
                arch.Dispose();
                return true;
            }
            return false;
        }
    }
}
