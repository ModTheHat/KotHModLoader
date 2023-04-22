using AssetsTools.NET;
using Fmod5Sharp.FmodTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Vurdalakov;
using static KotHModLoaderGUI.ModManager;

namespace KotHModLoaderGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
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

            console.Text = ".DISABLED mods and files won't be added to the game.\n" +
                "Double click on a Mod in the Mods tab or on a File in the Mod Info tab to toggle between enabled and disabled.\n" +
                "When you're happy with the enabled mods and files list, click on Build Mods to add them to the game and launch the game.";
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

                    Image<Bgra32> textureImage = _resMgr.GetTextureFromField(infos);
                    ImageSource imageSource = textureImage != null ? _resMgr.ToBitmapImage(ImageSharpExtensions.ToBitmap(textureImage)) : null;
                    VanillaImageViewer.Source = imageSource;
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
                var blacklistedAsset = modJson["BlackListedVanillaAssets"];
                string[] strings = (string[])blacklistedAsset.ToObject(typeof(string[]));

                CloseModFilesUI(AssetType.Resources);

                int candidateQty = 0;
                List<VanillaTextureAssetCandidate> candidates = modFile.VanillaCandidates;
                for (int i = 0; i < candidates.Count; i++)
                {
                    AssetTypeValueField values = candidates[i].values;
                    dynamic stream = values["m_StreamData"];
                    string path = stream["path"].AsString;
                    int offset = stream["offset"].AsInt;
                    int size = stream["size"].AsInt;
                    byte[] resSBytes = null;
                    byte[] bytes = null;
                    if (size > 0)
                    {
                        byte[] resSFile = File.ReadAllBytes("..\\KingOfTheHat_Data\\" + path);
                        resSBytes = new byte[size];
                        Buffer.BlockCopy(resSFile, offset, resSBytes, 0, size);
                        bytes = resSBytes;
                    }
                    else
                        bytes = values["image data"].AsByteArray;

                    string str = Encoding.UTF8.GetString(bytes);
                    bool contains = strings.Contains(str);

                    Image<Bgra32> textureImage = _resMgr.GetTextureFromField(values);
                    ImageSource imageSource = textureImage != null ? _resMgr.ToBitmapImage(ImageSharpExtensions.ToBitmap(textureImage)) : null;

                    if (candidateQty == 0)
                    {
                        CandidateImageViewer1.Source = imageSource;
                        if (contains)
                        {
                            VanillaImageStack1.Opacity = 0.3;
                            VanillaImageStack1.Background = new SolidColorBrush(Colors.Red);
                        }
                    }
                    else
                    {
                        CandidateImageViewer2.Source = imageSource;
                        if (contains)
                        {
                            VanillaImageStack2.Opacity = 0.3;
                            VanillaImageStack2.Background = new SolidColorBrush(Colors.Red);
                        }
                    }
                    candidateQty++;
                    VanillaImageLabel.Text = "\nBelow are vanilla assets automatically found that will be replaced by the mod file.\n" +
                        "Click an image to toggle between red background and normal.\n" +
                        "Reded out images won't be replaced by mod image and will stay vanilla.";
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
                    lstModFileInfo.Items.Add("Mod file name: " + fileName);
                    lstModFileInfo.Items.Add("Size w: " + _modFileImg.PixelWidth + " h: " + _modFileImg.PixelHeight);
                }

                AssignVanillaImageText.Text = "\nBelow are vanilla assets that are manually assigned to be replaced by the mod file.\n" +
                        "Select an image in the Vanilla Assets tab and click the Assign button below to assign it to the mod file.\n" +
                        "Click on the assigned images to unassign it.\n" + 
                        "If a mod file has an assigned image, only the assigned image will be modded.";
                if (modJson["AssignedVanillaAssets"]["\\" + fileName] != null)
                {
                    int ind = modJson["AssignedVanillaAssets"]["\\" + fileName]["index"];
                    string vanillaName = modJson["AssignedVanillaAssets"]["\\" + fileName]["name"];
                    string path = modJson["AssignedVanillaAssets"]["\\" + fileName]["path"];

                    if (ind > 0 && path != null)
                    {
                        AssetTypeValueField assignedValues = _resMgr.GetAssetInfo(ind);

                        Image<Bgra32> textureImage = _resMgr.GetTextureFromField(assignedValues);
                        ImageSource imageSource = textureImage != null ? _resMgr.ToBitmapImage(ImageSharpExtensions.ToBitmap(textureImage)) : null;
                        AssignedImageViewer1.Source = imageSource;
                    }
                }

                foreach (VanillaTextureAssetCandidate assigned in modFile.VanillaCandidates)
                {
                    lstModFileInfo.Items.Add("assigned to vanilla file: " + assigned.values["m_Name"].AsString);
                }
            }
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
                var blacklistedAsset = modJson["BlackListedVanillaAssets"];
                string[] strings = (string[])blacklistedAsset.ToObject(typeof(string[]));

                CloseModFilesUI(AssetType.FMOD);

                List<VanillaAudioAssetCandidate> candidates = modFile.VanillaAudioCandidates;

                //nom du fichier du mod
                lstModAudioFileInfo.Items.Add("Nom du fichier audio: " + fileName);

                //infos du fichier
                foreach (string s in _fmodManager.GetOggFileInfos(file.FullName))
                {
                    lstModAudioFileInfo.Items.Add(s);
                }
                //vanilla assignés automatiquement
                int i = 0;
                foreach (VanillaAudioAssetCandidate candidate in candidates)
                {
                    byte[] bytes = _fmodManager.GetSampleData(candidate.index, out var sample);

                    string str = Encoding.UTF8.GetString(bytes);
                    bool contains = strings.Contains(str);

                    if (i == 0)
                    {
                        CandidateAudioText1.Text = "Audio sample name: " + candidate.name;
                        if(contains)
                        {
                            CandidateAudioStack1.Opacity = 0.3;
                            CandidateAudioStack1.Background = new SolidColorBrush(Colors.Red);
                            CandidateAudioText1.Foreground = new SolidColorBrush(Colors.White);
                        }
                        else
                        {
                            CandidateAudioStack1.Opacity = 1;
                            CandidateAudioStack1.Background = new SolidColorBrush(Colors.White);
                            CandidateAudioText1.Foreground = new SolidColorBrush(Colors.Black);
                        }
                    }
                    else if (i == 1)
                    {
                        CandidateAudioText2.Text = "Audio sample name: " + candidate.name;
                        if (contains)
                        {
                            CandidateAudioStack2.Opacity = 0.3;
                            CandidateAudioStack2.Background = new SolidColorBrush(Colors.Red);
                            CandidateAudioText2.Foreground = new SolidColorBrush(Colors.White);
                        }
                        else
                        {
                            CandidateAudioStack2.Opacity = 1;
                            CandidateAudioStack2.Background = new SolidColorBrush(Colors.White);
                            CandidateAudioText2.Foreground = new SolidColorBrush(Colors.Black);
                        }
                    }

                    i++;
                }
                switch(candidates.Count)
                {
                    case 0:
                        CandidateAudioStack1.Visibility = Visibility.Collapsed;
                        CandidateAudioStack2.Visibility = Visibility.Collapsed;
                        break;
                    case 1:
                        CandidateAudioStack1.Visibility = Visibility.Visible;
                        CandidateAudioStack2.Visibility = Visibility.Collapsed;
                        break;
                    case 2:
                        CandidateAudioStack1.Visibility = Visibility.Visible;
                        CandidateAudioStack2.Visibility = Visibility.Visible;
                        break;
                }
                VanillaAudioLabel.Text = "\nBelow are vanilla assets automatically found that will be replaced by the mod file.\n" +
                    "Click a sound name to toggle between red background and normal.\n" +
                    "Reded out sounds won't be replaced by mod sound and will stay vanilla.";

                //vanilla assignés manuellement

                AssignVanillaAudioText.Text = "\nBelow are vanilla assets that are manually assigned to be replaced by the mod file.\n" +
                        "Select an sound in the Vanilla Sounds tab and click the Assign button below to assign it to the mod file.\n" +
                        "Click on the assigned sound to unassign it.\n" +
                        "If a mod file has an assigned sound, only the assigned image will be modded.";
                if (modJson["AssignedVanillaAssets"]["\\" + fileName] != null)
                {
                    int ind = modJson["AssignedVanillaAssets"]["\\" + fileName]["index"];
                    string vanillaName = modJson["AssignedVanillaAssets"]["\\" + fileName]["name"];
                    string path = modJson["AssignedVanillaAssets"]["\\" + fileName]["path"];

                    if (ind > 0 && path != null)
                    {
                        //AssetTypeValueField assignedValues = _resMgr.GetAssetInfo(ind);
                        FmodSample assignedValues = _fmodManager.GetAssetSample(ind);

                        //Image<Bgra32> textureImage = _resMgr.GetTextureFromField(assignedValues);
                        //ImageSource imageSource = textureImage != null ? _resMgr.ToBitmapImage(ImageSharpExtensions.ToBitmap(textureImage)) : null;

                        //AssignedAudioViewer1.Source = audioSource;
                        AssignedAudioName1.Text = assignedValues.Name;
                    }
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
            
            dynamic stream = vanillaFile["m_StreamData"];
            string path = stream["path"].AsString;
            int offset = stream["offset"].AsInt;
            int size = stream["size"].AsInt;
            byte[] resSBytes = null;

            if (size > 0)
            {
                byte[] resSFile = File.ReadAllBytes("..\\KingOfTheHat_Data\\" + path);

                resSBytes = new byte[size];
                Buffer.BlockCopy(resSFile, offset, resSBytes, 0, size);
            }

            byte[] bytes = null;

            if (vanillaFile["image data"].AsByteArray.Length > 0)
                bytes = vanillaFile["image data"].AsByteArray;
            else
            {
                bytes = resSBytes;
            }

            if (image.Name == "CandidateImageViewer1")
            {
                if (VanillaImageStack1.Opacity == 0.3)
                {
                    VanillaImageStack1.Opacity = 1;
                    VanillaImageStack1.Background = new SolidColorBrush(Colors.LightGray);

                    _modManager.WriteToMetaFile(selectedMod.MetaFile, Encoding.UTF8.GetString(bytes), true);
                }
                else
                {
                    VanillaImageStack1.Opacity = 0.3;
                    VanillaImageStack1.Background = new SolidColorBrush(Colors.Red);

                    _modManager.WriteToMetaFile(selectedMod.MetaFile, Encoding.UTF8.GetString(bytes));
                }
            }
            if (image.Name == "CandidateImageViewer2")
            {
                if (VanillaImageStack2.Opacity == 0.3)
                {
                    VanillaImageStack2.Opacity = 1;
                    VanillaImageStack2.Background = new SolidColorBrush(Colors.LightGray);

                    _modManager.WriteToMetaFile(selectedMod.MetaFile, Encoding.UTF8.GetString(bytes), true);
                }
                else
                {
                    VanillaImageStack2.Opacity = 0.3;
                    VanillaImageStack2.Background = new SolidColorBrush(Colors.Red);

                    _modManager.WriteToMetaFile(selectedMod.MetaFile, Encoding.UTF8.GetString(bytes));
                }
            }
        }

        private void ToggleAssignVanillaAudio(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            TextBlock textBlock = (TextBlock)sender;

            Mod selectedMod = _modManager.FindMod(lstNames.SelectedItem.ToString().Replace(".DISABLED", ""));
            ModFile modFile = selectedMod.ModFiles[lstModAudioInfo.SelectedIndex + lstModInfo.Items.Count];

            string vanillaFile = modFile.VanillaAudioCandidates[textBlock.Name.Contains("1") ? 0 : 1].name;
            int index = modFile.VanillaAudioCandidates[textBlock.Name.Contains("1") ? 0 : 1].index;

            byte[] bytes = _fmodManager.GetSampleData(index, out var sample);

            if (textBlock.Name == "CandidateAudioText1")
            {
                if (CandidateAudioStack1.Opacity == 0.3)
                {
                    CandidateAudioStack1.Opacity = 1;
                    CandidateAudioStack1.Background = new SolidColorBrush(Colors.White);
                    CandidateAudioText1.Foreground = new SolidColorBrush(Colors.Black);

                    _modManager.WriteToMetaFile(selectedMod.MetaFile, Encoding.UTF8.GetString(bytes), true);
                }
                else
                {
                    CandidateAudioStack1.Opacity = 0.3;
                    CandidateAudioStack1.Background = new SolidColorBrush(Colors.Red);
                    CandidateAudioText1.Foreground = new SolidColorBrush(Colors.White);

                    _modManager.WriteToMetaFile(selectedMod.MetaFile, Encoding.UTF8.GetString(bytes));
                }
            }
            if (textBlock.Name == "CandidateAudioText2")
            {
                if (CandidateAudioStack2.Opacity == 0.3)
                {
                    CandidateAudioStack2.Opacity = 1;
                    CandidateAudioStack2.Background = new SolidColorBrush(Colors.White);
                    CandidateAudioText2.Foreground = new SolidColorBrush(Colors.Black);

                    _modManager.WriteToMetaFile(selectedMod.MetaFile, Encoding.UTF8.GetString(bytes), true);
                }
                else
                {
                    CandidateAudioStack2.Opacity = 0.3;
                    CandidateAudioStack2.Background = new SolidColorBrush(Colors.Red);
                    CandidateAudioText2.Foreground = new SolidColorBrush(Colors.White);

                    _modManager.WriteToMetaFile(selectedMod.MetaFile, Encoding.UTF8.GetString(bytes));
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
            System.Windows.Controls.TextBox textBox = (System.Windows.Controls.TextBox)sender;

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
            VanillaImageLabel.Text = "";
            ModImageLabel.Content = "";
            CandidateImageViewer1.Source = null;
            CandidateImageViewer2.Source = null;
            VanillaImageStack1.Opacity = 1;
            VanillaImageStack2.Opacity = 1;
            VanillaImageStack1.Background = new SolidColorBrush(Colors.LightGray);
            VanillaImageStack2.Background = new SolidColorBrush(Colors.LightGray);
            AssignedImageStack1.Background = new SolidColorBrush(Colors.LightGray);
            AssignedImageViewer1.Source = null;
            AssignedImageViewer.Source = null;
            ModdedImageViewer.Source = null;
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
            System.Windows.Controls.Button btn = (System.Windows.Controls.Button)sender;

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

        private void EditModInfo(object sender, System.Windows.Input.KeyEventArgs e)
        {
            System.Windows.Controls.TextBox textBox = (System.Windows.Controls.TextBox)sender;

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
            System.Windows.Controls.Button button = (System.Windows.Controls.Button)sender;

            switch(button.Name)
            {
                case "btnExtractAll":
                    _resMgr.ExtractAssets();
                    _fmodManager.ExtractAssets();
                    break;
                case "btnExtractListed":
                    switch (_currentAssetDisplayed)
                    {
                        case AssetType.Resources:
                            _resMgr.ExtractAssets(_displayedIndexes);
                            break;
                        case AssetType.FMOD:
                            _fmodManager.ExtractAssets(_displayedIndexes);
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
                                List<int> listTexture = new List<int>
                                {
                                    _displayedIndexes != null ? _displayedIndexes[lstVanilla.SelectedIndex] : lstVanilla.SelectedIndex
                                };
                                _resMgr.ExtractAssets(listTexture);
                                break;
                            case AssetType.FMOD:
                                List<int> listAudio = new List<int>
                                {
                                    _displayedIndexes != null ? _displayedIndexes[lstVanilla.SelectedIndex] : lstVanilla.SelectedIndex
                                };
                                _fmodManager.ExtractAssets(listAudio);
                                break;
                            case AssetType.None:
                                break;
                        }
                    }
                    break;
            }
        }

        private void ButtonResetToVanilla(object sender, RoutedEventArgs e)
        {
            if (File.Exists("..\\KingOfTheHat_Data\\resources.assets"))
                File.Delete("..\\KingOfTheHat_Data\\resources.assets");

            File.Copy("..\\KingOfTheHat_Data\\resources.assets.VANILLA", "..\\KingOfTheHat_Data\\resources.assets");
            console.Text = "The game data has been reset to original.";
        }

        public static void Warning(string message)
        {
            MessageBox.Show(message);
        }

        private void RemoveAssignedVanillaAudio(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {

        }

        private void AssignVanillaAudio(object sender, RoutedEventArgs e)
        {
            Mod selectedMod = _modManager.FindMod(lstNames.SelectedItem.ToString().Replace(".DISABLED", ""));
            ModFile modFile = selectedMod.ModFiles[lstModInfo.Items.Count + lstModAudioInfo.SelectedIndex];

            if (_currentAssetDisplayed != AssetType.FMOD)
            {
                MessageBox.Show("You selected an asset that is not an audio file.\n" +
                    "Select a vanilla audio in the Vanilla Audio Tab and then assign it to the mod asset.");
                return;
            }

            int index = _displayedIndexes != null ? _displayedIndexes[lstVanilla.SelectedIndex] : lstVanilla.SelectedIndex;

            //AssetTypeValueField vanillaFile = _resMgr.GetAssetInfo(index);
            FmodSample vanillaFile = _fmodManager.GetAssetSample(index);

            AssignedVanillaAssets assigned = new AssignedVanillaAssets();
            assigned.index = index;
            assigned.name = vanillaFile.Name;
            assigned.path = modFile.File.FullName.Substring(modFile.File.FullName.IndexOf(selectedMod.Name) + selectedMod.Name.Length);

            dynamic modJson = LoadJson(selectedMod.MetaFile.FullName);
            modJson["AssignedVanillaAssets"][assigned.path] = JToken.FromObject(assigned);

            File.WriteAllText(selectedMod.MetaFile.FullName, modJson.ToString());

            DisplaySelectedModAudioInfo();
        }
    }
}
