//
// Copyright (c) 2022 Esco Medical ApS
//
// This file is licensed under the MIT License.
// Full license text is available in 'licenses/MIT.txt'.
//

using System.Linq;
using Antmicro.Renode.Core.Structure.Registers;
using Antmicro.Renode.Logging;
using Antmicro.Renode.Peripherals.Bus.Wrappers;
using Antmicro.Renode.Peripherals.I2C;
using Antmicro.Renode.Peripherals.Sensor;
using Antmicro.Renode.Utilities;

namespace Antmicro.Renode.Peripherals.Sensors
{
    // TODO use I2CPeripheralBase<TI_BQ2477X.Registers> ??
    public class TI_BQ2477X : II2CPeripheral, ISensor, IProvidesRegisterCollection<ByteRegisterCollection>
    {
        public TI_BQ2477X()
        {
            // TODO support ACOK and BAT_PRESENT gpio callbacks
            RegistersCollection = new ByteRegisterCollection(this);
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
            this.Log(LogLevel.Noisy, "Write with {0} bytes of data: {1}", data.Length,
                Misc.PrettyPrintCollectionHex(data));

            if (data.Length > 1)
            {
                foreach (var b in data.Skip(1))
                {
                    this.Log(LogLevel.Noisy, "Writing 0x{0:X} to {1} (0x{1:X})", b, regAddress);
                    RegistersCollection.Write((byte)regAddress, b);
                }
            }
            else
            {
                this.Log(LogLevel.Noisy, "Preparing to read register {0} (0x{0:X})", regAddress);
            }
        }

        public byte[] Read(int count)
        {
            this.Log(LogLevel.Noisy, "Reading {0} bytes from  {1} (0x{1:X})", count, regAddress);
            var result = new byte[count];

            for (var i = 0; i < result.Length; i++)
            {
                result[i] = RegistersCollection.Read((byte)regAddress);
                regAddress = (Registers)((int)regAddress + 1);
            }

            return result;
        }

        public void FinishTransmission()
        {

        }

        public ByteRegisterCollection RegistersCollection { get; }

        private IFlagRegisterField isChargeInhibited;
        private Registers regAddress;

        private void DefineRegisters()
        {
            // TODO add remaining registers...
            
            Registers.ChargeOption0Lsb.Define(this, 0x4E)
                .WithFlag(0, out isChargeInhibited, name: "ChargeInhibited")
                .WithTaggedFlag("IDPMEnable", 1)
                .WithReservedBits(2, 1, 1)
                .WithTaggedFlag("IBATAmpRatioDischCur", 3)
                .WithTaggedFlag("IADPAmpRatio", 4)
                .WithTaggedFlag("LEARNEnable", 5)
                .WithTaggedFlag("LSFET_OCPThreshold", 6)
                .WithTaggedFlag("ACOCSetting", 7);
            Registers.ChargeOption0Msb.Define(this, 0xE1)
                .WithTag("SwitchingFrequency", 0, 2)
                .WithTaggedFlag("AudioFrequencyLimit", 2)
                .WithTaggedFlag("SYSOVPStatusAndClear", 3)
                .WithTaggedFlag("IDPMAutoDisable", 4)
                .WithTag("WatchdogTimerAdjust", 5, 2)
                .WithTaggedFlag("LowPowerModeEnable", 7);
            
            Registers.ChargeOption1Lsb.Define(this, 0x11);
            Registers.ChargeOption1Msb.Define(this, 0x02);
            Registers.ChargeOption2Lsb.Define(this, 0x80);
            Registers.ChargeOption2Msb.Define(this, 0x00);
            Registers.ProchotOption0Lsb.Define(this, 0x54);
            Registers.ProchotOption0Msb.Define(this, 0x4B);
            Registers.ProchotOption1Lsb.Define(this, 0x20);
            Registers.ProchotOption1Msb.Define(this, 0x81);
            Registers.ChargeCurrentLsb.Define(this, 0x00);
            Registers.ChargeCurrentMsb.Define(this, 0x00);
            Registers.MaxChargeVoltageLsb.Define(this); // 4.4V
            Registers.MaxChargeVoltageMsb.Define(this); // 
            Registers.MinSysVoltage.Define(this); // 3.584V
            Registers.InputCurrent.Define(this); // 2944mA
            Registers.DeviceIdReg.Define(this)
                .WithValueField(0, 8, FieldMode.Read, valueProviderCallback: _ => 0x41);
        }

        // Note: MSB is transmitted first
        [RegisterMapper.RegistersDescription] // needed?
        private enum Registers : byte
        {
            ChargeOption0Msb = 0x00,
            ChargeOption0Lsb = 0x01,
            ChargeOption1Msb = 0x02,
            ChargeOption1Lsb = 0x03,
            ChargeOption2Msb = 0x10,
            ChargeOption2Lsb = 0x11,
            ProchotOption0Msb = 0x04,
            ProchotOption0Lsb = 0x05,
            ProchotOption1Msb = 0x06,
            ProchotOption1Lsb = 0x07,
            ChargeCurrentMsb = 0x0A,
            ChargeCurrentLsb = 0x0B,
            MaxChargeVoltageMsb = 0x0C,
            MaxChargeVoltageLsb = 0x0D,
            MinSysVoltage = 0x0E,
            InputCurrent = 0x0F,
            DeviceIdReg = 0x09,
        }
    }
}