using Fmod5Sharp;
using Fmod5Sharp.FmodTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Media;
using System.Text;
using System.Xml.Linq;

namespace KotHModLoaderGUI
{
    public class FMODManager
    {
        private string _dataDirPath = @"..\KingOfTheHat_Data";
        private string _streamingDirPath = @"\StreamingAssets";
        private string _bankFilePath = @"\Master.bank";

        FmodSoundBank _fmodSounds;

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
            foreach(var samplebytes in sample.SampleBytes)
            {
                //infos.Add(samplebytes + ", ");
            }
            infos.Add("Channels: " + sample.Metadata.Channels + "\n");
            infos.Add("Is Stereo: " + sample.Metadata.IsStereo + "\n");
            infos.Add("Sample Count: " + sample.Metadata.SampleCount + "\n");
            infos.Add("Frequency: " + sample.Metadata.Frequency);

            //// Place the data into a stream
            //using (MemoryStream ms = new MemoryStream(sample.SampleBytes))
            //{
            //    // Construct the sound player
            //    SoundPlayer player = new SoundPlayer(ms);
            //    player.Play();
            //}
            //if (!sample.RebuildAsStandardFileFormat(out var data, out var extension))
            //{
            //    Console.WriteLine($"Failed to extract sample {sample.Name}");
            //    //continue;
            //}

            return infos;
        }
    }
}
