using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using static KotHModLoaderGUI.ModManager;

namespace KotHModLoaderGUI
{
    public class ResourcesManager
    {
        string _resDir = "../KingOfTheHat_Data/";
        string _resVanilla = "resources.assets.VANILLA";
        //string _resVanilla = "StreamingAssets/aa/StandaloneWindows/defaultlocalgroup_assets_all_a6ca6ecdc93afbba6372a699e3258dc3.bundle";
        string _resNoFlavor = "resources.assets";
        string _classPackage = "lz4.tpk";
        string _modsDir = "../Mods/";

        private FileInfo[] _files;
        private DirectoryInfo _dirInfoMod = new DirectoryInfo(@"..\Mods");

        private AssetsManager _assetsManagerVanilla;
        private AssetsFileInstance _afileInstVanilla;
        private AssetsFile _afileVanilla;

        private AssetsManager[] _assetsManagersModded;
        private AssetsFileInstance[] _afilesInstModded;
        private AssetsFile[] _afilesModded;

        //Load Vanilla and Mods Resources.assets
        public string[] LoadManagers()
        {
            LoadVanillaManager();

            return LoadModdedManagers();
        }

        //Vanilla manager
        private void LoadVanillaManager()
        {
            _assetsManagerVanilla = new AssetsManager();
            _assetsManagerVanilla.LoadClassPackage(_classPackage);

            if (!File.Exists(_resDir + _resVanilla))
                File.Copy(_resDir + _resNoFlavor, _resDir + _resVanilla);

            File.Delete(_resDir + _resNoFlavor);

            _afileInstVanilla = _assetsManagerVanilla.LoadAssetsFile(_resDir + _resVanilla, true);
            _afileVanilla = _afileInstVanilla.file;

            _assetsManagerVanilla.LoadClassDatabaseFromPackage(_afileVanilla.Metadata.UnityVersion);
        }

        //Mods folder managers
        public string[] LoadModdedManagers()
        {
            _files = _dirInfoMod.GetFiles("*");

            _assetsManagersModded = new AssetsManager[_files.Length];
            _afilesInstModded = new AssetsFileInstance[_files.Length];
            _afilesModded = new AssetsFile[_files.Length];

            //Build managers for resources.assets mods
            string[] modList = new string[_files.Length];
            for (int a = 0; a < _files.Length; a++)
            {
                FileInfo file = _files[a];
                modList[a] = file.Name;
                if (!file.Name.Contains(".disabled"))
                {
                    _assetsManagersModded[a] = new AssetsManager();
                    _assetsManagersModded[a].LoadClassPackage(_classPackage);
                    _afilesInstModded[a] = _assetsManagersModded[a].LoadAssetsFile(_modsDir + file.Name, true);
                    if (_assetsManagersModded[a] != null)
                    {
                        _afilesModded[a] = _afilesInstModded[a].file;
                        _assetsManagersModded[a].LoadClassDatabaseFromPackage(_afilesModded[a].Metadata.UnityVersion);
                    }
                }
            }
            return modList;
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
                        ModVanillaTextureFromFileName(file.Name, GetRGBA(file));
                    }
            }

            var writer = new AssetsFileWriter(_resDir + _resNoFlavor);
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

            public string BuildMods()
        {
            LoadModdedManagers();

            //Build replacers for merging resources.assets
            List<string> alreadyModded = new List<string>();
            var replacers = new List<AssetsReplacer>();
            int i = 0;
            foreach (var goInfo in _afileVanilla.GetAssetsOfType(AssetClassID.Texture2D))
            {
                var goBaseVanilla = _assetsManagerVanilla.GetBaseField(_afileInstVanilla, goInfo);
                var name = goBaseVanilla["m_Name"].AsString;

                for (int j = 0; j < _assetsManagersModded.Length; j++)
                {
                    if (_afilesInstModded[j] != null)
                    {
                        var goInfoModded = _afilesModded[j].GetAssetsOfType(AssetClassID.Texture2D)[i];
                        var goBaseModded = _assetsManagersModded[j].GetBaseField(_afilesInstModded[j], goInfoModded);
                        if (goBaseModded["image data"].Value.ToString() != goBaseVanilla["image data"].Value.ToString() && !alreadyModded.Contains(goBaseVanilla["m_Name"].AsString))
                        {
                            Console.WriteLine(goBaseVanilla["m_Name"].AsString + " has changed.");

                            goBaseVanilla["image data"].Value = goBaseModded["image data"].Value;

                            replacers.Add(new AssetsReplacerFromMemory(_afileVanilla, goInfo, goBaseVanilla));
                            alreadyModded.Add(goBaseVanilla["m_Name"].AsString);
                        }
                    }
                }
                i++;
            }

            var writer = new AssetsFileWriter(_resDir + _resNoFlavor);
            _afileVanilla.Write(writer, 0, replacers);
            writer.Close();

            return UnloadModdedManagers();
        }

        private string UnloadModdedManagers()
        {
            string s = "";
            for (int j = 0; j < _assetsManagersModded.Length; j++)
            {
                if (!_files[j].Name.Contains(".disabled"))
                if (_assetsManagersModded[j].Files.Count > 0)
                {
                    _assetsManagersModded[j].UnloadAll();
                }
                if (_assetsManagersModded[j] != null)
                    s = s + _assetsManagersModded[j].Files.Count;
            }
            return s;
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
    }
}
