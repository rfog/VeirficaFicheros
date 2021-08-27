using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Ookii.Dialogs.Wpf;

namespace VeirficaFicheros
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private FolderController m_folderController = new FolderController();
        public MainWindow()
        {
            InitializeComponent();

            //m_folderController.Load(Path.Combine(System.Environment.GetEnvironmentVariable("USERPROFILE"), "VerificaFicheros"));
            m_folderController.Load(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));
            FoldersListBox.ItemsSource = m_folderController.MonitorizedFolders;
        }

        private void AddFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new VistaFolderBrowserDialog();
            dialog.ShowNewFolderButton = false;
            if (dialog.ShowDialog().Value)
            {
                Log.Text = "Please be patient while folder is processed";
                m_folderController.Add(dialog.SelectedPath);
                FoldersListBox.ItemsSource = null;
                FoldersListBox.ItemsSource = m_folderController.MonitorizedFolders;
            }
        }

        private void RemoveFolderButton_Click(object sender, RoutedEventArgs e)
        {
            if (FoldersListBox.SelectedItem == null)
                return;

            var selected = FoldersListBox.SelectedItem.ToString();
            if (!String.IsNullOrEmpty(selected))
            {
                m_folderController.Delete(selected);
                FoldersListBox.ItemsSource = null;
                FoldersListBox.ItemsSource = m_folderController.MonitorizedFolders;
                DetailListBox.ItemsSource = null;
            }
        }

        private void CheckButton_Click(object sender, RoutedEventArgs e)
        {
            if (FoldersListBox.SelectedItem == null)
                return;

            var selected = FoldersListBox.SelectedItem.ToString();
            if (!String.IsNullOrEmpty(selected))
            {
                m_folderController.Verify();
                DetailListBox.ItemsSource = null;
                DetailListBox.ItemsSource = m_folderController.FilesNotMatching;
            }
        }

        private void UpdatePendingButton_Click(object sender, RoutedEventArgs e)
        {
            if (DetailListBox.SelectedItems.Count > 0)
            {
                m_folderController.Update(DetailListBox.SelectedItems);
                DetailListBox.ItemsSource = null;
                DetailListBox.ItemsSource = m_folderController.FilesNotMatching;
            }
        }

        private void FoldersListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                m_folderController.Select(e.AddedItems[0].ToString());
                DetailListBox.ItemsSource = null;
                DetailListBox.ItemsSource = m_folderController.FilesNotMatching;
            }
        }

        private void VerifyAllFolders_Click(object sender, RoutedEventArgs e)
        {
            var selected = FoldersListBox.SelectedItem;
            foreach (var line in m_folderController.MonitorizedFolders)
            {
                m_folderController.Select(line);
                m_folderController.Verify();
            }

            if(selected!=null)
                m_folderController.Select(selected.ToString());
        }
    }
}
