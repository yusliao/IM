using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDKClient.WebAPI
{
    public class CustomServerModel
    {
        public int code { get; set; }
        public string message { get; set; }
        
    }
    public class QuickReplycategory:CustomServerModel
    {
        public class Data
        {
            public int categoryId { get; set; }
            public string categoryName { get; set; }
        }
        public List<Data> data { get; set; }
    }
    public class CSSysConfig : CustomServerModel
    {
        public class Data
        {
            public int SysMsgTimeInterval { get; set; }
           
        }
        public List<Data> data { get; set; }
    }
    public class IPEntity : CustomServerModel
    {
        
        public string data { get; set; }
    }
    public class OnlineStatusEntity : CustomServerModel
    {
        public class Data
        {
            public int servicerid { get; set; }
            public string nickname { get; set; }
            public int station { get; set; }
            public string imopenid { get; set; }
            public int status { get; set; }
            public int workstatus { get; set; }
            public int onlinecount { get; set; }
            public int offlinecount { get; set; }
            public int maxcount { get; set; }
            public Double rate { get; set; }

        }
        public List<Data> data { get; set; }
    }
    public class CustomExchange : CustomServerModel
    {
        public class Data
        {


            /// <summary>
            /// 会话id
            /// </summary>
            public string sessionid
            {
                get;
                set;
            }

            /// <summary>
            /// 客服im标识
            /// </summary>
            public string imopenid
            {
                get;
                set;
            }

            /// <summary>
            /// 客服工作时间
            /// </summary>
            public string worktime
            {
                get;
                set;
            }

            /// <summary>
            /// 客服电话
            /// </summary>
            public string servicertel
            {
                get;
                set;
            }

            /// <summary>
            /// 超时时间
            /// </summary>
            public int timeout
            {
                get;
                set;
            }

            /// <summary>
            /// WebSocket服务器地址;
            /// </summary>
            public string host
            {
                get;
                set;
            }

            /// <summary>
            /// WebSocket服务器端口;
            /// </summary>
            public int port
            {
                get;
                set;
            }
            /// <summary>
            /// 是否关闭其他的会话（0: 如果存在与其他客服的会话，则分配失败。1: 如果存在与其他客服的会话，则强制结束会话后，继续分配。）
            /// </summary>
            public int closeOtherSession { get; set; }
        }
        public Data data { get; set; }
    }
    public class QuickReplycontent:CustomServerModel
    {
        public class Data
        {
            public int quickReplyCateId { get; set; }
            public string quickReplyCate { get; set; }
            public List<item> quickReplyContent { get; set; }
            public class item
            {
                public int contentId { get; set; }
                public string content { get; set; }
            }
        }
        public List<Data> data { get; set; }
    }
    public class QuickReplyCategorycontents : CustomServerModel
    {
        public class Data
        {
            public int quickReplyCateId { get; set; }
            public string quickReplyCate { get; set; }
            public List<item> quickReplyContent { get; set; }
            public class item
            {
                public int contentId { get; set; }
                public string content { get; set; }
            }
        }
        public Data data { get; set; }
    }
    public class HistoryRecordListResp : CustomServerModel
    {
        public int pageIndex { get; set; }
        public int pageCount { get; set; }
        public int totalCount { get; set; }
        public List<Data> list { get; set; }
        public class Data
        {
            public int mobile { get; set; }
            public string userPhoto { get; set; }
            public string userName { get; set; }
            public string  endDate { get; set; }
            public string servicersName { get; set; }
            public string imOpenId { get; set; }
            public int sessionType { get; set; }
            public int sessionId { get; set; }
        }
        public Data data { get; set; }
    }
    public class QuickReplyCategoryedit 
    {
        public int editType { get; set; }
        public int categoryId { get; set; }
        public int categoryType { get; set; }
        public int servicerId { get; set; }
        public string categoryName { get; set; }
        
    }
    public class QuickReplyContentedit
    {
        public int editType { get; set; }
        public int contentId { get; set; }
        public int categoryId { get; set; }
        public int content { get; set; }
    }

}
