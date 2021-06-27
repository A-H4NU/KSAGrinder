using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace KSAGrinder.Extensions
{
    // Copy of DataGridHelper by Fredrik Hedblad https://stackoverflow.com/questions/6888892/datagrid-get-cell-index
    public static class DataGridExtension
    {
        public static DataGridCell GetCell(this DataGrid dg, int row, int column)
        {
            DataGridRow rowContainer = GetRow(dg, row);

            if (rowContainer != null)
            {
                DataGridCellsPresenter presenter = GetVisualChild<DataGridCellsPresenter>(rowContainer);

                // try to get the cell but it may possibly be virtualized
                var cell = (DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(column);
                if (cell == null)
                {
                    // now try to bring into view and retreive the cell
                    dg.ScrollIntoView(rowContainer, dg.Columns[column]);
                    cell = (DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(column);
                }
                return cell;
            }
            return null;
        }

        public static DataGridRow GetRow(this DataGrid dg, int index)
        {
            var row = (DataGridRow)dg.ItemContainerGenerator.ContainerFromIndex(index);
            if (row == null)
            {
                // may be virtualized, bring into view and try again
                dg.ScrollIntoView(dg.Items[index]);
                row = (DataGridRow)dg.ItemContainerGenerator.ContainerFromIndex(index);
            }
            return row;
        }

        private static T GetVisualChild<T>(Visual parent) where T : Visual
        {
            var child = default(T);
            int numVisuals = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < numVisuals; i++)
            {
                var v = (Visual)VisualTreeHelper.GetChild(parent, i);
                child = v as T;
                if (child == null) child = GetVisualChild<T>(v);
                else break;
            }
            return child;
        }
    }
}
