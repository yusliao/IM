using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDKClient.DB
{
    public class friendApplyItem
    {
        /// <summary>
        /// 主键，自增
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int friendApplyId { get; set; }
        public bool IsRead { get; set; }
        public DateTime? TopTime { get; set; }
        [SQLite.Unique]
        public int userId { get; set; }
        /// <summary>
        /// 是否审核通过
        /// </summary>
        public bool IsChecked { get; set; }
        public string name { get; set; }
        public string photo { get; set; }

        public string msgId { get; set; }
        /// <summary>
        /// 性别
        /// </summary>
        public byte sex { get; set; }
        /// <summary>
        /// 省份
        /// </summary>
        public string province { get; set; }

        /// <summary>
        /// 城市
        /// </summary>
        public string city { get; set; }

        /// <summary>
        /// 好友申请验证消息
        /// </summary>
        public string applyRemark { get; set; }

        /// <summary>
        /// 好友备注
        /// </summary>
        public string friendMemo { get; set; }
        /// <summary>
        ///好友申请的时间
        /// </summary>
        public DateTime? time { get; set; }

        /// <summary>
        /// 好友来源 1：群 2：手机号搜索 3：扫一扫 4：好友推荐 5：开店邀请 6:朋友验证 7：可访号搜索
        /// </summary>
        public int friendSource { get; set; }

        /// <summary>
        /// 来源群ID
        /// </summary>
        public string sourceGroup { get; set; }
        /// <summary>
        /// 群名称
        /// </summary>
        public string sourceGroupName { get; set; }


    }
}
