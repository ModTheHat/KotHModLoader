using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace KotHModLoaderGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ResourcesManager _resMgr = new ResourcesManager();
        private string[] _files;

        public MainWindow()
        {
            InitializeComponent();

            _files = _resMgr.LoadManagers();
            foreach(string file in _files)
            {
                lstNames.Items.Add(file);
            }
        }

        private void ButtonBuildMods_Click(object sender, RoutedEventArgs e)
        {
            _resMgr.BuildMods();
        }

        private void ToggleModActive(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ListBox lstBox = (ListBox)(sender);
            //lstNames.Items.Add(lstBox.SelectedItem);
            _resMgr.ToggleModActive(lstBox.SelectedItem.ToString());
        }
    }
}
