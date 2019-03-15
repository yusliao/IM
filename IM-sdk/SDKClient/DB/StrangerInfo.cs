using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDKClient.DB
{
    /// <summary>
    /// 陌生人信息设置
    /// </summary>
    class StrangerInfoDB
    {
        [PrimaryKey]
        /// <summary>
        /// 用户ID
        /// </summary>
        public int UserId { get; set; }
        /// <summary>
        /// 手机号
        /// </summary>
        public string Mobile { get; set; }
        /// <summary>
        /// 性别
        /// </summary>
        public int Sex { get; set; }

        /// <summary>
        /// 昵称
        /// </summary>
        public string NickName { get; set; }


        /// <summary>
        /// 头像
        /// </summary>
        public string HeadImgMD5 { get; set; }
        /// <summary>
        /// 置顶聊天时间
        /// </summary>
        public DateTime? ChatTopTime { get; set; }

        /// <summary>
        /// 消息免打扰(0 未设置免打扰 1已设置免打扰)
        /// </summary>
        public int doNotDisturb { get; set; }
       
    }
}
