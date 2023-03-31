using Fmod5Sharp;
using Fmod5Sharp.FmodTypes;
using NAudio.Vorbis;
using NAudio.Wave;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using static KotHModLoaderGUI.ModManager;

namespace KotHModLoaderGUI
{
    public class FMODManager : ModManager
    {
        private static string _rootPath = @"..";
        private static string _dataDirPath = _rootPath + @"\KingOfTheHat_Data";
        private static string _streamingDirPath = _dataDirPath + @"\StreamingAssets";
        private static string _bankFilePath = _streamingDirPath + @"\Master.bank";
        private static string _extractedPath = _rootPath + @"\Extracted Assets";
        private static string _soundsPath = _extractedPath + @"\Sounds";


        FmodSoundBank _fmodSounds;

        public FmodSoundBank FmodSoundBank => _fmodSounds;
        public void InitialisePaths()
        {
            LoadFMODManager();
        }

        private void LoadFMODManager()
        {
            var bytes = File.ReadAllBytes(_bankFilePath);

            var index = bytes.AsSpan().IndexOf(Encoding.ASCII.GetBytes("FSB5"));

            if (index > 0)
            {
                bytes = bytes.AsSpan(index).ToArray();
            }

            _fmodSounds = FsbLoader.LoadFsbFromByteArray(bytes);
        }

        public List<string> GetBankAssets()
        {
            List<string> assets = new List<string>();
            var i = 0;
            foreach (var bankSample in _fmodSounds.Samples)
            {
                assets.Add(bankSample.Name);
            }

            return assets;
        }
        public List<string> GetAssetInfo(int index)
        {
            List<string> infos = new List<string>();

            byte[] data = GetSampleData(index, out var sample);

            infos.Add(_fmodSounds.Header.AudioType.ToString() + "\n");

            infos.Add("Name: " + sample.Name + "\n");
            infos.Add("Sample Bytes: " + sample.SampleBytes.Length + "\n");
#if false
            foreach(var samplebytes in sample.SampleBytes)
            {
                infos.Add(samplebytes + ", ");
            }
#endif
            infos.Add("Channels: " + sample.Metadata.Channels + "\n");
            infos.Add("Is Stereo: " + sample.Metadata.IsStereo + "\n");
            infos.Add("Sample Count: " + sample.Metadata.SampleCount + "\n");
            infos.Add("Frequency: " + sample.Metadata.Frequency);

            PlayOgg(null, data);

            return infos;
        }

        //Play from path if data is null, play from data in any other case
        //Load into memory, doesn't open the file
        public void PlayOgg(string path = null, byte[] data = null)
        {
            VorbisWaveReader vorbis;
            if (data == null)
            {
                Stream stream = new MemoryStream(File.ReadAllBytes(path));
                vorbis = new VorbisWaveReader(stream);
            }
            else
            {
                Stream stream = new MemoryStream(data);
                vorbis = new VorbisWaveReader(stream);
            }

            var waveOut = new WaveOut();

            waveOut.Init(vorbis);
            waveOut.Play();
        }

        public List<string> GetOggFileInfos(string filename)
        {
            VorbisWaveReader vorbis = new VorbisWaveReader(filename);
            List<string> infos = new List<string>();

            infos.Add("TotalTime :" + vorbis.TotalTime.ToString());
            infos.Add("Vendor :" + vorbis.Vendor.ToString());
            infos.Add("WaveFormat :" + vorbis.WaveFormat.ToString());
            infos.Add("StreamCount :" + vorbis.StreamCount.ToString());
            infos.Add("NominalBitrate :" + vorbis.NominalBitrate.ToString());
            infos.Add("Length :" + vorbis.Length.ToString());
            infos.Add("BlockAlign :" + vorbis.BlockAlign.ToString());

            vorbis.Close();
            return infos;
        }

