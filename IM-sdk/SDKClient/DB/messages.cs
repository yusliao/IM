using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;
namespace SDKClient.DB
{
    /// <summary>
    /// 历史消息
    /// </summary>
    [Serializable]
    public class messageDB
    {
        /// <summary>
        /// 主键，自增
        /// </summary>
        [PrimaryKey,AutoIncrement]
        public int Id { get; set; }
        /// <summary>
        /// 消息ID
        /// </summary>
        [SQLite.Unique]
        public string msgId { get; set; }
        /// <summary>
        /// from
        /// </summary>
        public string from { get; set; }
        
        public string to { get; set; }
        /// <summary>
        /// 房间ID
        /// </summary>
        [Indexed]
        public int roomId { get; set; }
        /// <summary>
        /// 聊天窗类型，chat:0 or groupchat:1
        /// </summary>
        public int roomType { get; set; }
        /// <summary>
        /// 消息类型 txt,img,file,audio,video,retract  回显消息时 过滤掉retarct消息
        /// </summary>
        public string msgType { get; set; } //txt,img,file,audio,video,retract  回显消息时 过滤掉retarct消息
        /// <summary>
        /// 资源标识
        /// </summary>
        public string resourceId { get; set; }
        public string fileName { get; set; } //资源名称 带扩展名
        public long fileSize { get; set; }
        /// <summary>
        /// 文件状态，0：未开始，1：下载中，2：已完成,3:取消，4：异常
        /// </summary>
        public int fileState { get; set; } = 0;
        /// <summary>
        /// 资源缩略图标识
        /// </summary>
        public string resourcesmallId{ get; set; }
        [Indexed(Name ="msgtime")]
        /// <summary>
        /// 消息时间
        /// </summary>
        public DateTime msgTime { get; set; }
        [Indexed]
        /// <summary>
        /// 消息操作记录 （未读0、已读1，2删除，4取消, 8发送失败，16发送中）
        /// </summary>
       
        public int  optionRecord { get; set; }//（未读、已读）
        /// <summary>
        /// 公告ID
        /// </summary>
        public int noticeId { get; set; }
        /// <summary>
        /// 消息内容
        /// </summary>
        public string content { get; set; }
        public string body { get; set; }
        public int sessionType { get; set; }

        /// <summary>
        /// 数据源
        /// </summary>
        public string Source { get; set; }
        /// <summary>
        /// @列表（用户ID），已逗号分隔。@ALL用数值-1代表
        /// </summary>
        public string TokenIds { get; set; }
        public int UnReadCount { get; set; }
        public string SenderName { get; set; }
        public string SenderPhoto { get; set; }
    }
}
