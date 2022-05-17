//
// Copyright (c) 2022 Esco Medical ApS
//
// This file is licensed under the MIT License.
// Full license text is available in 'licenses/MIT.txt'.
//

using Antmicro.Renode.Backends.Display;
using Antmicro.Renode.Core;
using Antmicro.Renode.Core.Structure.Registers;
using Antmicro.Renode.Logging;
using Antmicro.Renode.Peripherals.SPI;
using System;

namespace Antmicro.Renode.Peripherals.Video
{
    public class ST7789V : AutoRepaintingVideo, IProvidesRegisterCollection<ByteRegisterCollection>, ISPIPeripheral
    {
        /*
        use datasheet and zephyr driver to figure out emulator implementation.
        look at other renode video drivers to figure out renode API

        reset:
            reset gpio high for 6ms
            reset gpio low for 20ms
            receive reset sw cmd
             -> resets display

        write display data:
            set memory area command
            send ram write command
            send pixel data (variable size)

        misc:
            lcd margins (offsets)
            enable command
            porch command
            digital gamma 
            etc

        see  for inspiration:
            src/Infrastructure/src/Emulator/Peripherals/Peripherals/Sensors/MC3635.cs
            https://github.com/sergeykhbr/riscv_vhdl/blob/master/debugger/src/cpu_arm_plugin/stm32l4/st7789v.h

        */
        public ST7789V(Machine machine, int? height = null, int? width = null) : base(machine)
        {
            Reconfigure(width ?? defaultWidth, height ?? defaultHeight);

            internalLock = new object();

            RegistersCollection = new ByteRegisterCollection(this);
            DefineRegisters();
        }

        public byte Transmit(byte data)
        {
            return 0;
        }

        public void FinishTransmission()
        {

        }

        public override void Reset()
        {
            RegistersCollection.Reset();
        }

        protected override void Repaint()
        {

        }

        public ByteRegisterCollection RegistersCollection { get; }

        private void DefineRegisters()
        {

        }

        private enum Registers : byte
        {

        }

        private readonly object internalLock;
        private const int defaultWidth = 320;
        private const int defaultHeight = 240;
    }
}