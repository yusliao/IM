using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDKClient.Model
{
   public class MessagePackage : PackageInfo
    {
        
        public override string api => Protocol.ProtocolBase.message;
        public override int apiId =>Protocol.ProtocolBase.messageCode;
        public message data { get; set; }
    }
    public class SyncMsgStatusPackage : PackageInfo
    {

        public override string api => Protocol.ProtocolBase.SyncMsgStatus;
        public override int apiId => Protocol.ProtocolBase.SyncMsgStatusCode;
        public Data data { get; set; }
        public class Data
        {
            /// <summary>
            /// 已读条数
            /// </summary>
            public int readNum { get; set; }
            /// <summary>
            /// 设备类型
            /// </summary>
            public int deviceType { get; set; } = 1;
            /// <summary>
            /// 
            /// </summary>
            public int groupId { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public int partnerId { get; set; }
            public string lastMsgID  { get; set; }
        }
    }
    public class GetOfflineMessageListPackage : PackageInfo
    {
        public override string api => Protocol.ProtocolBase.GetOfflineMessageList;
        public override int apiId => Protocol.ProtocolBase.GetOfflineMessageListCode;
        public Data data { get; set; }
        public class Data
        {

            public List<dynamic> items { get; set; }
         
            public int devType { get; set; } = 1;
           
            public int count { get; set; }
            public DateTime? time { get; set; }
           

        }
    }

 

    public class message
    {
        public string type { get; set; }
        public msgGroup groupInfo { get; set; }
        public string subType { get; set; }
        public SenderInfo senderInfo { get; set; }
        public ReceiverInfo receiverInfo { get; set; }
        /// <summary>
        /// 0是好友聊天，1是陌生人聊天
        /// </summary>
        public int chatType { get; set; } = 0;
        public dynamic body { get; set; }
        public DateTime Time  => DateTime.Now;
        /// <summary>
        /// @列表（用户ID），已逗号分隔。@ALL用数值1代表
        /// </summary>

        public IList<int> tokenIds { get; set; }
        public class msgGroup
        {
            public int groupId { get; set; }

            /// <summary>
            /// 群主ID
            /// </summary>
            public int groupOwnerId { get; set; }
            /// <summary>
            /// 群类型(0普通群1官方群)
            /// </summary>
            public int groupType { get; set; } = 0;
            /// <summary>
            /// 群名
            /// </summary>
            public string groupName { get; set; }

            /// <summary>
            /// 群头像
            /// </summary>
            public string groupPhoto { get; set; }

            /// <summary>
            /// 群简介
            /// </summary>
            public string groupIntroduction { get; set; }
            public List<groupmemberInfo> items { get; set; }
            /// <summary>
            /// 群置顶时间
            /// </summary>
            public DateTime? groupTopTime { get; set; }
            /// <summary>
            /// 消息免打扰
            /// </summary>
            public bool doNotDisturb { get; set; }
            /// <summary>
            /// 入群验证方式: 1 管理员审批入群 2 自由入群 3 密码入群
            /// </summary>
            public byte joinAuthType { get; set; }
        }


        public class SenderInfo
        {
            public string userName { get; set; }
            public string photo { get; set; }
        }
        public class ReceiverInfo
        {
            public string userName { get; set; }
            public string photo { get; set; }
        }



    }


    public class redEnvelopesSendOutBody
    {
        /// <summary>
        /// 红包ID
        /// </summary>
        public long id { get; set; }
        /// <summary>
        /// 1-普通 2-拼手气
        /// </summary>
        public int disType { get; set; }
        /// <summary>
        /// 金额
        /// </summary>
        public decimal amount { get; set; }
        /// <summary>
        /// 祝福语
        /// </summary>
        public string remark { get; set; }
    }
    public class redEnvelopesReceiveBody
    {
        /// <summary>
        /// 红包ID
        /// </summary>
        public long id { get; set; }

        /// <summary>
        /// 领取金额
        /// </summary>
        public decimal amount { get; set; }
        /// <summary>
        /// 是否已领完
        /// </summary>
        public bool isOver { get; set; }
    }
    public class TxtBody
    {
        public string text { get; set; }
    }
  
    public class BigBody
    {
        /// <summary>
        /// 包ID，可重用包ID
        /// </summary>
        public string partName { get; set; }
        /// <summary>
        /// 包序号，0开始
        /// </summary>
        public int partOrder { get; set; }
        public string text { get; set; }
        /// <summary>
        /// 总包数，200长度/包
        /// </summary>
        public int partTotal { get; set; }//200一包，总包数
    }
    
    public class Callbody
    {
        public int distPort { get; set; }
        public string gatewayIp{ get; set; }
        public string Ip { get; set; }
        public string localIp { get; set; }
        public int localPort { get; set; }
        public int srcPort { get; set; }
        public string status { get; set; }
        public string callTime { get; set; }
        public int type { get; set; }

    }
    public class ImgBody
    {
        public string id { get; set; }
        public string fileName { get; set; }
        public string smallId { get; set; }
        public string width { get; set; }
        public string height { get; set; }

    }
    public class retractBody
    {
        public string retractId { get; set; }
        /// <summary>
        /// 0 普通撤回，1发送方在线文件撤回，2在线转离线,3离线转在线，4接收方在线文件取消
        /// </summary>
        public int? retractType { get; set; }

    }
    public class audioBody
    {
        public string id { get; set; }
        public string recordTime { get; set; }
        public string filePath { get; set; }

    }
    public class videoBody
    {
        public int fileSize { get; set; }
        public string id { get; set; }
        public string filePath { get; set; }
        public string recordTime { get; set; }
        public int width { get; set; }
        public int height { get; set; }

    }
    public class smallVideoBody
    {
        public long fileSize { get; set; }
        public string id { get; set; }
        public string previewId { get; set; }
        public string fileName { get; set; }
        public string recordTime { get; set; }
        public int width { get; set; }
        public int height { get; set; }

    }
    public class fileBody
    {
        public long fileSize { get; set; }
        public string id { get; set; }
        public string fileName { get; set; }
        
        public string img { get; set; }
        public int width { get; set; }
        public int height { get; set; }
    }
   
    public class OnlineFileBody
    {
        public long fileSize { get; set; }
        public string id { get; set; }
        public string fileName { get; set; }
        public string IP { get; set; }
        public int Port { get; set; }
       

    }
    public class EvalBody
    {
       
        public string id { get; set; }
        /// <summary>
        /// 1-问，2-答
        /// </summary>
        public int type { get; set; } = 1;
        /// <summary>
        /// [1-很满意 2-满意 3-一般 4-不满意]
        /// </summary>
        public int subType { get; set; }
        
        /// <summary>
        /// 评价结果
        /// </summary>
        public string result { get; set; }
       
    }
    public class GoodsBody
    {

        public string goodsid { get; set; }
        
        public string goodspic { get; set; }
        public string goodsname { get; set; }
        public string goodsprice { get; set; }
        public string mfronturl { get; set; }
        public string afterurl { get; set; }

    }
    public class OrderBody
    {
        public string orderno { get; set; }
        public DateTime? ordertime { get; set; }
        public string h5fronturl { get; set; }
        public string mfronturl { get; set; }
        public string afterurl { get; set; }
        public string goodspic { get; set; }
        public string goodsname { get; set; }
        public string goodsprice { get; set; }
        public string skuname { get; set; }
    }
    public class CustomBody
    {
        public string customno { get; set; }
        public DateTime? customtime { get; set; }
        public string fronturl { get; set; }
        public string afterurl { get; set; }
        
        public string goodspic { get; set; }
        public string goodsname { get; set; }
        public string goodsprice { get; set; }
        public string skuname { get; set; }
    }
    public class faceBody
    {
        public int fileSize { get; set; }
        public string Id { get; set; }
        public string filePath { get; set; }
        public string fileName { get; set; }
        public string showTitle { get; set; }
        public string packName { get; set; }
        public string img { get; set; }
    }
    public class addGroupNoticeBody
    {
        public int groupId { get; set; }
        public string title { get; set; }
        public string content { get; set; }
        public int noticeId { get; set; }
       
        /// <summary>
        /// 公告类型 0：普通公告，1：入群须知
        /// </summary>
        public int type { get; set; }
        public DateTime? publishTime { get; set; }
    }
    public class deleteGroupNoticeBody
    {
        public string title { get; set; }
     
        public int noticeId { get; set; }
        public int groupId { get; set; }
        /// <summary>
        /// 公告类型 0：普通公告，1：入群须知
        /// </summary>
        public int type { get; set; }
        public DateTime? publishTime { get; set; }
    }

    public class UserCardBody
    {
        public string name { set; get; }
        public string photo { set; get; }
        public string phone { set; get; }
        public int userId { set; get; }
    }


    public class ForeignDynBody
    {
        public string id { get; set; }
        /// <summary>
        /// 是否是视频
        /// </summary>
        public int mediaType { get; set; }
        /// <summary>
        /// 摘要
        /// </summary>
        public string text { get; set; }
        /// <summary>
        /// 网址
        /// </summary>
        public string url { get; set; }
        /// <summary>
        /// 图片
        /// </summary>
        public string img { get; set; }
    }
    public class BigtxtHelper
    {
        private static System.Collections.Concurrent.ConcurrentDictionary<string, BigtxtHelper> bigtxtDic = new System.Collections.Concurrent.ConcurrentDictionary<string, BigtxtHelper>();
        public string partName { get; set; }
        public System.Collections.Concurrent.ConcurrentBag<BigBody> bodyList { get; set; } = new System.Collections.Concurrent.ConcurrentBag<BigBody>();
        System.Threading.CancellationTokenSource CancellationTokenSource = new System.Threading.CancellationTokenSource();
        private bool IsWatched = false;
        private static object obj_lock = new object();
        private event Action<string> callBack;
       
        public static void AddBigtxtMsg(BigBody bigBody,Action<string> addBigtxtMsgCallback)
        {
            BigtxtHelper helper = bigtxtDic.GetOrAdd(bigBody.partName, new BigtxtHelper() { partName = bigBody.partName });
            helper.callBack += addBigtxtMsgCallback;
           
            if (!helper.bodyList.Any(b => b.partOrder == bigBody.partOrder))
            {
                
                    helper.bodyList.Add(bigBody);
            }
            if (helper.bodyList.Count == helper.bodyList.First().partTotal)
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < helper.bodyList.Count; i++)
                {
                    var body = helper.bodyList.First(b => b.partOrder == i);
                    sb.Append(body.text);
                }
                helper.CancellationTokenSource.Cancel();
                helper.callBack?.Invoke(sb.ToString());
               // bigtxtDic.TryRemove(bigBody.partName, out helper);
            }
            else
            {
                if (!helper.IsWatched)
                    helper.RaiseTimeoutHandle(helper, addBigtxtMsgCallback);
            }
            
            
            
        }
       
        private void RaiseTimeoutHandle(BigtxtHelper helper,Action<string> addBigtxtMsgCallback)
        {
            this.IsWatched = true;
            Task.Delay(30 * 1000, CancellationTokenSource.Token).ContinueWith(t =>
            {
               // BigtxtHelper bigtxtHelper;
               // bigtxtDic.TryRemove(partName, out bigtxtHelper);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < helper.bodyList.Count; i++)
                {
                    var body = helper.bodyList.FirstOrDefault(b => b.partOrder == i);

                    sb.Append(body?.text);
                }
                addBigtxtMsgCallback?.Invoke(sb.ToString());

            }, TaskContinuationOptions.OnlyOnRanToCompletion);
        }

    }



}
