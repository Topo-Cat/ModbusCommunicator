using Microsoft.Extensions.Logging;
using ModbusCommunicator.Core.Abstractions;
using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModbusCommunicator.Core.Interfaces
{
    public interface IModbusMasterServiceFactory
    {
        IModbusMasterService CreateMaster(MasterConnectionConfig config);
    }
}
