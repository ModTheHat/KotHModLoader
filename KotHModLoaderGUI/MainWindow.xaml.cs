using AssetsTools.NET;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using static KotHModLoaderGUI.ModManager;

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
                console.Text += _modManager.ToggleModActive(new DirectoryInfo(_modManager.DirInfoMod + @"\" + lstBox.SelectedItem.ToString()));

                //_modManager.BuildModsDatabase();
                DisplayMods();
                DisplaySelectedModInfo();
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

        FileInfo[] _displayedModFilesInfo;
        private void DisplayModInfo(object sender, SelectionChangedEventArgs e)
        {
            ListBox lstBox = (ListBox)(sender);
            if (lstBox.SelectedIndex > -1)
            {
                string modName = lstBox.SelectedItem.ToString();

                lstModFileInfo.Items.Clear();
                VanillaImageViewer.Source = null;
                ModdedImageViewer.Source = null;

                _displayedModFilesInfo = _modManager.GetModFiles(modName);

                lstModInfo.Items.Clear();
                foreach (var info in _displayedModFilesInfo)
                {
                    lstModInfo.Items.Add(info.Name);
                }

                _activeMod = modName;
            }
        }

        private void DisplaySelectedModInfo()
        {
            if (lstNames.SelectedIndex > -1)
            {
                string modName = lstNames.SelectedItem.ToString();

                lstModFileInfo.Items.Clear();
                VanillaImageViewer.Source = null;
                ModdedImageViewer.Source = null;

                _displayedModFilesInfo = _modManager.GetModFiles(modName);

                lstModInfo.Items.Clear();
                foreach (var info in _displayedModFilesInfo)
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
                ModFile modFile = _modManager.FindModFile(fileName);
                
                AssetTypeValueField vanillaAssetInfo = _resMgr.GetVanillaDataImage(modFile.AssignedVanillaFile);
                if (vanillaAssetInfo != null)
                {
                    if (vanillaAssetInfo["image data"].AsByteArray.Length > 0)
                    {
                        Bitmap vanillaImage = GetDataPicture(vanillaAssetInfo["m_Width"].AsInt, vanillaAssetInfo["m_Height"].AsInt, vanillaAssetInfo["image data"].AsByteArray);
                        VanillaImageViewer.Source = ToBitmapImage(vanillaImage);
                    }
                }

                using (FileStream fs = new FileStream(file.FullName, FileMode.Open))
                {
                    BitmapImage _modFileImg = new BitmapImage();
                    _modFileImg.BeginInit();
                    _modFileImg.StreamSource = fs;
                    _modFileImg.CacheOption = BitmapCacheOption.OnLoad;
                    _modFileImg.EndInit();
                    fs.Close();
                    ModdedImageViewer.Source = _modFileImg;
                }

                lstModFileInfo.Items.Add(files.Length);
                lstModFileInfo.Items.Add("mod file name: " + fileName);
                lstModFileInfo.Items.Add("assigned to vanilla file: " + modFile.AssignedVanillaFile);
            }
        }

        public Bitmap GetDataPicture(int w, int h, byte[] data)
        {
            Bitmap pic = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    int arrayIndex = y * w + x;
                    Color c = Color.FromArgb(
                       data[arrayIndex + 3],
                       data[arrayIndex],
                       data[arrayIndex + 1],
                       data[arrayIndex + 2]
                    );
                    pic.SetPixel(x, y, c);
                }
            }

            return pic;
        }
        
        public static BitmapImage ToBitmapImage(Bitmap bitmap)
        {
            using (var memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Png);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                return bitmapImage;
            }
        }

        private void ToggleModFileActive(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ListBox lstBox = (ListBox)(sender);
            if (lstBox.SelectedIndex > -1)
            {
                console.Text += "\n" + _modManager.ToggleModFileActive(_displayedModFilesInfo[lstBox.SelectedIndex]);

                if (lstNames.SelectedIndex > -1)
                {
                    string modName = lstNames.SelectedItem.ToString();

                    _displayedModFilesInfo = _modManager.GetModFiles(modName);

                    lstModInfo.Items.Clear();
                    foreach (var info in _displayedModFilesInfo)
                    {
                        lstModInfo.Items.Add(info.Name);
                    }

                    _activeMod = modName;
                }
                _modManager.BuildModsDatabase();
                DisplaySelectedModInfo();
            }
        }
    }
}
