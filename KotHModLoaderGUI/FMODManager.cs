using Fmod5Sharp;
using Fmod5Sharp.FmodTypes;
using NAudio.Vorbis;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices.ActiveDirectory;
using System.IO;
using System.Linq;
using System.Text;
using static Fmod5Sharp.Util.Extensions;
using Fmod5Sharp.ChunkData;

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
                                if (ModVanillaFMODAudioFromFileName(file.Name, bytes, 0, 0, blacklistedAssets, file))
                                    _alreadyModded.Add(file.Name);
                            }
                        }
                    }
            }

            MemoryStream streamWrite = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(streamWrite);
            //File.WriteAllBytes("Master.modded.bank", vanillaBytes);

            FileInfo oggfile = new FileInfo(@"..\Mods\Forest-Take012.ogg");

            //byte[] dataModFile = GetSampleBytes(oggfile);

            //VANILLA MASTER.BANK FULL FILE DATA
            byte[] vanillaMasterBytes = File.ReadAllBytes(_bankFilePath);

            //VANILLA MASTER.BANK FILE HEADER
            int headerIndex = vanillaMasterBytes.AsSpan().IndexOf(Encoding.ASCII.GetBytes("FSB5"));
            byte[] headerBytes = new byte[headerIndex];
            Buffer.BlockCopy(vanillaMasterBytes, 0, headerBytes, 0, headerIndex);

            //VANILLA MASTER.BANK METADATA, SAMPLES AND STREAMING DATA
            byte[] vanillaMasterNoHeaderBytes = null;
            if (headerIndex > 0)
            {
                vanillaMasterNoHeaderBytes = vanillaMasterBytes.AsSpan(headerIndex).ToArray();
            }

            //VANILLA MASTER.BANK METADATA, SAMPLES AND STREAMING FMODSOUNDBANK OBJECT
            FmodSoundBank masterBank = FsbLoader.LoadFsbFromByteArray(vanillaMasterNoHeaderBytes);

            //VANILLA MASTER.BANK STREAM AND BINARYREADER
            using MemoryStream stream = new(vanillaMasterNoHeaderBytes);
            using BinaryReader reader = new(stream);

            //VANILLA MASTER.BANK FMODAUDIOHEADER METADATA PART
            FmodAudioHeader header = new(reader);

            //READ VANILLA MASTER.BANK BYTES FOR METADATA
            reader.BaseStream.Position = 0;

            var version = reader.ReadUInt32(); //0x04
            var numSamples = reader.ReadUInt32(); //0x08
            var sizeOfSampleHeaders = reader.ReadUInt32();
            var sizeOfNameTable = reader.ReadUInt32();
            var sizeOfData = reader.ReadUInt32(); //0x14
            var audioType = (FmodAudioType)reader.ReadUInt32(); //0x18

            //WRITE NEW MASTER.BANK METADATA
            writer.Write(version);
            writer.Write(numSamples);
            writer.Write(sizeOfSampleHeaders);
            writer.Write(sizeOfNameTable);
            writer.Write(sizeOfData);
            writer.Write((UInt32)audioType);

            writer.Write(reader.ReadUInt32()); //Reader Skip 0x1C which is always 0

            if (version == 0)
            {
                var sizeOfThisHeader = 0x40;
                writer.Write(reader.ReadUInt32()); //Version 0 has an extra field at 0x20 before flags
            }
            else
            {
                var sizeOfThisHeader = 0x3C;
            }

            writer.Write(reader.ReadUInt32()); //Skip 0x20 (flags)

            //128-bit hash
            var hashLower = reader.ReadUInt64(); //0x24
            var hashUpper = reader.ReadUInt64(); //0x30

            writer.Write(hashLower);
            writer.Write(hashUpper);

            writer.Write(reader.ReadUInt64()); //Skip unknown value at 0x34

           // File.WriteAllText(writer);
            //VANILLA MASTER.BANK END OF METADATA READ BYTES
            
            //VANILLA MASTER.BANK SAMPLES AND STREAMING START
            var sampleHeadersStart = reader.BaseStream.Position;

            List<FmodSampleMetadata> Samples = new();
            object ChunkReadingLock = new();

            for (var i = 0; i < numSamples; i++)
            {
                var sampleMetadata = reader.ReadEndian<FmodSampleMetadata>();

                if (!sampleMetadata.HasAnyChunks)
                {
                    Samples.Add(sampleMetadata);
                    continue;
                }

                lock (ChunkReadingLock)
                {
                    List<FmodSampleChunk> chunks = new();
                    FmodSampleChunk.CurrentSample = sampleMetadata;

                    FmodSampleChunk nextChunk;
                    do
                    {
                        nextChunk = reader.ReadEndian<FmodSampleChunk>();
                        chunks.Add(nextChunk);
                    } while (nextChunk.MoreChunks);

                    FmodSampleChunk.CurrentSample = null;

                    if (chunks.FirstOrDefault(c => c.ChunkType == FmodSampleChunkType.FREQUENCY) is { ChunkData: FrequencyChunkData fcd })
                    {
                        sampleMetadata.FrequencyId = fcd.ActualFrequencyId;
                    }

                    sampleMetadata.Chunks = chunks;

                    Samples.Add(sampleMetadata);
                }
            }
            //VANILLA MASTER.BANK SAMPLES, STREAMING AND FILE END

            return null;
        }

        string _alreadyModdedWarning = "";
        List<int> _alreadyModdedAsset;
        byte[] _resSData;
        private bool ModVanillaFMODAudioFromFileName(string filename, byte[] dataAudio, int width, int height, List<string> blacklisted, FileInfo modFile)
        {
            bool replaced = false;
            _alreadyModdedAsset = new List<int>();


            for (int i = 0; i < _fmodSounds.Samples.Count; i++)
            {
                FmodSample sample = _fmodSounds.Samples[i];
                string name = sample.Name;

                if (filename.Contains(name))
                {
                    if (_alreadyModdedAsset.Contains(i))
                    {
                        _alreadyModdedWarning += "More than one modded file tried to modify the asset: " + name + ".\n" +
                            "Only one file will modify the asset.";
                        continue;
                    }

                    if (!sample.RebuildAsStandardFileFormat(out var bytes, out var extension))
                    {
                        Console.WriteLine($"Failed to extract sample {sample.Name}");
                    }

                    string str = Encoding.UTF8.GetString(bytes);
                    bool contains = blacklisted.Contains(str);

                    if (!contains)
                    {
                        FmodSampleMetadata header = new FmodSampleMetadata();

                        //rebuild new header
                        //dataoffset
                        //header.IsStereo = ;
                        //header.Channels = ;
                        //header.Frequency = ;
                        //header.SampleCount = ;

                        replaced = true;
                        _alreadyModdedAsset.Add(i);
                    }
                }
            }
            //if (differentSizesWarning != "")
            //    MessageBox.Show(differentSizesWarning + "Modifying asset with different image sizes will bring weird behaviour unless modded in the code.");

            return replaced;
        }

        private byte[] GetSampleBytes(FileInfo file)
        {
            //string longPathSpecifier = @"\\?" + file.FullName;
            byte[] data = File.ReadAllBytes(file.FullName);

            return data;
        }
    }
}
