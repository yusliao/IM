using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDKClient.Model
{
    public class RequestIpPackage : PackageInfo
    {
        public override string api => Protocol.ProtocolBase.RequestIp;
        public override int apiId => Protocol.ProtocolBase.RequestIpCode;
        public Data data { get; set; }
        public class Data
        {
            public string ip { get; set; }
            public int port { get; set; }
        }
    }
    public enum FileType
    {
        img =0,
        file =1
    }
}
