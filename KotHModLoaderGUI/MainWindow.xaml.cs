using AssetsTools.NET;
using AssetsTools.NET.Texture;
using Fmod5Sharp.FmodTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static KotHModLoaderGUI.ModManager;

namespace KotHModLoaderGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private FileInfo[] _displayedModFilesInfo;
        private FileInfo[] _displayedModAudioFilesInfo;

        private AssetType _currentAssetDisplayed = AssetType.Resources;
        private List<int> _displayedIndexes;

        private static ResourcesManager _resMgr = new ResourcesManager();
        private string[] _folders;
        private static ModManager _modManager = new ModManager();
        private static FMODManager _fmodManager = new FMODManager();

        public static ModManager ModManager => _modManager;
        public static ResourcesManager ResMgr => _resMgr;
        public static FMODManager FMODManager => _fmodManager;

        public MainWindow()
        {
            InitializeComponent();

            _modManager.InitialisePaths();
            _resMgr.InitialisePaths();
            _fmodManager.InitialisePaths();

            DisplayMods();

            DisplayVanillaCatalog();

            console.Text = "Greyed out mods and files won't be added to the game.\n\n" +
                "Double click on a Mod in the Mods tab or on a File in the Mod Info tab to toggle between enabled and disabled.\n\n" +
                "When you're happy with the enabled mods and files list, click on Build Mods to add them to the game.";
        }

        private void DisplayMods()
        {
            lstNames.Items.Clear();
            _folders = _modManager.BuildModsDatabase();
            for (int i = 0; i < _folders.Length; i++)
            {
                string folder = _folders[i];
                Mod mod = _modManager.ModsList[i];
                FileInfo metaFile = mod.MetaFile;
                dynamic modJson = LoadJson(metaFile.FullName);
                List<string> disabled = (List<string>)modJson["DisabledModsOrFiles"].ToObject(typeof(List<string>));
                if(disabled.Contains("\\" + folder))
                {
                    folder += ".DISABLED";
                }
                lstNames.Items.Add(folder);
            }
            ModInfoStack.Visibility = Visibility.Hidden;
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
                int ind = lstBox.SelectedIndex;
                _modManager.ToggleModActive(lstBox.SelectedIndex);

                _resMgr.LoadManagers();
                DisplayMods();
                DisplaySelectedModInfo();
                lstBox.SelectedIndex = ind;
            }
        }

        private void DisplayVanillaCatalog()
        {
            _currentAssetDisplayed = AssetType.Resources;

            lstVanilla.Items.Clear();

            List<string> assets = _resMgr.GetVanillaAssets();

            if (_displayedIndexes != null)
            {
                GetSearchResults(search.Text);
                DisplaySearchResults();
            }
            else
            {
                foreach (string asset in assets)
                {
                    lstVanilla.Items.Add(asset);
                }
            }

            textAssetInfo.Text = null;
            VanillaImageViewer.Source = null;
        }

        private void DisplayVanillaAssetInfo(object sender, SelectionChangedEventArgs e)
        {
            ListBox lstBox = (ListBox)(sender);
            if (lstBox.SelectedIndex == -1) return;

            int index;

            string assetName = lstBox.SelectedItem.ToString().Replace(".DISABLED", "");

            switch (_currentAssetDisplayed)
            {
                case AssetType.Resources:
                    index = _displayedIndexes != null ? _displayedIndexes[lstBox.SelectedIndex] : lstBox.SelectedIndex;

                    AssetTypeValueField infos = _resMgr.GetAssetInfo(index);

                    textAssetInfo.Text = "";

                    foreach (var info in infos)
                    {
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
                        Bitmap vanillaImage = _resMgr.GetDataPicture(infos["m_Width"].AsInt, infos["m_Height"].AsInt, infos["image data"].AsByteArray);
                        VanillaImageViewer.Source = _resMgr.ToBitmapImage(vanillaImage);
                    }
                    else if(infos["image data"].AsByteArray.Length == 0)
                    {
                        AssetTypeValueField data = infos["m_StreamData"];
                        int offset = data["offset"].AsInt;
                        int size = data["size"].AsInt;
                        string path = data["path"].AsString;
                        
                        byte[] bytes = new byte[size];
                        for(int i = 0; i < size; i++)
                        {
                            bytes[i] = _resMgr.RessData[offset + i];
                        }

                        byte[] decoded = TextureFile.DecodeManaged(bytes, (TextureFormat)(infos["m_TextureFormat"].AsInt), infos["m_Width"].AsInt, infos["m_Height"].AsInt);
                        Bitmap vanillaImage = _resMgr.GetDataPicture(infos["m_Width"].AsInt, infos["m_Height"].AsInt, decoded);
                        if(vanillaImage != null)
                            VanillaImageViewer.Source = _resMgr.ToBitmapImage(vanillaImage);
                    }
                    else
                        VanillaImageViewer.Source = null;
                    break;
                case AssetType.FMOD:
                    index = _displayedIndexes != null ? _displayedIndexes[lstBox.SelectedIndex] : lstBox.SelectedIndex;
                    List<string> fmodInfos = _fmodManager.GetAssetInfo(index);

                    textAssetInfo.Text = "";

                    foreach (var info in fmodInfos)
                    {
                        textAssetInfo.Text += info;
                    }
                    break;
            }

        }

        private void DisplayModInfo(object sender, SelectionChangedEventArgs e)
        {
            DisplaySelectedModInfo();
        }

        private void DisplaySelectedModInfo()
        {
            if (lstNames.SelectedIndex > -1)
            {
                Mod selectedMod = _modManager.FindMod(lstNames.SelectedItem.ToString().Replace(".DISABLED", ""));
                string modName = lstNames.SelectedItem.ToString().Replace(".DISABLED", "");
                dynamic modJson = LoadJson(selectedMod.MetaFile.FullName);
                List<string> disabled = (List<string>)modJson["DisabledModsOrFiles"].ToObject(typeof(List<string>));

                CloseModFilesUI(AssetType.None);

                _displayedModFilesInfo = _modManager.GetModFiles(modName);
                _displayedModAudioFilesInfo = _modManager.GetModFiles(modName, AssetType.FMOD);

                ModDescriptionTextBox.Text = _modManager.FindMod(modName).Description != null && _modManager.FindMod(modName).Description != "" ? "Description: " + _modManager.FindMod(modName).Description : "Description: Edit text and press Enter";
                ModVersionTextBox.Text = _modManager.FindMod(modName).Version != null && _modManager.FindMod(modName).Version != "" ? "Version: " + _modManager.FindMod(modName).Version : "Version: Edit text and press Enter";
                ModAuthorTextBox.Text = _modManager.FindMod(modName).Author != null && _modManager.FindMod(modName).Author != "" ? "Author: " + _modManager.FindMod(modName).Author : "Author: Edit text and press Enter";

                lstModInfo.Items.Clear();
                foreach (var info in _displayedModFilesInfo)
                {
                    lstModInfo.Items.Add(info.Name + (disabled.Contains("\\" + info.FullName.Substring(info.FullName.IndexOf(selectedMod.Name))) ? ".DISABLED": ""));
                }
                lstModInfo.Visibility = lstModInfo.Items.Count > 0 ? Visibility.Visible : Visibility.Hidden;

                lstModAudioInfo.Items.Clear();
                foreach (var info in _displayedModAudioFilesInfo)
                {
                    lstModAudioInfo.Items.Add(info.Name + (disabled.Contains("\\" + info.FullName.Substring(info.FullName.IndexOf(selectedMod.Name))) ? ".DISABLED" : ""));
                }
                lstModAudioInfo.Visibility = lstModAudioInfo.Items.Count > 0 ? Visibility.Visible : Visibility.Hidden;

                ModInfoStack.Visibility = Visibility.Visible;
            }
        }

        private void DisplayModFileInfo(object sender, SelectionChangedEventArgs e)
        {
            DisplaySelectedModFileInfo();
        }

        private void DisplaySelectedModFileInfo()
        {
            lstModFileInfo.Items.Clear();
            if (lstModInfo.SelectedIndex > -1 && lstNames.SelectedIndex > -1)
            {
                Mod mod = _modManager.FindMod(lstNames.SelectedItem.ToString().Replace(".DISABLED", ""));
                string fileName = lstModInfo.SelectedItem.ToString().Replace(".DISABLED", "");
                DirectoryInfo folder = _modManager.DirInfoMod;
                FileInfo[] files = folder.GetFiles(fileName, SearchOption.AllDirectories);
                FileInfo file = files[0];
                ModFile modFile = _modManager.FindModFile(fileName);
                FileInfo metaFile = mod.MetaFile;
                dynamic modJson = LoadJson(metaFile.FullName);

                CloseModFilesUI(AssetType.Resources);

                int candidateQty = 0;
                List<VanillaTextureAssetCandidate> candidates = modFile.VanillaCandidates;
                for (int i = 0; i < candidates.Count; i++)
                {
                    AssetTypeValueField values = candidates[i].values;

                    Bitmap vanillaImage = null;

                    if (values["image data"].AsByteArray.Length == 0)
                    {
                        AssetTypeValueField data = values["m_StreamData"];
                        int offset = data["offset"].AsInt;
                        int size = data["size"].AsInt;
                        string path = data["path"].AsString;

                        byte[] bytes = new byte[size];
                        for (int b = 0; b < size; b++)
                        {
                            bytes[b] = _resMgr.RessData[offset + b];
                        }
                        byte[] decoded = TextureFile.DecodeManaged(bytes, (TextureFormat)(values["m_TextureFormat"].AsInt), values["m_Width"].AsInt, values["m_Height"].AsInt);

                        vanillaImage = _resMgr.GetDataPicture(values["m_Width"].AsInt, values["m_Height"].AsInt, decoded);                        
                    }
                    else if (values["image data"].AsByteArray.Length > 0 && values["m_Width"].AsInt * values["m_Height"].AsInt * 4 == values["image data"].AsByteArray.Length)
                    {
                        vanillaImage = _resMgr.GetDataPicture(values["m_Width"].AsInt, values["m_Height"].AsInt, values["image data"].AsByteArray);
                    }

                    if (candidateQty == 0)
                    {
                        CandidateImageViewer1.Source = _resMgr.ToBitmapImage(vanillaImage);
                        var blacklistedAsset = modJson["BlackListedVanillaAssets"][file.FullName.Substring(file.FullName.IndexOf(mod.Name) + mod.Name.Length)];
                        if (blacklistedAsset != null)
                        {
                            if (blacklistedAsset[values["m_Name"].AsString + "-" + candidates[i].index] != null)
                            {
                                VanillaImageStack1.Opacity = 0.3;
                                VanillaImageStack1.Background = new SolidColorBrush(Colors.Black);
                            }
                        }
                    }
                    else
                    {
                        CandidateImageViewer2.Source = _resMgr.ToBitmapImage(vanillaImage);
                        var blacklistedAsset = modJson["BlackListedVanillaAssets"][file.FullName.Substring(file.FullName.IndexOf(mod.Name) + mod.Name.Length)];
                        if (blacklistedAsset != null)
                        {
                            if (blacklistedAsset[values["m_Name"].AsString + "-" + candidates[i].index] != null)
                            {
                                VanillaImageStack2.Opacity = 0.3;
                                VanillaImageStack2.Background = new SolidColorBrush(Colors.Black);
                            }
                        }
                    }
                    candidateQty++;
                    VanillaImageLabel.Content = "Vanilla files that will be replaced. Click image to toggle between greyed out and normal. Greyed out images won't be replaced by mod asset file.";


                    //if (values["image data"].AsByteArray.Length > 0 && values["m_Width"].AsInt * values["m_Height"].AsInt * 4 == values["image data"].AsByteArray.Length)
                    //{
                    //    Bitmap vanillaImage = _resMgr.GetDataPicture(values["m_Width"].AsInt, values["m_Height"].AsInt, values["image data"].AsByteArray);
                    //    if (candidateQty == 0)
                    //    {
                    //        CandidateImageViewer1.Source = _resMgr.ToBitmapImage(vanillaImage);
                    //        var blacklistedAsset = modJson["BlackListedVanillaAssets"][file.FullName.Substring(file.FullName.IndexOf(mod.Name) + mod.Name.Length)];
                    //        if (blacklistedAsset != null)
                    //        {
                    //            if (blacklistedAsset[values["m_Name"].AsString + "-" + candidates[i].index] != null)
                    //            {
                    //                VanillaImageStack1.Opacity = 0.3;
                    //                VanillaImageStack1.Background = new SolidColorBrush(Colors.Black);
                    //            }
                    //        }
                    //    }
                    //    else
                    //    {
                    //        CandidateImageViewer2.Source = _resMgr.ToBitmapImage(vanillaImage);
                    //        var blacklistedAsset = modJson["BlackListedVanillaAssets"][file.FullName.Substring(file.FullName.IndexOf(mod.Name) + mod.Name.Length)];
                    //        if (blacklistedAsset != null)
                    //        {
                    //            if (blacklistedAsset[values["m_Name"].AsString + "-" + candidates[i].index] != null)
                    //            {
                    //                VanillaImageStack2.Opacity = 0.3;
                    //                VanillaImageStack2.Background = new SolidColorBrush(Colors.Black);
                    //            }
                    //        }
                    //    }
                    //    candidateQty++;
                    //    VanillaImageLabel.Content = "Vanilla files that will be replaced. Click image to toggle between greyed out and normal. Greyed out images won't be replaced by mod asset file.";
                    //}
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
                    ModImageLabel.Content = "Mod file texture";
                }

                if (modJson["AssignedVanillaAssets"]["\\" + fileName] != null)
                {
                    int ind = modJson["AssignedVanillaAssets"]["\\" + fileName]["index"];
                    string vanillaName = modJson["AssignedVanillaAssets"]["\\" + fileName]["name"];
                    string path = modJson["AssignedVanillaAssets"]["\\" + fileName]["path"];

                    //get assigned file AssetTypeValueField
                    if (ind > 0 && path != null)
                    {
                        AssetTypeValueField assignedValues = _resMgr.GetAssetInfo(ind);
                        //create Bitmap from assigned values with GetDataPicture
                        if (assignedValues["image data"].AsByteArray.Length > 0)
                        {
                            Bitmap assignedBitmap = _resMgr.GetDataPicture(assignedValues["m_Width"].AsInt, assignedValues["m_Height"].AsInt, assignedValues["image data"].AsByteArray);
                            //assign viewer.source with ToBitmapImage
                            AssignedImageViewer1.Source = _resMgr.ToBitmapImage(assignedBitmap);
                        }
                    }
                }

                lstModFileInfo.Items.Add("mod file name: " + fileName);
                foreach (VanillaTextureAssetCandidate assigned in modFile.VanillaCandidates)
                {
                    lstModFileInfo.Items.Add("assigned to vanilla file: " + assigned.values["m_Name"].AsString);
                }
            }
        }

        private void ToggleModFileActive(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ListBox lstBox = (ListBox)(sender);
            if (lstBox.SelectedIndex > -1)
            {
                FileInfo[] currentDisplayed = lstBox.Name == "lstModInfo" ? _displayedModFilesInfo : _displayedModAudioFilesInfo;

                _modManager.ToggleModFileActive(lstNames.SelectedIndex, lstBox.SelectedIndex, lstBox.Name == "lstModInfo" ? AssetType.Resources : AssetType.FMOD);

                if (lstNames.SelectedIndex > -1)
                {
                    string modName = lstNames.SelectedItem.ToString().Replace(".DISABLED", "");

                    if (lstBox.Name == "lstModInfo")
                    {
                        _displayedModFilesInfo = _modManager.GetModFiles(modName);
                        currentDisplayed = _displayedModFilesInfo;
                    }
                    else
                    {
                        _displayedModAudioFilesInfo = _modManager.GetModFiles(modName, AssetType.FMOD);
                        currentDisplayed = _displayedModAudioFilesInfo;
                    }

                    lstModInfo.Items.Clear();
                    foreach (var info in currentDisplayed)
                    {
                        lstModInfo.Items.Add(info.Name);
                    }
                }
                _resMgr.LoadManagers();
                _modManager.BuildModsDatabase();
                DisplaySelectedModInfo();
            }
        }

        private void ToggleAssignVanillaImage(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            System.Windows.Controls.Image image = (System.Windows.Controls.Image)sender;

            Mod selectedMod = _modManager.FindMod(lstNames.SelectedItem.ToString().Replace(".DISABLED", ""));
            ModFile modFile = selectedMod.ModFiles[lstModInfo.SelectedIndex];

            AssetTypeValueField vanillaFile = modFile.VanillaCandidates[image.Name.Contains("1") ? 0 : 1].values;

            BlackListedVanillaAssets blacklisted = new BlackListedVanillaAssets();
            blacklisted.index = modFile.VanillaCandidates[image.Name.Contains("1") ? 0 : 1].index;
            blacklisted.name = vanillaFile["m_Name"].AsString;
            blacklisted.path = modFile.File.FullName.Substring(modFile.File.FullName.IndexOf(selectedMod.Name) + selectedMod.Name.Length);

            if (image.Name == "CandidateImageViewer1")
            {
                if (VanillaImageStack1.Opacity == 0.3)
                {
                    VanillaImageStack1.Opacity = 1;
                    VanillaImageStack1.Background = null;

                    _modManager.WriteToMetaFile(selectedMod.MetaFile, blacklisted, true);
                }
                else
                {
                    VanillaImageStack1.Opacity = 0.3;
                    VanillaImageStack1.Background = new SolidColorBrush(Colors.Black);

                    _modManager.WriteToMetaFile(selectedMod.MetaFile, blacklisted);
                }
            }
            if (image.Name == "CandidateImageViewer2")
            {
                if (VanillaImageStack2.Opacity == 0.3)
                {
                    VanillaImageStack2.Opacity = 1;
                    VanillaImageStack2.Background = null;

                    _modManager.WriteToMetaFile(selectedMod.MetaFile, blacklisted, true);
                }
                else
                {
                    VanillaImageStack2.Opacity = 0.3;
                    VanillaImageStack2.Background = new SolidColorBrush(Colors.Black);

                    _modManager.WriteToMetaFile(selectedMod.MetaFile, blacklisted);
                }
            }
        }

        private void AssignVanillaTexture(object sender, RoutedEventArgs e)
        {
            Mod selectedMod = _modManager.FindMod(lstNames.SelectedItem.ToString().Replace(".DISABLED", ""));
            ModFile modFile = selectedMod.ModFiles[lstModInfo.SelectedIndex];

            if (_currentAssetDisplayed != AssetType.Resources)
            {
                MessageBox.Show("You selected an asset that is not a texture.\n" +
                    "Select a vanilla texture in the Vanilla Texture Tab and then assign it to the mod asset.");
                return;
            }

            int index = _displayedIndexes != null ? _displayedIndexes[lstVanilla.SelectedIndex] : lstVanilla.SelectedIndex;

            AssetTypeValueField vanillaFile = _resMgr.GetAssetInfo(index);

            AssignedVanillaAssets assigned = new AssignedVanillaAssets();
            assigned.index = index;
            assigned.name = vanillaFile["m_Name"].AsString;
            assigned.path = modFile.File.FullName.Substring(modFile.File.FullName.IndexOf(selectedMod.Name) + selectedMod.Name.Length);

            dynamic modJson = LoadJson(selectedMod.MetaFile.FullName);
            modJson["AssignedVanillaAssets"][assigned.path] = JToken.FromObject(assigned);

            File.WriteAllText(selectedMod.MetaFile.FullName, modJson.ToString());

            DisplaySelectedModFileInfo();
        }

        private static dynamic LoadJson(string path)
        {
            using (StreamReader r = new StreamReader(path))
            {
                string json = r.ReadToEnd();
                object metafile = JsonConvert.DeserializeObject(json);

                return metafile;
            }
        }

        private void RemoveAssignedVanillaAsset(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Mod selectedMod = _modManager.FindMod(lstNames.SelectedItem.ToString().Replace(".DISABLED", ""));
            ModFile modFile = selectedMod.ModFiles[lstModInfo.SelectedIndex];

            string path = modFile.File.FullName.Substring(modFile.File.FullName.IndexOf(selectedMod.Name) + selectedMod.Name.Length);

            dynamic modJson = LoadJson(selectedMod.MetaFile.FullName);
            modJson["AssignedVanillaAssets"].Remove(path);

            File.WriteAllText(selectedMod.MetaFile.FullName, modJson.ToString());

            DisplaySelectedModFileInfo();
        }

        private void DisplayFMODAssets(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DisplayFMODAssets();
        }

        private void DisplayFMODAssets()
        {
            _currentAssetDisplayed = AssetType.FMOD;
            List<string> fmodAssets = _fmodManager.GetBankAssets();

            lstVanilla.Items.Clear();

            if (_displayedIndexes != null)
            {
                GetSearchResults(search.Text);
                DisplaySearchResults();
            }
            else
            {
                foreach (string asset in fmodAssets)
                {
                    lstVanilla.Items.Add(asset);
                }
            }

            textAssetInfo.Text = null;
            VanillaImageViewer.Source = null;
        }

        private void DisplayTextureAssets(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DisplayTextureAssets();
        }

        private void DisplayTextureAssets()
        {
            DisplayVanillaCatalog();
        }

        private void SelectAllText(object sender, System.Windows.Input.MouseEventArgs e)
        {
           TextBox textBox = (TextBox)sender;

            textBox.SelectAll();
        }

        private void SearchEntry(object sender, System.Windows.Input.KeyEventArgs e)
        {
            System.Windows.Controls.TextBox textBox = (System.Windows.Controls.TextBox)sender;
            string query = textBox.Text;

            if (e.Key == System.Windows.Input.Key.Return)
            {
                GetSearchResults(query);
                DisplaySearchResults();
            }
        }

        private void GetSearchResults(string query)
        {
            _displayedIndexes = new List<int>();
            List<string> list = new List<string>();

            switch (_currentAssetDisplayed)
            {
                case AssetType.FMOD:
                    list = _fmodManager.GetBankAssets();
                    break;
                case AssetType.Resources:
                    list = _resMgr.GetVanillaAssets();
                    break;
            }

            for (int i = 0; i < list.Count; i++)
            {
                string item = list[i];
                if (item.ToLower().Contains(query.ToLower()))
                    _displayedIndexes.Add(i);
            }
        }

        private void DisplaySearchResults()
        {
            lstVanilla.Items.Clear();

            List<string> assets = _resMgr.GetVanillaAssets();

            switch (_currentAssetDisplayed)
            {
                case AssetType.FMOD:
                    assets = _fmodManager.GetBankAssets();
                    break; 
                case AssetType.Resources:
                    assets = _resMgr.GetVanillaAssets();
                    break;
            }

            foreach (int index in _displayedIndexes)
            {
                lstVanilla.Items.Add(assets[index]);
            }

            textAssetInfo.Text = null;
            VanillaImageViewer.Source = null;
        }

        private void DisplayModAudioInfo(object sender, SelectionChangedEventArgs e)
        {
            DisplaySelectedModAudioInfo();
        }

        private void CloseModFilesUI(AssetType type)
        {
            switch (type)
            {
                case AssetType.Resources:
                    lstModAudioInfo.SelectedIndex = -1;
                    ModAudioFileStack.Visibility = Visibility.Hidden;
                    ModImageFileStack.Visibility = Visibility.Visible;
                    break;
                case AssetType.FMOD:
                    lstModInfo.SelectedIndex = -1;
                    ModImageFileStack.Visibility = Visibility.Hidden;
                    ModAudioFileStack.Visibility = Visibility.Visible;
                    break;
                case AssetType.None:
                    ModAudioFileStack.Visibility = Visibility.Hidden;
                    ModImageFileStack.Visibility = Visibility.Hidden;
                    lstModFileInfo.Items.Clear();
                    break;
            }
            VanillaImageLabel.Content = "";
            ModImageLabel.Content = "";
            CandidateImageViewer1.Source = null;
            CandidateImageViewer2.Source = null;
            VanillaImageStack1.Opacity = 1;
            VanillaImageStack2.Opacity = 1;
            VanillaImageStack1.Background = null;
            VanillaImageStack2.Background = null;
            AssignedImageViewer1.Source = null;
            AssignedImageViewer.Source = null;
            ModdedImageViewer.Source = null;
        }

        private void DisplaySelectedModAudioInfo()
        {
            lstModAudioFileInfo.Items.Clear();
            if (lstModAudioInfo.SelectedIndex > -1 && lstNames.SelectedIndex > -1)
            {
                Mod mod = _modManager.FindMod(lstNames.SelectedItem.ToString().Replace(".DISABLED", ""));
                string fileName = lstModAudioInfo.SelectedItem.ToString().Replace(".DISABLED", "");
                DirectoryInfo folder = _modManager.DirInfoMod;
                FileInfo[] files = folder.GetFiles(fileName, SearchOption.AllDirectories);
                FileInfo file = files[0];
                ModFile modFile = _modManager.FindModFile(fileName);
                FileInfo metaFile = mod.MetaFile;
                dynamic modJson = LoadJson(metaFile.FullName);

                CloseModFilesUI(AssetType.FMOD);

                int candidateQty = 0;
                List<VanillaAudioAssetCandidate> candidates = modFile.VanillaAudioCandidates;

                //nom du fichier du mod
                lstModAudioFileInfo.Items.Add("Nom du fichier audio: " + fileName);

                //infos du fichier
                foreach(string s in _fmodManager.GetOggFileInfos(file.FullName))
                {
                    lstModAudioFileInfo.Items.Add(s);
                }
                //vanilla assignés automatiquement
                int i = 0;
                foreach(VanillaAudioAssetCandidate candidate in candidates)
                {
                    if (i == 0)
                    {
                        CandidateAudioText1.Text = "Audio sample name: " + candidate.name;
                        dynamic blacklisted = modJson["BlackListedVanillaAssets"][file.FullName.Substring(file.FullName.IndexOf(mod.Name) + mod.Name.Length)];
                        if (blacklisted != null)
                        {
                            if(blacklisted[candidate.name + "-" + candidates[i].index] != null)
                            {
                                
                                CandidateAudioStack1.Opacity = 0.3;
                                CandidateAudioStack1.Background = new SolidColorBrush(Colors.Black);
                                CandidateAudioText1.Foreground = new SolidColorBrush(Colors.White);
                            }
                        }
                    }
                    else if(i == 1)
                    {
                        CandidateAudioText2.Text = "Audio sample name: " + candidate.name;
                        dynamic blacklisted = modJson["BlackListedVanillaAssets"][file.FullName.Substring(file.FullName.IndexOf(mod.Name) + mod.Name.Length)];
                        if (blacklisted != null)
                        {
                            if (blacklisted[candidate.name + "-" + candidates[i].index] != null)
                            { 
                                CandidateAudioStack2.Opacity = 0.3;
                                CandidateAudioStack2.Background = new SolidColorBrush(Colors.Black);
                                CandidateAudioText2.Foreground = new SolidColorBrush(Colors.White);
                            }
                        }
                    }                   

                    i++;
                }

                //vanilla assignés manuellement
            }
        }

        private byte[] GetSampleData(string fileName, int index)
        {
            ModFile modFile = _modManager.FindModFile(fileName.Replace(".DISABLED", ""));
            FmodSample sample = _fmodManager.FmodSoundBank.Samples[modFile.VanillaAudioCandidates[index].index];

            if (!sample.RebuildAsStandardFileFormat(out var data, out var extension))
            {
                Console.WriteLine($"Failed to extract sample {sample.Name}");
            }

            return data;
        }

        private void PlayOgg(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;

            if (btn.Name == "CandidateAudioButton1" || btn.Name == "CandidateAudioButton2")
            {
                if (lstModAudioInfo.SelectedIndex > -1 && lstNames.SelectedIndex > -1)
                {
                    string fileName = lstModAudioInfo.SelectedItem.ToString().Replace(".DISABLED", "");
                    _fmodManager.PlayOgg(null, GetSampleData(fileName, (btn.Name == "CandidateAudioButton1" ? 0 : 1)));
                }
            }
            else
            {
                if (lstModAudioInfo.SelectedIndex > -1)
                {
                    string path = _displayedModAudioFilesInfo[lstModAudioInfo.SelectedIndex].FullName;

                    _fmodManager.PlayOgg(path);
                }
            }
        }

        private void ToggleAssignVanillaAudio(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            TextBlock textBlock = (TextBlock)sender;

            Mod selectedMod = _modManager.FindMod(lstNames.SelectedItem.ToString().Replace(".DISABLED", ""));
            ModFile modFile = selectedMod.ModFiles[lstModAudioInfo.SelectedIndex + lstModInfo.Items.Count];

            string vanillaFile = modFile.VanillaAudioCandidates[textBlock.Name.Contains("1") ? 0 : 1].name;

            BlackListedVanillaAssets blacklisted = new BlackListedVanillaAssets();
            blacklisted.index = modFile.VanillaAudioCandidates[textBlock.Name.Contains("1") ? 0 : 1].index;
            blacklisted.name = modFile.VanillaAudioCandidates[textBlock.Name.Contains("1") ? 0 : 1].name;
            blacklisted.path = modFile.File.FullName.Substring(modFile.File.FullName.IndexOf(selectedMod.Name) + selectedMod.Name.Length);

            if (textBlock.Name == "CandidateAudioText1")
            {
                if (CandidateAudioStack1.Opacity == 0.3)
                {
                    CandidateAudioStack1.Opacity = 1;
                    CandidateAudioStack1.Background = null;
                    CandidateAudioText1.Foreground = new SolidColorBrush(Colors.Black);

                    _modManager.WriteToMetaFile(selectedMod.MetaFile, blacklisted, true);
                }
                else
                {
                    CandidateAudioStack1.Opacity = 0.3;
                    CandidateAudioStack1.Background = new SolidColorBrush(Colors.Black);
                    CandidateAudioText1.Foreground = new SolidColorBrush(Colors.White);

                    _modManager.WriteToMetaFile(selectedMod.MetaFile, blacklisted);
                }
            }
            if (textBlock.Name == "CandidateAudioText2")
            {
                if (CandidateAudioStack2.Opacity == 0.3)
                {
                    CandidateAudioStack2.Opacity = 1;
                    CandidateAudioStack2.Background = null;
                    CandidateAudioText2.Foreground = new SolidColorBrush(Colors.Black);

                    _modManager.WriteToMetaFile(selectedMod.MetaFile, blacklisted, true);
                }
                else
                {
                    CandidateAudioStack2.Opacity = 0.3;
                    CandidateAudioStack2.Background = new SolidColorBrush(Colors.Black);
                    CandidateAudioText2.Foreground = new SolidColorBrush(Colors.White);

                    _modManager.WriteToMetaFile(selectedMod.MetaFile, blacklisted);
                }
            }
        }

        private void EditModInfo(object sender, System.Windows.Input.KeyEventArgs e)
        {
            TextBox textBox = (TextBox)sender;

            if(e.Key == System.Windows.Input.Key.Enter)
            {
                //write text to mod meta file
                Mod selectedMod = _modManager.FindMod(lstNames.SelectedItem.ToString().Replace(".DISABLED", ""));
                dynamic modJson = LoadJson(selectedMod.MetaFile.FullName);
                
                switch(textBox.Name)
                {
                    case "ModDescriptionTextBox":
                        modJson["pack"]["description"] = textBox.Text.Replace("Description: ", "");
                        break;
                    case "ModVersionTextBox":
                        modJson["pack"]["version"] = textBox.Text.Replace("Version: ", "");
                        break;
                    case "ModAuthorTextBox":
                        modJson["pack"]["author"] = textBox.Text.Replace("Author: ", "");
                        break;
                }

                File.WriteAllText(selectedMod.MetaFile.FullName, modJson.ToString());

                _resMgr.LoadManagers();
                _modManager.BuildModsDatabase();
                DisplaySelectedModInfo();
            }
        }

        private void ExtractVanillaAssets(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;

            switch(button.Name)
            {
                case "btnExtractAll":
                    _resMgr.ExtractAssets();
                    break;
                case "btnExtractListed":
                    switch (_currentAssetDisplayed)
                    {
                        case AssetType.Resources:
                            _resMgr.ExtractAssets(_displayedIndexes);
                            break;
                        case AssetType.FMOD:
                            break;
                        case AssetType.None:
                            break;
                    }
                    break;
                case "btnExtractSelected":
                    if(lstVanilla.SelectedIndex > -1)
                    {
                        switch(_currentAssetDisplayed)
                        {
                            case AssetType.Resources:
                                List<int> list = new List<int>
                                {
                                    _displayedIndexes != null ? _displayedIndexes[lstVanilla.SelectedIndex] : lstVanilla.SelectedIndex
                                };
                                _resMgr.ExtractAssets(list);
                                break;
                            case AssetType.FMOD:
                                break;
                            case AssetType.None:
                                break;
                        }
                    }
                    break;
            }
        }
    }
}
