using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDKClient.DTO
{
    public class StrangerEntity
    {
        internal DB.StrangerInfoDB db;
        /// <summary>
        /// 昵称
        /// </summary>
        public string NickName => db.NickName;


        /// <summary>
        /// 头像
        /// </summary>
        public string HeadImgMD5 => db.HeadImgMD5;
        /// <summary>
        /// 置顶聊天时间
        /// </summary>
        public DateTime? ChatTopTime => db.ChatTopTime;

        /// <summary>
        /// 消息免打扰(0 未设置免打扰 1已设置免打扰)
        /// </summary>
        public int doNotDisturb => db.doNotDisturb;
        
    }
}
