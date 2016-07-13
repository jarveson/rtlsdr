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
            // todo: search all, oops
            string selector = UsbDevice.GetDeviceSelector(enums.Devices.KnownDevices[1].vid, enums.Devices.KnownDevices[1].pid);
             return await DeviceInformation.FindAllAsync(selector);

            /*for (int x = 1; x < enums.Devices.KnownDevices.Length; ++x) {
                selector = UsbDevice.GetDeviceSelector(enums.Devices.KnownDevices[x].vid, enums.Devices.KnownDevices[x].pid);
                var col = await DeviceInformation.FindAllAsync(selector);
                devices.Concat(col);
            }

            return devices;            */
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
                Index = wIndex,
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
                Index = wIndex,
            };


            uint bytesTransferred = await usbDevice.SendControlOutTransferAsync(setupPacket, bufferToSend);
            if (bytesTransferred == wLength) {
                return (int)bytesTransferred;
            }
            else {
                return -1;
            }
        }

        public async Task<byte[]> BulkReadDefault(uint size) {
            uint bytesRead = 0;
            UsbBulkInPipe readPipe = usbDevice.DefaultInterface.BulkInPipes[0];
            readPipe.ReadOptions = UsbReadOptions.AutoClearStall | UsbReadOptions.OverrideAutomaticBufferManagement;
            //readPipe.FlushBuffer();
            var stream = readPipe.InputStream;
            DataReader reader = new DataReader(stream);

            try {
                bytesRead = await reader.LoadAsync(size);// readPipe.MaxTransferSizeBytes);
            }
            catch (Exception e) {
                throw e;
            }

            //byte[] data = reader.ReadBuffer(bytesRead).ToArray();
            //return data;
            byte[] rtnData = new byte[bytesRead];
            reader.ReadBytes(rtnData);
            return rtnData;
        }

        public void Dispose() {
            usbDevice.Dispose();
        }
    }
}
