// //
// // Copyright (c) 2022 Esco Medical ApS
// //
// // This file is licensed under the MIT License.
// // Full license text is available in 'licenses/MIT.txt'.
// //

// using System.Linq;
// using Antmicro.Renode.Core.Structure.Registers;
// using Antmicro.Renode.Logging;
// using Antmicro.Renode.Peripherals.Bus.Wrappers;
// using Antmicro.Renode.Peripherals.SPI;
// using Antmicro.Renode.Peripherals.Sensor;
// using Antmicro.Renode.Utilities;

// namespace Antmicro.Renode.Peripherals.Sensors
// {
//     // TODO use I2CPeripheralBase<ICM42670.Registers> ??
//     public class ICM42670 : ISPIPeripheral, ISensor, ITemperatureSensor, IProvidesRegisterCollection<ByteRegisterCollection>
//     {
//         public ICM42670()
//         {
//             RegistersCollection = new ByteRegisterCollection(this);
//             DefineRegisters();
//         }

//         public void Reset()
//         {

//             RegistersCollection.Reset();
//             regAddress = 0;
//         }

//         public void Write(byte[] data)
//         {
//             if (data.Length == 0)
//             {
//                 this.Log(LogLevel.Warning, "Unexpected write with no data");
//                 return;
//             }

//             regAddress = (Registers)data[0];
//             this.Log(LogLevel.Noisy, "Write with {0} bytes of data: {1}", data.Length,
//                 Misc.PrettyPrintCollectionHex(data));

//             if (data.Length > 1)
//             {
//                 foreach (var b in data.Skip(1))
//                 {
//                     RegistersCollection.Write((byte)regAddress, b);
//                     regAddress = (Registers)((int)regAddress + 1);
//                 }
//             }
//             else
//             {
//                 this.Log(LogLevel.Noisy, "Preparing to read register {0} (0x{0:X})", regAddress);
//             }
//         }

//         public byte[] Read(int count)
//         {
//             this.Log(LogLevel.Noisy, "Reading {0} bytes from {1} (0x{1:X})", count, regAddress);
//             var result = new byte[count];

//             for (var i = 0; i < result.Length; i++)
//             {
//                 result[i] = RegistersCollection.Read((byte)regAddress);
//                 regAddress = (Registers)((int)regAddress + 1);
//             }

//             return result;
//         }

//         public byte Transmit(byte data)
//         {
//             // TODO SPI 
//             return 0;
//         }

//         public void FinishTransmission()
//         {

//         }

//         public ByteRegisterCollection RegistersCollection { get; }

//         public decimal Temperature
//         {
//             // LSB value is 1 deg. Celsius
//             // TODO not tested

//             get => (decimal)temperature.Value;

//             set 
//             { 
//                 temperature.Value = (uint)value; 
//             }
//         }

//         private IValueRegisterField temperature;
//         private Registers regAddress;

//         private void DefineRegisters()
//         {
         
//         }

//         // TODO support the multiple banks setup of the ICM.
//         // maybe separate register enums for each of the banks and a bank address variable

//         [RegisterMapper.RegistersDescription]
//         private enum Registers : byte
//         {
 
//         }
//     }
// }