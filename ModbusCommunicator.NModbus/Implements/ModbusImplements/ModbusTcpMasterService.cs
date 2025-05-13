using Microsoft.Extensions.Logging;
using ModbusCommunicator.Core.Abstractions;
using ModbusCommunicator.Core.Abstractions.ModbusTcpConfig;
using ModbusCommunicator.Core.Interfaces;
using NModbus;
using Polly;
using System.Net;
using System.Net.Sockets;

namespace ModbusCommunicator.NModbusTcp
{
    public class ModbusTcpMasterService : IModbusMasterService, IDisposable
    {

        //日志级别分类(请按照规划好的日志级别进行记录信息，避免混乱)
        //清理相关的非错误日志以及追踪信息使用 LogTrace
        //非严重错误无法继续时使用 LogWarning
        //严重错误无法继续时使用 LogError
        //调试信息使用 LogDebug

        // --- 依赖注入字段 --- // 区域注释：通过构造函数注入的服务实例。
        private readonly ILogger<ModbusTcpMasterService> _logger; // 日志记录器实例。
        private readonly IModbusFactory _modbusFactory; // NModbus 工厂实例，用于创建 Modbus 主站。

        private TcpMasterConnectionConfig _config;

        // 用于处理连接操作的异步策略，根据配置动态更新。初始化为无操作策略。
        // 类型更改为 IAsyncPolicy 以接受 NoOpAsync 和 RetryAsync 策略
        private IAsyncPolicy _connectRetryPolicy = Policy.NoOpAsync(); // 使用 IAsyncPolicy 接口
        // 用于处理读/写操作的异步策略，根据配置动态更新。初始化为无操作策略。
        // 类型更改为 IAsyncPolicy 以接受 NoOpAsync 和 RetryAsync 策略
        private IAsyncPolicy _operationRetryPolicy = Policy.NoOpAsync(); // 使用 IAsyncPolicy 接口

        // --- 连接状态字段 --- // 区域注释：与 TCP 连接和 Modbus 会话状态相关的字段。
        private TcpClient? _tcpClient; // TCP 客户端实例，用于底层 TCP 连接。可为 null。
        private IModbusMaster? _master; // Modbus 主站接口实例，用于执行 Modbus 操作。可为 null。
        private readonly SemaphoreSlim _connectionLock = new(1, 1); // 信号量，用于控制对连接和断开，初始化操作的并发访问，防止竞态条件。
        private long _disposed; // 标记服务是否已被释放，用于实现 IDisposable 模式。使用 long 是为了 Interlocked 操作。
        private readonly CancellationTokenSource _cts = new(); //取消信号
        private CancellationToken token => _cts.Token; //返回取消令牌

        public bool IsConnected => _master != null && _tcpClient != null && _tcpClient.Client != null && _tcpClient.Connected;

        public ModbusTcpMasterService(
            IModbusFactory modbusFactory,
            ILogger<ModbusTcpMasterService> logger
            )
        {
            try
            {
                _modbusFactory = modbusFactory ?? throw new ArgumentNullException(nameof(modbusFactory));
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));

