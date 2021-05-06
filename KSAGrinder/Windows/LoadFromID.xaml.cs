using System;
using System.Collections.Generic;
using System.Data;
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
using System.Windows.Shapes;

namespace KSAGrinder.Windows
{
    /// <summary>
    /// Interaction logic for LoadFromID.xaml
    /// </summary>
    public partial class LoadFromID : Window
    {
        private readonly DataSet _data;

        public DataRow Result { get; private set; } = null;

        public LoadFromID(DataSet data)
        {
            InitializeComponent();

            _data = data;
        }

        private void BtnLoad_Click(Object sender, RoutedEventArgs e)
        {
            DataTable tableStudent = _data.Tables["Student"];
            DataRow row = tableStudent.Rows.Find(TxtID.Text);
            if (row != null)
            {
                Result = row;
                Close();
            }
            else
            {
                MessageBox.Show($"There is no student with ID {TxtID.Text}.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnCancel_Click(Object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
