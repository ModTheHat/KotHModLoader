using AssetsTools.NET;
using AssetsTools.NET.Extra;
using AssetsTools.NET.Texture;
using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using static KotHModLoaderGUI.ModManager;

namespace KotHModLoaderGUI
{
    public class ResourcesManager
    {
        string _resDir = @"..\KingOfTheHat_Data";
        string _resVanilla = "resources.assets.VANILLA";
        string _resNoFlavor = "resources.assets";
        string _classPackage = "lz4.tpk";
        private List<AssetTypeValueField> _unassignedTextureFiles = new List<AssetTypeValueField>();

        private AssetsManager _assetsManagerVanilla;
        private AssetsFileInstance _afileInstVanilla;
        private AssetsFile _afileVanilla;
        private List<AssetTypeValueField> _afilesValueFields = new List<AssetTypeValueField>();

        public AssetsFileInstance AssetsFileInstanceVanilla => _afileInstVanilla;
        public AssetsFile AssetsFileVanilla => _afileVanilla;
        public List<AssetTypeValueField> AFilesValueFields => _afilesValueFields;

        public List<AssetTypeValueField> UnassignedTextureFiles
        {
            get { return _unassignedTextureFiles; }
            set { _unassignedTextureFiles = value; }
        }

        //Initialise paths to mods, resources and other needed files
        public void InitialisePaths()
        {
            DirectoryInfo rootInfo = new DirectoryInfo("..");
            string appPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string rootPath = Path.GetDirectoryName(appPath);

            //Initialise KingOfTheHat_Data folder
            if (!Directory.Exists(_resDir))
            {
                DirectoryInfo[] folders = rootInfo.GetDirectories("KingOfTheHat_Data", SearchOption.AllDirectories);
                if (folders.Length == 0)
                {
                    FolderBrowserDialog d = new FolderBrowserDialog();
                    d.Description = "KingOfTheHat_Data folder not found. Are you sure you have installed the Modloader in the KotH game's folder?" +
                        " Either reinstall the Modloader in the game's folder or choose the KingOfTheHat_Data folder in the file browser.";
                    d.SelectedPath = rootPath;
                    if(d.ShowDialog() == DialogResult.OK)
                    {
                        _resDir = d.SelectedPath;
                    }

                    if (!Directory.Exists(_resDir))
                        Environment.Exit(1);

                }
                else
                {
                    _resDir = folders[0].FullName;
                }
            }
            
            //Initialise lz4.tpk classpackage handler for .assets
            if(!File.Exists(_classPackage))
            {
                FileInfo[] files = rootInfo.GetFiles("lz4.tpk", SearchOption.AllDirectories);
                if (files.Length == 0)
                {
                    OpenFileDialog f = new OpenFileDialog();
                    f.Title = "lz4.tpk found. Navigate to it and choose it or reinstall the Modloader.";

                    f.InitialDirectory = rootPath;
                    f.RestoreDirectory = true;

                    if (f.ShowDialog() == DialogResult.OK)
                    {
                        _classPackage = f.FileName;
                    }

                    if (!File.Exists(_classPackage))
                        Environment.Exit(1);

                }
                else
                {
                    _classPackage = files[0].FullName;
                }
            }

            LoadManagers();
        }

        //Load Vanilla and Mods Resources.assets
        public string[] LoadManagers()
        {
            LoadVanillaManager();

            foreach (var goInfo in _afileVanilla.GetAssetsOfType(AssetClassID.Texture2D))
            {
                var goBaseVanilla = _assetsManagerVanilla.GetBaseField(_afileInstVanilla, goInfo);
                var name = goBaseVanilla["m_Name"].AsString;
                _unassignedTextureFiles.Add(goBaseVanilla);
                _afilesValueFields.Add(goBaseVanilla);
            }

            return null;
        }

