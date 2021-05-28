using Microsoft.Win32;

using System;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
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
                if (TryUnzip(ofd.FileName, out DataSet result, out string hash))
                {
                    _main.Main.Navigate(new MainPage(_main, result, hash));
                }
                else
                {
                    MessageBox.Show("Failed to load the file!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        private static bool TryUnzip(string fileName, out DataSet result, out string hash)
        {
            result = null; hash = null;
            if (fileName == null) return false;
            try
            {
                using (FileStream fs = File.OpenRead(fileName))
                {
                    var sha = SHA256.Create();
                    byte[] bytes = sha.ComputeHash(fs);
                    hash = "";
                    foreach (byte b in bytes) hash += b.ToString("x2");
                }

                ZipArchive arch = ZipFile.OpenRead(fileName);
                if (arch.Entries.Count != 2)
                {
                    return false;
                }

                string[] f = (from entry in arch.Entries select entry.Name).ToArray();
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
            catch
            {
                return false;
            }
        }

        private void Page_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            
            // Note that you can have more than one file.
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            if (TryUnzip(files[0], out DataSet result, out string hash))
            {
                _main.Main.Navigate(new MainPage(_main, result, hash));
            }
            else
            {
                MessageBox.Show("Failed to load the file!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