                _logger.LogInformation("ModbusTcpMasterService 初始化成功。");
            }
            catch (Exception e)
            {
                if (_logger == null) throw;
                _logger.LogError(e, "ModbusTcpMasterService 初始化失败");
            }
        }

        public async Task Initialize(TcpMasterConnectionConfig tcpConfig)
        {
            await _connectionLock.WaitAsync();
            _config = tcpConfig;

            if (_config.NumberOfRetries > 0)// 如果配置了重试次数，则创建相应的连接策略。
            {
                _connectRetryPolicy = Policy.
                    Handle<SocketException>().
                    Or<TimeoutException>().
                    Or<IOException>().
                    WaitAndRetryAsync(_config.NumberOfRetries,
                    _ => TimeSpan.FromMilliseconds(_config.RetryDelayMilliseconds),
                    (exception, timeSpan, retryCount, context) => _logger.LogWarning(exception,
                    "连接失败，将在 {Delay}ms 后进行第{RetryCount}次重试...",
                    timeSpan.TotalMilliseconds,
                    retryCount));
            }
            else
            {
                _connectRetryPolicy = Policy.NoOpAsync(); // 无重试策略。
            }

            if (_config.NumberOfRetries > 0)// 如果配置了重试次数，则创建相应的操作策略。
            {
                _operationRetryPolicy = Policy.
                    Handle<TimeoutException>().
                    Or<IOException>().
                    WaitAndRetryAsync(_config.NumberOfRetries,
                    _ => TimeSpan.FromMilliseconds(_config.RetryDelayMilliseconds),
                    (exception, timeSpan, retryCount, context) => _logger.LogWarning(exception,
                    "操作失败，将在 {Delay}ms 后进行第{RetryCount}次重试..."
                    , timeSpan.TotalMilliseconds,
                    retryCount));
            }
            else
            {
                _operationRetryPolicy = Policy.NoOpAsync(); // 无重试策略。
            }

            _logger.LogInformation($"配置加载完成。IP地址:{_config.IpAddress},端口:{_config.Port},连接的从站号:{_config.UnitId},重试次数:{_config.NumberOfRetries},发送超时时长:{_config.SendTimeoutMilliseconds}ms,接收超时时长:{_config.ReceiveTimeoutMilliseconds}ms,重试间隔时间:{_config.RetryDelayMilliseconds}ms");
            _connectionLock.Release();
        }

        public async Task ConnectAsync()
        {
            ThrowIfDisposed();// 检查服务是否已被释放，如果是则抛出异常。如果未被释放，继续执行连接逻辑。

            if (IsConnected)// 如果已经连接，则直接返回。
            {
                _logger.LogDebug("已经连接，切勿重复连接。");
                return;
            }

            await _connectionLock.WaitAsync();

            try
            {
                if (IsConnected)
                {
                    _logger.LogError("锁内再次检测已连接，这通常不会发生，请检查ConnectAsync相关的上下文！");
                    throw new InvalidOperationException("锁内外读取IsConnected状态不一致，请检查代码逻辑！");// 不捕获该错误使其继续向上传递。
                }

                //配置的检测交给相关服务处理, 工厂创建对象时调用Initialize初始化配置。
                string ipAddress = _config.IpAddress;
                int port = _config.Port;
                var convretedAddress = default(IPAddress);

                if (IPAddress.TryParse(ipAddress, out IPAddress address) && address != null)// 确认IP地址格式正确，同时将传入的字符串格式的IP地址转换为IPAddress对象。
                {
                    convretedAddress = address;
                }
                else
                {
                    _logger.LogError("IP地址格式不正确，无法解析为IPAddress对象！确保调用ConnectAsync之前进行过配置验证。");
                    throw new ArgumentException($"IP地址格式不正确，无法解析为IPAddress对象！提供的IP地址是：{ipAddress}");
                }

                _logger.LogInformation("尝试连接到ModbusTcp从站: {IpAddress}:{Port}...", ipAddress, port);

                var currentConnectRetryPolicy = _connectRetryPolicy;// 备份当前连接策略，以防它在外部更改(防御性编程)。
                var currentRetries = _config.NumberOfRetries;// 备份当前重试次数，以防它在外部更改(防御性编程)。
                var currentRetryDelay = _config.RetryDelayMilliseconds;// 备份当前重试间隔，以防它在外部更改(防御性编程)。

                var policyResult = await currentConnectRetryPolicy.ExecuteAndCaptureAsync(async (cts) =>
                {
                    CleanupConnectionResources();

                    _tcpClient = new TcpClient()// 创建新的TCP客户端实例。
                    {
                        ReceiveTimeout = _config.ReceiveTimeoutMilliseconds,
                        SendTimeout = _config.SendTimeoutMilliseconds
                    };

                    _logger.LogDebug("正在连接 TCP 到 {IpAddress}:{Port}...", ipAddress, port);// 记录调试信息。

                    await _tcpClient.ConnectAsync(convretedAddress, port, cts);// 尝试连接到指定的IP地址和端口，使用取消令牌。
                    _logger.LogDebug("TCP 连接成功。");

                    _master = _modbusFactory.CreateMaster(_tcpClient);
                    _logger.LogDebug("ModbusTcp主站创建成功。");

                    //可在ModbusTcp主站连接成功后发布事件，也可交给相关服务类处理。
                }
                , token);

                if (policyResult.Outcome == OutcomeType.Failure)
                {
                    _logger.LogError(policyResult.FinalException, "连接ModbusTcp从站失败！使用IP：{IPAddress}，端口：{Port},已尝试 {Retries} 次, 重试间隔{Delay}", ipAddress, port, currentRetries, currentRetryDelay);
                    CleanupConnectionResources();// 清理相关资源。
                    throw policyResult.FinalException ?? new IOException("从站连接失败，且 Polly 未捕获到具体异常。");
                }
            }
            catch (OperationCanceledException e)
            {
                _logger.LogTrace(e, "连接操作被取消。");
            }
            finally
            {
                _connectionLock.Release(); // 释放信号量
            }
        }

        private void CleanupConnectionResources()
        {
            _logger.LogInformation("正在清理连接资源...");

            if (_master != null)
            {
                try
                {
                    (_master as IDisposable)?.Dispose();
                }
                catch (Exception e)
                {
                    _logger.LogWarning("清理ModbusTcp 主站时发生异常。{Message}", e);// 记录警告但不抛出异常，因为清理操作不应中断正常流程。
                }
                finally
                {
                    _master = null;
                    _logger.LogTrace("ModbusTcp 主站已释放。");
                }
            }
            if (_tcpClient != null)
            {
                try
                {
                    _tcpClient.Close();// 关闭TCP客户端连接。
                    _tcpClient.Dispose();// 释放TCP客户端资源。
                }
                catch (Exception e)
                {
                    _logger.LogWarning("清理TCP客户端时发生异常。{Message}", e);// 记录警告但不抛出异常，因为清理操作不应中断正常流程。
                }
                finally
                {
                    _tcpClient = null;
                    _logger.LogTrace("TCP客户端已释放。");
                }
            }
        }

        private void ThrowIfDisposed()// 如果服务已被释放，则抛出异常。
        {
            if (Interlocked.Read(ref _disposed) == 1)// 原子读取标志位
            {
                throw new ObjectDisposedException(nameof(ModbusTcpMasterService));
            }
        }

        public async Task DisConnectAsync()
        {
            if (IsConnected == false && _tcpClient == null && _master == null)// 如果未连接并且客户端主站为空，则直接返回，连接/客户端/主站至少有一项存在/连接都执行(主要是清理资源)。
            {
                _logger.LogInformation("未连接或资源已清理，跳过断开操作。");
                return;
            }

            await _connectionLock.WaitAsync().ConfigureAwait(false);// 等待信号量且释放上下文，该函数内操作通常无需原始上下文，需要时可删除ConfigureAwait。

            try// 通常不会冒泡至该层，CleanupConnectionResources函数内捕获了所有错误，此处仅为防御性编程和释放锁操作。
            {
                _logger.LogInformation("正在断开Tcp连接...");
                CleanupConnectionResources();
                _logger.LogInformation("已断开Tcp连接");
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "在DisConnectAsync执行断开和清理资源的过程中发生未预料的异常。");// 记录警告但不抛出异常，因为清理操作不应中断正常流程。
            }
            finally
            {
                _connectionLock.Release();// 释放信号量, 确保任何情况都能释放锁。
            }
        }

        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 0)// 原子设置标志位，确保只执行一次清理操作。
            {
                _logger.LogInformation("正在释放 ModbusTcpMasterService...");

                //订阅相关确保在释放前取消。

                try
                {
                    _cts?.Cancel();// 令牌取消所有操作,暂且放在锁外以更快让某些操作相应，如需优化后期可以考虑放在锁内
                }
                catch (ObjectDisposedException e)
                {
                    if (_cts != null && e.ObjectName == _cts.GetType().FullName)
                    {
                        _logger.LogWarning(e, "令牌在尝试取消时已被释放。");
                    }
                    else
                    {
                        _logger.LogWarning(e, "在Dispose中尝试取消操作时发生对象已释放异常（非令牌）。");
                    }
                }

                _connectionLock.Wait();// 同步等待

                try// 通常不会冒泡至该层，CleanupConnectionResources函数内捕获了所有错误，此处仅为防御性编程和释放锁操作。
                {
                    CleanupConnectionResources();
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "在Dispose执行清理资源的过程中发生未预料的异常。");// 记录警告但不抛出异常，因为清理操作不应中断正常流程。
                }
                finally
                {
                    _connectionLock.Release();// 释放信号量
                    _logger.LogInformation("连接资源已清理。");
                }

                _connectionLock.Dispose();// 释放信号量资源
                _logger.LogTrace("连接信号量已释放。");
                _cts?.Dispose();// 释放令牌资源
                _logger.LogTrace("令牌已释放。");
                _logger.LogInformation("ModbusTcpMasterService 已释放。");
            }
        }

        public Task<bool[]> ReadCoilsAsync(byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        {
            throw new NotImplementedException();
        }

        public Task<ushort[]> ReadHoldingRegistersAsync(byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        {
            throw new NotImplementedException();
        }

        public Task<ushort[]> ReadInputRegistersAsync(byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        {
            throw new NotImplementedException();
        }

        public Task<bool[]> ReadInputsAsync(byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        {
            throw new NotImplementedException();
        }

        public Task WriteMultipleCoilsAsync(byte slaveAddress, ushort startAddress, bool[] data)
        {
            throw new NotImplementedException();
        }

        public Task WriteMultipleRegistersAsync(byte slaveAddress, ushort startAddress, ushort[] data)
        {
            throw new NotImplementedException();
        }

        public Task WriteSingleCoilAsync(byte slaveAddress, ushort coilAddress, bool value)
        {
            throw new NotImplementedException();
        }

        public Task WriteSingleRegisterAsync(byte slaveAddress, ushort registerAddress, ushort value)
        {
            throw new NotImplementedException();
        }
    }
}
