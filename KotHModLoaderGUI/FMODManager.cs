using Fmod5Sharp;
using Fmod5Sharp.FmodTypes;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Media;
using System.Text;
using System.Windows.Controls;
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

            if (!sample.RebuildAsStandardFileFormat(out var data, out var extension))
            {
                Console.WriteLine($"Failed to extract sample {sample.Name}");
                //continue;
            }
            else
            {
                //IWaveProvider provider = new RawSourceWaveStream(
                //         new MemoryStream(sample.SampleBytes), new WaveFormat(sample.Metadata.Frequency, 24, (int)sample.Metadata.Channels));
                //var waveOut = new WaveOut(); // or WaveOutEvent()
                //waveOut.Init(provider);
                //waveOut.Play();

                //WaveFileReader reader = new WaveFileReader(new MemoryStream(data));

                //var waveOut = new WaveOut(); // or WaveOutEvent()
                //waveOut.Init(reader);
                //waveOut.Play();
            }

            return infos;
        }

        public void PlayOgg(string path)
        {
            var vorbis = new NAudio.Vorbis.VorbisWaveReader(path);
            var waveOut = new WaveOut(); // or WaveOutEvent()
            waveOut.Init(vorbis);
            waveOut.Play();

            //using (var vorbis = new NVorbis.VorbisReader("D:\\KotHModLoader\\KotHModLoaderGUI\\bin\\Debug\\Mods(new structure)\\TANGO\\Sounds\\Forest-Take01.ogg"))
            //{
            //    // get the channels & sample rate
            //    var channels = vorbis.Channels;
            //    var sampleRate = vorbis.SampleRate;

            //    // OPTIONALLY: get a TimeSpan indicating the total length of the Vorbis stream
            //    var totalTime = vorbis.TotalTime;

            //    // create a buffer for reading samples
            //    var readBuffer = new float[channels * sampleRate / 5];  // 200ms

            //    // get the initial position (obviously the start)
            //    var position = TimeSpan.Zero;

            //    // go grab samples
            //    int cnt;
            //    while ((cnt = vorbis.ReadSamples(readBuffer, 0, readBuffer.Length)) > 0)
            //    {
            //        // do stuff with the buffer
            //        // samples are interleaved (chan0, chan1, chan0, chan1, etc.)
            //        // sample value range is -0.99999994f to 0.99999994f unless vorbis.ClipSamples == false

            //        // OPTIONALLY: get the position we just read through to...
            //        position = vorbis.TimePosition;
            //    }

            //}

        }
    }
}
