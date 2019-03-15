using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDKClient.DTO
{
    public class CSRoomListEntity
    {
        public class entity
        {
            public int userId { get; set; }
            public string photo { get; set; }
            public string mobile { get; set; }
            /// <summary>
            /// 移动设备地址信息（上网IP地址）
            /// </summary>
            public string address { get; set; }
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
            /// 会话类型：0空闲，1自己的会话，2别人的会话
            /// </summary>
            public int sessionType { get; set; }
            public string sessionId { get; set; }
            /// <summary>
            /// 终端类型：小程序，H5买家，APP 
            /// </summary>
            public string appType { get; set; }
        }
        public List<entity> data { get; set; }
       
    }
    public class CSTempCustomItem
    {
        public int userId { get; set; }
        public string userName { get; set; }
        public string photo { get; set; }
        public int UnreadCount { get; set; }
        public string  message { get; set; }
        public DateTime msgTime { get; set; }
    }
}
