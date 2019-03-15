using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDKClient.Model
{
    public class LoginPackage : PackageInfo
    {
        public override string api => Protocol.ProtocolBase.login; 
        public override int apiId  => Protocol.ProtocolBase.loginCode; 
        public login data { get; set; }

    }
    public class login
    {
        public string deviceId { get; set; }
        public string version { get; set; }
        public DateTime? time { get; set; }
        public string session { get; set; }
    }
    public class ForceExitPackage : PackageInfo
    {
        public override string api => Protocol.ProtocolBase.ForceExit;
        public override int apiId => Protocol.ProtocolBase.ForceExitCode;
        public Data data { get; set; }
        public class Data
        {
            public int userId { get; set; }
            public int resourceType { get; set; } = 1;
            
        }

    }
    public class ExitNotifyPackage : PackageInfo
    {
        public override string api => Protocol.ProtocolBase.ExitNotify;
        public override int apiId => Protocol.ProtocolBase.ExitNotifyCode;
        public Data data { get; set; }
        public class Data
        {
            public int userId { get; set; }
            public int resourceType { get; set; } = 1;
            /// <summary>
            /// 0表示主动退出，1表示被动退出
            /// </summary>
            public int type { get; set; }

        }

    }
    public class LogoutPackage : PackageInfo
    {
        public override string api => Protocol.ProtocolBase.Logout;
        public override int apiId => Protocol.ProtocolBase.LogoutCode;
        public Data data { get; set; }
        public class Data
        {
            public int userId { get; set; }
            /// <summary>
            /// 1表示PC ， 2表示移动端
            /// </summary>
            public int resourceType { get; set; } = 1;
            /// <summary>
            /// 0-离线（退出到后台）  1-退出应用(主动)2-被动退出应用
            /// </summary>
            public int status { get; set; }

        }

    }

}
