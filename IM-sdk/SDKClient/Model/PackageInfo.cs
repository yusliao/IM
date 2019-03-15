using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDKClient.Model
{
   
    public class PackageInfo: SuperSocket.ProtoBase.IPackageInfo
    {
        public string id { get; set; }
        public string error { get; set; }
        public int code { get; set; }
#if CUSTOMSERVER
        public string appName { get; set; } = "MJD.IM";
#else
        public string appName { get; set; } = "kefang";
#endif
        public virtual string api { get; set; }
        //public int resourceType { get; set; } = 1;
        public virtual int apiId { get; set; }
        public DateTime? time { get; set; }
        public string from { get; set; }
        public string to { get; set; }
        public int? syncMsg { get; set; }//是否同步消息1：是；0：不是。
        public int? reply { get; set; }

        public string version { get; set; } = SDKClient.Instance.property.CurrentAccount.imVersion;
        /// <summary>
        /// 0:未读，1:已读
        /// </summary>
        public int read { get; set; }
        public override string ToString()
        {
            return Util.Helpers.Json.ToJson(this);
        }

    }
    class MessageConfirmPackage:PackageInfo
    {
        public override string api => Protocol.ProtocolBase.MessageConfirm;
        public override int apiId => Protocol.ProtocolBase.MessageConfirmCode;
    }
    class NoHandlePackage : PackageInfo
    {
        public override string api => Protocol.ProtocolBase.NoHandlePackage;
        public override int apiId => Protocol.ProtocolBase.NoHandlePackageCode;
        public string source { get; set; }
    }
    class HeartMsgPackage:PackageInfo
    {
        public override string api => Protocol.ProtocolBase.HeartMsg;
        public override int apiId => Protocol.ProtocolBase.HeartMsgCode;
    }
    class ErrorPackage:PackageInfo
    {
        public override string api => Protocol.ProtocolBase.ErrorPackage;
        public override int apiId => Protocol.ProtocolBase.ErrorPackageCode;
        public string Content { get; set; }

    }

    
    
}
