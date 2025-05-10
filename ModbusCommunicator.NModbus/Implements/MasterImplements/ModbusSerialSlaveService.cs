using ModbusCommunicator.Core.Abstractions;
using ModbusCommunicator.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModbusCommunicator.NModbus.MasterImplements.FactoryImplement
{
    public class ModbusSerialSlaveService : IModbusSlaveService
    {
        public void Initialize(SlaveListenerConfig serialConfig)
        {

        }

        public bool IsSlaveReady => throw new NotImplementedException();

        public int ConnectedCount => throw new NotImplementedException();

        public Task<bool[]> GetCoilsAsync(ushort startAddress, ushort numberOfPoints)
        {
            throw new NotImplementedException();
        }

        public Task<ushort[]> GetHoldingRegistersAsync(ushort startAddress, ushort numberOfPoints)
        {
            throw new NotImplementedException();
        }

        public Task<ushort[]> GetInputRegistersAsync(ushort startAddress, ushort numberOfPoints)
        {
            throw new NotImplementedException();
        }

        public Task<bool[]> GetInputsAsync(ushort startAddress, ushort numberOfPoints)
        {
            throw new NotImplementedException();
        }

        public Task SetMultipleCoilsAsync(ushort startAddress, bool[] data)
        {
            throw new NotImplementedException();
        }

        public Task SetMultipleRegistersAsync(ushort startAddress, ushort[] data)
        {
            throw new NotImplementedException();
        }

        public Task SetSingleCoilAsync(ushort coilAddress, bool value)
        {
            throw new NotImplementedException();
        }

        public Task SetSingleRegisterAsync(ushort registerAddress, ushort value)
        {
            throw new NotImplementedException();
        }

        public Task StartSlaveAsync()
        {
            throw new NotImplementedException();
        }

        public Task StopSlaveAsync()
        {
            throw new NotImplementedException();
        }
    }
}
