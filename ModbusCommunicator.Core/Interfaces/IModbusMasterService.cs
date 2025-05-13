using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModbusCommunicator.Core.Interfaces
{
    public interface IModbusMasterService
    {
        Task ConnectAsync();
        Task DisConnectAsync();

        bool IsConnected { get; }

        Task WriteMultipleCoilsAsync(byte slaveAddress, ushort startAddress, bool[] data);
        Task WriteMultipleRegistersAsync(byte slaveAddress, ushort startAddress, ushort[] data);
        Task WriteSingleRegisterAsync(byte slaveAddress, ushort registerAddress, ushort value);
        Task WriteSingleCoilAsync(byte slaveAddress, ushort coilAddress, bool value);
        Task<ushort[]> ReadInputRegistersAsync(byte slaveAddress, ushort startAddress, ushort numberOfPoints);
        Task<ushort[]> ReadHoldingRegistersAsync(byte slaveAddress, ushort startAddress, ushort numberOfPoints);
        Task<bool[]> ReadInputsAsync(byte slaveAddress, ushort startAddress, ushort numberOfPoints);
        Task<bool[]> ReadCoilsAsync(byte slaveAddress, ushort startAddress, ushort numberOfPoints);
    }
}
