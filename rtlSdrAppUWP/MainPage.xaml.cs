using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page {
        public MainPage() {
            this.InitializeComponent();
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e) {
            var devices = await rtlSdrUWP.Device.GetDevices();
            foreach (var device in devices) {
                System.Diagnostics.Debug.WriteLine(device.Name);
            }

            rtlSdrUWP.Device dev = new rtlSdrUWP.Device();
            //rtlSdrUWP.tuners.R82XX r8 = new rtlSdrUWP.tuners.R82XX();

            bool res = await dev.OpenDevice(devices[0].Id);
            //bool res = await r8.OpenDevice(devices[0].Id);
            if (!res) {
                throw new Exception();
            }

            // manual gain mode
            await dev.SetOffsetTuning(false);
            await dev.SetFreqCorrection(0);
            await dev.SetAgcMode(false);

            await dev.SetSampleRate(2400000);
            await dev.SetCenterFreq(97100000);
            await dev.SetTunerGainMode(1);
            await dev.SetTunerGain(254);

            //await dev.SetTunerBandwidth(250000);

            await dev.ResetBuffer();

            var rawr = await dev.ReadAsync();

            foreach (var ra in rawr) {
                //System.Diagnostics.Debug.WriteLine("");
            }
            rawr = await dev.ReadAsync();

            foreach (var ra in rawr) {
                //System.Diagnostics.Debug.WriteLine("");
            }
            rawr = await dev.ReadAsync();
            foreach (var ra in rawr) {
                //System.Diagnostics.Debug.WriteLine("");
            }
            rawr = await dev.ReadAsync();
            foreach (var ra in rawr) {
                System.Diagnostics.Debug.WriteLine("");
            }
        }
    }
}
