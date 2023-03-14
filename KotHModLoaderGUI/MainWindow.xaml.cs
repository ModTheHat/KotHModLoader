using AssetsTools.NET;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
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
        private static ResourcesManager _resMgr = new ResourcesManager();
        private string[] _folders;
        private ModManager _modManager = new ModManager();
        private FMODManager _fmodManager = new FMODManager();
        private FileInfo[] _displayedModFilesInfo;
        private FileInfo[] _displayedModAudioFilesInfo;

        public static ResourcesManager ResMgr => _resMgr;

        public MainWindow()
        {
            InitializeComponent();

            _modManager.InitialisePaths();
            _resMgr.InitialisePaths();
            _fmodManager.InitialisePaths();

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
            System.Windows.Controls.ListBox lstBox = (System.Windows.Controls.ListBox)(sender);
            if (lstBox.SelectedIndex > -1)
            {
                console.Text += _modManager.ToggleModActive(new DirectoryInfo(_modManager.DirInfoMod + @"\" + lstBox.SelectedItem.ToString()));

                //_modManager.BuildModsDatabase();
                _resMgr.LoadManagers();
                DisplayMods();
                DisplaySelectedModInfo();
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

        public enum AssetType
        {
            Resources,
            FMOD
        }
        private AssetType _currentAssetDisplayed = AssetType.Resources;
        private void DisplayVanillaAssetInfo(object sender, SelectionChangedEventArgs e)
        {
            ListBox lstBox = (ListBox)(sender);
            if (lstBox.SelectedIndex == -1) return;

            int index;

            string assetName = lstBox.SelectedItem.ToString();

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
                        Bitmap vanillaImage = GetDataPicture(infos["m_Width"].AsInt, infos["m_Height"].AsInt, infos["image data"].AsByteArray);
                        VanillaImageViewer.Source = ToBitmapImage(vanillaImage);
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
            ListBox lstBox = (ListBox)(sender);
            if (lstBox.SelectedIndex > -1)
            {
                string modName = lstBox.SelectedItem.ToString();

                lstModFileInfo.Items.Clear();
                VanillaImageLabel.Content = "";
                ModImageLabel.Content = "";
                AssignedImageViewer.Source = null;
                CandidateImageViewer1.Source = null;
                CandidateImageViewer2.Source = null;
                AssignedImageViewer1.Source = null;
                AddAssignedButton.Visibility = Visibility.Hidden;
                ModdedImageViewer.Source = null;

                _displayedModFilesInfo = _modManager.GetModFiles(modName);
                _displayedModAudioFilesInfo = _modManager.GetModFiles(modName, AssetType.FMOD);

                lstModFilesInfo.Items.Clear();
                lstModFilesInfo.Items.Add("Description: " + _modManager.FindMod(modName).Description);
                lstModFilesInfo.Items.Add("Version: " + _modManager.FindMod(modName).Version);
                lstModFilesInfo.Items.Add("Author: " + _modManager.FindMod(modName).Author);

                lstModInfo.Items.Clear();
                foreach (var info in _displayedModFilesInfo)
                {
                    lstModInfo.Items.Add(info.Name);
                }

                lstModAudioInfo.Items.Clear();
                foreach (var info in _displayedModAudioFilesInfo)
                {
                    lstModAudioInfo.Items.Add(info.Name);
                }
            }
        }

        private void DisplaySelectedModInfo()
        {
            if (lstNames.SelectedIndex > -1)
            {
                string modName = lstNames.SelectedItem.ToString();

                lstModFileInfo.Items.Clear();
                VanillaImageLabel.Content = "";
                ModImageLabel.Content = "";
                AssignedImageViewer.Source = null;
                CandidateImageViewer1.Source = null;
                CandidateImageViewer2.Source = null;
                AssignedImageViewer1.Source = null;
                AddAssignedButton.Visibility = Visibility.Hidden;
                ModdedImageViewer.Source = null;

                _displayedModFilesInfo = _modManager.GetModFiles(modName);
                _displayedModAudioFilesInfo = _modManager.GetModFiles(modName, AssetType.FMOD);

                lstModFilesInfo.Items.Clear();
                lstModFilesInfo.Items.Add("Description: " + _modManager.FindMod(modName).Description);
                lstModFilesInfo.Items.Add("Version: " + _modManager.FindMod(modName).Version);
                lstModFilesInfo.Items.Add("Author: " + _modManager.FindMod(modName).Author);

                lstModInfo.Items.Clear();
                foreach (var info in _displayedModFilesInfo)
                {
                    lstModInfo.Items.Add(info.Name);
                }

                lstModAudioInfo.Items.Clear();
                foreach (var info in _displayedModAudioFilesInfo)
                {
                    lstModAudioInfo.Items.Add(info.Name);
                }
            }
        }

        private void DisplayModFileInfo(object sender, SelectionChangedEventArgs e)
        {
            DisplayModFileInfoRaw();
        }

        private void DisplayModFileInfoRaw()
        {
            lstModFileInfo.Items.Clear();
            if (lstModInfo.SelectedIndex > -1)
            {
                Mod mod = _modManager.FindMod(lstNames.SelectedItem.ToString());
                string fileName = lstModInfo.SelectedItem.ToString();
                DirectoryInfo folder = _modManager.DirInfoMod;
                FileInfo[] files = folder.GetFiles(fileName, SearchOption.AllDirectories);
                FileInfo file = files[0];
                ModFile modFile = _modManager.FindModFile(fileName);
                FileInfo metaFile = mod.MetaFile;
                dynamic modJson = LoadJson(metaFile.FullName);

                lstModAudioInfo.SelectedIndex = -1;
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
                AddAssignedButton.Visibility = Visibility.Hidden;

                int candidateQty = 0;
                List<VanillaAssetCandidate> candidates = modFile.VanillaCandidates;
                for (int i = 0; i < candidates.Count; i++)
                {
                    AssetTypeValueField values = candidates[i].values;
                    if (values["image data"].AsByteArray.Length > 0 && values["m_Width"].AsInt * values["m_Height"].AsInt * 4 == values["image data"].AsByteArray.Length)
                    {
                        Bitmap vanillaImage = GetDataPicture(values["m_Width"].AsInt, values["m_Height"].AsInt, values["image data"].AsByteArray);
                        if (candidateQty == 0)
                        {
                            CandidateImageViewer1.Source = ToBitmapImage(vanillaImage);
                            var blacklistedAsset = modJson["BlackListedVanillaAssets"][file.FullName.Substring(file.FullName.IndexOf(mod.Name) + mod.Name.Length)];
                            if (blacklistedAsset != null)
                            {
                                VanillaImageStack1.Opacity = 0.3;
                                VanillaImageStack1.Background = new SolidColorBrush(Colors.Black);
                            }
                        }
                        else
                        {
                            CandidateImageViewer2.Source = ToBitmapImage(vanillaImage);
                            var blacklistedAsset = modJson["BlackListedVanillaAssets"][file.FullName.Substring(file.FullName.IndexOf(mod.Name) + mod.Name.Length)];
                            if (blacklistedAsset != null)
                            {
                                VanillaImageStack1.Opacity = 0.3;
                                VanillaImageStack1.Background = new SolidColorBrush(Colors.Black);
                            }
                        }
                        candidateQty++;
                        VanillaImageLabel.Content = "Vanilla files that will be replaced. Click image to toggle between greyed out and normal. Greyed out images won't be replaced by mod asset file.";
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
                    ModImageLabel.Content = "Mod file texture";
                }

                AddAssignedButton.Visibility = Visibility.Visible;

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
                            Bitmap assignedBitmap = GetDataPicture(assignedValues["m_Width"].AsInt, assignedValues["m_Height"].AsInt, assignedValues["image data"].AsByteArray);
                            //assign viewer.source with ToBitmapImage
                            AssignedImageViewer1.Source = ToBitmapImage(assignedBitmap);
                        }
                    }
                }

                lstModFileInfo.Items.Add("mod file name: " + fileName);
                foreach (VanillaAssetCandidate assigned in modFile.VanillaCandidates)
                {
                    lstModFileInfo.Items.Add("assigned to vanilla file: " + assigned.values["m_Name"].AsString);
                }
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
                    System.Drawing.Color c = System.Drawing.Color.FromArgb(
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
                }
                _resMgr.LoadManagers();
                _modManager.BuildModsDatabase();
                DisplaySelectedModInfo();
            }
        }

        private void ToggleAssignVanillaImage(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            System.Windows.Controls.Image image = (System.Windows.Controls.Image)sender;

            Mod selectedMod = _modManager.FindMod(lstNames.SelectedItem.ToString());
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

                    WriteToMetaFile(selectedMod.MetaFile, blacklisted, true);
                }
                else
                {
                    VanillaImageStack1.Opacity = 0.3;
                    VanillaImageStack1.Background = new SolidColorBrush(Colors.Black);

                    WriteToMetaFile(selectedMod.MetaFile, blacklisted);
                }
            }
            if (image.Name == "CandidateImageViewer2")
            {
                if (VanillaImageStack2.Opacity == 0.3)
                {
                    VanillaImageStack2.Opacity = 1;
                    VanillaImageStack2.Background = null;

                    WriteToMetaFile(selectedMod.MetaFile, blacklisted, true);
                }
                else
                {
                    VanillaImageStack2.Opacity = 0.3;
                    VanillaImageStack2.Background = new SolidColorBrush(Colors.Black);

                    WriteToMetaFile(selectedMod.MetaFile, blacklisted);
                }
            }
        }

        private void WriteToMetaFile(FileInfo metafile, BlackListedVanillaAssets blacklisted, bool remove = false)
        {
            dynamic modJson = LoadJson(metafile.FullName);

            if (!remove)
                modJson["BlackListedVanillaAssets"][blacklisted.path] = JToken.FromObject(blacklisted);
            else
                modJson["BlackListedVanillaAssets"].Remove(blacklisted.path);

            File.WriteAllText(metafile.FullName, modJson.ToString());
        }

        private void AssignVanillaTexture(object sender, RoutedEventArgs e)
        {
            Mod selectedMod = _modManager.FindMod(lstNames.SelectedItem.ToString());
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
            //modJson["AssignedVanillaAssets"] = System.Text.Json.JsonSerializer.Serialize(assigned);

            File.WriteAllText(selectedMod.MetaFile.FullName, modJson.ToString());

            DisplayModFileInfoRaw();
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
            Mod selectedMod = _modManager.FindMod(lstNames.SelectedItem.ToString());
            ModFile modFile = selectedMod.ModFiles[lstModInfo.SelectedIndex];

            string path = modFile.File.FullName.Substring(modFile.File.FullName.IndexOf(selectedMod.Name) + selectedMod.Name.Length);

            dynamic modJson = LoadJson(selectedMod.MetaFile.FullName);

            modJson["AssignedVanillaAssets"].Remove(path);
            //modJson["AssignedVanillaAssets"] = System.Text.Json.JsonSerializer.Serialize(assigned);

            File.WriteAllText(selectedMod.MetaFile.FullName, modJson.ToString());

            DisplayModFileInfoRaw();
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
            System.Windows.Controls.TextBox textBox = (System.Windows.Controls.TextBox)sender;

            textBox.SelectAll();
        }

        private List<int> _displayedIndexes;
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
            //string query = textBox.Text;
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

        private void DisplaySelectedModAudioInfo()
        {
            lstModAudioFileInfo.Items.Clear();
            if (lstModAudioInfo.SelectedIndex > -1)
            {
                Mod mod = _modManager.FindMod(lstNames.SelectedItem.ToString());
                string fileName = lstModAudioInfo.SelectedItem.ToString();
                DirectoryInfo folder = _modManager.DirInfoMod;
                FileInfo[] files = folder.GetFiles(fileName, SearchOption.AllDirectories);
                FileInfo file = files[0];
                ModFile modFile = _modManager.FindModFile(fileName);
                FileInfo metaFile = mod.MetaFile;
                dynamic modJson = LoadJson(metaFile.FullName);

                lstModInfo.SelectedIndex = -1;
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
                AddAssignedButton.Visibility = Visibility.Hidden;

                int candidateQty = 0;
                List<VanillaAudioAssetCandidate> candidates = modFile.VanillaAudioCandidates;

                //nom du fichier du mod
                lstModAudioFileInfo.Items.Add("Nom du fichier audio: " + fileName);

                //infos du fichier
                //bouton play

                //vanilla assignés automatiquement

                //vanilla assignés manuellement
            }
        }

        private void PlayOgg(object sender, RoutedEventArgs e)
        {
            ListBox lstBox = (ListBox)sender;

            
        }
    }
}
