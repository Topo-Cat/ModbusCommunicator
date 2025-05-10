using ModbusCommunicator.Core.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModbusCommunicator.Core.Interfaces
{
    public interface IModbusSlaveServiceFactory
    {
        IModbusSlaveService CreateSlaveService(SlaveListenerConfig slaveConfig);
    }
}
