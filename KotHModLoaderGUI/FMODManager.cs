using Fmod5Sharp;
using Fmod5Sharp.ChunkData;
using Fmod5Sharp.FmodTypes;
using NAudio.SoundFont;
using NAudio.Vorbis;
using NAudio.Wave;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using static Fmod5Sharp.Util.Extensions;

namespace KotHModLoaderGUI
{
    public class FMODManager : ModManager
    {
        [DllImport("FSBankNative.dll")]
        public static extern int Create(string path);

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

        private Dictionary<int, string> _replacers = new Dictionary<int, string>();

        public void InitialisePaths()
        {
            //int test = Create("D:/file_example_WAV_1MG.wav");
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

        private byte[] ReadModdedOGGFile(FileInfo oggFile)
        {
            //FileInfo oggfile = new FileInfo(@"..\Mods\New folder\KotH_UI_LandingScreen_PressStart_v2-01.ogg");

            byte[] dataModFile = GetSampleBytes(oggFile);

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
            masterBank = null;
            masterBytes = null;

            if (!File.Exists(file)) return;

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
                if(index == 138)
                {
                }
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
        public string BuildActiveModsFMODAudio(List<Mod> mods)
        {
            _alreadyModded = new List<string>();
            _replacers = new Dictionary<int, string>();

            //ReadModdedMasterFile(out var moddedFileByte, out var moddedSoundBank);
            ReadMasterFileHeaders("Master.modded.bank", out var moddedSoundBank, out var moddedFileByte);

            ReadMasterFileHeaders("Master.FMODBANKTOOL.bank", out var fbtMasterBank, out var fbtMasterBytes);
            int fbtMasterHeaderIndex = fbtMasterBytes.AsSpan().IndexOf(System.Text.Encoding.ASCII.GetBytes("FSB5"));
            byte[] fbtHeaderBytes = new byte[fbtMasterHeaderIndex];
            Buffer.BlockCopy(fbtMasterBytes, 0, fbtHeaderBytes, 0, fbtMasterHeaderIndex);
            byte[] fbtInfoBytes = new byte[60];
            Buffer.BlockCopy(fbtMasterBytes, fbtMasterHeaderIndex, fbtInfoBytes, 0, 60);
            byte[] fbtSampleHeadersBytes = new byte[fbtMasterBank.Header.SampleHeadersSize];
            Buffer.BlockCopy(fbtMasterBytes, fbtMasterHeaderIndex + 60, fbtSampleHeadersBytes, 0, (int)fbtMasterBank.Header.SampleHeadersSize);

            ReadMasterFileHeaders(_bankFilePath, out var masterBank, out var vanillaMasterBytes);
            int masterHeaderIndex = vanillaMasterBytes.AsSpan().IndexOf(System.Text.Encoding.ASCII.GetBytes("FSB5"));
            byte[] masterHeaderBytes = new byte[masterHeaderIndex];
            Buffer.BlockCopy(vanillaMasterBytes, 0, masterHeaderBytes, 0, masterHeaderIndex);
            byte[] masterInfoBytes = new byte[60];
            Buffer.BlockCopy(vanillaMasterBytes, masterHeaderIndex, masterInfoBytes, 0, 60);
            byte[] masterSampleHeadersBytes = new byte[masterBank.Header.SampleHeadersSize];
            Buffer.BlockCopy(vanillaMasterBytes, masterHeaderIndex + 60, masterSampleHeadersBytes, 0, (int)masterBank.Header.SampleHeadersSize);
            byte[] masterNameTableBytes = new byte[masterBank.Header.NameTableSize];
            Buffer.BlockCopy(vanillaMasterBytes, masterHeaderIndex + 60 + (int)masterBank.Header.SampleHeadersSize, masterNameTableBytes, 0, (int)masterBank.Header.NameTableSize);
            byte[] masterSampleStreamingDataBytes = new byte[masterBank.Header.DataSize];
            Buffer.BlockCopy(vanillaMasterBytes, masterHeaderIndex + 60 + (int)masterBank.Header.SampleHeadersSize + (int)masterBank.Header.NameTableSize, masterSampleStreamingDataBytes, 0, (int)masterBank.Header.DataSize);

            //NEW BIG HEADER
            byte[] newBigHeader = new byte[masterHeaderIndex];
            Buffer.BlockCopy(vanillaMasterBytes, 0, newBigHeader, 0, masterHeaderIndex);

            //NEW FMOD SOUND BANK HEADER
            byte[] newBankHeader = new byte[60];
            Buffer.BlockCopy(vanillaMasterBytes, masterHeaderIndex, newBankHeader, 0, 60);

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

                    if (disabledAssets.Contains(@"\" + file.FullName.Substring(file.FullName.IndexOf(mod.Name))) || _alreadyModded.Contains(file.Name)) continue;
                                        
                    byte[] bytes = GetSampleBytes(file);

                    if (blacklistedAssets.Contains(file.FullName.Substring(file.FullName.IndexOf(mod.Name) + mod.Name.Length)) || assignedAsset != null) continue;
                    
                    if (ModVanillaFMODAudioFromFileName(file.Name, bytes, 0, 0, blacklistedAssets, file))
                        _alreadyModded.Add(file.Name);
                }
            }
            //VANILLA MASTER.BANK SAMPLES, STREAMING AND FILE END

            ReplaceSamples(masterSampleStreamingDataBytes, in newStreamingData, masterSampleHeadersBytes, in newSampleHeader);

            List<byte> newMaster = new List<byte>();

            //BIG FIRST HEADER
            byte[] newHeaderBytes = new byte[masterHeaderBytes.Length];
            Buffer.BlockCopy(masterHeaderBytes, 0, newHeaderBytes, 0, masterHeaderBytes.Length);
            int fullSize = newHeaderBytes.Length + newBankHeader.Length + newSampleHeader.Count + masterNameTableBytes.Length + newStreamingData.Count - 8;
            string headerFull = Convert.ToString(fullSize, 2).PadLeft(32, '0');
            string header4 = headerFull[24..32];
            string header5 = headerFull[16..24];
            string header6 = headerFull[8..16]; 
            string header7 = headerFull[0..8]; 
            int header4Byte = Convert.ToInt32(header4, 2);
            int header5Byte = Convert.ToInt32(header5, 2);
            int header6Byte = Convert.ToInt32(header6, 2);
            int header7Byte = Convert.ToInt32(header7, 2);
            newHeaderBytes[4] = (byte)header4Byte;
            newHeaderBytes[5] = (byte)header5Byte;
            newHeaderBytes[6] = (byte)header6Byte;
            newHeaderBytes[7] = (byte)header7Byte;

            int noHeaderSize = newBankHeader.Length + newSampleHeader.Count + masterNameTableBytes.Length + newStreamingData.Count;
            string noHeaderFull = Convert.ToString(noHeaderSize, 2).PadLeft(32, '0');
            string noHeaderFourStr = noHeaderFull[0..8];
            string noHeaderFiveStr = noHeaderFull[8..16];
            string noHeaderSixStr = noHeaderFull[16..24];
            string noHeaderSevenStr = noHeaderFull[24..32];
            int noHeaderFour = Convert.ToInt32(noHeaderFourStr, 2);
            int noHeaderFive = Convert.ToInt32(noHeaderFiveStr, 2);
            int noHeaderSix = Convert.ToInt32(noHeaderSixStr, 2);
            int noHeaderSeven = Convert.ToInt32(noHeaderSevenStr, 2);
            newHeaderBytes[1856504] = (byte)noHeaderSeven;
            newHeaderBytes[1856505] = (byte)noHeaderSix;
            newHeaderBytes[1856506] = (byte)noHeaderFive;
            newHeaderBytes[1856507] = (byte)noHeaderFour;

            //BANK HEADER (60-64 bytes)

                //sample header (12-15)
            string newBankLength = Convert.ToString(newSampleHeader.Count, 2).PadLeft(32, '0');
            string newBank12 = newBankLength[24..32];
            string newBank13 = newBankLength[16..24];
            string newBank14 = newBankLength[8..16];
            string newBank15 = newBankLength[0..8];
            byte newBank12Byte = (byte)Convert.ToInt32(newBank12, 2);
            byte newBank13Byte = (byte)Convert.ToInt32(newBank13, 2);
            byte newBank14Byte = (byte)Convert.ToInt32(newBank14, 2);
            byte newBank15Byte = (byte)Convert.ToInt32(newBank15, 2);

            newBankHeader[12] = newBank12Byte;
            newBankHeader[13] = newBank13Byte;
            newBankHeader[14] = newBank14Byte;
            newBankHeader[15] = newBank15Byte;

                //size of data  (20-23)
            string newStreamingDataLength = Convert.ToString(newStreamingData.Count, 2).PadLeft(32, '0');
            string newBank20 = newStreamingDataLength[24..32];
            string newBank21 = newStreamingDataLength[16..24];
            string newBank22 = newStreamingDataLength[8..16];
            string newBank23 = newStreamingDataLength[0..8];
            byte newBank20Byte = (byte)Convert.ToInt32(newBank20, 2);
            byte newBank21Byte = (byte)Convert.ToInt32(newBank21, 2);
            byte newBank22Byte = (byte)Convert.ToInt32(newBank22, 2);
            byte newBank23Byte = (byte)Convert.ToInt32(newBank23, 2);

            newBankHeader[20] = newBank20Byte;
            newBankHeader[21] = newBank21Byte;
            newBankHeader[22] = newBank22Byte;
            newBankHeader[23] = newBank23Byte;

            newMaster.AddRange(newHeaderBytes);
            newMaster.AddRange(newBankHeader);
            newMaster.AddRange(newSampleHeader);
            newMaster.AddRange(masterNameTableBytes);
            newMaster.AddRange(newStreamingData);

            byte[] test = new byte[masterHeaderBytes.Length + masterInfoBytes.Length + masterSampleHeadersBytes.Length + masterNameTableBytes.Length + masterSampleStreamingDataBytes.Length];
            Buffer.BlockCopy(masterHeaderBytes, 0, test, 0, masterHeaderBytes.Length);
            Buffer.BlockCopy(masterInfoBytes, 0, test, masterHeaderBytes.Length, masterInfoBytes.Length);
            Buffer.BlockCopy(masterSampleHeadersBytes, 0, test, masterHeaderBytes.Length + masterInfoBytes.Length, masterSampleHeadersBytes.Length);
            Buffer.BlockCopy(masterNameTableBytes, 0, test, masterHeaderBytes.Length + masterInfoBytes.Length + masterSampleHeadersBytes.Length, masterNameTableBytes.Length);
            Buffer.BlockCopy(masterSampleStreamingDataBytes, 0, test, masterHeaderBytes.Length + masterInfoBytes.Length + masterSampleHeadersBytes.Length + masterNameTableBytes.Length, masterSampleStreamingDataBytes.Length);


            File.WriteAllBytes("Master.modded.bank", newMaster.ToArray());

            return null;
        }

        private void ReplaceSamples(byte[] vanillaStreamingBytes, in List<byte> newStreamingDataBytes, byte[] vanillaSampleHeadersBytes, in List<byte> newSampleHeadersBytes)
        {
            var _sortedReplacers = _replacers.ToImmutableSortedDictionary();

            int lastStreamReplacementIndex = 0;
            int lastHeaderReplacementIndex = 0;
            int lastReplacerIndex = -1;
            int streamOffset = 0;
            int newStreamingIndex = 0;

            foreach (KeyValuePair<int, string> replacement in _sortedReplacers)
            {
                int index = replacement.Key;
                string filePath = replacement.Value;

                //Streaming replacements
                int vanillaStreamingIndex = (int)_fmodSounds.Samples[index].Metadata.DataOffset;
                int vanillaStreamingNextIndex = index + 1 >= _fmodSounds.Samples.Count ? vanillaStreamingBytes.Length : (int)_fmodSounds.Samples[index + 1].Metadata.DataOffset;

                byte[] vanillaSampleBytes = new byte[vanillaStreamingNextIndex - vanillaStreamingIndex];
                Buffer.BlockCopy(vanillaStreamingBytes, vanillaStreamingIndex, vanillaSampleBytes, 0, vanillaStreamingNextIndex - vanillaStreamingIndex);
                byte[] vanillaSampleBytesBefore = new byte[vanillaStreamingIndex - lastStreamReplacementIndex];
                Buffer.BlockCopy(vanillaStreamingBytes, lastStreamReplacementIndex, vanillaSampleBytesBefore, 0, vanillaStreamingIndex - lastStreamReplacementIndex);

                lastStreamReplacementIndex = vanillaStreamingNextIndex;

                Create(filePath);
                ReadMasterFileHeaders("temp.fsb", out var fsbBank, out var fsbBytes);

                newStreamingDataBytes.AddRange(vanillaSampleBytesBefore);

                byte[] newStream = new byte[vanillaStreamingNextIndex - vanillaStreamingIndex];
                Buffer.BlockCopy(fsbBank.Samples[0].SampleBytes, 0, newStream, 0, vanillaStreamingNextIndex - vanillaStreamingIndex > fsbBank.Samples[0].SampleBytes.Length ? fsbBank.Samples[0].SampleBytes.Length : vanillaStreamingNextIndex - vanillaStreamingIndex);
                //newStreamingDataBytes.Clear();
                //newStreamingDataBytes.AddRange(newStream);


                newStreamingIndex = newStreamingDataBytes.Count;
                newStreamingDataBytes.AddRange(fsbBank.Samples[0].SampleBytes);
                //newStreamingDataBytes.AddRange(newStream);

                //Headers Replacement
                int vanillaHeaderIndex = _sampleHeadersIndexes[index];
                int vanillaHeaderNextIndex = index + 1 >= _fmodSounds.Samples.Count ? vanillaSampleHeadersBytes.Length : _sampleHeadersIndexes[index + 1];

                if (lastReplacerIndex >= 0)
                {
                    //go through all headers before and change the dataoffset
                    for(int h = _sortedReplacers.Keys.ToArray()[lastReplacerIndex] + 1; h < index; h++)
                    {
                        int size = h + 1 < _sampleHeadersIndexes.Length ? _sampleHeadersIndexes[h + 1] - _sampleHeadersIndexes[h] : vanillaSampleHeadersBytes.Length - _sampleHeadersIndexes[h];
                        byte[] sampleHeader = new byte[size];
                        Buffer.BlockCopy(vanillaSampleHeadersBytes, _sampleHeadersIndexes[h], sampleHeader, 0, size);
                        string zero = Convert.ToString(sampleHeader[0], 2).PadLeft(8, '0');
                        string one = Convert.ToString(sampleHeader[1], 2).PadLeft(8, '0');
                        string two = Convert.ToString(sampleHeader[2], 2).PadLeft(8, '0');
                        string three = Convert.ToString(sampleHeader[3], 2).PadLeft(8, '0');
                        string four = Convert.ToString(sampleHeader[4], 2).PadLeft(8, '0');
                        string full = four[6].ToString() + four[7].ToString() + three + two + one + zero[0].ToString();
                        int dataOffset = Convert.ToInt32(full, 2) * 32;
                        dataOffset += streamOffset;
                        //dataOffset = newStreamingDataBytes.Count;
                        dataOffset /= 32;
                        string newData = Convert.ToString(dataOffset, 2).PadLeft(27, '0');
                        string newFour = four[0..6] + newData[0].ToString() + newData[1].ToString();
                        string newThree = newData[2..10].ToString();
                        string newTwo = newData[10..18].ToString();
                        string newOne = newData[18..26].ToString();
                        string newZero = newData[26].ToString() + zero[1..8];
                        int byteZero = Convert.ToInt32(newZero, 2);
                        int byteOne = Convert.ToInt32(newOne, 2);
                        int byteTwo = Convert.ToInt32(newTwo, 2);
                        int byteThree = Convert.ToInt32(newThree, 2);
                        int byteFour = Convert.ToInt32(newFour, 2);

                        sampleHeader[0] = (byte)byteZero;
                        sampleHeader[1] = (byte)byteOne;
                        sampleHeader[2] = (byte)byteTwo;
                        sampleHeader[3] = (byte)byteThree;
                        sampleHeader[4] = (byte)byteFour;

                        newSampleHeadersBytes.AddRange(sampleHeader);
                    }
                }
                streamOffset = (int)newStreamingDataBytes.Count - (int)vanillaStreamingNextIndex;

                byte[] vanillaHeaderBytes = new byte[vanillaHeaderNextIndex - vanillaHeaderIndex];
                Buffer.BlockCopy(vanillaSampleHeadersBytes, vanillaHeaderIndex, vanillaHeaderBytes, 0, vanillaHeaderNextIndex - vanillaHeaderIndex);
                byte[] vanillaHeaderBytesBefore = new byte[vanillaHeaderIndex - lastHeaderReplacementIndex];
                Buffer.BlockCopy(vanillaSampleHeadersBytes, lastHeaderReplacementIndex, vanillaHeaderBytesBefore, 0, vanillaHeaderIndex - lastHeaderReplacementIndex);

                lastHeaderReplacementIndex = vanillaHeaderNextIndex;

                byte[] fsbHeaderBytes = new byte[fsbBank.Header.SampleHeadersSize];
                Buffer.BlockCopy(fsbBytes, (int)fsbBank.Header.ThisHeaderSize, fsbHeaderBytes, 0, (int)fsbBank.Header.SampleHeadersSize);

                string fsbZero = Convert.ToString(fsbHeaderBytes[0], 2).PadLeft(8, '0');
                string fsbOne = Convert.ToString(fsbHeaderBytes[1], 2).PadLeft(8, '0');
                string fsbTwo = Convert.ToString(fsbHeaderBytes[2], 2).PadLeft(8, '0');
                string fsbThree = Convert.ToString(fsbHeaderBytes[3], 2).PadLeft(8, '0');
                string fsbFour = Convert.ToString(fsbHeaderBytes[4], 2).PadLeft(8, '0');
                string fsbFull = fsbFour[6].ToString() + fsbFour[7].ToString() + fsbThree + fsbTwo + fsbOne + fsbZero[0].ToString();
                int fsbDataOffset = Convert.ToInt32(fsbFull, 2) * 32;
                fsbDataOffset = newStreamingIndex / 32;
                string fsbNewData = Convert.ToString(fsbDataOffset, 2).PadLeft(27, '0');
                string fsbNewFour = fsbFour[0..6] + fsbNewData[0].ToString() + fsbNewData[1].ToString();
                string fsbNewThree = fsbNewData[2..10].ToString();
                string fsbNewTwo = fsbNewData[10..18].ToString();
                string fsbNewOne = fsbNewData[18..26].ToString();
                string fsbNewZero = fsbNewData[26].ToString() + fsbZero[1..8];
                int fsbByteZero = Convert.ToInt32(fsbNewZero, 2);
                int fsbByteOne = Convert.ToInt32(fsbNewOne, 2);
                int fsbByteTwo = Convert.ToInt32(fsbNewTwo, 2);
                int fsbByteThree = Convert.ToInt32(fsbNewThree, 2);
                int fsbByteFour = Convert.ToInt32(fsbNewFour, 2);

                fsbHeaderBytes[0] = (byte)fsbByteZero;
                fsbHeaderBytes[1] = (byte)fsbByteOne;
                fsbHeaderBytes[2] = (byte)fsbByteTwo;
                fsbHeaderBytes[3] = (byte)fsbByteThree;
                fsbHeaderBytes[4] = (byte)fsbByteFour;

                if(lastReplacerIndex == -1)
                    newSampleHeadersBytes.AddRange(vanillaHeaderBytesBefore);

                //TESTSTTSTS
               // fsbHeaderBytes = 

                //FIN TESTSTS

                newSampleHeadersBytes.AddRange(fsbHeaderBytes);

                lastReplacerIndex++;
            }

            if (lastStreamReplacementIndex < _fmodSounds.Header.DataSize)
            {
                byte[] vanillaSampleBytesToEnd = new byte[_fmodSounds.Header.DataSize - lastStreamReplacementIndex];
                Buffer.BlockCopy(vanillaStreamingBytes, lastStreamReplacementIndex, vanillaSampleBytesToEnd, 0, (int)_fmodSounds.Header.DataSize - lastStreamReplacementIndex);
                newStreamingDataBytes.AddRange(vanillaSampleBytesToEnd);
            }

            if (lastHeaderReplacementIndex < _fmodSounds.Header.SampleHeadersSize)
            {
                byte[] vanillaHeadersBytesToEnd = new byte[_fmodSounds.Header.SampleHeadersSize - lastHeaderReplacementIndex];
                Buffer.BlockCopy(vanillaSampleHeadersBytes, lastHeaderReplacementIndex, vanillaHeadersBytesToEnd, 0, (int)_fmodSounds.Header.SampleHeadersSize - lastHeaderReplacementIndex);
                //newSampleHeadersBytes.AddRange(vanillaHeadersBytesToEnd);

                //go through all headers before and change the dataoffset
                for (int h = _sortedReplacers.Keys.ToArray()[lastReplacerIndex] + 1; h < _fmodSounds.Samples.Count; h++)
                {
                    int size = h + 1 < _sampleHeadersIndexes.Length ? _sampleHeadersIndexes[h + 1] - _sampleHeadersIndexes[h] : vanillaSampleHeadersBytes.Length - _sampleHeadersIndexes[h];
                    byte[] sampleHeader = new byte[size];
                    Buffer.BlockCopy(vanillaSampleHeadersBytes, _sampleHeadersIndexes[h], sampleHeader, 0, size);
                    string zero = Convert.ToString(sampleHeader[0], 2).PadLeft(8, '0');
                    string one = Convert.ToString(sampleHeader[1], 2).PadLeft(8, '0');
                    string two = Convert.ToString(sampleHeader[2], 2).PadLeft(8, '0');
                    string three = Convert.ToString(sampleHeader[3], 2).PadLeft(8, '0');
                    string four = Convert.ToString(sampleHeader[4], 2).PadLeft(8, '0');
                    string full = four[6].ToString() + four[7].ToString() + three + two + one + zero[0].ToString();
                    int dataOffset = Convert.ToInt32(full, 2) * 32;
                    dataOffset += streamOffset;
                    dataOffset /= 32;
                    string newData = Convert.ToString(dataOffset, 2).PadLeft(27, '0');
                    string newFour = four[0..6] + newData[0].ToString() + newData[1].ToString();
                    string newThree = newData[2..10].ToString();
                    string newTwo = newData[10..18].ToString();
                    string newOne = newData[18..26].ToString();
                    string newZero = newData[26].ToString() + zero[1..8];
                    int byteZero = Convert.ToInt32(newZero, 2);
                    int byteOne = Convert.ToInt32(newOne, 2);
                    int byteTwo = Convert.ToInt32(newTwo, 2);
                    int byteThree = Convert.ToInt32(newThree, 2);
                    int byteFour = Convert.ToInt32(newFour, 2);

                    sampleHeader[0] = (byte)byteZero;
                    sampleHeader[1] = (byte)byteOne;
                    sampleHeader[2] = (byte)byteTwo;
                    sampleHeader[3] = (byte)byteThree;
                    sampleHeader[4] = (byte)byteFour;

                    newSampleHeadersBytes.AddRange(sampleHeader);
                }
            }
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
            int headerIndex = vanillaMasterBytes.AsSpan().IndexOf(System.Text.Encoding.ASCII.GetBytes("FSB5"));
            byte[] masterSampleHeadersBytes = new byte[masterBank.Header.SampleHeadersSize];
            Buffer.BlockCopy(vanillaMasterBytes, headerIndex + 60, masterSampleHeadersBytes, 0, (int)masterBank.Header.SampleHeadersSize);

            int streamDataIndex = vanillaMasterBytes.Length - (int)_fmodSounds.Header.DataSize;

            if (indexes == null) return replaced;

            foreach (int index in indexes)
            {
                FmodSample sample = _fmodSounds.Samples[index];
                string name = sample.Name;

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

                byte[] byteSample = GetSampleData(index, sample);

                byte[] dataModFile = ReadModdedOGGFile(modFile);
                ////ADD SAMPLE STREAMING DATA TO NEW MASTER
                int sampleLength = index + 1 < _fmodSounds.Samples.Count ?
                    (int)_fmodSounds.Samples[index + 1].Metadata.DataOffset - (int)_fmodSounds.Samples[index].Metadata.DataOffset :
                    (int)_fmodSounds.Header.DataSize - (int)_fmodSounds.Samples[index].Metadata.DataOffset;

                byte[] theseBytes = new byte[sampleLength];
                byte[] originalBytes = new byte[sampleLength];
                Buffer.BlockCopy(dataModFile, 0, theseBytes, 0, dataModFile.Length > sampleLength ? sampleLength : dataModFile.Length);
                int headerLength = headerIndex - (int)_fmodSounds.Header.ThisHeaderSize - (int)_fmodSounds.Header.SampleHeadersSize;

                _replacers.Add(index, modFile.FullName);
                replaced = true;
                _alreadyModdedAsset.Add(index);
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
