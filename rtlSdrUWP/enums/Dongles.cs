using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rtlSdrUWP.enums {
    public static class Devices {
        public struct Dongle {
            public ushort vid;
            public ushort pid;
            public string name;
        }

        public static Dongle[] KnownDevices = {
            new Dongle { vid = 0x0bda, pid = 0x2832, name="Generic RTL2832U"},
            new Dongle { vid = 0x0bda, pid = 0x2838, name="Generic RTL2832U OEM" },
            new Dongle { vid = 0x0413, pid = 0x6680, name="DigitalNow Quad DVB-T PCI-E card" },
            new Dongle { vid = 0x0413, pid = 0x6f0f, name="Leadtek WinFast DTV Dongle mini D" },
            new Dongle { vid = 0x0458, pid = 0x707f, name="Genius TVGo DVB-T03 USB dongle (Ver. B)" },
            new Dongle { vid = 0x0ccd, pid = 0x00a9, name="Terratec Cinergy T Stick Black (rev 1)" },
            new Dongle { vid = 0x0ccd, pid = 0x00b3, name="Terratec NOXON DAB/DAB+ USB dongle (rev 1)" },
            new Dongle { vid = 0x0ccd, pid = 0x00b4, name="Terratec Deutschlandradio DAB Stick" },
            new Dongle { vid = 0x0ccd, pid = 0x00b5, name="Terratec NOXON DAB Stick - Radio Energy" },
            new Dongle { vid = 0x0ccd, pid = 0x00b7, name="Terratec Media Broadcast DAB Stick" },
            new Dongle { vid = 0x0ccd, pid = 0x00b8, name="Terratec BR DAB Stick" },
            new Dongle { vid = 0x0ccd, pid = 0x00b9, name="Terratec WDR DAB Stick" },
            new Dongle { vid = 0x0ccd, pid = 0x00c0, name="Terratec MuellerVerlag DAB Stick" },
            new Dongle { vid = 0x0ccd, pid = 0x00c6, name="Terratec Fraunhofer DAB Stick" },
            new Dongle { vid = 0x0ccd, pid = 0x00d3, name="Terratec Cinergy T Stick RC (Rev.3)" },
            new Dongle { vid = 0x0ccd, pid = 0x00d7, name="Terratec T Stick PLUS" },
            new Dongle { vid = 0x0ccd, pid = 0x00e0, name="Terratec NOXON DAB/DAB+ USB dongle (rev 2)" },
            new Dongle { vid = 0x1554, pid = 0x5020, name="PixelView PV-DT235U(RN)" },
            new Dongle { vid = 0x15f4, pid = 0x0131, name="Astrometa DVB-T/DVB-T2" },
            new Dongle { vid = 0x185b, pid = 0x0620, name="Compro Videomate U620F"},
            new Dongle { vid = 0x185b, pid = 0x0650, name="Compro Videomate U650F"},
            new Dongle { vid = 0x185b, pid = 0x0680, name="Compro Videomate U680F"},
            new Dongle { vid = 0x1b80, pid = 0xd393, name="GIGABYTE GT-U7300" },
            new Dongle { vid = 0x1b80, pid = 0xd394, name="DIKOM USB-DVBT HD" },
            new Dongle { vid = 0x1b80, pid = 0xd395, name="Peak 102569AGPK" },
            new Dongle { vid = 0x1b80, pid = 0xd397, name="KWorld KW-UB450-T USB DVB-T Pico TV" },
            new Dongle { vid = 0x1b80, pid = 0xd398, name="Zaapa ZT-MINDVBZP" },
            new Dongle { vid = 0x1b80, pid = 0xd39d, name="SVEON STV20 DVB-T USB & FM" },
            new Dongle { vid = 0x1b80, pid = 0xd3a4, name="Twintech UT-40" },
            new Dongle { vid = 0x1b80, pid = 0xd3a8, name="ASUS U3100MINI_PLUS_V2" },
            new Dongle { vid = 0x1b80, pid = 0xd3af, name="SVEON STV27 DVB-T USB & FM" },
            new Dongle { vid = 0x1b80, pid = 0xd3b0, name="SVEON STV21 DVB-T USB & FM" },
            new Dongle { vid = 0x1d19, pid = 0x1101, name="Dexatek DK DVB-T Dongle (Logilink VG0002A)" },
            new Dongle { vid = 0x1d19, pid = 0x1102, name="Dexatek DK DVB-T Dongle (MSI DigiVox mini II V3.0)" },
            new Dongle { vid = 0x1d19, pid = 0x1103, name="Dexatek Technology Ltd. DK 5217 DVB-T Dongle" },
            new Dongle { vid = 0x1d19, pid = 0x1104, name="MSI DigiVox Micro HD" },
            new Dongle { vid = 0x1f4d, pid = 0xa803, name="Sweex DVB-T USB" },
            new Dongle { vid = 0x1f4d, pid = 0xb803, name="GTek T803" },
            new Dongle { vid = 0x1f4d, pid = 0xc803, name="Lifeview LV5TDeluxe" },
            new Dongle { vid = 0x1f4d, pid = 0xd286, name="MyGica TD312" },
            new Dongle { vid = 0x1f4d, pid = 0xd803, name="PROlectrix DV107669" },
        };
    }
}
