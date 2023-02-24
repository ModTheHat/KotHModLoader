using AssetsTools.NET;
using AssetsTools.NET.Extra;
using System.IO;

void Load()
{
    //Vanilla manager
    var assetsManagerVanilla = new AssetsManager();
    assetsManagerVanilla.LoadClassPackage("lz4.tpk");

    var afileInst = assetsManagerVanilla.LoadAssetsFile("../KingOfTheHat_Data/resources.assets", true);
    var afile = afileInst.file;

    assetsManagerVanilla.LoadClassDatabaseFromPackage(afile.Metadata.UnityVersion);
    //

    //Mods folder managers
    DirectoryInfo d = new DirectoryInfo(@"..\Mods");

    FileInfo[] Files = d.GetFiles("*");

    AssetsManager[] assetsManagersModded = new AssetsManager[Files.Length];
    AssetsFileInstance[] afilesInstModded = new AssetsFileInstance[Files.Length];
    AssetsFile[] afilesModded = new AssetsFile[Files.Length];
    //

    //Build managers for resources.assets mods
    for (int a = 0; a < Files.Length; a++)
    {
        FileInfo file = Files[a];
        Console.WriteLine(file.Name);
        assetsManagersModded[a] = new AssetsManager();
        assetsManagersModded[a].LoadClassPackage("lz4.tpk");
        afilesInstModded[a] = assetsManagersModded[a].LoadAssetsFile("../Mods/" + file.Name, true);
        if (assetsManagersModded[a] != null)
        {
            afilesModded[a] = afilesInstModded[a].file;
            assetsManagersModded[a].LoadClassDatabaseFromPackage(afilesModded[a].Metadata.UnityVersion);
        }
    }

    List<string> alreadyModded = new List<string>();
    var replacers = new List<AssetsReplacer>();
    int i = 0;
    foreach (var goInfo in afile.GetAssetsOfType(AssetClassID.Texture2D))
    {
        var goBaseVanilla = assetsManagerVanilla.GetBaseField(afileInst, goInfo);
        var name = goBaseVanilla["m_Name"].AsString;

        for (int j = 0; j < assetsManagersModded.Length; j++)
        {
            var goInfoModded = afilesModded[j].GetAssetsOfType(AssetClassID.Texture2D)[i];
            var goBaseModded = assetsManagersModded[j].GetBaseField(afilesInstModded[j], goInfoModded);
            if (goBaseModded["image data"].Value.ToString() != goBaseVanilla["image data"].Value.ToString() && !alreadyModded.Contains(goBaseVanilla["m_Name"].AsString))
            {
                Console.WriteLine(goBaseVanilla["m_Name"].AsString + " has changed.");

                goBaseVanilla["image data"].Value = goBaseModded["image data"].Value;

                replacers.Add(new AssetsReplacerFromMemory(afile, goInfo, goBaseVanilla));
                alreadyModded.Add(goBaseVanilla["m_Name"].AsString);
            }
        }
        i++;
    }

    var writer = new AssetsFileWriter("resources.assets" + ".mod");
    afile.Write(writer, 0, replacers);
    writer.Close();

    Console.WriteLine("Press a key to exit.");
    Console.ReadKey();
}

Load();
