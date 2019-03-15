using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDKClient.WebAPI
{
   /// <summary>
   /// 登陆IM验证
   /// </summary>
    public class AuthResponse
    {
        public string mobile { get; set; }
        public string error { get; set; }
        public int userId { get; set; }
        public string token { get; set; }
        public int code { get; set; }
        public string preImei { get; set; }
        public DateTime? preLoginTime { get; set; }
        /// <summary>
        /// 解除禁止登录时间（单位秒）
        /// </summary>
        public long removeNologinTime { get; set; }
        /// <summary>
        /// 解禁禁言时间（单位秒）
        /// </summary>
        public long removeGagTime { get; set; }
    }
    public class fileInfo
    {
        public string fileCode { get; set; }
        public long fileSize { get; set; }
        public bool isExist { get; set; }
        public string error { get; set; }
        public bool isBreakPoint { get; set; }
        public List<BlockPoint> blocks { get; set; }
        public class BlockPoint
        {
            /// <summary>
            /// 文件名
            /// </summary>
            public string fileName { get; set; }
            /// <summary>
            /// 块号
            /// </summary>
            public int blockNum { get; set; }
            /// <summary>
            /// 当前文件大小
            /// </summary>
            public long currentSize { get; set; }
            /// <summary>
            /// 块大小
            /// </summary>
            public long blockSize { get; set; }
            /// <summary>
            /// 是否上传完成
            /// </summary>
            public bool isFinished { get; set; }


        }
    }
    public class SearchResult
    {
        public string appName { get; set; }
        public int code { get; set; }
        public string error { get; set; }
        public bool success { get; set; }
        public IList<SearchInfo> searchResult { get; set; }
    }
    public class SearchInfo
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string NickName { get; set; }
        public string MobilePhone { get; set; }
        public string Description { get; set; }

        public string Photo { get; set; }
        /// <summary>
        /// "0"女；"1"男
        /// </summary>
        public string Sex { get; set; }
        public string Province { get; set; }
        public string City { get; set; }
        public string kfId { get; set; }
    }
    class WebResponse
    {
        public string appName { get; set; }
        /// <summary>
        /// 错误编码，0正确
        /// </summary>
        public int code { get; set; }
        public string error { get; set; }
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool success { get; set; }
      
    }
    class RoomInfo
    {
        public int entryId { get; set; }
        public int entryType { get; set; }
        public int num { get; set; }
        public int pullType { get; set; }
        public int pageNum { get; set; }
        public double fromTime { get; set; }
        public double earlyTime { get; set; }
    }
    class SummaryInfo:WebResponse
    {
        public int userId { get; set; }
        public double deadTime { get; set; }
        public List<RoomInfo> entryList { get; set; }
        public List<MessageWapper> operMsgList { get; set; }
        public class MessageWapper
        {
            public double time { get; set; }
            public dynamic content { get; set; }
        }
    }
    class offlineMsg:WebResponse
    {
        public int userId { get; set; }
        public List<MessageWapper> msgList { get; set; }
        public class MessageWapper
        {
            public double time { get; set; }
            public Model.MessagePackage content { get; set; }
        }
    }
    class PCVersion:WebResponse
    {
        public int VersionNum { get; set; }
        public string VersionName { get; set; }
        public bool IsSubUpgrade { get; set; }//是否需要升级升级程序

    }
    class GetNoticeListResponse : WebResponse
    {
        public List<NoticeInfo> noticeList { get; set; }
        public class NoticeInfo
        {
            public int noticeId { get; set; }
            public int groupId { get; set; }
            public int type { get; set; }
            public string title { get; set; }
            public string content { get; set; }
            public DateTime? publishTime { get; set; }
        }
    }
    /// <summary>
    /// 客服实体
    /// </summary>
    class CSEntity : WebResponse
    {
        public int? servicerId { get; set; }
        public string nickname { get; set; }
        /// <summary>
        /// 职位;1.综合客服 2.商品客服 3.订单客服 4.售后客服 
        /// </summary>
        public int station { get; set; }
        public string token { get; set; }
        public int? role { get; set; }
    }
    class NoticeResponse:WebResponse
    {
        public int noticeId { get; set; }
        public int groupId { get; set; }
        public int type { get; set; }
        public string title { get; set; }
        public string content { get; set; }
        public DateTime? publishTime { get; set; }
    }
    class CreateNoticeResponse : WebResponse
    {
        public int noticeId { get; set; }
        public DateTime? publishTime { get; set; }
    }
    class TempCustomSetResponse : WebResponse
    {
        public class Data
        {
            public int userId { get; set; }
            public string userName { get; set; }
            public string photo { get; set; }
            public int count { get; set; }
            public string lastMessage { get; set; }
        }
        public List<Data> entryList { get; set; }

    }
    /// <summary>
    /// 客服条目列表
    /// </summary>
    class CSRoomList
    {
        /// <summary>
        /// 客服条目实体
        /// </summary>
        public class entity
        {
            public int imopenid { get; set; }
            /// <summary>
            /// 头像
            /// </summary>
            public string photo { get; set; }
            /// <summary>
            /// 手机号
            /// </summary>
            public string mobile { get; set; }
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
            /// 当前会话ID，值为空说明会话空闲，有值说明会话进行中
            /// </summary>
            public string sesssionId { get; set; }
            /// <summary>
            /// 会话类型：0空闲，1自己的会话，2别人的会话
            /// </summary>
            public int sessionType { get; set; }
            /// <summary>
            /// 终端类型：小程序，H5买家，APP 
            /// </summary>
            public string appType { get; set; }


        }
        public List<entity> data { get; set; }
        public string message { get; set; }

    }
    

    class QrCodeResponse
    {
        public string appName { get; set; }
        /// <summary>
        /// 错误编码，0正确
        /// </summary>
        public int code { get; set; }
        public string error { get; set; }
        public string qrCode { get; set; }
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool success { get; set; }
        /// <summary>
        /// 名称
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// 头像
        /// </summary>
        public string photo { get; set; }
    }
    class QrCodeRequest
    {
        /// <summary>
        /// 主键值
        /// </summary>
        public string keyId { get; set; }
        /// <summary>
        /// 二维码类型'/'1':个人二维码 '2':群二维码
        /// </summary>
        public string qrType { get; set; }
    }
    class ErrorPackage
    {
       
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
    //{"fileName":null,"uploadResult":[{"fileId":45610,"fileName":"7078D33AB19BE25884C4A0E9122FCDF1.txt",
    //"fileCode":"7078D33AB19BE25884C4A0E9122FCDF1.txt","isSuccess":true,"error":"上传成功"}],
    //"appName":null,"code":0,"error":"","Success":true}
    class UploadResult:WebResponse
    {
        public List<Data> uploadResult { get; set; }
        public class Data
        {
            public int fileId { get; set; }
            public string fileName { get; set; }
            public string fileCode { get; set; }
            public bool isSuccess { get; set; }
            public string error { get; set; }
        }
    }

    public class GetSensitiveWordsResponse
    {
        public string appName { set; get; }
        public int code { set; get; }
        public string error { set; get; }
        public string time { get; set; }
        public List<Keyword> items { get; set; }
        public class Keyword
        {
            public int ID { get; set; }
            public string word { get; set; }
            public int type { get; set; }
            public int flag { get; set; }
        }
    }
}
