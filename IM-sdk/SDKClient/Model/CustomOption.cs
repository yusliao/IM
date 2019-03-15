using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDKClient.Model
{
   public  class CustomServicePackage:PackageInfo
    {
        public override string api => Protocol.ProtocolBase.CustomService;
        public override int apiId => Protocol.ProtocolBase.CustomServiceCode;
        public Data data { get; set; }
        public class Data
        {
            /// <summary>
            /// 发送给原目标人
            /// </summary>
            public string originTo { get; set; }
            /// <summary>
            /// 1:发起请求2：结束会话3：客服确认
            /// </summary>
            public int type { get; set; }
            public string sessionId { get; set; }
            public string mobile { get; set; }
            public string photo { get; set; }
            /// <summary>
            /// 移动设备地址信息（上网IP地址）
            /// </summary>
            public string address { get; set; }
            /// <summary>
            /// 移动设备系统名称
            /// </summary>
            public string deviceName { get; set; }
            /// <summary>
            /// 移动设备系统型号
            /// </summary>
            public string deviceType { get; set; }
            /// <summary>
            /// 显示名
            /// </summary>
            public string shopName { get; set; }
            /// <summary>
            /// 店铺URL
            /// </summary>
            public string shopBackUrl { get; set; }
            /// <summary>
            /// 用户ID
            /// </summary>
            public int shopId { get; set; }
            /// <summary>
            /// 终端类型：小程序，H5买家，APP 
            /// </summary>
            public string appType { get; set; }



        }
    }
    public class CustomExchangePackage : PackageInfo
    {
        public override string api => Protocol.ProtocolBase.CustomExchange;
        public override int apiId => Protocol.ProtocolBase.CustomExchangeCode;
        public Data data { get; set; }
        public class Data
        {
           
            /// <summary>
            /// 转接后交互session
            /// </summary>
            public string sessionId { get; set; }
            /// <summary>
            /// 新客服ID
            /// </summary>
            public int userId { get; set; }
            /// <summary>
            /// 新客服头像
            /// </summary>
            public string photo { get; set; }
            /// <summary>
            /// 发送给原目标人
            /// </summary>
            public string originTo { get; set; }

        }
    }
    public class CSMessagePackage :MessagePackage
    {

        public   string api => Protocol.ProtocolBase.CSMessage;
        public  int apiId => Protocol.ProtocolBase.CSMessageCode;
       
    }
    public class CSSyncMsgStatusPackage : PackageInfo
    {

        public override string api => Protocol.ProtocolBase.CSSyncMsgStatus;
        public override int apiId => Protocol.ProtocolBase.CSSyncMsgStatusCode;
        public class Data
        {
            /// <summary>
            /// 点击的用户ID
            /// </summary>
            public int userId { get; set; }
            public string photo { get; set; }
            public string userName { get; set; }
            /// <summary>
            /// 发送给原目标人
            /// </summary>
            public string originTo { get; set; }
        }
        public Data data { get; set; }
    }
}
