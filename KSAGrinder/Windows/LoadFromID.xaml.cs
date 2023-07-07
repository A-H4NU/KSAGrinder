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

        public string? ResultID { get; private set; } = null;

        public DataRow? ResultRow { get; private set; } = null;

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
            if (row is not null)
            {
                ResultRow = row;
                ResultID = TxtID.Text;
                Close();
            }
            else
            {
                MessageBox.Show(
                    $"학번이 {TxtID.Text}인 학생을 찾을 수 없습니다.",
                    "학번에서 불러오기",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
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
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }
    }
}
