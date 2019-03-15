using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDKClient.Model
{
    public class SetStrangerDoNotDisturbPackage : PackageInfo
    {
        public override string api => Protocol.ProtocolBase.SetStrangerDoNotDisturb;
        public override int apiId => Protocol.ProtocolBase.SetStrangerDoNotDisturbCode;
        public Data data { get; set; }
        public class Data
        {
            /// <summary>
            /// 是否消息免打扰 1是 0否
            /// </summary>
            public int isNotdisturb { get; set; }
            /// <summary>
            /// 陌生人ID
            /// </summary>
            public int strangerId { get; set; }
            /// <summary>
            /// 用户ID
            /// </summary>
            public int userId { get; set; }

        }

    }


}
