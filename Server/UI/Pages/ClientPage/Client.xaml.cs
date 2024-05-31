using Common.DTOs.MessagePack;
using Server.CoreServerFunctionality;
using Server.Helper;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Server.UI.Pages.ClientPage
{
    public partial class Client : UserControl
    {
        private SemaphoreSlim semaphoreSlim = new(1, 1);
        public Client()
        {
            InitializeComponent();
            ClientHandler._sessions.CollectionChanged += UpdateClientGrid;
        }

        private void OnMenuItemClick(object sender, RoutedEventArgs e)
        {

        }

        private void UpdateClientGrid(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    UpdateClientGrid(sender, e);
                });
                return;
            }
            MapCollectionToGrid();
        }

        private void MapCollectionToGrid()
        {
            dataGrid.Items.Clear();
            foreach (var item in ClientHandler._sessions) 
            { 
                dataGrid.Items.Add(item);
            }
        }

        private async void dataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            //e.Row.ContextMenu = await BuildContextMenu();
        }

        #region DataGrid Features
        private void DataGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DataGrid dataGrid = sender as DataGrid;
            DataGridRow clickedRow = GetDataGridRowUnderMouse(dataGrid, e.GetPosition(dataGrid));

            if (clickedRow != null)
            {
                if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                {
                    HandleShiftSelection(dataGrid, clickedRow);
                }
                else if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                {
                    HandleCtrlSelection(dataGrid, clickedRow);
                }
                else
                {
                    dataGrid.SelectedItems.Clear();
                    dataGrid.SelectedItem = clickedRow.Item;
                }
                e.Handled = true;
            }
        }

        private void HandleShiftSelection(DataGrid dataGrid, DataGridRow clickedRow)
        {
            int currentIndex = dataGrid.ItemContainerGenerator.IndexFromContainer(clickedRow);

            if (dataGrid.SelectedItems.Count == 0)
            {
                dataGrid.SelectedItem = clickedRow.Item;
                return;
            }

            int firstSelectedIndex = dataGrid.Items.IndexOf(dataGrid.SelectedItems[0]);
            dataGrid.SelectedItems.Clear();

            int start = Math.Min(firstSelectedIndex, currentIndex);
            int end = Math.Max(firstSelectedIndex, currentIndex);

            for (int i = start; i <= end; i++)
            {
                dataGrid.SelectedItems.Add(dataGrid.Items[i]);
            }
        }

        private void HandleCtrlSelection(DataGrid dataGrid, DataGridRow clickedRow)
        {
            if (dataGrid.SelectedItems.Contains(clickedRow.Item))
            {
                dataGrid.SelectedItems.Remove(clickedRow.Item);
            }
            else
            {
                dataGrid.SelectedItems.Add(clickedRow.Item);
            }
        }

        private DataGridRow GetDataGridRowUnderMouse(DataGrid dataGrid, Point position)
        {
            IInputElement clickedElement = dataGrid.InputHitTest(position);
            return FindParent<DataGridRow>(clickedElement as DependencyObject);
        }

        private T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;

            return parentObject is T parent ? parent : FindParent<T>(parentObject);
        }
        #endregion
    }
}
