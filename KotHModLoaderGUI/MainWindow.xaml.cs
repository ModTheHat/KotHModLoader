using AssetsTools.NET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using static AssetsTools.NET.Texture.TextureFile;

namespace KotHModLoaderGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static ResourcesManager _resMgr = new ResourcesManager();
        private string[] _folders;
        private ModManager _modManager = new ModManager();
        private string _activeMod = "";

        public static ResourcesManager ResMgr => _resMgr;

        public MainWindow()
        {
            InitializeComponent();

            _resMgr.LoadManagers();
            DisplayMods();

            DisplayVanillaCatalog();

            console.Text = ".disabled Mods won't be added to the game.\n\n" +
                "Double click on a Mod in the Mods tab to toggle between enabled and disabled.\n\n" +
                "When you're happy with the enabled mods list, click on Build Mods to add them to the game.";
        }

        private void DisplayMods()
        {
            lstNames.Items.Clear();
            _folders = _modManager.BuildModsDatabase();
            foreach (string folder in _folders)
            {
                lstNames.Items.Add(folder);
            }
        }

        private void ButtonBuildMods_Click(object sender, RoutedEventArgs e)
        {
            console.Text += _modManager.BuildMods();
        }

        private void ToggleModActive(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ListBox lstBox = (ListBox)(sender);
            if (lstBox.SelectedIndex > -1)
            {
                _modManager.ToggleModActive(new DirectoryInfo(_modManager.DirInfoMod + @"\" + lstBox.SelectedItem.ToString()));

                DisplayMods();

                _modManager.BuildModsDatabase();
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
                    case "TypelessData":
                        var t = (Byte[])info.AsObject;
                        foreach (var o in t)
                        {
                            lstAssetInfo.Items.Add(o);
                        }
                        lstAssetInfo.Items.Add(t.Length);
                        break;
                    case "StreamingInfo":
                        lstAssetInfo.Items.Add("offset " + info["offset"].AsString + ", size " + info["size"].AsString + ", path " + info["path"].AsString);
                        break;
                }
            }
        }

        private void DisplayModInfo(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ListBox lstBox = (ListBox)(sender);
            if (lstBox.SelectedIndex > -1)
            {
                string modName = lstBox.SelectedItem.ToString();

                FileInfo[] infos = _modManager.GetModFiles(modName);

                lstModInfo.Items.Clear();
                foreach (var info in infos)
                {
                    lstModInfo.Items.Add(info.Name);
                }

                _activeMod = modName;
            }
        }

        private void DisplayModFileInfo(object sender, SelectionChangedEventArgs e)
        {
            ListBox lstBox = (ListBox)(sender);
            lstModFileInfo.Items.Clear();
            if (lstBox.SelectedIndex > -1)
            {
                string fileName = lstBox.SelectedItem.ToString();
                DirectoryInfo folder = _modManager.DirInfoMod;
                FileInfo[] files = folder.GetFiles(fileName, SearchOption.AllDirectories);
                FileInfo file = files[0];

                byte[] byteArray = _resMgr.GetRGBA(file);
                int i = 0;
                int j = 1;
                foreach (byte b in byteArray)
                {
                    lstModFileInfo.Items.Add((i == 0 ? j + " " : "") + (i == 0 ? "r: " : (i == 1 ? "g: " : (i == 2 ? "b: " : "a: "))) + b);
                    i++;
                    j = i == 4 ? j + 1 : j;
                    i = i == 4 ? 0 : i;
                }
                lstModFileInfo.Items.Add(files.Length);
            }
        }

        private void DisplayModInfo(object sender, SelectionChangedEventArgs e)
        {
            ListBox lstBox = (ListBox)(sender);
            if (lstBox.SelectedIndex > -1)
            {
                string modName = lstBox.SelectedItem.ToString();

                FileInfo[] infos = _modManager.GetModFiles(modName);

                lstModInfo.Items.Clear();
                foreach (var info in infos)
                {
                    lstModInfo.Items.Add(info.Name);
                }

                _activeMod = modName;
            }
        }
    }
}
