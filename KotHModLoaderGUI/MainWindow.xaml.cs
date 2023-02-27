using AssetsTools.NET;
using System;
using System.Collections.Generic;
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

            DisplayVanillaCatalog();
        }

        private void ButtonBuildMods_Click(object sender, RoutedEventArgs e)
        {
            lstNames.Items.Add(_resMgr.BuildMods());
        }

        private void ToggleModActive(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ListBox lstBox = (ListBox)(sender);
            //lstNames.Items.Add(lstBox.SelectedItem);
            lstNames.Items.Add(_resMgr.ToggleModActive(lstBox.SelectedItem.ToString()));
            _files = _resMgr.LoadModdedManagers();
            lstNames.Items.Clear();
            foreach (string file in _files)
            {
                lstNames.Items.Add(file);
            }
        }

        private void DisplayVanillaCatalog()
        {
            lstVanilla.Items.Clear();

            List<string> assets = _resMgr.GetVanillaAssets();

            foreach (string asset in assets)
            {
                lstVanilla.Items.Add(asset);
            }
        }

        private void DisplayAssetInfo(object sender, SelectionChangedEventArgs e)
        {
            ListBox lstBox = (ListBox)(sender);
            string assetName = lstBox.SelectedItem.ToString();

            AssetTypeValueField infos = _resMgr.GetAssetInfo(assetName);

            lstAssetInfo.Items.Clear();

            foreach (var info in infos)
            {
                //var test = (info[info.FieldName]).As;
                lstAssetInfo.Items.Add(info.FieldName + " " + info.TypeName);

                switch (info.TypeName)
                {
                    case "string":
                        var s = info.AsString;
                        lstAssetInfo.Items.Add(s);
                        break;
                    case "int":
                        var i = info.AsInt;
                        lstAssetInfo.Items.Add(i);
                        break;
                    case "unsigned int":
                        var ui = info.AsUInt;
                        lstAssetInfo.Items.Add(ui);
                        break;
                    case "bool":
                        var b = info.AsBool;
                        lstAssetInfo.Items.Add(b);
                        break;
                    case "float":
                        var f = info.AsFloat;
                        lstAssetInfo.Items.Add(f);
                        break;
                    case "array":
                        var a = info.AsArray;
                        lstAssetInfo.Items.Add(a);
                        break;
                }
            }
            lstAssetInfo.Items.Add("why");
        }
    }
}
