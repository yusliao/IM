using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDKClient.Model
{
    public class AuthPackage : PackageInfo
    {
        public override string api  => Protocol.ProtocolBase.auth; 
        public override int  apiId  => Protocol.ProtocolBase.authCode; 
        public auth data { get; set; }

    }
    public class auth
    {
        public String mobile { get; set; }
        public String password { get; set; }
        public String session { get; set; }
        public String deviceModel  => "PC";
        public string deviceOs { get; set; }
        public String deviceId { get; set; }
        public String deviceToken { get; set; }
        public String imVersion { get; set; }
        /// <summary>
        ///  1:正常登入, 2:扫码登入
        /// </summary>
        public int login { get; set; }
        /// <summary>
        /// 0-未登录 1,主动登录 2. 扫码
        /// </summary>
        public int pcStatus { get; set; }
        /// <summary>
        /// 0-未登录 1,已登录
        /// </summary>
        public int appStatus { get; set; }
        public int resourceType => 1;
        public int userId { get; set; }
        public DateTime? lastLoginTime { get; set; }
        public string token { get; set; }
        /// <summary>
        /// 用户类型：0：IM，1：满金店用户，2：满金店客服
        /// </summary>
        public int userType { get; set; }
        /// <summary>
        /// 设备标识
        /// </summary>

        public string imei => System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(N => N.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback)?.GetPhysicalAddress().ToString();

    }
}
