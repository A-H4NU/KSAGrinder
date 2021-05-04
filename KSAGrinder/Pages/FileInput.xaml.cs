using Microsoft.Win32;

using System;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

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
            var ofd = new OpenFileDialog
            {
                Title = "Please select a provided file.",
                Filter = "ZIP files (*.zip)|*.zip"
            };
            if (ofd.ShowDialog() == true)
            {
                if (TryUnzip(ofd.FileName, out DataSet result))
                {
                    _main.Main.Navigate(new MainPage(result));
                }
                else
                {
                    MessageBox.Show("Failed to load the file!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private bool TryUnzip(string fileName, out DataSet result)
        {
            ZipArchive arch = ZipFile.OpenRead(fileName);
            result = null;
            if (arch.Entries.Count != 2)
            {
                return false;
            }

            String[] f = (from entry in arch.Entries select entry.Name).ToArray();
            if (!(f[0].EndsWith(".xml", StringComparison.OrdinalIgnoreCase) && f[1].EndsWith(".xsd", StringComparison.OrdinalIgnoreCase) ||
                f[1].EndsWith(".xml", StringComparison.OrdinalIgnoreCase) && f[0].EndsWith(".xsd", StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            result = new DataSet();
            int xmlIdx = Array.FindIndex(f, (s) => s.EndsWith(".xml", StringComparison.OrdinalIgnoreCase));
            int xsdIdx = 1 - xmlIdx;
            using (var sr = new StreamReader(arch.Entries[xsdIdx].Open()))
            {
                result.ReadXmlSchema(sr);
            }

            using (var sr = new StreamReader(arch.Entries[xmlIdx].Open()))
            {
                result.ReadXml(sr);
            }

            arch.Dispose();
            return true;
        }

        private void Page_Drop(Object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Note that you can have more than one file.
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                if (TryUnzip(files[0], out DataSet result))
                {
                    _main.Main.Navigate(new MainPage(result));
                }
                else
                {
                    MessageBox.Show("Failed to load the file!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CmbLang_SelectionChanged(Object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
