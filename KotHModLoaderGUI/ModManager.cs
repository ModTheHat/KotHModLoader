using AssetsTools.NET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace KotHModLoaderGUI
{
    public class ModManager
    {
        private string _modDir = @"..\Mods(new structure)";
        private DirectoryInfo _dirInfoMod;
        private DirectoryInfo[] _folders;

        public DirectoryInfo DirInfoMod => _dirInfoMod;

        public struct ModFile
        {
            public FileInfo File;
            public string AssignedVanillaFile;
            public List<AssetTypeValueField> AssignedVanillaFiles;
            public List<AssetTypeValueField> VanillaCandidates;

            public ModFile(FileInfo file, string assigned = "")
            {
                File = file;
                AssignedVanillaFile = assigned;
                AssignedVanillaFiles = new List<AssetTypeValueField>();
                VanillaCandidates = new List<AssetTypeValueField>();
            }
        }

        public struct Mod
        {
            public string Name;
            public string Description;
            public string Version;
            public string Author;
            public DirectoryInfo ModDirectoryInfo;
            public FileInfo[] Files;
            public ModFile[] ModFiles;

            public Mod(DirectoryInfo dirInfo, string description = "", string version = "", string author = "unknown")
            {
                ModDirectoryInfo = dirInfo;
                Description = description;
                Version = version;
                Author = author;
                Name = dirInfo.Name;
                Files = GetFilesInfo(ModDirectoryInfo);
                ModFiles = GetModFilesInfo(ModDirectoryInfo);
            }
        }

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

        private Mod FindMod(string name)
        {
            foreach (Mod mod in _modsList) 
            {
                if (mod.Name == name)
                    return mod;
            }
            return new Mod();
        }

        //Go through mod folder and list all modded files
        public FileInfo[] GetModFiles(string modName)
        {
            FileInfo[] files = FindMod(modName).Files;
           
            return files;
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

        private static FileInfo[] GetFilesInfo(DirectoryInfo folder)
        {
            FileInfo[] files = folder.GetFiles("*.png", SearchOption.AllDirectories);
            files = files.Concat(folder.GetFiles("*.disabled", SearchOption.AllDirectories)).ToArray();

            return files;
        }
        private static ModFile[] GetModFilesInfo(DirectoryInfo folder)
        {
            FileInfo[] fileInfos = GetFilesInfo(folder);
            ModFile[] files = new ModFile[fileInfos.Length];

            for(int i = 0; i < fileInfos.Length;i++)
            {
                ModFile file = files[i];
                file.File = fileInfos[i];
                file.AssignedVanillaFiles = AssignVanillaFiles(fileInfos[i]);
                files[i] = file;
            }

            return files;
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
    }
}