        //Vanilla manager
        private void LoadVanillaManager()
        {
            //resources.assets (encrypted, header)
            _assetsManagerVanilla = new AssetsManager();
            _assetsManagerVanilla.LoadClassPackage(_classPackage);

            if (!File.Exists(_resDir + @"\" + _resVanilla))
                File.Copy(_resDir + @"\" + _resNoFlavor, _resDir + @"\" + _resVanilla);

            _afileInstVanilla = _assetsManagerVanilla.LoadAssetsFile(_resDir + @"\" + _resVanilla, true);
            _afileVanilla = _afileInstVanilla.file;

            _assetsManagerVanilla.LoadClassDatabaseFromPackage(_afileVanilla.Metadata.UnityVersion);
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

        List<string> _alreadyModded;
        List<AssetsReplacer> _replacers;
        public string BuildActiveModsTextures(List<Mod> mods)
        {
            _replacers = new List<AssetsReplacer>();
            _alreadyModded = new List<string>();
            _alreadyModdedAsset = new List<int>();
            _alreadyModdedWarning = "";

            //byte array for new .resS file
            _resSData = new byte[0];

            //list of index to be replaced, byte array for changes from mod files

            foreach (var mod in mods)
            {
                FileInfo metaFile = mod.MetaFile;
                dynamic modJson = LoadJson(metaFile.FullName);
                var blacklistedAsset = modJson["BlackListedVanillaAssets"];
                List<string> strings = (List<string>)blacklistedAsset.ToObject(typeof(List<string>));
                List<string> disabled = (List<string>)modJson["DisabledModsOrFiles"].ToObject(typeof(List<string>));

                if (!disabled.Contains("\\" + mod.Name))
                    foreach (FileInfo file in mod.TextureFiles)
                    {
                        var assigned = modJson["AssignedVanillaAssets"][file.FullName.Substring(file.FullName.IndexOf(mod.Name) + mod.Name.Length)];
                        var blacklisted = modJson["BlackListedVanillaAssets"];
                        blacklisted = null;

                        if (!disabled.Contains("\\" + file.FullName.Substring(file.FullName.IndexOf(mod.Name))) && !_alreadyModded.Contains(file.Name))
                        {
                            byte[] bytes = GetRGBA(file);
                            SixLabors.ImageSharp.Image image;
                            using (image = SixLabors.ImageSharp.Image.Load(file.FullName))
                            {
                                var width = image.Width;
                                var height = image.Height;
                            }
                            if (assigned != null)
                            {
                                if (ModVanillaTextureFromFileName(assigned.name.Value, bytes, image.Width, image.Height, strings, file))
                                    _alreadyModded.Add(file.Name);
                            }
                            if(blacklisted == null && assigned == null)
                            {
                                if (ModVanillaTextureFromFileName(file.Name, bytes, image.Width, image.Height, strings, file))
                                    _alreadyModded.Add(file.Name);
                            }
                        }
                    }
            }
            if (_alreadyModdedWarning != "")
                MessageBox.Show(_alreadyModdedWarning);

            File.WriteAllBytes(_resDir + @"\resources.assets.modded.resS", _resSData);

            var writer = new AssetsFileWriter(_resDir + @"\" + _resNoFlavor);
            _afileVanilla.Write(writer, 0, _replacers);
            writer.Close();

            return _replacers.Count + " vanilla textures replaced.";
        }

        string _alreadyModdedWarning = "";
        List<int> _alreadyModdedAsset;
        byte[] _resSData;
        private bool ModVanillaTextureFromFileName(string filename, byte[] dataImage, int width, int height, List<string> blacklisted, FileInfo modFile)
        {
            string differentSizesWarning = "";
            bool replaced = false;
            int i = 0;
            foreach (var goInfo in _afileVanilla.GetAssetsOfType(AssetClassID.Texture2D))
            {
                var goBaseVanilla = _assetsManagerVanilla.GetBaseField(_afileInstVanilla, goInfo);
                var name = goBaseVanilla["m_Name"].AsString;

                if (filename.Contains(name))
                {
                    if (_alreadyModdedAsset.Contains(i))
                    {
                        _alreadyModdedWarning += "More than one modded file tried to modify the asset: " + name + ".\n" +
                            "Only one file will modify the asset.";
                        i++;
                        continue;
                    }

                    dynamic stream = goBaseVanilla["m_StreamData"];
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
                        bytes = goBaseVanilla["image data"].AsByteArray;

                    string str = Encoding.UTF8.GetString(bytes);
                    bool contains = blacklisted.Contains(str);

                    if (!contains)
                    {
                        if (goBaseVanilla["image data"].AsByteArray.Length > 0)
                        {
                            AssetTypeValue value = new AssetTypeValue(dataImage, false);

                            if (goBaseVanilla["m_CompleteImageSize"].AsInt == dataImage.Length)
                            {
                                goBaseVanilla["image data"].Value = value;

                                AssetsReplacerFromMemory replacer = new AssetsReplacerFromMemory(_afileVanilla, goInfo, goBaseVanilla);
                                _replacers.Add(replacer);
                                replaced = true;
                                _alreadyModdedAsset.Add(i);
                            }
                            else
                            {
                                differentSizesWarning += "File: " + modFile.Name + ", trying to replace asset: " + goBaseVanilla["m_Name"].AsString + "\n";
                                goBaseVanilla["image data"].Value = value;
                                goBaseVanilla["m_Width"].Value = new AssetTypeValue(width);
                                goBaseVanilla["m_Height"].Value = new AssetTypeValue(height);
                                goBaseVanilla["m_CompleteSize"].Value = new AssetTypeValue(width * height * 4);

                                AssetsReplacerFromMemory replacer = new AssetsReplacerFromMemory(_afileVanilla, goInfo, goBaseVanilla);
                                _replacers.Add(replacer);
                                replaced = true;
                                _alreadyModdedAsset.Add(i);
                            }
                        }
                        else
                        {
                            stream = goBaseVanilla["m_StreamData"];
                            stream["path"].Value = new AssetTypeValue("resources.assets.modded.resS");
                            stream["offset"].Value = new AssetTypeValue(_resSData.Length);
                            stream["size"].Value = new AssetTypeValue(dataImage.Length / 4);
                            goBaseVanilla["m_StreamData"].Value = new AssetTypeValue(AssetValueType.Array, stream);

                            Array.Resize(ref _resSData, _resSData.Length + dataImage.Length);
                            Buffer.BlockCopy(dataImage, 0, _resSData, _resSData.Length - dataImage.Length, dataImage.Length);

                            AssetsReplacerFromMemory replacer = new AssetsReplacerFromMemory(_afileVanilla, goInfo, goBaseVanilla);
                            _replacers.Add(replacer);
                            replaced = true;
                            _alreadyModdedAsset.Add(i);
                        }
                    }
                }
                i++;
            }
            if (differentSizesWarning != "")
                MessageBox.Show(differentSizesWarning + "Modifying asset with different image sizes will bring weird behaviour unless modded in the code.");
            
            return replaced;
        }

        public List<string> GetVanillaAssets()
        {
            LoadVanillaManager();

            List<string> assets = new List<string>();

            foreach (var goInfo in _afileVanilla.GetAssetsOfType(AssetClassID.Texture2D))
            {
                var goBaseVanilla = _assetsManagerVanilla.GetBaseField(_afileInstVanilla, goInfo);
                var name = goBaseVanilla["m_Name"].AsString;

                assets.Add(name);
            }

                return assets;
        }

        public AssetTypeValueField GetAssetInfo(int index)
        {
            List<AssetFileInfo> vanillaAssets = _afileVanilla.GetAssetsOfType(AssetClassID.Texture2D);
            AssetTypeValueField assetInfos = _assetsManagerVanilla.GetBaseField(_afileInstVanilla, vanillaAssets[index]);

            return assetInfos;
        }

        //Return byte array of rgba value for all pixels; start from bottom left to top right
        public byte[] GetRGBA(FileInfo file)
        {
            Bitmap myBitmap = new Bitmap(file.FullName);

            byte[] rgba = new byte[4 * myBitmap.Width * myBitmap.Height];
            System.Drawing.Color pixelColor;

            for (int j = 0; j < myBitmap.Height; j++)
            {
                for (int i = 0; i < myBitmap.Width; i++)
                {
                    pixelColor = myBitmap.GetPixel(i, j);

                    int index = (i + ((myBitmap.Height - 1 - j) * myBitmap.Width)) * 4;
                    rgba[index] = pixelColor.R;
                    rgba[index + 1] = pixelColor.G;
                    rgba[index + 2] = pixelColor.B;
                    rgba[index + 3] = pixelColor.A;
                }
            }

            return rgba;
        }

        public Image<Bgra32> GetTextureFromField(AssetTypeValueField field)
        {
            Image<Bgra32> textureImage = null;

            TextureFile texture = TextureFile.ReadTextureFile(field); // load base field into helper class
            byte[] textureBgraRaw = texture.GetTextureData(_afileInstVanilla); // get the raw bgra32 data
            if (textureBgraRaw != null)
            {
                textureImage = SixLabors.ImageSharp.Image.LoadPixelData<Bgra32>(textureBgraRaw, texture.m_Width, texture.m_Height); // use imagesharp to convert to image
                textureImage.Mutate(i => i.Flip(FlipMode.Vertical)); // flip on x-axis (all textures in unity are stored flipped like this)
            }

            return textureImage;
        }

        public BitmapImage ToBitmapImage(Bitmap bitmap)
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

        public void ExtractAssets(List<int> indexes = null)
        {
            int assetsQty = indexes == null ? _afilesValueFields.Count : indexes.Count;

            if (Directory.Exists("..\\Extracted Assets"))
                if (!Directory.Exists("..\\Extracted Assets\\Textures"))
                    Directory.CreateDirectory("..\\Extracted Assets\\Textures");
            else
                Directory.CreateDirectory("..\\Extracted Assets\\Textures");

            for (int i = 0; i < assetsQty; i++)
            {
                AssetTypeValueField field = indexes == null ? _afilesValueFields[i] : _afilesValueFields[indexes[i]];
                {
                    AssetTypeValueField textureBase = _afilesValueFields[indexes == null ? i : indexes[i]];

                    TextureFile texture = TextureFile.ReadTextureFile(textureBase); // load base field into helper class
                    byte[] textureBgraRaw = texture.GetTextureData(_afileInstVanilla); // get the raw bgra32 data
                    if (textureBgraRaw != null)
                    {
                        SixLabors.ImageSharp.Image textureImage = SixLabors.ImageSharp.Image.LoadPixelData<Bgra32>(textureBgraRaw, texture.m_Width, texture.m_Height); // use imagesharp to convert to image
                        textureImage.Mutate(i => i.Flip(FlipMode.Vertical)); // flip on x-axis (all textures in unity are stored flipped like this)
                        textureImage.SaveAsPng("..\\Extracted Assets\\Textures" + "\\" + field["m_Name"].AsString + "-" + (indexes == null ? i : indexes[i]) + ".png");
                    }
                }
            }
        }
    }
}
