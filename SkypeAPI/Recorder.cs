using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using CSCore;
using CSCore.Codecs.WAV;
using CSCore.CoreAudioAPI;
using CSCore.SoundIn;
using CSCore.Streams;

namespace SkypeAPI
{
    
    public class Recorder
    {
        private const int BitsPerSample = 16;
        private const int Channels = 1;
        private const int SampleRate = 16000;

        private WasapiCapture _soundIn;
        private IWaveSource _convertedSource;
        private WaveWriter _waveWriter;
        private Stopwatch _timer;

        public void Record(string audioFilePath = @"C:\Temp\output.wav")
        {
            _timer = new Stopwatch(); 
            _timer.Start();

            // choose the capture mod
            CaptureMode captureMode = CaptureMode.LoopbackCapture;
            DataFlow dataFlow = captureMode == CaptureMode.Capture ? DataFlow.Capture : DataFlow.Render;

            //select the device:
            var devices = MMDeviceEnumerator.EnumerateDevices(dataFlow, DeviceState.Active);
            if (!devices.Any())
            {
                Console.WriteLine("### No devices found.");
                return;
            }

            Console.WriteLine("### Available devices:");
            for (int i = 0; i < devices.Count; i++)
            {
                Console.WriteLine("### - {0:#00}: {1}", i, devices[i].FriendlyName);
            }

            var device = devices[1]; //TODO


            //start recording
            //create a new soundIn instance
            _soundIn = captureMode == CaptureMode.Capture
                ? new WasapiCapture()
                : new WasapiLoopbackCapture();


            //optional: set some properties 
            _soundIn.Device = device;


            //initialize the soundIn instance
            _soundIn.Initialize();

            //create a SoundSource around the the soundIn instance
            //this SoundSource will provide data, captured by the soundIn instance
            SoundInSource soundInSource = new SoundInSource(_soundIn) {FillWithZeros = false};

            //create a source, that converts the data provided by the
            //soundInSource to any other format
            //in this case the "Fluent"-extension methods are being used
            _convertedSource = soundInSource
                .ChangeSampleRate(SampleRate) // sample rate
                .ToSampleSource()
                .ToWaveSource(BitsPerSample); //bits per sample

            //channels...
            _convertedSource = _convertedSource.ToMono();

            //create a new wavefile
            _waveWriter = new WaveWriter(audioFilePath, _convertedSource.WaveFormat);

            //register an event handler for the DataAvailable event of 
            //the soundInSource
            //Important: use the DataAvailable of the SoundInSource
            //If you use the DataAvailable event of the ISoundIn itself
            //the data recorded by that event might won't be available at the
            //soundInSource yet
            soundInSource.DataAvailable += (s, e) =>
            {
                //read data from the converedSource
                //important: don't use the e.Data here
                //the e.Data contains the raw data provided by the 
                //soundInSource which won't have your target format
                byte[] buffer = new byte[_convertedSource.WaveFormat.BytesPerSecond / 2];
                int read;

                //keep reading as long as we still get some data
                //if you're using such a loop, make sure that soundInSource.FillWithZeros is set to false
                while ((read = _convertedSource.Read(buffer, 0, buffer.Length)) > 0)
                {
                    //write the read data to a file
                    // ReSharper disable once AccessToDisposedClosure
                    _waveWriter.Write(buffer, 0, read);
                }
            };

            //we've set everything we need -> start capturing data
            _soundIn.Start();
            Console.WriteLine($"### RECORDING {audioFilePath}");

            while (_timer.ElapsedMilliseconds / 1000 < 15 && _timer.IsRunning)
            {
                Thread.Sleep(500);
            }

            Console.WriteLine("### STOP RECORDING");
            _soundIn.Stop();
            _timer.Stop();

            _waveWriter.Dispose();
            _convertedSource.Dispose();
            _soundIn.Dispose();

        }

        public void Stop()
        {
            _timer?.Stop();
        }

        enum CaptureMode
        {
            Capture = 1,
            // ReSharper disable once UnusedMember.Local
            LoopbackCapture = 2
        }
    }
}
