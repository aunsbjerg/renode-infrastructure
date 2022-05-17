//
// Copyright (c) 2022 Esco Medical ApS
//
// This file is licensed under the MIT License.
// Full license text is available in 'licenses/MIT.txt'.
//

using System;
using System.Linq;
using Antmicro.Renode.Core.Structure.Registers;
using Antmicro.Renode.Logging;
using Antmicro.Renode.Peripherals.Bus.Wrappers;
using Antmicro.Renode.Peripherals.I2C;
using Antmicro.Renode.Peripherals.Sensor;
using Antmicro.Renode.Utilities;

namespace Antmicro.Renode.Peripherals.Sensors
{
    // see HiMaxHM01B0
    // TODO use I2CPeripheralBase<TI_TMP116.Registers> ??
    public class TI_TMP116 : II2CPeripheral, ITemperatureSensor, IProvidesRegisterCollection<WordRegisterCollection>
    {
        public TI_TMP116()
        {
            RegistersCollection = new WordRegisterCollection(this);
            DefineRegisters();
        }

        public void Reset()
        {
            RegistersCollection.Reset();
            regAddress = 0;
        }

        public void Write(byte[] data)
        {
            if (data.Length == 0)
            {
                this.Log(LogLevel.Warning, "Unexpected write with no data");
                return;
            }

            regAddress = (Registers)data[0];
            this.Log(LogLevel.Noisy, "Setting register ID to 0x{0:X}", regAddress);

            if (data.Length > 1)
            {
                this.Log(LogLevel.Noisy, "Handling register write");

                foreach (var b in data.Skip(1))
                {
                    RegistersCollection.Write((byte)regAddress, b);
                }
            }
        }

        public byte[] Read(int count)
        {
            var temp = RegistersCollection.Read((byte)regAddress);
            var result = BitConverter.GetBytes(temp).Reverse().ToArray();
            // Array.Resize(ref result, result.Length + 1);

            this.Log(LogLevel.Noisy, "Register value 0x{0:X}, reading {1} from register 0x{2:X}",
                     temp, Misc.PrettyPrintCollectionHex(result), regAddress);

            return result;
        }

        public void FinishTransmission()
        {
        }

        public WordRegisterCollection RegistersCollection { get; }

        // TODO add IValueRegisterField and wrap w. Temperature property
        public decimal Temperature { get; set; }

        private ushort ConvertTemperature(decimal temperature)
        {
            // see datasheet 7.6.1.1 for details
            int temp = (int)((temperature / 7.8125m) * 1000);
            return (ushort)temp;
        }

        private void DefineRegisters()
        {
            // TODO convert ignored bits to Tag fields as per docs
            Registers.Temperature.Define(this)
                .WithValueField(0, 16, FieldMode.Read,
                    valueProviderCallback: _ => ConvertTemperature(Temperature));
            
            Registers.Configuration.Define(this)
                .WithReservedBits(0, 1)
                .WithIgnoredBits(1, 11)
                .WithFlag(13, FieldMode.Read, name: "DATA_READY", valueProviderCallback: _ => true)
                .WithIgnoredBits(14, 2);
            
            Registers.HighLimit.Define(this)
                .WithIgnoredBits(0, 16);
            
            Registers.LowLimit.Define(this)
                .WithIgnoredBits(0, 16);
            
            Registers.EepromUnlock.Define(this)
                .WithIgnoredBits(0, 16);
            
            Registers.Eeprom1.Define(this)
                .WithIgnoredBits(0, 16);
            
            Registers.Eeprom2.Define(this)
                .WithIgnoredBits(0, 16);
            
            Registers.Eeprom3.Define(this)
                .WithIgnoredBits(0, 16);
            
            Registers.Eeprom4.Define(this)
                .WithIgnoredBits(0, 16);
                
            Registers.DeviceId.Define(this, 0x1116);
        }

        private Registers regAddress;

        [RegisterMapper.RegistersDescription]
        private enum Registers : byte
        {
            Temperature = 0x00,
            Configuration = 0x01,
            HighLimit = 0x02,
            LowLimit = 0x03,
            EepromUnlock = 0x04,
            Eeprom1 = 0x05,
            Eeprom2 = 0x06,
            Eeprom3 = 0x07,
            Eeprom4 = 0x08,
            DeviceId = 0x0F,
        }
    }
}