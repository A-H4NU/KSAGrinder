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

            if (rowContainer is not null)
            {
                DataGridCellsPresenter presenter = GetVisualChild<DataGridCellsPresenter>(rowContainer);

                // try to get the cell but it may possibly be virtualized
                DataGridCell cell = (DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(column);
                if (cell is null)
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
            DataGridRow row = (DataGridRow)dg.ItemContainerGenerator.ContainerFromIndex(index);
            if (row is null)
            {
                // may be virtualized, bring into view and try again
                dg.ScrollIntoView(dg.Items[index]);
                row = (DataGridRow)dg.ItemContainerGenerator.ContainerFromIndex(index);
            }
            return row;
        }

        private static T GetVisualChild<T>(Visual parent) where T : Visual
        {
            T child = default;
            int numVisuals = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < numVisuals; i++)
            {
                Visual v = (Visual)VisualTreeHelper.GetChild(parent, i);
                child = v as T;
                if (child is null) child = GetVisualChild<T>(v);
                else break;
            }
            return child;
        }
    }
}
