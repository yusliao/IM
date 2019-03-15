using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDKClient.Model
{
    public class SysNotifyPackage:PackageInfo
    {
        public override string api => Protocol.ProtocolBase.SysNotify;
        public override int apiId => Protocol.ProtocolBase.SysNotifyCode;
        public Data data { get; set; }
        public class Data
        {
            /// <summary>
            ///  1-IM官方消息 2-社交消息推送 3-满金店消息, 4-红包系统消息
            /// </summary>
            public int type { get; set; }
            ///
            ///  101 –帐户封停
            ///   102 –帐户解封
            ///   103-禁止发消息 
            ///   104-解禁发消息
            ///   105-禁止发动态
            /// 106解禁发动态
            /// 107 用户登录官网通知
            /// 108用户退出官网通知
            /// <summary>
            /// 301-下单成功302-支付成功303-已发货304-被分享人开店305-店主获得收益
            /// </summary>
            public int subType { get; set; }
           
            public dynamic body { get; set; }
           
           
        }
    }
    public class fengjinBody
    {
        public int userId { get; set; }
        /// <summary>
        /// 失效时间 
        /// </summary>
        public long disableTime { get; set; }
    }
    public class HongbaoBody
    {
        /// <summary>
        /// chat-个人红包 groupChat-群红包
        /// </summary>
        public string type { get; set; }
        public group groupInfo { get; set; }
        public Model.message.SenderInfo sendInfo { get; set; }
        public long id { get; set; }
        public decimal amount { get; set; }
        public int returnReason { get; set; }
    }
 
}
