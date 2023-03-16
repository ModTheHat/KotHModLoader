using AssetsTools.NET;
using Fmod5Sharp.FmodTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace KotHModLoaderGUI
{
    public enum AssetType
    {
        None = -1,
        Resources = 0,
        FMOD
    }

    public class MetaFile
    {
        public PackMeta pack { get; set; }
        public AssignedVanillaAssets AssignedVanillaAssets { get; set; }
        public BlackListedVanillaAssets BlackListedVanillaAssets { get; set; }
    }

    public struct PackMeta
    {
        public string version { get; set; }
        public string description { get; set; }
        public string author { get; set; }
        public string mods { get; set; }
    }

    public struct AssignedVanillaAssets
    {
        public int index { get; set; }
        public string name { get; set; }
        public string path { get; set; }
    }
    public struct BlackListedVanillaAssets
    {
        public int index { get; set; }
        public string name { get; set; }
        public string path { get; set; }
    }

    public struct VanillaTextureAssetCandidate
    {
        public int index { get; set; }
        public AssetTypeValueField values { get; set; }
    }

    public struct VanillaAudioAssetCandidate
    {
        public int index { get; set; }
        public string name { get; set; }
    }

    public struct ModFile
    {
        public FileInfo File;
        public List<VanillaTextureAssetCandidate> VanillaCandidates;
        public List<VanillaAudioAssetCandidate> VanillaAudioCandidates;

        public ModFile(FileInfo file, string assigned = "")
        {
            File = file;
            VanillaCandidates = new List<VanillaTextureAssetCandidate>();
            VanillaAudioCandidates = new List<VanillaAudioAssetCandidate>();
        }
    }

    public class ModManager
    {
        public struct Mod
        {
            public string Name;
            public string Description;
            public string Version;
            public string Author;
            public DirectoryInfo ModDirectoryInfo;
            public FileInfo[] TextureFiles;
            public FileInfo[] AudioFiles;
            public ModFile[] ModFiles;
            public FileInfo MetaFile;

            public Mod(DirectoryInfo dirInfo, string description = "", string version = "", string author = "unknown")
            {
                ModDirectoryInfo = dirInfo;
                MetaFile = GetMetaFileInfo(ModDirectoryInfo);
                Description = description;
                Version = version;
                Author = author;
                if (MetaFile != null)
                {
                    dynamic json = LoadJson(MetaFile.FullName);
                    Description = json["pack"]["description"];
                    Version = json["pack"]["version"];
                    Author = json["pack"]["author"];
                }
                Name = dirInfo.Name;
                TextureFiles = GetTextureFilesInfo(ModDirectoryInfo);
                AudioFiles = GetAudioFilesInfo(ModDirectoryInfo);
                ModFiles = GetModFilesInfo(ModDirectoryInfo);
            }
        }

        private string _modDir = @"..\Mods(new structure)";
        private static string _metaFile = @"\packmeta.json";
        private DirectoryInfo _dirInfoMod;
        private DirectoryInfo[] _folders;

        public DirectoryInfo DirInfoMod => _dirInfoMod;


        private List<Mod> _modsList = new();
        public List<Mod> ModsList => _modsList;


        //Initialise paths to mods, resources and other needed files
        public void InitialisePaths()
        {
            DirectoryInfo rootInfo = new DirectoryInfo("..");
            string appPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string rootPath = Path.GetDirectoryName(appPath);

            //Initialise KingOfTheHat_Data folder
            if (!Directory.Exists(_modDir))
            {
                DirectoryInfo[] folders = rootInfo.GetDirectories("Mods(new structure)", SearchOption.AllDirectories);
                if (folders.Length == 0)
                {
                    FolderBrowserDialog d = new FolderBrowserDialog();
                    d.Description = "Mods folder not found." +
                        " Either reinstall the Modloader in the game's folder or choose the Mods folder in the folder browser.";
                    d.SelectedPath = rootPath;
                    if (d.ShowDialog() == DialogResult.OK)
                    {
                        _modDir = d.SelectedPath;
                    }

                    if (!Directory.Exists(_modDir))
                        Environment.Exit(1);

                }
                else
                {
                    _modDir = folders[0].FullName;
                }
            }

            _dirInfoMod = new DirectoryInfo(_modDir);
        }


            //Go through all mods in mods folder and add them to manager mods list
            public string[] BuildModsDatabase()
        {
            _modsList.Clear();
            _folders = _dirInfoMod.GetDirectories("*");
            string[] foldersNames = new string[_folders.Length];
            for (int i = 0; i < _folders.Length; i++)
            {
                DirectoryInfo folder = _folders[i];
                foldersNames[i] = folder.Name;
                _modsList.Add(new Mod(folder));
            }
            return foldersNames;
        }

        public string BuildMods()
        {
            return MainWindow.ResMgr.BuildActiveModsTextures(_modsList);
        }

        public Mod FindMod(string name)
        {
            foreach (Mod mod in _modsList) 
            {
                if (mod.Name == name)
                    return mod;
            }
            return new Mod();
        }

        //Go through mod folder and list all modded files
        public FileInfo[] GetModFiles(string modName, AssetType type = AssetType.Resources)
        {
            switch(type)
            {
                case AssetType.Resources:
                    return FindMod(modName).TextureFiles;
                case AssetType.FMOD:
                    return FindMod(modName).AudioFiles;
            }

            return null;
        }

        public ModFile FindModFile(string filename)
        {
            foreach (Mod mod in _modsList)
            {
                foreach(ModFile file in mod.ModFiles)
                {
                    if(file.File != null)
                    if(filename.Contains(file.File.Name)) return file;
                }
            }
            return new ModFile();
        }

        private static FileInfo[] GetTextureFilesInfo(DirectoryInfo folder)
        {
            FileInfo[] files = folder.GetFiles("*.png", SearchOption.AllDirectories);
            files = files.Concat(folder.GetFiles("*.png.disabled", SearchOption.AllDirectories)).ToArray();

            return files;
        }
        
        private static FileInfo[] GetAudioFilesInfo(DirectoryInfo folder)
        {
            FileInfo[] files = folder.GetFiles("*.ogg", SearchOption.AllDirectories);
            files = files.Concat(folder.GetFiles("*.wav", SearchOption.AllDirectories)).ToArray();
            files = files.Concat(folder.GetFiles("*.mp3", SearchOption.AllDirectories)).ToArray();
            files = files.Concat(folder.GetFiles("*.ogg.disabled", SearchOption.AllDirectories)).ToArray();
            files = files.Concat(folder.GetFiles("*.wav.disabled", SearchOption.AllDirectories)).ToArray();
            files = files.Concat(folder.GetFiles("*.mp3.disabled", SearchOption.AllDirectories)).ToArray();

            return files;
        }

        private static ModFile[] GetModFilesInfo(DirectoryInfo folder)
        {
            FileInfo[] fileInfos = GetTextureFilesInfo(folder);
            FileInfo[] audioFileInfos = GetAudioFilesInfo(folder);
            ModFile[] files = new ModFile[fileInfos.Length];

            for(int i = 0; i < fileInfos.Length;i++)
            {
                ModFile file = files[i];
                file.File = fileInfos[i];
                file.VanillaCandidates = AssignVanillaFilesIndexes(fileInfos[i]);
                files[i] = file;
            }
            for (int i = 0; i < audioFileInfos.Length; i++)
            {
                ModFile file = files[i];
                file.File = audioFileInfos[i];
                file.VanillaAudioCandidates = AssignVanillaAudioFilesIndexes(audioFileInfos[i]);
                files[i] = file;
            }

            return files;
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

        private static bool ValidateNewMeta(DirectoryInfo folder)
        {
            DialogResult dialogResult = MessageBox.Show("No compatible meta file found in " + folder.Name.Replace(".disabled", "") + ", do you want to create one? Without one, options like Assigning assets manually won't be saved.", folder.Name.Replace(".disabled", "") + ": No meta file found", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                return true;
            }

            return false;
        }

        private static FileInfo GetMetaFileInfo(DirectoryInfo folder)
        {
            bool createNewMeta = false; 
            FileInfo info = null;

            if (File.Exists(folder.FullName + _metaFile))
            {
                info = new FileInfo(folder.FullName + _metaFile);
                dynamic metafile = LoadJson(info.FullName);

                if (metafile.GetType() == typeof(JArray))
                    createNewMeta = ValidateNewMeta(folder);

                if (metafile.GetType() == typeof(JObject))
                    if (metafile["pack"] == null)
                        createNewMeta = ValidateNewMeta(folder);
            }
            else
                createNewMeta = ValidateNewMeta(folder);

            if (createNewMeta)
            {
                MetaFile data = new MetaFile()
                {
                    AssignedVanillaAssets = new AssignedVanillaAssets(),
                    BlackListedVanillaAssets = new BlackListedVanillaAssets()
                };

                string json = System.Text.Json.JsonSerializer.Serialize(data);
                File.WriteAllText(folder.FullName + _metaFile, json);
                info = new FileInfo(folder.FullName + _metaFile);
            }

            return info;
        }

        private static List<AssetTypeValueField> AssignVanillaFiles(FileInfo file)
        {
            List<AssetTypeValueField> unassigned = MainWindow.ResMgr.UnassignedTextureFiles;
            List<AssetTypeValueField> assigned = new List<AssetTypeValueField>();

            for (int i = 0; i < unassigned.Count; i++)
            {
                AssetTypeValueField unassignedFile = unassigned[i];
                if (file.Name.Contains(unassignedFile["m_Name"].AsString))
                {
                    MainWindow.ResMgr.UnassignedTextureFiles.Remove(unassignedFile);
                    assigned.Add(unassignedFile);
                }
            }

            return assigned;
        }

        private static List<int> _assignedIndexes = new List<int>();
        private static List<VanillaTextureAssetCandidate> AssignVanillaFilesIndexes(FileInfo file)
        {
            List<AssetTypeValueField> assetsValues = MainWindow.ResMgr.AFilesValueFields;
            List<VanillaTextureAssetCandidate> assigned = new List<VanillaTextureAssetCandidate>();

            for (int i = 0; i < assetsValues.Count; i++)
            {
                AssetTypeValueField values = assetsValues[i];

                if (file.Name.Contains(values["m_Name"].AsString) && !_assignedIndexes.Contains(i))
                {
                    VanillaTextureAssetCandidate assets = new VanillaTextureAssetCandidate();
                    assets.index = i;
                    assets.values = values;
                    assigned.Add(assets);
                    _assignedIndexes.Add(i);
                }
            }

            return assigned;
        }

        private static List<int> _assignedFMODIndexes = new List<int>();
        private static List<VanillaAudioAssetCandidate> AssignVanillaAudioFilesIndexes(FileInfo file)
        {
            FmodSoundBank assetsValues = MainWindow.FMODManager.FmodSoundBank;
            List<VanillaAudioAssetCandidate> assigned = new List<VanillaAudioAssetCandidate>();

            for (int i = 0; i < assetsValues.Samples.Count; i++)
            {
                FmodSample sample = assetsValues.Samples[i];

                if(sample.Name.Contains(file.Name.Substring(0, file.Name.IndexOf("."))) && !_assignedFMODIndexes.Contains(i))
                {
                    VanillaAudioAssetCandidate assets = new VanillaAudioAssetCandidate();
                    assets.index = i;
                    assets.name = sample.Name;
                    assigned.Add(assets);
                    _assignedFMODIndexes.Add(i);
                }
            }

            return assigned;
        }

        private bool IsDirectoryWritable(string dirPath, bool throwIfFails = false)
        {
            try
            {
                using (FileStream fs = File.Create(
                    Path.Combine(
                        dirPath,
                        Path.GetRandomFileName()
                    ),
                    1,
                    FileOptions.DeleteOnClose)
                )
                { }
                return true;
            }
            catch
            {
                if (throwIfFails)
                    throw;
                else
                    return false;
            }
        }

        private bool IsFileLocked(FileInfo file)
        {
            try
            {
                using (FileStream stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    stream.Close();
                }
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }

            //file is not locked
            return false;
        }

        public string ToggleModActive(DirectoryInfo modDir)
        {
            if (Directory.Exists(modDir.FullName))
            {
                DirectoryInfo[] folders = modDir.GetDirectories("*");
                foreach (DirectoryInfo folder in folders)
                {
                    if (!IsDirectoryWritable(folder.FullName))
                        return "\nImpossible to write to " + folder.Name + " folder.\nDo you have this subfolder open somewhere?";
                }

                if (IsDirectoryWritable(modDir.FullName))
                {
                    Directory.Move(modDir.FullName, modDir.FullName.Contains(".disabled") ? modDir.FullName.Replace(".disabled", "") : modDir.FullName + ".disabled");
                }
            }
            return modDir.Name;
        }
        public string ToggleModFileActive(FileInfo fileInfo)
        {
            if (File.Exists(fileInfo.FullName))
            {
                if(fileInfo.FullName.LastIndexOf(".disabled") <= fileInfo.FullName.LastIndexOf(@"\"))
                    File.Move(fileInfo.FullName, fileInfo.FullName + ".disabled");
                else
                    File.Move(fileInfo.FullName, fileInfo.FullName.Substring(0, fileInfo.FullName.LastIndexOf(".disabled")));
            }
            return fileInfo.FullName;
        }

        public void WriteToMetaFile(FileInfo metafile, BlackListedVanillaAssets blacklisted, bool remove = false)
        {
            dynamic modJson = LoadJson(metafile.FullName);

            if (!remove)
            {
                if (modJson["BlackListedVanillaAssets"][blacklisted.path] == null)
                    modJson["BlackListedVanillaAssets"][blacklisted.path] = new JObject();
                modJson["BlackListedVanillaAssets"][blacklisted.path][blacklisted.name] = JToken.FromObject(blacklisted);
            }
            else
                modJson["BlackListedVanillaAssets"][blacklisted.path].Remove(blacklisted.name);

            File.WriteAllText(metafile.FullName, modJson.ToString());
        }
    }
}
