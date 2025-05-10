using ModbusCommunicator.Core.Interfaces;
using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModbusCommunicator.NModbusTcp
{
    public class ModbusMasterServiceFactory : IModbusMasterServiceFactory
    {
        IContainerProvider containerProvider;
    }
}
