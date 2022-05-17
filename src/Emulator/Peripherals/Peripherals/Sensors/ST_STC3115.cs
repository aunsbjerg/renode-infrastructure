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
    // TODO use I2CPeripheralBase<ST_STC3115.Registers> ??
    public class ST_STC3115 : II2CPeripheral, ISensor, ITemperatureSensor, IProvidesRegisterCollection<ByteRegisterCollection>
    {
        public ST_STC3115(uint senseResistance, uint batteryInternalResistance)
        {
            RegistersCollection = new ByteRegisterCollection(this);
            DefineRegisters();

            // TODO alert gpios
            // flag control, e.g. batfail, POR, reset etc.

            this.senseResistance = senseResistance;
            this.batteryInternalResistance = batteryInternalResistance;
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
                    RegistersCollection.Write((byte)regAddress, b);
                    regAddress = (Registers)((int)regAddress + 1);
                }
            }
            else
            {
                this.Log(LogLevel.Noisy, "Preparing to read register {0} (0x{0:X})", regAddress);
            }
        }

        public byte[] Read(int count)
        {
            switch (regAddress)
            {
                case Registers.RAM:
                    count = (int)RAM_SIZE;
                    break;
                case Registers.OCVAdjustmentTable:
                    count = (int)OCV_TABLE_SIZE;
                    break;
                case Registers.StateOfChargeLow:
                case Registers.CounterLow:
                case Registers.CurrentLow:
                case Registers.VoltageLow:
                case Registers.CoulombModeAdjustLow:
                case Registers.VoltageModeAdjustLow:
                case Registers.OCVLow:
                case Registers.CoulombModeConfigLow:
                case Registers.VoltageModeConfigLow:
                    count = 2;
                    break;
            }

            this.Log(LogLevel.Noisy, "Reading {0} bytes from {1} (0x{1:X})", count, regAddress);
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

        public decimal Temperature
        {
            // LSB value is 1 deg. Celsius
            // TODO not tested

            get => (decimal)temperature.Value;

            set
            {
                temperature.Value = (uint)value;
            }
        }

        public decimal StateOfCharge
        {
            // LSB value 1/512%

            get => (stateOfChargeLow.Value | (stateOfChargeHigh.Value << 8)) / 512;

            set
            {
                int temp = (int)(value * 512);

                stateOfChargeLow.Value = (byte)temp;
                stateOfChargeHigh.Value = (byte)(temp >> 8);
            }
        }

        public decimal Voltage
        {
            // LSB value is 2.20mV

            get => ((voltageLow.Value | (voltageHigh.Value << 8)) * 2.20m) / 1000;

            set
            {
                int temp = (int)((value * 1000) / 2.20m);

                voltageLow.Value = (byte)temp;
                voltageHigh.Value = (byte)(temp >> 8);
            }
        }

        public decimal Current
        {
            // LSB value is 5.88uV / Rsense
            // TODO negative values not working

            get => ((currentLow.Value | (currentHigh.Value << 8)) * 5.88m) / ((int)senseResistance * 1000);

            set
            {
                int temp = (int)((value * (int)senseResistance * 1000) / 5.88m);

                currentLow.Value = (byte)temp;
                currentHigh.Value = (byte)(temp >> 8);
            }
        }

        private uint senseResistance;
        private uint batteryInternalResistance;

        private const uint RAM_SIZE = 16;
        private const uint OCV_TABLE_SIZE = 16;

        private IValueRegisterField temperature;
        private IValueRegisterField stateOfChargeLow;
        private IValueRegisterField stateOfChargeHigh;
        private IValueRegisterField voltageLow;
        private IValueRegisterField voltageHigh;
        private IValueRegisterField currentLow;
        private IValueRegisterField currentHigh;
        private IValueRegisterField[] RAMContent = new IValueRegisterField[RAM_SIZE];
        private IValueRegisterField[] OCVContent = new IValueRegisterField[OCV_TABLE_SIZE];
        private IFlagRegisterField gasGaugeRun;
        private Registers regAddress;

        private void DefineRegisters()
        {
            Registers.Mode.Define(this)
                .WithTaggedFlag("VoltageMode", 0)
                .WithTaggedFlag("ClearVoltageModeAdjust", 1)
                .WithTaggedFlag("ClearCoulombModeAdjust", 2)
                .WithTaggedFlag("AlarmEnable", 3)
                .WithFlag(4, out gasGaugeRun, name: "GasGaugeRun")
                .WithTaggedFlag("ForceCoulombMode", 5)
                .WithTaggedFlag("ForceVoltageMode", 6)
                .WithIgnoredBits(7, 1);

            Registers.Ctrl.Define(this)
                .WithTaggedFlag("IO0Data", 0)
                .WithTaggedFlag("GasGaugeReset", 1)
                .WithTaggedFlag("GasGaugeVoltageMode", 2)
                .WithTaggedFlag("BatteryFail", 3)
                .WithTaggedFlag("PORDetected", 4)
                .WithTaggedFlag("AlarmStateOfCharge", 5)
                .WithTaggedFlag("AlarmVoltage", 6);

            Registers.StateOfChargeLow.Define(this, 0x00)
                .WithValueField(0, 8, out stateOfChargeLow, name: "StateOfChargeLow");

            Registers.StateOfChargeHigh.Define(this, 0x00)
                .WithValueField(0, 8, out stateOfChargeHigh, name: "StateOfChargeHigh");

            Registers.CounterLow.Define(this, 0x05)
                .WithTag("CounterLow", 0, 8);

            Registers.CounterHigh.Define(this, 0x00)
                .WithTag("CounterHigh", 0, 8);

            Registers.CurrentLow.Define(this, 0x00)
                .WithValueField(0, 8, out currentLow, FieldMode.Read, name: "CurrentLow");

            Registers.CurrentHigh.Define(this, 0x00)
                .WithValueField(0, 8, out currentHigh, FieldMode.Read, name: "CurrentHigh");

            Registers.VoltageLow.Define(this, 0x00)
                .WithValueField(0, 8, out voltageLow, FieldMode.Read, name: "VoltageLow");

            Registers.VoltageHigh.Define(this, 0x00)
                .WithValueField(0, 8, out voltageHigh, FieldMode.Read, name: "VoltageHigh");

            Registers.Temperature.Define(this, 0x00)
                .WithValueField(0, 8, out temperature, FieldMode.Read, name: "Temperature");

            Registers.CoulombModeAdjustHigh.Define(this, 0x00)
                .WithTag("CoulombModeAdjustHigh", 0, 8);

            Registers.CoulombModeAdjustLow.Define(this, 0x00)
                .WithTag("CoulombModeAdjustLow", 0, 8);

            Registers.VoltageModeAdjustHigh.Define(this, 0x00)
                .WithTag("VoltageModeAdjustHigh", 0, 8);

            Registers.VoltageModeAdjustLow.Define(this, 0x00)
                .WithTag("VoltageModeAdjustLow", 0, 8);

            Registers.OCVLow.Define(this, 0x00)
                .WithTag("OCVLow", 0, 8);

            Registers.OCVHigh.Define(this, 0x00)
                .WithTag("OCVHigh", 0, 8);

            Registers.CoulombModeConfigLow.Define(this, 0x01)
                .WithTag("CoulombModeConfigLow", 0, 8);

            Registers.CoulombModeConfigHigh.Define(this, 0x8B)
                .WithTag("CoulombModeConfigHigh", 0, 8);

            Registers.VoltageModeConfigLow.Define(this, 0x01)
                .WithTag("VoltageModeConfigLow", 0, 8);

            Registers.VoltageModeConfigHigh.Define(this, 0x41)
                .WithTag("VoltageModeConfigHigh", 0, 8);

            Registers.AlarmStateOfCharge.Define(this, 0x02)
                .WithTag("AlarmStateOfCharge", 0, 8);

            Registers.AlarmVoltage.Define(this, 0xAA)
                .WithTag("AlarmVoltage", 0, 8);

            Registers.CurrentThreshold.Define(this, 0x0A)
                .WithTag("CurrentThreshold", 0, 8);

            Registers.RelaxCount.Define(this, 0x78)
                .WithTag("RelaxCount", 0, 8);

            Registers.RelaxMax.Define(this, 0x78)
                .WithTag("RelaxMax", 0, 8);

            Registers.Id.Define(this, 0x14);

            Registers.RAM.Define8Many(this, RAM_SIZE, setup: (reg, idx) =>
                reg.WithValueField(0, 8, out RAMContent[idx], name: $"RAM{idx}"));

            Registers.OCVAdjustmentTable.Define8Many(this, OCV_TABLE_SIZE, setup: (reg, idx) =>
                reg.WithValueField(0, 8, out OCVContent[idx], name: $"OCV{idx}"));
        }

        [RegisterMapper.RegistersDescription]
        private enum Registers : byte
        {
            Mode = 0x00,
            Ctrl = 0x01,
            StateOfChargeLow = 0x02,
            StateOfChargeHigh = 0x03,
            CounterLow = 0x04,
            CounterHigh = 0x05,
            CurrentLow = 0x06,
            CurrentHigh = 0x07,
            VoltageLow = 0x08,
            VoltageHigh = 0x09,
            Temperature = 0x0A,
            CoulombModeAdjustHigh = 0x0B,
            CoulombModeAdjustLow = 0x19,
            VoltageModeAdjustHigh = 0x0C,
            VoltageModeAdjustLow = 0x1A,
            OCVLow = 0x0D,
            OCVHigh = 0x0E,
            CoulombModeConfigLow = 0x0F,
            CoulombModeConfigHigh = 0x10,
            VoltageModeConfigLow = 0x11,
            VoltageModeConfigHigh = 0x12,
            AlarmStateOfCharge = 0x13,
            AlarmVoltage = 0x14,
            CurrentThreshold = 0x15,
            RelaxCount = 0x16,
            RelaxMax = 0x17,
            Id = 0x18,
            RAM = 0x20,
            OCVAdjustmentTable = 0x30,
        }
    }
}