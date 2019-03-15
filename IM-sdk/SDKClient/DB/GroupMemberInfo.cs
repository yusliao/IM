using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDKClient.DB
{
    class GroupMemberInfo
    {

        /// <summary>
        /// 主键，自增
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        /// <summary>
        /// 群ID
        /// </summary>

        public int groupId { get; set; }
      

        public int userId { get; set; }
       
        public string userName { get; set; }

        public string photo { get; set; }

      

        /// <summary>
        /// 审核人
        /// </summary>
        public int auditUserId { get; set; }

        /// <summary>
        /// 谁邀请入群的
        /// </summary>
        public int recommendByUser { get; set; }

        /// <summary>
        /// 群昵称（在群里的昵称）
        /// </summary>
        public string memoInGroup { get; set; }

       

        /// <summary>
        /// 消息免打扰(0 未设置免打扰 1已设置免打扰)
        /// </summary>
        public bool doNotDisturb { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime? createTime { get; set; }
        /// <summary>
        /// 性别
        /// </summary>
        public byte sex { get; set; }
        public string mobile { get; set; }
        /// <summary>
        /// 省份
        /// </summary>
        public string province { get; set; }

        /// <summary>
        /// 城市
        /// </summary>
        public string city { get; set; }
    }
}
