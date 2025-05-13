using Microsoft.Extensions.Logging;
using ModbusCommunicator.Core.Abstractions;
using ModbusCommunicator.Core.Abstractions.ModbusSerialConfig;
using ModbusCommunicator.Core.Abstractions.ModbusTcpConfig;
using ModbusCommunicator.Core.Interfaces;
using ModbusCommunicator.NModbus.MasterImplements;
using ModbusCommunicator.NModbusTcp;
using NModbus.Device;
using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModbusCommunicator.ModbusCommunicator.NModbus.MasterImplements.FactoryImplement
{
    public class ModbusMasterServiceFactory : IModbusMasterServiceFactory
    {
        IContainerProvider _containerProvider;
        ILogger<ModbusMasterServiceFactory> _logger;

        public ModbusMasterServiceFactory(
            IContainerProvider containerProvider,
            ILogger<ModbusMasterServiceFactory> logger
            )
        {
            _containerProvider = containerProvider ?? throw new ArgumentNullException("ModbusMasterServiceFactory IContainerProvider注入失败！");
            _logger = logger ?? throw new ArgumentNullException("ModbusMasterServiceFactory ILogger注入失败！");
        }

        public async Task<IModbusMasterService> CreateMaster(MasterConnectionConfig masterConfig)
        {
            if(masterConfig is TcpMasterConnectionConfig tcpConfig)
            {
                if (_containerProvider == null) // 防御性编码
                {
                    _logger.LogError("工厂创建主站失败！IContainerProvider为空!");
                    throw new ArgumentException("工厂创建Tcp主站失败！IContainerProvider为空!");
                }

                var master = _containerProvider.Resolve<ModbusTcpMasterService>();
                await master.Initialize(tcpConfig);
                return master;
            }
            else if (masterConfig is SerialMasterConnectionConfig serialConfig)
            {
                if (_containerProvider == null) // 防御性编码
                {
                    _logger.LogError("工厂创建主站失败！IContainerProvider为空!");
                    throw new ArgumentException("工厂创建Serial主站失败！IContainerProvider为空!");
                }
                var master = _containerProvider.Resolve<ModbusSerialMasterService>();
                master.Initialize(serialConfig);
                return master;
            }
            else
            {
                _logger.LogError($"工厂创建主站失败！未知的配置类型: {masterConfig.GetType()}");
                throw new ArgumentException($"工厂创建主站失败！未知的配置类型: {masterConfig.GetType()}");
            }
        }
    }
}
