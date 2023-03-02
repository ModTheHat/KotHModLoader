using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.Windows.Forms;

namespace KotHModLoaderGUI
{
    public class ModManager
    {
        private DirectoryInfo _dirInfoMod = new DirectoryInfo(@"..\Mods(new structure)");
        private DirectoryInfo[] _folders;

        public DirectoryInfo DirInfoMod => _dirInfoMod;

        public struct Mod
        {
            public string Name;
            public string Description;
            public string Version;
            public string Author;
            public DirectoryInfo ModDirectoryInfo;
            public FileInfo[] Files;

            public Mod(DirectoryInfo dirInfo, string description = "", string version = "", string author = "unknown")
            {
                ModDirectoryInfo = dirInfo;
                Description = description;
                Version = version;
                Author = author;
                Name = dirInfo.Name;
                Files = GetFilesInfo(ModDirectoryInfo);
            }
        }

        private List<Mod> _modsList = new();
        public List<Mod> ModsList => _modsList;

        //Go through all mods in mods folder and add them to manager mods list
        public string[] BuildModsDatabase()
        {
            _modsList.Clear();
            _folders = _dirInfoMod.GetDirectories("*");
            string[] foldersNames = new string[_folders.Length];
            for (int i = 0; i < _folders.Length; i++)
            {
                //TODO: VALIDATE IF FOLDER CONTAINS A MOD
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

        private static FileInfo[] GetFilesInfo(DirectoryInfo folder)
        {
            FileInfo[] files = folder.GetFiles("*.png", SearchOption.AllDirectories);

            return files;
        }
        public string ToggleModActive(DirectoryInfo modDir)
        {
            if (Directory.Exists(modDir.FullName))
            {
                Directory.Move(modDir.FullName, modDir.FullName.Contains(".disabled") ? modDir.FullName.Replace(".disabled", "") : modDir.FullName + ".disabled");
            }
            return modDir.Name;
        }
    }
}
