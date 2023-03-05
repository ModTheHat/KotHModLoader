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

            _resMgr.InitialisePaths();
            _modManager.InitialisePaths();

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
            console.Text = _modManager.BuildMods();
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

            textAssetInfo.Text = "";

            foreach (var info in infos)
            {
                //var test = (info[info.FieldName]).As;
                textAssetInfo.Text += info.FieldName + "(" + info.TypeName + "): ";

                switch (info.TypeName)
                {
                    case "string":
                        var s = info.AsString;
                        textAssetInfo.Text += s;
                        break;
                    case "int":
                        var i = info.AsInt;
                        textAssetInfo.Text += i;
                        break;
                    case "unsigned int":
                        var ui = info.AsUInt;
                        textAssetInfo.Text += ui;
                        break;
                    case "bool":
                        var b = info.AsBool;
                        textAssetInfo.Text += b;
                        break;
                    case "float":
                        var f = info.AsFloat;
                        textAssetInfo.Text += f;
                        break;
                    case "array":
                        var a = info.AsArray;
                        textAssetInfo.Text += a;
                        break;
                    case "TypelessData":
                        var t = (Byte[])info.AsObject;
                        //foreach (var o in t)
                        //{
                        //    textAssetInfo.Text += o;
                        //}
                        textAssetInfo.Text += t.Length;
                        break;
                    case "StreamingInfo":
                        textAssetInfo.Text += "offset " + info["offset"].AsString + ", size " + info["size"].AsString + ", path " + info["path"].AsString;
                        break;
                }
                textAssetInfo.Text += "\n";
            }

            if (infos["image data"].AsByteArray.Length > 0 && infos["m_Width"].AsInt * infos["m_Height"].AsInt * 4 == infos["image data"].AsByteArray.Length)
            {
                Bitmap vanillaImage = GetDataPicture(infos["m_Width"].AsInt, infos["m_Height"].AsInt, infos["image data"].AsByteArray);
                VanillaImageViewer.Source = ToBitmapImage(vanillaImage);
            }
            else
                VanillaImageViewer.Source = null;
        }

        FileInfo[] _displayedModFilesInfo;
        private void DisplayModInfo(object sender, SelectionChangedEventArgs e)
        {
            ListBox lstBox = (ListBox)(sender);
            if (lstBox.SelectedIndex > -1)
            {
                string modName = lstBox.SelectedItem.ToString();

                lstModFileInfo.Items.Clear();
                AssignedImageViewer.Source = null;
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
                AssignedImageViewer.Source = null;
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
                    if (vanillaAssetInfo["image data"].AsByteArray.Length > 0 && vanillaAssetInfo["m_Width"].AsInt * vanillaAssetInfo["m_Height"].AsInt * 4 == vanillaAssetInfo["image data"].AsByteArray.Length)
                    {
                        Bitmap vanillaImage = GetDataPicture(vanillaAssetInfo["m_Width"].AsInt, vanillaAssetInfo["m_Height"].AsInt, vanillaAssetInfo["image data"].AsByteArray);
                        AssignedImageViewer.Source = ToBitmapImage(vanillaImage);
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

            for (int y = 0; y < h; y++) 
            {
                for (int x = 0; x < w; x++)
                {
                    int arrayIndex = (y * w + x) * 4;
                    Color c = Color.FromArgb(
                       data[arrayIndex + 3],
                       data[arrayIndex],
                       data[arrayIndex + 1],
                       data[arrayIndex + 2]
                    );
                    pic.SetPixel(x, h - 1 - y, c);
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