        public void ExtractAssets(List<int> indexes = null)
        {
            int assetsQty = indexes == null ? GetBankAssets().Count : indexes.Count;

            if (Directory.Exists(_extractedPath))
                if (!Directory.Exists(_soundsPath))
                    Directory.CreateDirectory(_soundsPath);
                else
                    Directory.CreateDirectory(_soundsPath);

            byte[] data;
            int index;
            for (int i = 0; i < assetsQty; i++)
            {
                index = indexes == null ? i : indexes[i];
                data = GetSampleData(index, out var sample);

                File.WriteAllBytes(_soundsPath + @"\" + sample.Name + @"-" + index + @".ogg", data);
            }
            Process.Start("explorer.exe", _soundsPath);
        }

        public byte[] GetSampleData(int index, out FmodSample sample)
        {
            sample = _fmodSounds.Samples[index];

            if (!sample.RebuildAsStandardFileFormat(out var data, out var extension))
            {
                Console.WriteLine($"Failed to extract sample {sample.Name}");
            }

            return data;
        }


        List<string> _alreadyModded;
        //List<AssetsReplacer> _replacers;
        public string BuildActiveModsFMODAudio(List<Mod> mods)
        {
            //_replacers = new List<AssetsReplacer>();
            _alreadyModded = new List<string>();
            //_alreadyModdedAsset = new List<int>();
            //_alreadyModdedWarning = "";

            ////byte array for new .resS file
            //_resSData = new byte[0];

            ////list of index to be replaced, byte array for changes from mod files

            foreach (var mod in mods)
            {
                FileInfo metaFile = mod.MetaFile;
                dynamic modJson = LoadJson(metaFile.FullName);
                List<string> blacklistedAssets = (List<string>)modJson["BlackListedVanillaAssets"].ToObject(typeof(List<string>));
                List<string> disabledAssets = (List<string>)modJson["DisabledModsOrFiles"].ToObject(typeof(List<string>));

                if (!disabledAssets.Contains(@"\" + mod.Name))
                    foreach (FileInfo file in mod.AudioFiles)
                    {
                        var assignedAsset = modJson["AssignedVanillaAssets"][file.FullName.Substring(file.FullName.IndexOf(mod.Name) + mod.Name.Length)];

                        if (!disabledAssets.Contains(@"\" + file.FullName.Substring(file.FullName.IndexOf(mod.Name))) && !_alreadyModded.Contains(file.Name))
                        {
                            byte[] bytes = GetSampleBytes(file);
                            //                SixLabors.ImageSharp.Image image;
                            //                using (image = SixLabors.ImageSharp.Image.Load(file.FullName))
                            //                {
                            //                    var width = image.Width;
                            //                    var height = image.Height;
                            //                }
                            //                if (assigned != null)
                            //                {
                            //                    if (ModVanillaTextureFromFileName(assigned.name.Value, bytes, image.Width, image.Height, strings, file))
                            //                        _alreadyModded.Add(file.Name);
                            //                }
                            if (!blacklistedAssets.Contains(file.FullName.Substring(file.FullName.IndexOf(mod.Name) + mod.Name.Length)) && assignedAsset == null)
                            {
                                if (ModVanillaFMODAudioFromFileName(file.Name, bytes, 0, 0, new List<string>(), file))
                                    _alreadyModded.Add(file.Name);
                            }
                        }
                    }
            }
            //if (_alreadyModdedWarning != "")
            //    MessageBox.Show(_alreadyModdedWarning);

            //File.WriteAllBytes(_resDir + @"\resources.assets.modded.resS", _resSData);

            //var writer = new AssetsFileWriter(_resDir + @"\" + _resNoFlavor);
            //_afileVanilla.Write(writer, 0, _replacers);
            //writer.Close();

            //return _replacers.Count + " vanilla textures replaced.";
            return null;
        }

        string _alreadyModdedWarning = "";
        List<int> _alreadyModdedAsset;
        byte[] _resSData;
        private bool ModVanillaFMODAudioFromFileName(string filename, byte[] dataImage, int width, int height, List<string> blacklisted, FileInfo modFile)
        {
            //string differentSizesWarning = "";
            //bool replaced = false;
            //int i = 0;
            //foreach (var goInfo in _afileVanilla.GetAssetsOfType(AssetClassID.Texture2D))
            //{
            //    var goBaseVanilla = _assetsManagerVanilla.GetBaseField(_afileInstVanilla, goInfo);
            //    var name = goBaseVanilla["m_Name"].AsString;

            //    if (filename.Contains(name))
            //    {
            //        if (_alreadyModdedAsset.Contains(i))
            //        {
            //            _alreadyModdedWarning += "More than one modded file tried to modify the asset: " + name + ".\n" +
            //                "Only one file will modify the asset.";
            //            i++;
            //            continue;
            //        }

            //        dynamic stream = goBaseVanilla["m_StreamData"];
            //        string path = stream["path"].AsString;
            //        int offset = stream["offset"].AsInt;
            //        int size = stream["size"].AsInt;
            //        byte[] resSBytes = null;
            //        byte[] bytes = null;
            //        if (size > 0)
            //        {
            //            byte[] resSFile = File.ReadAllBytes("..\\KingOfTheHat_Data\\" + path);
            //            resSBytes = new byte[size];
            //            Buffer.BlockCopy(resSFile, offset, resSBytes, 0, size);
            //            bytes = resSBytes;
            //        }
            //        else
            //            bytes = goBaseVanilla["image data"].AsByteArray;

            //        string str = Encoding.UTF8.GetString(bytes);
            //        bool contains = blacklisted.Contains(str);

            //        if (!contains)
            //        {
            //            if (goBaseVanilla["image data"].AsByteArray.Length > 0)
            //            {
            //                AssetTypeValue value = new AssetTypeValue(dataImage, false);

            //                if (goBaseVanilla["m_CompleteImageSize"].AsInt == dataImage.Length)
            //                {
            //                    goBaseVanilla["image data"].Value = value;

            //                    AssetsReplacerFromMemory replacer = new AssetsReplacerFromMemory(_afileVanilla, goInfo, goBaseVanilla);
            //                    _replacers.Add(replacer);
            //                    replaced = true;
            //                    _alreadyModdedAsset.Add(i);
            //                }
            //                else
            //                {
            //                    differentSizesWarning += "File: " + modFile.Name + ", trying to replace asset: " + goBaseVanilla["m_Name"].AsString + "\n";
            //                    goBaseVanilla["image data"].Value = value;
            //                    goBaseVanilla["m_Width"].Value = new AssetTypeValue(width);
            //                    goBaseVanilla["m_Height"].Value = new AssetTypeValue(height);
            //                    goBaseVanilla["m_CompleteSize"].Value = new AssetTypeValue(width * height * 4);

            //                    AssetsReplacerFromMemory replacer = new AssetsReplacerFromMemory(_afileVanilla, goInfo, goBaseVanilla);
            //                    _replacers.Add(replacer);
            //                    replaced = true;
            //                    _alreadyModdedAsset.Add(i);
            //                }
            //            }
            //            else
            //            {
            //                stream = goBaseVanilla["m_StreamData"];
            //                stream["path"].Value = new AssetTypeValue("resources.assets.modded.resS");
            //                stream["offset"].Value = new AssetTypeValue(_resSData.Length);
            //                stream["size"].Value = new AssetTypeValue(dataImage.Length / 4);
            //                goBaseVanilla["m_StreamData"].Value = new AssetTypeValue(AssetValueType.Array, stream);

            //                Array.Resize(ref _resSData, _resSData.Length + dataImage.Length);
            //                Buffer.BlockCopy(dataImage, 0, _resSData, _resSData.Length - dataImage.Length, dataImage.Length);

            //                AssetsReplacerFromMemory replacer = new AssetsReplacerFromMemory(_afileVanilla, goInfo, goBaseVanilla);
            //                _replacers.Add(replacer);
            //                replaced = true;
            //                _alreadyModdedAsset.Add(i);
            //            }
            //        }
            //    }
            //    i++;
            //}
            //if (differentSizesWarning != "")
            //    MessageBox.Show(differentSizesWarning + "Modifying asset with different image sizes will bring weird behaviour unless modded in the code.");

            //return replaced;
            return false;
        }

        private byte[] GetSampleBytes(FileInfo file)
        {
            byte[] data = File.ReadAllBytes(file.FullName);

            return data;
        }
    }
}
