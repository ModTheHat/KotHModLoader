using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using static KotHModLoaderGUI.ModManager;

namespace KotHModLoaderGUI
{
    public class ResourcesManager
    {
        string _resDir = @"..\KingOfTheHat_Data";
        string _resVanilla = "resources.assets.VANILLA";
        string _resNoFlavor = "resources.assets";
        string _classPackage = "lz4.tpk";
        private List<string> _unassignedTextureFiles = new List<string>();

        private AssetsManager _assetsManagerVanilla;
        private AssetsFileInstance _afileInstVanilla;
        private AssetsFile _afileVanilla;

        public List<string> UnassignedTextureFiles
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
                    System.Windows.Forms.OpenFileDialog f = new System.Windows.Forms.OpenFileDialog();
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
                _unassignedTextureFiles.Add(name);
            }

            return null;
        }

        //Vanilla manager
        private void LoadVanillaManager()
        {
            _assetsManagerVanilla = new AssetsManager();
            _assetsManagerVanilla.LoadClassPackage(_classPackage);

            if (!File.Exists(_resDir + @"\" + _resVanilla))
                File.Copy(_resDir + @"\" + _resNoFlavor, _resDir + @"\" + _resVanilla);

            File.Delete(_resDir + @"\" + _resNoFlavor);

            _afileInstVanilla = _assetsManagerVanilla.LoadAssetsFile(_resDir + @"\" + _resVanilla, true);
            _afileVanilla = _afileInstVanilla.file;

            _assetsManagerVanilla.LoadClassDatabaseFromPackage(_afileVanilla.Metadata.UnityVersion);
        }

        List<string> _alreadyModded;
        List<AssetsReplacer> _replacers;
        public string BuildActiveModsTextures(List<Mod> mods)
        {
            _replacers = new List<AssetsReplacer>();
            _alreadyModded = new List<string>();

            foreach (var mod in mods)
            {
                if(!mod.Name.Contains(".disabled"))
                    foreach (FileInfo file in mod.Files)
                    {
                        if(!file.Name.Contains(".disabled"))
                            ModVanillaTextureFromFileName(file.Name, GetRGBA(file));
                    }
            }

            var writer = new AssetsFileWriter(_resDir + @"\" + _resNoFlavor);
            _afileVanilla.Write(writer, 0, _replacers);
            writer.Close();

            return "Mods textures replaced.";
        }

        private string ModVanillaTextureFromFileName(string filename, byte[] dataImage)
        {
            foreach (var goInfo in _afileVanilla.GetAssetsOfType(AssetClassID.Texture2D))
            {
                var goBaseVanilla = _assetsManagerVanilla.GetBaseField(_afileInstVanilla, goInfo);
                var name = goBaseVanilla["m_Name"].AsString;

                if(filename.Contains(name))
                {
                    AssetTypeValue value = new AssetTypeValue(dataImage, false);

                    if (goBaseVanilla["m_CompleteImageSize"].AsInt == dataImage.Length && !_alreadyModded.Contains(name))
                    {
                        goBaseVanilla["image data"].Value = value;

                        AssetsReplacerFromMemory replacer = new AssetsReplacerFromMemory(_afileVanilla, goInfo, goBaseVanilla);
                        _replacers.Add(replacer);
                        _alreadyModded.Add(name);
                    }
                }
            }
            return null;
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

        public AssetTypeValueField GetAssetInfo(string assetName)
        {
            foreach (var goInfo in _afileVanilla.GetAssetsOfType(AssetClassID.Texture2D))
            {
                var goBaseVanilla = _assetsManagerVanilla.GetBaseField(_afileInstVanilla, goInfo);
                var name = goBaseVanilla["m_Name"].AsString;

                if(name == assetName)
                {
                    return goBaseVanilla;
                }
            }
            
            return null;
        }

        //Return byte array of rgba value for all pixels; start from bottom left to top right
        public byte[] GetRGBA(FileInfo file)
        {
            Bitmap myBitmap = new Bitmap(file.FullName);

            byte[] rgba = new byte[4 * myBitmap.Width * myBitmap.Height];
            Color pixelColor;

            for (int j = 0; j < myBitmap.Height; j++)
            {
                for (int i = 0; i < myBitmap.Width; i++)
                {
                    pixelColor = myBitmap.GetPixel(i, j);

                    rgba[(i + ((myBitmap.Height - 1 - j) * myBitmap.Width)) * 4] = pixelColor.R;
                    rgba[(i + ((myBitmap.Height - 1 - j) * myBitmap.Width)) * 4 + 1] = pixelColor.G;
                    rgba[(i + ((myBitmap.Height - 1 - j) * myBitmap.Width)) * 4 + 2] = pixelColor.B;
                    rgba[(i + ((myBitmap.Height - 1 - j) * myBitmap.Width)) * 4 + 3] = pixelColor.A;
                }
            }

            return rgba;
        }
        public AssetTypeValueField GetVanillaDataImage(string assetName)
        {
            foreach (var goInfo in _afileVanilla.GetAssetsOfType(AssetClassID.Texture2D))
            {
                var goBaseVanilla = _assetsManagerVanilla.GetBaseField(_afileInstVanilla, goInfo);
                var name = goBaseVanilla["m_Name"].AsString;

                if (name == assetName)
                {
                    return goBaseVanilla;
                }
            }

            return null;
        }
    }
}
