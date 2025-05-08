using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModbusCommunicator.Modbus.Core.Interfaces
{
    public interface IModbusSlave
    {
        Task StartSlaveAsync();
        Task StopSlaveAsync();

        bool IsSlaveReady { get; }
        bool ConnectedCount { get; }

        Task SetMultipleCoilsAsync(ushort startAddress, bool[] data);
        Task SetMultipleRegistersAsync(ushort startAddress, ushort[] data);
        Task SetSingleRegisterAsync(ushort registerAddress, ushort value);
        Task SetSingleCoilAsync(ushort coilAddress, bool value);
        Task<ushort[]> GetInputRegistersAsync(ushort startAddress, ushort numberOfPoints);
        Task<ushort[]> GetHoldingRegistersAsync(ushort startAddress, ushort numberOfPoints);
        Task<bool[]> GetInputsAsync(ushort startAddress, ushort numberOfPoints);
        Task<bool[]> GetCoilsAsync(ushort startAddress, ushort numberOfPoints);
    }
}
