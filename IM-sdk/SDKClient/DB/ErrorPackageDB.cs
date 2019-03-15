using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDKClient.DB
{
    class ErrorPackageDB
    {
        /// <summary>
        /// 主键，自增
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        /// <summary>
        /// 消息类型 1错发，2漏发，3重复发4解析出错
        /// </summary>
        public int msgType { get; set; }
        /// <summary>
        /// 实际接收消息者
        /// </summary>
        public int receiverId { get; set; }
        public int targetId { get; set; }
        public int senderId { get; set; }
        public string msgId { get; set; }
        /// <summary>
        /// 设备唯一码
        /// </summary>
        public string imei => System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(N => N.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback)?.GetPhysicalAddress().ToString();
        /// <summary>
        /// 来源平台
        /// </summary>
        public string sourceOS { get; set; } = "PC";
        public string content { get; set; }
    }
}
