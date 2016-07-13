using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media;
using Windows.Media.Audio;
using Windows.Media.MediaProperties;
using Windows.Media.Render;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace rtlSdrAppUWP
{
    // We are initializing a COM interface for use within the namespace
    // This interface allows access to memory at the byte level which we need to populate audio data that is generated
    [ComImport]
    [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]

    unsafe interface IMemoryBufferByteAccess {
        void GetBuffer(out byte* buffer, out uint capacity);
    }

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page {
        private AudioFrameInputNode frameInputNode;
        private AudioGraph graph;
        private AudioDeviceOutputNode deviceOutputNode;
        private double theta = 0;

        rtlSdrUWP.Device dev;
        rtlSdrUWP.FmDecoder fmDecoder;

        public MainPage() {
            this.InitializeComponent();
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e) {
            var devices = await rtlSdrUWP.Device.GetDevices();
            foreach (var device in devices) {
                System.Diagnostics.Debug.WriteLine(device.Name);
            }

            await CreateAudioGraph();

            dev = new rtlSdrUWP.Device();

            fmDecoder = new rtlSdrUWP.FmDecoder();

            bool res = await dev.OpenDevice(devices[0].Id);
            if (!res) {
                throw new Exception();
            }

            // manual gain mode
            await dev.SetOffsetTuning(false);
            await dev.SetFreqCorrection(0);
            await dev.SetAgcMode(false);

            //await dev.SetSampleRate(2400000);
            await dev.SetSampleRate(1024000);
            await dev.SetCenterFreq(97100000);
            await dev.SetTunerGainMode(1);
            await dev.SetTunerGain(254);

            //await dev.SetTunerBandwidth(250000);

            await dev.ResetBuffer();

            frameInputNode.Start();

            //byte[] rawData = await dev.ReadAsync();

            //var fmData = fmDecoder.Process(ref rawData, dev.Frequency);
        }


        unsafe private AudioFrame GenerateAudioData(uint samples) {
            // Buffer size is (number of samples) * (size of each sample)
            // We choose to generate single channel (mono) audio. For multi-channel, multiply by number of channels
            uint bufferSize = samples * sizeof(float);
            AudioFrame frame = new Windows.Media.AudioFrame(bufferSize);

            using (AudioBuffer buffer = frame.LockBuffer(AudioBufferAccessMode.Write))
            using (IMemoryBufferReference reference = buffer.CreateReference()) {
                byte* dataInBytes;
                uint capacityInBytes;
                float* dataInFloat;

                // Get the buffer from the AudioFrame
                ((IMemoryBufferByteAccess)reference).GetBuffer(out dataInBytes, out capacityInBytes);

                // Cast to float since the data we are generating is float
                dataInFloat = (float*)dataInBytes;


                //uint readSize = 1024000 / 50;
                uint readSize = 42 * samples;
                uint test = readSize % 512;
                readSize += 512 - test;

                byte[] rawData = dev.ReadAsync(1024000 / 50).Result;

                var fmData = fmDecoder.Process(ref rawData, 0);

                int sampleRate = (int)graph.EncodingProperties.SampleRate;

                if (samples > fmData.left.Length)
                    throw new Exception();

                Marshal.Copy(fmData.left, 0, (IntPtr)dataInFloat, (int)samples);

                /*float freq = 1000; // choosing to generate frequency of 1kHz
                float amplitude = 0.3f;
                int sampleRate = (int)graph.EncodingProperties.SampleRate;
                double sampleIncrement = (freq * (Math.PI * 2)) / sampleRate;

                // Generate a 1kHz sine wave and populate the values in the memory buffer
                for (int i = 0; i < samples; i++) {
                    double sinValue = amplitude * Math.Sin(theta);
                    dataInFloat[i] = (float)sinValue;
                    theta += sampleIncrement;
                }*/
            }

            return frame;
        }

        private async Task CreateAudioGraph() {
            // Create an AudioGraph with default settings
            AudioGraphSettings settings = new AudioGraphSettings(AudioRenderCategory.Media);

            CreateAudioGraphResult result = await AudioGraph.CreateAsync(settings);

            if (result.Status != AudioGraphCreationStatus.Success) {
                // Cannot create graph
                //rootPage.NotifyUser(String.Format("AudioGraph Creation Error because {0}", result.Status.ToString()), NotifyType.ErrorMessage);
                throw new Exception();
                //return;
            }

            graph = result.Graph;
            // Create a device output node
            CreateAudioDeviceOutputNodeResult deviceOutputNodeResult = await graph.CreateDeviceOutputNodeAsync();
            if (deviceOutputNodeResult.Status != AudioDeviceNodeCreationStatus.Success) {
                // Cannot create device output node
                //rootPage.NotifyUser(String.Format("Audio Device Output unavailable because {0}", deviceOutputNodeResult.Status.ToString()), NotifyType.ErrorMessage);
                throw new Exception();
            }

            deviceOutputNode = deviceOutputNodeResult.DeviceOutputNode;
            // Create the FrameInputNode at the same format as the graph, except explicitly set mono.
            AudioEncodingProperties nodeEncodingProperties = graph.EncodingProperties;
            nodeEncodingProperties.ChannelCount = 1;
            //nodeEncodingProperties.SampleRate = 44100; this doesnt work?
            frameInputNode = graph.CreateFrameInputNode(nodeEncodingProperties);
            frameInputNode.AddOutgoingConnection(deviceOutputNode);

            // Initialize the Frame Input Node in the stopped state
            frameInputNode.Stop();

            // Hook up an event handler so we can start generating samples when needed
            // This event is triggered when the node is required to provide data
            frameInputNode.QuantumStarted += node_QuantumStarted;

            // Start the graph since we will only start/stop the frame input node
            graph.Start();
        }

        private void node_QuantumStarted(AudioFrameInputNode sender, FrameInputNodeQuantumStartedEventArgs args) {
            // GenerateAudioData can provide PCM audio data by directly synthesizing it or reading from a file.
            // Need to know how many samples are required. In this case, the node is running at the same rate as the rest of the graph
            // For minimum latency, only provide the required amount of samples. Extra samples will introduce additional latency.
            uint numSamplesNeeded = (uint)args.RequiredSamples;

            if (numSamplesNeeded != 0) {
                AudioFrame audioData = GenerateAudioData(numSamplesNeeded);
                frameInputNode.AddFrame(audioData);
            }
        }
    }
}
