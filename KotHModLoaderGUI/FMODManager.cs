using Fmod5Sharp;
using Fmod5Sharp.ChunkData;
using Fmod5Sharp.FmodTypes;
using NAudio.Vorbis;
using NAudio.Wave;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using static Fmod5Sharp.Util.Extensions;

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

        private JObject _indexesByNames = new();

        private FmodSoundBank _fmodSounds;

        public FmodSoundBank FmodSoundBank => _fmodSounds;

        private int[] _sampleHeadersIndexes;

        public void InitialisePaths()
        {
            LoadFMODManager();
        }

        private void LoadFMODManager()
        {
            var bytes = File.ReadAllBytes(_bankFilePath);
            var index = bytes.AsSpan().IndexOf(System.Text.Encoding.ASCII.GetBytes("FSB5"));
            var bankBytes = index > 0 ? bytes.AsSpan(index).ToArray() : bytes;
            _fmodSounds = FsbLoader.LoadFsbFromByteArray(bankBytes);
            byte[] masterSampleHeadersBytes = new byte[_fmodSounds.Header.SampleHeadersSize];
            Buffer.BlockCopy(bytes, index + 60, masterSampleHeadersBytes, 0, (int)_fmodSounds.Header.SampleHeadersSize);



            _sampleHeadersIndexes = new int[_fmodSounds.Header.NumSamples];
            LoadSampleHeadersIndexes(masterSampleHeadersBytes);

            for (int i = 0; i < _fmodSounds.Samples.Count; i++) 
            {
                FmodSample sample = _fmodSounds.Samples[i];
                if (_indexesByNames[sample.Name] != null)
                    _indexesByNames[sample.Name].AddAfterSelf(i);
                else
                    _indexesByNames[sample.Name] = new JArray(i);
            }
        }

        public List<string> GetBankAssets()
        {
            List<string> assets = new List<string>();

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

            if (!Directory.Exists(_extractedPath))
                Directory.CreateDirectory(_extractedPath);
            if (!Directory.Exists(_soundsPath))
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

        public byte[] GetSampleData(int index, FmodSample sample)
        {
            if (!sample.RebuildAsStandardFileFormat(out var data, out var extension))
            {
                Console.WriteLine($"Failed to extract sample {sample.Name}");
            }

            return data;
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

        private void ReadModdedMasterFile(out byte[] moddedFileByte, out FmodSoundBank moddedSoundBank)
        {
            moddedFileByte = null;
            moddedSoundBank = null;
            if (File.Exists("Master.modded.bank"))
            {
                moddedFileByte = File.ReadAllBytes("Master.modded.bank");
                int headerIndexTest = moddedFileByte.AsSpan().IndexOf(System.Text.Encoding.ASCII.GetBytes("FSB5"));
                byte[] headerBytesTest = new byte[headerIndexTest];
                Buffer.BlockCopy(moddedFileByte, 0, headerBytesTest, 0, headerIndexTest);
                byte[] noHeaderBytes = null;
                if (headerIndexTest > 0)
                {
                    noHeaderBytes = moddedFileByte.AsSpan(headerIndexTest).ToArray();
                }
                moddedSoundBank = FsbLoader.LoadFsbFromByteArray(noHeaderBytes);
            }
        }

        private byte[] ReadModdedOGGFile()
        {
            FileInfo oggfile = new FileInfo(@"..\Mods\New folder\KotH_UI_LandingScreen_PressStart_v2-01.ogg");

            byte[] dataModFile = GetSampleBytes(oggfile);

            using MemoryStream stream = new MemoryStream(dataModFile);
            using BinaryReader reader = new BinaryReader(stream);

            reader.ReadBytes(27);
            int length = reader.ReadByte();
            reader.ReadBytes(length + 26);
            int length2 = reader.ReadByte();
            int lengths = 0;
            for(int i = 0; i < length2; i++)
            {
                lengths += reader.ReadByte();
            }
            reader.ReadBytes(lengths);

            List<byte> newStreamingData = new List<byte>();
            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                reader.ReadBytes(26);
                int chunkLength = reader.ReadByte();
                List<int> lengthsHeader = new List<int>();
                for (int i = 0; i < chunkLength; i++)
                {
                    lengthsHeader.Add(reader.ReadByte());
                }
                for (int i = 0; i < chunkLength; i++)
                {
                    newStreamingData.Add((byte)lengthsHeader[i]);
                    newStreamingData.Add(0);
                    newStreamingData.AddRange(reader.ReadBytes(lengthsHeader[i]).ToArray());
                }
            }

            return newStreamingData.ToArray();
        }

        private void ReadMasterFileHeaders(string file, out FmodSoundBank masterBank, out byte[] masterBytes)
        {
            //VANILLA MASTER.BANK FULL FILE DATA
            masterBytes = File.ReadAllBytes(file);

            //VANILLA MASTER.BANK FILE HEADER
            int headerIndex = masterBytes.AsSpan().IndexOf(System.Text.Encoding.ASCII.GetBytes("FSB5"));
            byte[] headerBytes = new byte[headerIndex + 60];
            Buffer.BlockCopy(masterBytes, 0, headerBytes, 0, headerIndex + 60);
            //File.WriteAllBytes("Master.modded.bank", headerBytes);

            //FMODBankTool MASTER.BANK METADATA, SAMPLES AND STREAMING DATA
            byte[] masterNoHeaderBytes = null;
            if (headerIndex > -1)
            {
                masterNoHeaderBytes = masterBytes.AsSpan(headerIndex).ToArray();
            }

            //FMODBankTool MASTER.BANK METADATA, SAMPLES AND STREAMING FMODSOUNDBANK OBJECT
            masterBank = FsbLoader.LoadFsbFromByteArray(masterNoHeaderBytes);

            //FMODBankTool MASTER.BANK STREAM AND BINARYREADER
            using MemoryStream stream = new(masterNoHeaderBytes);
            using BinaryReader reader = new(stream);

            //FMODBankTool MASTER.BANK FMODAUDIOHEADER METADATA PART
            FmodAudioHeader header = new(reader);

            //READ FMODBankTool MASTER.BANK BYTES FOR METADATA
            reader.BaseStream.Position = 4;

            var version = reader.ReadUInt32(); //0x04
            var numSamples = reader.ReadUInt32(); //0x08
            var sizeOfSampleHeaders = reader.ReadUInt32();
            var sizeOfNameTable = reader.ReadUInt32();
            var sizeOfData = reader.ReadUInt32(); //0x14
            var audioType = (FmodAudioType)reader.ReadUInt32(); //0x18

            reader.ReadUInt32(); //Reader Skip 0x1C which is always 0

            if (version == 0)
            {
                var sizeOfThisHeader = 0x40;
                reader.ReadUInt32(); //Version 0 has an extra field at 0x20 before flags
            }
            else
            {
                var sizeOfThisHeader = 0x3C;
            }

            reader.ReadUInt32(); //Skip 0x20 (flags)

            //128-bit hash
            var hashLower = reader.ReadUInt64(); //0x24
            var hashUpper = reader.ReadUInt64(); //0x30

            reader.ReadUInt64(); //Skip unknown value at 0x34

            //FMODBankTool MASTER.BANK END OF METADATA READ BYTES

            //FMODBankTool MASTER.BANK SAMPLES AND STREAMING START
            var sampleHeadersStart = reader.BaseStream.Position;

            List<FmodSampleMetadata> Samples = new();
            object ChunkReadingLock = new();

            int headersSize = (int)masterBank.Header.SampleHeadersSize;

            for (var i = 0; i < masterBank.Samples.Count; i++)
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
        }

        private long GetSampleHeaderIndex(byte[] headerBytes, int sampleIndex)
        {
            int index = 0;
            long address = -1;

            MemoryStream stream = new MemoryStream(headerBytes);
            BinaryReader reader = new BinaryReader(stream);

            byte[] header = null;
            string headerString = "";
            string sampleCount = null;
            int sampleC = -1;
            string dataOffset = null;
            int dataO = -1;
            string powpow = null;
            string freqId = null;
            int freqI = -1;
            string hasChunk = null;

            byte[] chunk = null;
            string chunkString = "";
            string chunkType = null;
            int chunkT = -1;
            string chunkSize = null;
            int chunkS = -1;
            string moreChunk = null;
            int moreC = -1;
            byte[] chunkData = null;

            while (reader.BaseStream.Position < stream.Length)
            {
                if (index == sampleIndex)
                    address = reader.BaseStream.Position;

                header = reader.ReadBytes(8);
                headerString = "";
                for (int i = header.Length - 1; i >= 0; i--)
                {
                    headerString += Convert.ToString(header[i], 2).PadLeft(8, '0');
                }
                sampleCount = headerString[0..30];
                sampleC = Convert.ToInt32(sampleCount, 2);
                dataOffset = headerString[30..57];
                dataO = Convert.ToInt32(dataOffset, 2);
                powpow = headerString[57..59];
                freqId = headerString[59..63];
                freqI = Convert.ToInt32(freqId, 2);
                hasChunk = headerString[63].ToString();

                bool stillHasChunk = hasChunk == "1" ? true : false;
                while (stillHasChunk)
                {
                    chunk = reader.ReadBytes(4);
                    chunkString = "";
                    for (int i = chunk.Length - 1; i >= 0; i--)
                    {
                        chunkString += Convert.ToString(chunk[i], 2).PadLeft(8, '0');
                    }
                    chunkType = chunkString[0..7];
                    chunkT = Convert.ToInt32(chunkType, 2);
                    chunkSize = chunkString[7..31];
                    chunkS = Convert.ToInt32(chunkSize, 2);
                    moreChunk = chunkString[31].ToString();

                    chunkData = reader.ReadBytes(chunkS);

                    stillHasChunk = moreChunk == "1" ? true : false;
                }

                index++;
            }

            return address;
        }

        private void LoadSampleHeadersIndexes(byte[] headerBytes)
        {
            int index = 0;
            long address = -1;

            MemoryStream stream = new MemoryStream(headerBytes);
            BinaryReader reader = new BinaryReader(stream);

            byte[] header = null;
            string headerString = "";
            string sampleCount = null;
            int sampleC = -1;
            string dataOffset = null;
            int dataO = -1;
            string powpow = null;
            string freqId = null;
            int freqI = -1;
            string hasChunk = null;

            byte[] chunk = null;
            string chunkString = "";
            string chunkType = null;
            int chunkT = -1;
            string chunkSize = null;
            int chunkS = -1;
            string moreChunk = null;
            int moreC = -1;
            byte[] chunkData = null;

            while (reader.BaseStream.Position < stream.Length)
            {
                _sampleHeadersIndexes[index] = (int)reader.BaseStream.Position;

                header = reader.ReadBytes(8);
                headerString = "";
                for (int i = header.Length - 1; i >= 0; i--)
                {
                    headerString += Convert.ToString(header[i], 2).PadLeft(8, '0');
                }
                sampleCount = headerString[0..30];
                sampleC = Convert.ToInt32(sampleCount, 2);
                dataOffset = headerString[30..57];
                dataO = Convert.ToInt32(dataOffset, 2);
                powpow = headerString[57..59];
                freqId = headerString[59..63];
                freqI = Convert.ToInt32(freqId, 2);
                hasChunk = headerString[63].ToString();

                bool stillHasChunk = hasChunk == "1" ? true : false;
                while (stillHasChunk)
                {
                    chunk = reader.ReadBytes(4);
                    chunkString = "";
                    for (int i = chunk.Length - 1; i >= 0; i--)
                    {
                        chunkString += Convert.ToString(chunk[i], 2).PadLeft(8, '0');
                    }
                    chunkType = chunkString[0..7];
                    chunkT = Convert.ToInt32(chunkType, 2);
                    chunkSize = chunkString[7..31];
                    chunkS = Convert.ToInt32(chunkSize, 2);
                    moreChunk = chunkString[31].ToString();

                    chunkData = reader.ReadBytes(chunkS);

                    stillHasChunk = moreChunk == "1" ? true : false;
                }

                index++;
            }
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

            ReadModdedMasterFile(out var moddedFileByte, out var moddedSoundBank);

            //ReadFBTMasterFileHeaders(out var fbtMasterBank, out var fbtMasterBytes);
            ReadMasterFileHeaders("Master.FMODBANKTOOL.bank", out var fbtMasterBank, out var fbtMasterBytes);
            int fbtMasterHeaderIndex = fbtMasterBytes.AsSpan().IndexOf(System.Text.Encoding.ASCII.GetBytes("FSB5"));
            byte[] fbtHeaderBytes = new byte[fbtMasterHeaderIndex];
            Buffer.BlockCopy(fbtMasterBytes, 0, fbtHeaderBytes, 0, fbtMasterHeaderIndex);
            byte[] fbtInfoBytes = new byte[60];
            Buffer.BlockCopy(fbtMasterBytes, fbtMasterHeaderIndex, fbtInfoBytes, 0, 60);
            byte[] fbtSampleHeadersBytes = new byte[fbtMasterBank.Header.SampleHeadersSize];
            Buffer.BlockCopy(fbtMasterBytes, fbtMasterHeaderIndex + 60, fbtSampleHeadersBytes, 0, (int)fbtMasterBank.Header.SampleHeadersSize);

            //ReadVanillaMasterFileHeaders(out var masterBank, out var vanillaMasterBytes);
            ReadMasterFileHeaders(_bankFilePath, out var masterBank, out var vanillaMasterBytes);
            int masterHeaderIndex = vanillaMasterBytes.AsSpan().IndexOf(System.Text.Encoding.ASCII.GetBytes("FSB5"));
            byte[] masterHeaderBytes = new byte[masterHeaderIndex];
            Buffer.BlockCopy(vanillaMasterBytes, 0, masterHeaderBytes, 0, masterHeaderIndex);
            byte[] masterInfoBytes = new byte[60];
            Buffer.BlockCopy(vanillaMasterBytes, masterHeaderIndex, masterInfoBytes, 0, 60);
            byte[] masterSampleHeadersBytes = new byte[masterBank.Header.SampleHeadersSize];
            Buffer.BlockCopy(vanillaMasterBytes, masterHeaderIndex + 60, masterSampleHeadersBytes, 0, (int)masterBank.Header.SampleHeadersSize);

            ReadMasterFileHeaders("D:\\FSBank5.Net 202\\FSBankNet\\bin\\Debug\\net5.0\\test.fsb5", out var fsbBank, out var fsbBytes);
            
            ReadMasterFileHeaders("D:\\smallfile.fsb", out var smallFileBank, out var smallFileBytes);

            //NEW BIG HEADER
            byte[] newBigHeader = new byte[masterHeaderIndex];
            Buffer.BlockCopy(vanillaMasterBytes, 0, newBigHeader, 0, masterHeaderIndex);

            //NEW FMOD SOUND BANK HEADER
            byte[] newBankHeader = new byte[60];
            Buffer.BlockCopy(vanillaMasterBytes, masterHeaderIndex, newBigHeader, 0, 60);

            //NEW SAMPLE HEADERS
            List<byte> newSampleHeader = new List<byte>();

            //NEW NAME TABLES

            //NEW STREAMING DATA
            List<byte> newStreamingData = new List<byte>();


            //NEW MASTER.BANK INITIALISATION
            byte[] newMasterBytes = new byte[vanillaMasterBytes.Length];
            Buffer.BlockCopy(vanillaMasterBytes, 0, newMasterBytes, 0, masterHeaderIndex + (int)masterBank.Header.ThisHeaderSize);

            //ADD SAMPLE HEADERS TO NEW MASTER
            Buffer.BlockCopy(vanillaMasterBytes, masterHeaderIndex + 60, newMasterBytes, masterHeaderIndex + 60, (int)masterBank.Header.ThisHeaderSize);
            int lastPos = masterHeaderIndex + 60 + (int)masterBank.Header.ThisHeaderSize;

            foreach (var mod in mods)
            {
                FileInfo metaFile = mod.MetaFile;
                dynamic modJson = LoadJson(metaFile.FullName);
                List<string> blacklistedAssets = (List<string>)modJson["BlackListedVanillaAssets"].ToObject(typeof(List<string>));
                List<string> disabledAssets = (List<string>)modJson["DisabledModsOrFiles"].ToObject(typeof(List<string>));

                //if (!disabledAssets.Contains(@"\" + mod.Name))
                if (disabledAssets.Contains(@"\" + mod.Name)) continue;

                foreach (FileInfo file in mod.AudioFiles)
                {
                    var assignedAsset = modJson["AssignedVanillaAssets"][file.FullName.Substring(file.FullName.IndexOf(mod.Name) + mod.Name.Length)];

                    //if (!disabledAssets.Contains(@"\" + file.FullName.Substring(file.FullName.IndexOf(mod.Name))) && !_alreadyModded.Contains(file.Name))
                    if (disabledAssets.Contains(@"\" + file.FullName.Substring(file.FullName.IndexOf(mod.Name))) || _alreadyModded.Contains(file.Name)) continue;
                                        
                    byte[] bytes = GetSampleBytes(file);

                    //                if (assigned != null)
                    //                {
                    //                    if (ModVanillaTextureFromFileName(assigned.name.Value, bytes, image.Width, image.Height, strings, file))
                    //                        _alreadyModded.Add(file.Name);
                    //                }
                    //if (!blacklistedAssets.Contains(file.FullName.Substring(file.FullName.IndexOf(mod.Name) + mod.Name.Length)) && assignedAsset == null)
                    if (blacklistedAssets.Contains(file.FullName.Substring(file.FullName.IndexOf(mod.Name) + mod.Name.Length)) || assignedAsset != null) continue;
                    
                    if (ModVanillaFMODAudioFromFileName(file.Name, bytes, 0, 0, blacklistedAssets, file))
                        _alreadyModded.Add(file.Name);
                }
            }
            //VANILLA MASTER.BANK SAMPLES, STREAMING AND FILE END

            //File.WriteAllBytes("Master.modded.bank", newMasterBytes);

            return null;
        }

        string _alreadyModdedWarning = "";
        List<int> _alreadyModdedAsset;
        byte[] _resSData;
        private bool ModVanillaFMODAudioFromFileName(string filename, byte[] dataAudio, int width, int height, List<string> blacklisted, FileInfo modFile)
        {
            bool replaced = false;
            _alreadyModdedAsset = new List<int>();

            JArray indexes = (JArray)_indexesByNames[filename.Replace(".ogg", "")];

            ReadMasterFileHeaders(_bankFilePath, out var masterBank, out var vanillaMasterBytes);
            //int masterHeaderIndex = vanillaMasterBytes.AsSpan().IndexOf(System.Text.Encoding.ASCII.GetBytes("FSB5"));
            int headerIndex = vanillaMasterBytes.AsSpan().IndexOf(System.Text.Encoding.ASCII.GetBytes("FSB5"));
            //byte[] masterHeaderBytes = new byte[headerIndex];
            //Buffer.BlockCopy(vanillaMasterBytes, 0, masterHeaderBytes, 0, headerIndex);
            //byte[] masterInfoBytes = new byte[60];
            //Buffer.BlockCopy(vanillaMasterBytes, headerIndex, masterInfoBytes, 0, 60);
            byte[] masterSampleHeadersBytes = new byte[masterBank.Header.SampleHeadersSize];
            Buffer.BlockCopy(vanillaMasterBytes, headerIndex + 60, masterSampleHeadersBytes, 0, (int)masterBank.Header.SampleHeadersSize);

            int streamDataIndex = vanillaMasterBytes.Length - (int)_fmodSounds.Header.DataSize;

            if (indexes == null) return replaced;

            foreach (int index in indexes)
            {
                FmodSample sample = _fmodSounds.Samples[index];
                string name = sample.Name;

                //if (filename.Contains(name))
                if (!filename.Contains(name)) continue;

                if (_alreadyModdedAsset.Contains(index))
                {
                    _alreadyModdedWarning += "More than one modded file tried to modify the asset: " + name + ".\n" +
                        "Only one file will modify the asset.";
                    continue;
                }

                if (!sample.RebuildAsStandardFileFormat(out var bytes, out var extension))
                    Console.WriteLine($"Failed to extract sample {sample.Name}");

                string str = System.Text.Encoding.UTF8.GetString(bytes);
                bool contains = blacklisted.Contains(str);

                if (contains) continue;

                FmodSampleMetadata header = new FmodSampleMetadata();

                //rebuild new header
                //dataoffset    
                //header.IsStereo = ;
                //header.Channels = ;
                //header.Frequency = ;
                //header.SampleCount = ;

                byte[] byteSample = GetSampleData(index, sample);

                byte[] dataModFile = ReadModdedOGGFile();
                ////ADD SAMPLE STREAMING DATA TO NEW MASTER
                int sampleLength = index + 1 < _fmodSounds.Samples.Count ?
                    (int)_fmodSounds.Samples[index + 1].Metadata.DataOffset - (int)_fmodSounds.Samples[index].Metadata.DataOffset :
                    (int)_fmodSounds.Header.DataSize - (int)_fmodSounds.Samples[index].Metadata.DataOffset;
                //if (sampleName)
                //{
                //    byte[] zeros = new byte[sampleLength];
                byte[] theseBytes = new byte[sampleLength];
                byte[] originalBytes = new byte[sampleLength];
                Buffer.BlockCopy(dataModFile, 0, theseBytes, 0, dataModFile.Length > sampleLength ? sampleLength : dataModFile.Length);
                int headerLength = headerIndex - (int)_fmodSounds.Header.ThisHeaderSize - (int)_fmodSounds.Header.SampleHeadersSize;
                //int headerLength = headerIndex - 60 - headersSize;

                //Buffer.BlockCopy(vanillaMasterBytes, (int)sample.Metadata.DataOffset + streamDataIndex, originalBytes, 0, sampleLength);
                //vanillaMasterBytes[headerIndex + 44] = 0;   //hashupper change to 0, still play fine
                //vanillaMasterBytes[headerIndex + 45] = 0;
                //vanillaMasterBytes[headerIndex + 46] = 0;
                //vanillaMasterBytes[headerIndex + 47] = 0;
                //vanillaMasterBytes[headerIndex + 48] = 0;
                //vanillaMasterBytes[headerIndex + 49] = 0;
                //vanillaMasterBytes[headerIndex + 50] = 0;
                //vanillaMasterBytes[headerIndex + 51] = 0;

                ////vanillaMasterBytes[headerIndex + 8] = 110;  //changing sample number definitely changes something
                ////vanillaMasterBytes[headerIndex + 9] = 5;

                //vanillaMasterBytes[headerIndex + 52] = 0;   //unknown value change to 0, still play fine
                //vanillaMasterBytes[headerIndex + 53] = 0;
                //vanillaMasterBytes[headerIndex + 54] = 0;
                //vanillaMasterBytes[headerIndex + 55] = 0;
                //vanillaMasterBytes[headerIndex + 56] = 0;
                //vanillaMasterBytes[headerIndex + 57] = 0;
                //vanillaMasterBytes[headerIndex + 58] = 0;
                //vanillaMasterBytes[headerIndex + 59] = 0;


                //ReadMasterFileHeaders("D:\\FSBank5.Net 202\\FSBankNet\\bin\\Debug\\net5.0\\test.fsb5", out var fsbBank, out var fsbBytes);
                //theseBytes = fsbBank.Samples[0].SampleBytes;

                //Buffer.BlockCopy(theseBytes, 0, vanillaMasterBytes, (int)sample.Metadata.DataOffset + streamDataIndex, sampleLength);

                //Buffer.BlockCopy(theseBytes, 0, vanillaMasterBytes, (int)sample.Metadata.DataOffset + streamDataIndex, sampleLength);
                //    Buffer.BlockCopy(zeros, 0, newMasterBytes, (int)sampleMetadata.DataOffset + headerIndex + 60 + headersSize, sampleLength);
                //}
                //else
                //{
                //    byte[] zeros = new byte[vanillaMasterBytes.Length - headerIndex - 60 - headersSize - (int)sampleMetadata.DataOffset];
                //    byte[] theseBytes = new byte[sampleLength];
                //    Buffer.BlockCopy(vanillaMasterBytes, (int)sampleMetadata.DataOffset + headerLength, theseBytes, 0, sampleLength);
                //    Buffer.BlockCopy(vanillaMasterBytes, (int)sampleMetadata.DataOffset + headerLength, newMasterBytes, (int)sampleMetadata.DataOffset + headerLength, sampleLength);
                //    //Buffer.BlockCopy(zeros, 0, newMasterBytes, (int)sampleMetadata.DataOffset + headerIndex + 60 + (int)sizeOfSampleHeaders, zeros.Length);
                //}

                replaced = true;
                _alreadyModdedAsset.Add(index);


                if (index == 138)
                {
                    //int test = vanillaMasterBytes[headerIndex + 60 + (int)address + 12];
                    //vanillaMasterBytes[headerIndex + 60 + (int)address + 12] = 202;
                    //Buffer.BlockCopy(sampleHeaderBytes, 0, vanillaMasterBytes, headerIndex + 60 + (int)address, 8);
                    long testad = GetSampleHeaderIndex(masterSampleHeadersBytes, 9);

                    long address = GetSampleHeaderIndex(masterSampleHeadersBytes, index);
                    byte[] sampleHeaderBytes = new byte[8];
                    Buffer.BlockCopy(vanillaMasterBytes, headerIndex + 60 + (int)address, sampleHeaderBytes, 0, 8);
                    sampleHeaderBytes[6] = 0;      //change nothing...
                    //sampleHeaderBytes[5] = 0;
                    //sampleHeaderBytes[4] = 0;
                    int test = vanillaMasterBytes[headerIndex + 60 + 8842];
                    vanillaMasterBytes[headerIndex + 60 + 8840] = 172;
                    vanillaMasterBytes[headerIndex + 60 + 8841] = 156;
                    vanillaMasterBytes[headerIndex + 60 + 8842] = 8;
                    int test2 = vanillaMasterBytes[headerIndex + 60 + 8848];
                    vanillaMasterBytes[headerIndex + 60 + 8848] = 2;
                    vanillaMasterBytes[headerIndex + 60 + 8849] = 99;
                    vanillaMasterBytes[headerIndex + 60 + 8850] = 239;
                    vanillaMasterBytes[headerIndex + 60 + 8851] = 105;
                    vanillaMasterBytes[headerIndex + 60 + 8852] = 16;
                    vanillaMasterBytes[headerIndex + 60 + 8853] = 0;
                    vanillaMasterBytes[headerIndex + 60 + 8854] = 0;
                    vanillaMasterBytes[headerIndex + 60 + 8855] = 0;
                    vanillaMasterBytes[headerIndex + 60 + 8856] = 64;
                    vanillaMasterBytes[headerIndex + 60 + 8857] = 185;
                    vanillaMasterBytes[headerIndex + 60 + 8858] = 0;
                    vanillaMasterBytes[headerIndex + 60 + 8859] = 0;
                    vanillaMasterBytes[headerIndex + 60 + 8860] = 175;
                    vanillaMasterBytes[headerIndex + 60 + 8861] = 93;
                    vanillaMasterBytes[headerIndex + 60 + 8862] = 0;
                    vanillaMasterBytes[headerIndex + 60 + 8863] = 0;
                    vanillaMasterBytes[headerIndex + 60 + 8864] = 64;
                    vanillaMasterBytes[headerIndex + 60 + 8865] = 117;
                    vanillaMasterBytes[headerIndex + 60 + 8866] = 1;
                    vanillaMasterBytes[headerIndex + 60 + 8867] = 0;
                    vanillaMasterBytes[headerIndex + 60 + 8868] = 146;
                    vanillaMasterBytes[headerIndex + 60 + 8869] = 107;
                    vanillaMasterBytes[headerIndex + 60 + 8870] = 0;
                    vanillaMasterBytes[headerIndex + 60 + 8871] = 0;

                    vanillaMasterBytes[headerIndex + 60 + 8876] = 0;
                    vanillaMasterBytes[headerIndex + 60 + 8877] = 254;
                    vanillaMasterBytes[headerIndex + 60 + 8878] = 127;
                    vanillaMasterBytes[headerIndex + 60 + 8879] = 63;


                    ReadMasterFileHeaders("D:\\FSBank5.Net 202\\FSBankNet\\bin\\Debug\\net5.0\\test.fsb5", out var fsbBank, out var fsbBytes);
                    theseBytes = fsbBank.Samples[0].SampleBytes;

                    Buffer.BlockCopy(theseBytes, 0, vanillaMasterBytes, (int)sample.Metadata.DataOffset + streamDataIndex, theseBytes.Length);
                }

                File.WriteAllBytes("Master.modded.bank", vanillaMasterBytes);
            }

            return replaced;
        }
        private static byte[] RebuildOggSample(byte[] fileBytes)
        {
            //Need to rebuild the vorbis header, which requires reading the known blobs from the json file.
            //This requires knowing the crc32 of the data, which is in a VORBISDATA chunk.
            //var dataChunk = sample.Metadata.Chunks.FirstOrDefault(f => f.ChunkType == FmodSampleChunkType.VORBISDATA);

            //if (dataChunk == null)
            //{
            //    throw new Exception("Rebuilding Vorbis data requires a VORBISDATA chunk, which wasn't found");
            //}

            //var chunkData = (VorbisChunkData)dataChunk.ChunkData;
            //var crc32 = chunkData.Crc32;

            //Ok, we have the crc32, now we need to find the header data.
            //if (headers == null)
            //    LoadVorbisHeaders();
            //var vorbisData = headers![crc32];

            //vorbisData.InitBlockFlags();

            //var infoPacket = BuildInfoPacket((byte)sample.Metadata.Channels, sample.Metadata.Frequency);
            //var commentPacket = BuildCommentPacket("Fmod5Sharp (Samboy063)");
            //var setupPacket = new OggPacket(vorbisData.HeaderBytes, false, 0, 2);

            //Begin building the final stream
            //var oggStream = new OggStream(1);
            //using var outputStream = new MemoryStream();

            //oggStream.PacketIn(infoPacket);
            //oggStream.PacketIn(commentPacket);
            //oggStream.PacketIn(setupPacket);

            //oggStream.FlushAndCopyTo(outputStream, true);

            //CopySampleData(vorbisData, sample.SampleBytes, oggStream, outputStream);

            //return outputStream.ToArray();
            return null;
        }

        private byte[] GetSampleBytes(FileInfo file)
        {
            //string longPathSpecifier = @"\\?" + file.FullName;
            byte[] data = File.ReadAllBytes(file.FullName);

            return data;
        }
    }
}
