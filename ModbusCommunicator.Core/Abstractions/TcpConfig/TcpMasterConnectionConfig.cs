using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModbusCommunicator.Core.Abstractions
{
    public class TcpMasterConnectionConfig : MasterConnectionConfig
    {
        /// <summary>
        /// 目标从站 IP 地址。
        /// 使用 "0.0.0.0" 或 "::" 监听所有网络接口。
        /// </summary>
        [Required(AllowEmptyStrings = false, ErrorMessage = "IP 地址是必需的，且不能为空字符串！")] // 标记为必需字段
        public string IpAddress { get; set; } = "192.168.0.1"; // 默认监听所有 IPv4 地址

        /// <summary>
        /// 目标从站 TCP 端口号。
        /// </summary>
        [Required(ErrorMessage = "端口号是必需的！")] // 标记为必需字段
        [Range(1, 65535, ErrorMessage = "端口号必须介于 {1} 至 {2} 之间！")] // 限制端口范围
        public int Port { get; set; } = 502; // 标准 Modbus TCP 端口

        /// <summary>
        /// Modbus 从站的单元 ID (Slave Address)。
        /// </summary>
        [Required(ErrorMessage = "从站 ID 是必需的！")] // 标记为必需字段
        [Range(1, 247, ErrorMessage = "Modbus 从站 ID 必须介于 {1} 至 {2} 之间。")] // Modbus 标准范围是 1-247，0 为广播，248-255 保留
        public byte UnitId { get; set; } = 1;

        //--- 以下选项根据具体需求添加 ---
        /// <summary>
        /// 获取或设置一个值，指示请求失败的最大重试次数。
        /// </summary>
        [Range(1, 10)] // 设置一个合理的范围
        public int NumberOfRetries { get; set; } = 3;

        //--- 以下选项根据具体需求添加 ---
        /// <summary>
        /// 获取或设置一个值，指示重试时的延时间隔。
        /// </summary>
        [Range(5, 2000)] // 设置一个合理的范围
        public int RetryDelayMilliseconds { get; set; } = 20;

        /// <summary>
        /// 获取或设置发送数据时的超时时间（毫秒）。
        /// </summary>
        [Range(50, 5000, ErrorMessage = "发送超时必须介于 {1} 至 {2} 毫秒之间！")]
        public int SendTimeoutMilliseconds { get; set; } = 500; // 默认值可以根据典型场景调整

        /// <summary>
        /// 获取或设置接收数据时的超时时间（毫秒）。
        /// 如果在此时间内未收到响应，则认为请求超时。
        /// </summary>
        [Range(50, 5000, ErrorMessage = "接收超时必须介于 {1} 至 {2} 毫秒之间！")]
        public int ReceiveTimeoutMilliseconds { get; set; } = 1000; // 接收超时通常可以设置得比发送略长一些
    }
}
