using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.Windows.Forms;

namespace KotHModLoaderGUI
{
    internal class ModManager
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

        public byte[] ConvertImageToBytesArray(FileInfo file)
        {
            //File bitmap = new File(file.FullName);
            //pictureBox1.Image = bitmap;
            //Color pixel5by10 = bitmap.GetPixel(5, 10);
            byte[] tst = File.ReadAllBytes(file.FullName);
            //ImageConversion.LoadImage(tst, file);
            //byte[] tst = new byte[1];
            //tst[0] = 1;
            //BitmapImage fgdf = new BitmapImage();
            return tst;
        }
        //public static byte[] converterDemo(Image x)
        //{
        //    AssetsTools.NET.Texture.TextureFile.Encode();
        //}
        public byte[] GetPixel_Example(FileInfo file)
        {

            // Create a Bitmap object from an image file.
            Bitmap myBitmap = new Bitmap(file.FullName);

            
            byte[] rgba = new byte[4 * myBitmap.Width * myBitmap.Height + 2];

            for (int i = 0; i < myBitmap.Width; i++)
            {
                for (int j = 0; j < myBitmap.Height; j++)
                {
                    // Get the color of a pixel within myBitmap.
                    Color pixelColor = myBitmap.GetPixel(i, j);

                    rgba[i * j * 4] = pixelColor.R;
                    rgba[i * j * 4 + 1] = pixelColor.G;
                    rgba[i * j * 4 + 2] = pixelColor.B;
                    rgba[i * j * 4 + 3] = pixelColor.A;
                }
            }

            rgba[rgba.Length - 2] = (byte)myBitmap.Width;
            rgba[rgba.Length - 1] = (byte)myBitmap.Height;

            return rgba;
        }
    }
}
