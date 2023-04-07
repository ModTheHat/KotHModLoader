using Fmod5Sharp;
using Fmod5Sharp.ChunkData;
using Fmod5Sharp.FmodTypes;
using NAudio.Vorbis;
using NAudio.Wave;
using Newtonsoft.Json.Linq;
using OggVorbisEncoder;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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
        public void InitialisePaths()
        {
            LoadFMODManager();
        }

        private void LoadFMODManager()
        {
            var bytes = File.ReadAllBytes(_bankFilePath);

            var index = bytes.AsSpan().IndexOf(System.Text.Encoding.ASCII.GetBytes("FSB5"));

            bytes = index > 0 ? bytes.AsSpan(index).ToArray() : bytes;

            _fmodSounds = FsbLoader.LoadFsbFromByteArray(bytes);

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

        private void ReadVanillaMasterFileHeaders(out FmodSoundBank masterBank, out byte[] vanillaMasterBytes)
        {
            //VANILLA MASTER.BANK FULL FILE DATA
            vanillaMasterBytes = File.ReadAllBytes(_bankFilePath);

            //VANILLA MASTER.BANK FILE HEADER
            int headerIndex = vanillaMasterBytes.AsSpan().IndexOf(System.Text.Encoding.ASCII.GetBytes("FSB5"));
            byte[] headerBytes = new byte[headerIndex + 60];
            Buffer.BlockCopy(vanillaMasterBytes, 0, headerBytes, 0, headerIndex + 60);
            //File.WriteAllBytes("Master.modded.bank", headerBytes);

            //VANILLA MASTER.BANK METADATA, SAMPLES AND STREAMING DATA
            byte[] vanillaMasterNoHeaderBytes = null;
            if (headerIndex > 0)
            {
                vanillaMasterNoHeaderBytes = vanillaMasterBytes.AsSpan(headerIndex).ToArray();
            }

            //VANILLA MASTER.BANK METADATA, SAMPLES AND STREAMING FMODSOUNDBANK OBJECT
            masterBank = FsbLoader.LoadFsbFromByteArray(vanillaMasterNoHeaderBytes);

            //VANILLA MASTER.BANK STREAM AND BINARYREADER
            using MemoryStream stream = new(vanillaMasterNoHeaderBytes);
            using BinaryReader reader = new(stream);

            //VANILLA MASTER.BANK FMODAUDIOHEADER METADATA PART
            FmodAudioHeader header = new(reader);

            //READ VANILLA MASTER.BANK BYTES FOR METADATA
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

            //VANILLA MASTER.BANK END OF METADATA READ BYTES

            //VANILLA MASTER.BANK SAMPLES AND STREAMING START
            var sampleHeadersStart = reader.BaseStream.Position;

            List<FmodSampleMetadata> Samples = new();
            object ChunkReadingLock = new();

            int headersSize = (int)masterBank.Header.SampleHeadersSize;

            //NEW MASTER.BANK INITIALISATION
            byte[] newMasterBytes = new byte[vanillaMasterBytes.Length];
            Buffer.BlockCopy(vanillaMasterBytes, 0, newMasterBytes, 0, headerIndex + 60);

            //ADD SAMPLE HEADERS TO NEW MASTER
            Buffer.BlockCopy(vanillaMasterBytes, headerIndex + 60, newMasterBytes, headerIndex + 60, headersSize);
            int lastPos = headerIndex + 60 + headersSize;

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
                    if (masterBank.Samples[i].Name.Equals("Forest-Take01"))
                    {

                    }
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

            ReadVanillaMasterFileHeaders(out var masterBank, out var vanillaMasterBytes);
            int masterHeaderIndex = vanillaMasterBytes.AsSpan().IndexOf(System.Text.Encoding.ASCII.GetBytes("FSB5"));

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

            for (var i = 0; i < masterBank.Samples.Count; i++)
            {
                //byte[] byteTest = GetSampleData(i, out var sampleTest);

                ////ADD SAMPLE STREAMING DATA TO NEW MASTER
                //var sampleName = masterBank.Samples[i].Name.Contains("Forest-Take01");
                ////int headerLength = headerIndex - 60 - headersSize;
                //int headerLength = (int)masterBank.Header.NameTableSize;
                //int 
                //int sampleLength;
                //if (i + 1 < masterBank.Samples.Count)
                //{
                //    sampleLength = (int)masterBank.Samples[i + 1].Metadata.DataOffset - (int)sampleMetadata.DataOffset;
                //}
                //else
                //{
                //    sampleLength = vanillaMasterBytes.Length - (int)sampleMetadata.DataOffset - headerLength;
                //}
                //if (sampleName)
                //{
                //    byte[] zeros = new byte[sampleLength];
                //    byte[] theseBytes = new byte[sampleLength];
                //    Buffer.BlockCopy(vanillaMasterBytes, (int)sampleMetadata.DataOffset + headerLength, theseBytes, 0, sampleLength);
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

            ReadVanillaMasterFileHeaders(out var masterBank, out var vanillaMasterBytes);

            int headerIndex = vanillaMasterBytes.AsSpan().IndexOf(System.Text.Encoding.ASCII.GetBytes("FSB5"));
            int streamDataIndex = vanillaMasterBytes.Length - (int)_fmodSounds.Header.DataSize;

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

                //if (!contains)
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
                Buffer.BlockCopy(dataModFile, 0, theseBytes, 0, dataModFile.Length > sampleLength ? sampleLength : dataModFile.Length);
                int headerLength = headerIndex - (int)_fmodSounds.Header.ThisHeaderSize - (int)_fmodSounds.Header.SampleHeadersSize;
                //int headerLength = headerIndex - 60 - headersSize;
                Buffer.BlockCopy(theseBytes, 0, vanillaMasterBytes, (int)sample.Metadata.DataOffset + streamDataIndex, sampleLength);
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
