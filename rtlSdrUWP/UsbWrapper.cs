using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Usb;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;

namespace rtlSdrUWP {
    public sealed class UsbWrapper : IDisposable {
        private UsbDevice usbDevice;

        public async Task<bool> ConnectToDevice(string id) {
            try {
                usbDevice = await UsbDevice.FromIdAsync(id);
                return true;
            }
            catch {
                return false;
            }
        }

        public static async Task<DeviceInformationCollection> FindAllDevices() {
            return await DeviceInformation.FindAllAsync();
        }

        public async Task<byte[]> ControlTransferIn(byte bRequest, ushort wValue, ushort wIndex, ushort wLength) {
            var buffer = new Windows.Storage.Streams.Buffer(wLength);

            UsbSetupPacket setupPacket = new UsbSetupPacket {
                RequestType = new UsbControlRequestType {
                    Direction = UsbTransferDirection.In,
                    Recipient = UsbControlRecipient.Device,
                    ControlTransferType = UsbControlTransferType.Vendor,
                },
                Request = bRequest,
                Value = wValue,
                Length = wLength,
            };

            var ret = await usbDevice.SendControlInTransferAsync(setupPacket, buffer);
            return ret.ToArray();
        }

        public async Task<int> ControlTransferOut(byte bRequest, ushort wValue,
            ushort wIndex, byte[] data, ushort wLength) {

            DataWriter writer = new DataWriter();
            writer.WriteBytes(data);
            var bufferToSend = writer.DetachBuffer();

            UsbSetupPacket setupPacket = new UsbSetupPacket {
                RequestType = new UsbControlRequestType {
                    Direction = UsbTransferDirection.Out ,
                    Recipient = UsbControlRecipient.Device,
                    ControlTransferType = UsbControlTransferType.Vendor,
                },
                Request = bRequest,
                Value = wValue,
                Length = wLength,
            };


            uint bytesTransferred = await usbDevice.SendControlOutTransferAsync(setupPacket, bufferToSend);
            return (int)bytesTransferred;
        }

        public async Task<byte[]> BulkReadDefault(uint size) {
            uint bytesRead = 0;
            UsbBulkInPipe readPipe = usbDevice.DefaultInterface.BulkInPipes[0];
            readPipe.ReadOptions |= UsbReadOptions.AutoClearStall | UsbReadOptions.OverrideAutomaticBufferManagement;

            var stream = readPipe.InputStream;
            DataReader reader = new DataReader(stream);

            try {
                bytesRead = await reader.LoadAsync(readPipe.MaxTransferSizeBytes);
            }
            catch (Exception e) {
                throw e;
            }

            byte[] rtnData = new byte[bytesRead];
            reader.ReadBytes(rtnData);
            return rtnData;
        }

        public void Dispose() {
            usbDevice.Dispose();
        }
    }
}
