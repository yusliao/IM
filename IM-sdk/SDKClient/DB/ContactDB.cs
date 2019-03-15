using SQLite;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDKClient.DB
{
    /// <summary>
    /// 联系人
    /// </summary>
    public class ContactDB
    {
       /// <summary>
       /// 备注名
       /// </summary>
        public string Remark { get; set; }

        /// <summary>
        /// 地区
        /// </summary>
        public string Area { get; set; }


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
        public byte Sex { get; set; }

        /// <summary>
        /// 昵称
        /// </summary>
        public string NickName { get; set; }
       

        /// <summary>
        /// 头像
        /// </summary>
        public string HeadImgMD5 { get; set; }
     

        /// <summary>
        /// 标签
        /// </summary>
        public string label { get; set; }
        //二维码
        public string QrCode { get; set; }
        /// <summary>
        /// 好友状态0:待验证；1已验证
        /// </summary>
        public int State { get; set; } = 1;
        /// <summary>
        /// 可访号
        /// </summary>
        public string KfId { get; set; }

        /// <summary>
        /// 可访号是否可以修改(0未修改1已修改过)
        /// </summary>
        public int haveModifiedKfid { get; set; }

        /// <summary>
        /// 好友来源 1：群 2：搜索 3：扫一扫 4：好友推荐 5：开店邀请 6：名片邀请 7:朋友验证
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
