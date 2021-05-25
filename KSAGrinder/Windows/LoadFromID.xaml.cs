using System.Data;
using System.Windows;
using System.Windows.Input;

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
            TxtID.Focus();
        }

        private void Return()
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

        private void BtnLoad_Click(object sender, RoutedEventArgs e) => Return();

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => Close();

        private void TxtID_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Return();
            }
        }
    }
}
