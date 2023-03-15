using Fmod5Sharp;
using Fmod5Sharp.FmodTypes;
using NAudio.Vorbis;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace KotHModLoaderGUI
{
    public class FMODManager
    {
        private string _dataDirPath = @"..\KingOfTheHat_Data";
        private string _streamingDirPath = @"\StreamingAssets";
        private string _bankFilePath = @"\Master.bank";

        FmodSoundBank _fmodSounds;

        public FmodSoundBank FmodSoundBank => _fmodSounds;
        public void InitialisePaths()
        {
            LoadFMODManager();
        }

        private void LoadFMODManager()
        {
            var bytes = File.ReadAllBytes(_dataDirPath + _streamingDirPath + _bankFilePath);

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

            FmodSample sample = _fmodSounds.Samples[index];

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

            if (!sample.RebuildAsStandardFileFormat(out var data, out var extension))
            {
                Console.WriteLine($"Failed to extract sample {sample.Name}");
            }

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
    }
}
