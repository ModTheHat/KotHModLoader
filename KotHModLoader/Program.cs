using AssetsTools.NET;
using AssetsTools.NET.Extra;

string _resDir = "../KingOfTheHat_Data/";
string _resVanilla = "resources.assets.VANILLA";
string _resNoFlavor = "resources.assets";
string _classPackage = "lz4.tpk";
string _modsDir = "../Mods/";

void Load()
{
    //Vanilla manager
    var assetsManagerVanilla = new AssetsManager();
    assetsManagerVanilla.LoadClassPackage(_classPackage);

    if (!File.Exists(_resDir + _resVanilla))
        File.Copy(_resDir + _resNoFlavor, _resDir + _resVanilla);

    File.Delete(_resDir + _resNoFlavor);

    var afileInst = assetsManagerVanilla.LoadAssetsFile(_resDir + _resVanilla, true);
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
        if (!file.Name.Contains(".disabled"))
        {
            assetsManagersModded[a] = new AssetsManager();
            assetsManagersModded[a].LoadClassPackage(_classPackage);
            afilesInstModded[a] = assetsManagersModded[a].LoadAssetsFile(_modsDir + file.Name, true);
            if (assetsManagersModded[a] != null)
            {
                Console.WriteLine("Mod: " + file.Name);
                afilesModded[a] = afilesInstModded[a].file;
                assetsManagersModded[a].LoadClassDatabaseFromPackage(afilesModded[a].Metadata.UnityVersion);
            }
        }
    }

    //Build replacers for merging resources.assets
    List<string> alreadyModded = new List<string>();
    var replacers = new List<AssetsReplacer>();
    int i = 0;
    foreach (var goInfo in afile.GetAssetsOfType(AssetClassID.Texture2D))
    {
        var goBaseVanilla = assetsManagerVanilla.GetBaseField(afileInst, goInfo);
        var name = goBaseVanilla["m_Name"].AsString;

        for (int j = 0; j < assetsManagersModded.Length; j++)
        {
            if (afilesInstModded[j] != null)
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
        }
        i++;
    }

    var writer = new AssetsFileWriter(_resDir + _resNoFlavor);
    afile.Write(writer, 0, replacers);
    writer.Close();

    Console.WriteLine("Press a key to exit.");
    Console.ReadKey();
}

Load();
