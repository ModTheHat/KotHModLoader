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

    //Modded manager
    var assetsManagerModded = new AssetsManager();
    assetsManagerModded.LoadClassPackage("lz4.tpk");

    var afileInstModded = assetsManagerModded.LoadAssetsFile("resources.assets.KIARAJUMPOPENMOUTH", true);
    var afileModded = afileInstModded.file;

    assetsManagerModded.LoadClassDatabaseFromPackage(afileModded.Metadata.UnityVersion);
    //

    //Mods folder
    DirectoryInfo d = new DirectoryInfo(@"..\Mods");

    FileInfo[] Files = d.GetFiles("*");
    string str = "";

    AssetsManager[] assetsManagersModded = new AssetsManager[Files.Length];
    AssetsFileInstance[] afilesInstModded = new AssetsFileInstance[Files.Length];
    AssetsFile[] afilesModded = new AssetsFile[Files.Length];

    for (int a = 0; a < Files.Length; a++)
    {
        FileInfo file = Files[a];
        assetsManagersModded[a] = new AssetsManager();
        assetsManagersModded[a].LoadClassPackage("lz4.tpk");
        afilesInstModded[a] = assetsManagersModded[a].LoadAssetsFile(file.Name, true);
        if (afilesInstModded[a] != null)
        {
            afilesModded[a] = afilesInstModded[a].file;
            assetsManagersModded[a].LoadClassDatabaseFromPackage(afilesModded[a].Metadata.UnityVersion);
            str = str + ", " + file.Name;
        }
    }
    Console.WriteLine(str);

    Console.WriteLine("vanilla " + afile.GetAssetsOfType(AssetClassID.Texture2D).Count);
    Console.WriteLine("modded " + afileModded.GetAssetsOfType(AssetClassID.Texture2D).Count);

    var replacers = new List<AssetsReplacer>();
    int i = 0;
    foreach (var goInfo in afile.GetAssetsOfType(AssetClassID.Texture2D))
    {
        var goBaseVanilla = assetsManagerVanilla.GetBaseField(afileInst, goInfo);
        var goInfoModded = afileModded.GetAssetsOfType(AssetClassID.Texture2D)[i];
        var goBaseModded = assetsManagerModded.GetBaseField(afileInstModded, goInfoModded);
        var name = goBaseVanilla["m_Name"].AsString;

        if (goBaseModded["image data"].Value.ToString() != goBaseVanilla["image data"].Value.ToString())
        {
            Console.WriteLine(goBaseVanilla["m_Name"].AsString);
            goBaseVanilla["image data"].Value = goBaseModded["image data"].Value;
        }

        replacers.Add(new AssetsReplacerFromMemory(afile, goInfo, goBaseVanilla));

        i++;
    }

    var writer = new AssetsFileWriter("resources.assets" + ".mod");
    afile.Write(writer, 0, replacers);
    writer.Close();

    Console.WriteLine("Press a key to exit.");
    Console.ReadKey();
}

Load();
