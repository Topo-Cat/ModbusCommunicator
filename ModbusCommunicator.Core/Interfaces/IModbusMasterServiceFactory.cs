using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModbusCommunicator.Core.Interfaces
{
    public interface IModbusMasterServiceFactory
    {
        IModbusMasterService CreatMaster();
    }
}
