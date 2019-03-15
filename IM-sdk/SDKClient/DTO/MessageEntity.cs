using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDKClient.DTO
{
    [Serializable]
    public class MessageEntity
    {
        internal DB.messageDB db;
        /// <summary>
        /// 消息ID
        /// </summary>
        public string MsgId => db.msgId;
        /// <summary>
        /// from
        /// </summary>
        public string From => db.from;

        public string To => db.to;
        /// <summary>
        /// 房间ID
        /// </summary>

        public int RoomId => db.roomId;
        /// <summary>
        /// 聊天窗类型，chat:0 or groupchat:1
        /// </summary>
        public int RoomType => db.roomType;
        /// <summary>
        /// 消息类型 txt,img,file,audio,video,retract  回显消息时 过滤掉retarct消息
        /// </summary>
        public string MsgType => db.msgType; //txt,img,file,audio,video,retract  回显消息时 过滤掉retarct消息
        /// <summary>
        /// 资源标识
        /// </summary>
        public string ResourceId => db.resourceId;
        public string FileName => db.fileName; //资源名称 带扩展名
        public long FileSize => db.fileSize;
        /// <summary>
        /// 资源缩略图标识
        /// </summary>
        public string ResourcesmallId => db.resourcesmallId;
        /// <summary>
        /// 消息时间
        /// </summary>
        public DateTime MsgTime => db.msgTime;

        /// <summary>
        /// 消息操作记录 （未读0、已读1，2删除，4取消,16发送中）
        /// </summary>

        public int OptionRecord => db.optionRecord;//（未读、已读）
        /// <summary>
        /// 消息内容
        /// </summary>
        public string Content => db.content;
        /// <summary>
        /// 消息BODY，<see cref="Model.message.body"/>
        /// </summary>
        public dynamic Data => Util.Helpers.Json.ToObject<dynamic>(db.body);
        /// <summary>
        /// <see cref="SDKProperty.SessionType"/>
        /// 会话类型，0：正常聊天，1：陌生人聊天，2：临时会话，3:文件小助手，4：主动发起陌生人聊天
        /// </summary>
        public int SessionType => db.sessionType;
        /// <summary>
        /// @列表（用户ID），已逗号分隔。@ALL用数值-1代表
        /// </summary>
        public string TokenIds => db.TokenIds;
        public string SenderName => db.SenderName;
        public string SenderPhoto => db.SenderPhoto;
        /// <summary>
        /// 文件状态，0：未开始，1：下载中，2：已完成,3:取消，4：异常
        /// </summary>
        public int FileState => db.fileState;
        /// <summary>
        /// 向下兼容，用于<see cref="MessageEntity.Data"/> 值为NULL的时候。Source为源协议包，JSON字符串格式
        /// </summary>
        public string Source => db.Source;

    }
    public class MessageContext
    {
        public SDKProperty.chatType ChatType { get; set; }
        public int RoomId { get; set; }
        public List<MessageEntity> UnReadList { get; set; } = new List<MessageEntity>();
        public List<MessageEntity> StrangerMsgList { get; set; } = new List<MessageEntity>();
        public bool IsCallMe { get; set; } = false;
        public int UnReadCount { get; set; }
        public MessageEntity LastMessage { get; set; }
        public List<MessageEntity> PreloadLists { get; set; } = new List<MessageEntity>();


    }
    /// <summary>
    /// 基础数据上下文
    /// </summary>
    public class InfrastructureContext
    {

        public Model.GetGroupListPackage GroupListPackage { get; set; }
        public Model.GetContactsListPackage FriendListPackage { get; set; }


    }
    [Serializable]
    public class OfflineMessageContext
    {
        /// <summary>
        /// 离线消息集合，key=(聊天室ID,是否是群聊)
        /// </summary>

        public System.Collections.Concurrent.ConcurrentDictionary<Tuple<int, bool>, IList<MessageEntity>> context =
            new System.Collections.Concurrent.ConcurrentDictionary<Tuple<int, bool>, IList<MessageEntity>>();
        //valuetuple不支持序列化
        //public System.Collections.Concurrent.ConcurrentDictionary<(int, bool), IList<MessageEntity>> context =
        //    new System.Collections.Concurrent.ConcurrentDictionary<(int, bool), IList<MessageEntity>>();
    }
}
