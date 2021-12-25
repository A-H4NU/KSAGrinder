using KSAGrinder.Properties;
using KSAGrinder.Windows;

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

        public static void ClearSettings()
        {
            Settings.Default.LastDataset = null;
            Settings.Default.LastFile = null;
            Settings.Default.Save();
        }

        private void BtnSelect_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Title = "제공된 파일을 선택하세요.",
                Filter = "ZIP files (*.zip)|*.zip"
            };
            if (ofd.ShowDialog() == true)
            {
                if (TryUnzip(ofd.FileName, out DataSet result, out string hash))
                {
                    Settings.Default.LastDataset = ofd.FileName;
                    Settings.Default.Save();
                    _main.Main.Navigate(new MainPage(_main, result, hash));
                }
                else
                {
                    MessageBox.Show("데이터셋을 불러오든 데 실패했습니다!", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Page_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

            // Note that you can have more than one file.
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            if (TryUnzip(files[0], out DataSet result, out string hash))
            {
                Settings.Default.LastDataset = files[0];
                Settings.Default.Save();
                _main.Main.Navigate(new MainPage(_main, result, hash));
            }
            else
            {
                MessageBox.Show("데이터셋을 불러오든 데 실패했습니다!", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    SHA256 sha = SHA256.Create();
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
                using (StreamReader sr = new StreamReader(arch.Entries[xsdIdx].Open()))
                {
                    result.ReadXmlSchema(sr);
                }

                using (StreamReader sr = new StreamReader(arch.Entries[xmlIdx].Open()))
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

        private void Hyperlink_RequestNavigate_Where(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            MessageBox.Show("지금 열릴 구글 드라이브에 접근하기 위해 학교가 제공한 구글 계정이 필요합니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
            System.Diagnostics.Process.Start(e.Uri.AbsoluteUri);
            e.Handled = true;
        }

        private void Hyperlink_RequestNavigate_Credit(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            DetailView credit = new DetailView(Properties.Resources.ResourceManager.GetString("Credit"))
            {
                Title = "도움주신 분들"
            };
            credit.TxtDetail.TextWrapping = TextWrapping.WrapWithOverflow;
            credit.ShowDialog();
            e.Handled = true;
        }

        private void page_Loaded(object sender, RoutedEventArgs e)
        {
            if (File.Exists(Settings.Default.LastDataset))
            {
                if (File.Exists(Settings.Default.LastFile))
                {
                    MessageBoxResult messageResult = MessageBox.Show("마지막으로 연 \"저장본\"을 다시 여시겠습니까?", "마지막으로 연 저장본", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (messageResult == MessageBoxResult.Yes)
                    {
                        if (TryUnzip(Settings.Default.LastDataset, out DataSet result, out string hash))
                        {
                            MainPage mainPage = new MainPage(_main, result, hash, Settings.Default.LastFile);
                            _main.Main.Navigate(mainPage);
                        }
                        else
                        {
                            MessageBox.Show("데이터셋을 불러오든 데 실패했습니다!", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                            ClearSettings();
                        }
                    }
                    else
                    {
                        ClearSettings();
                    }
                }
                else
                {
                    MessageBoxResult messageResult = MessageBox.Show("마지막으로 연 \"데이터셋\"을 다시 여시겠습니까?", "마지막으로 연 데이터셋", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (messageResult == MessageBoxResult.Yes)
                    {
                        if (TryUnzip(Settings.Default.LastDataset, out DataSet result, out string hash))
                        {
                            MainPage mainPage = new MainPage(_main, result, hash);
                            _main.Main.Navigate(mainPage);
                        }
                        else
                        {
                            MessageBox.Show("데이터셋을 불러오든 데 실패했습니다!", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                            ClearSettings();
                        }
                    }
                    else
                    {
                        ClearSettings();
                    }
                }
            }
        }
    }
}
