using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDKClient.Model
{
    public class GetContactsListPackage : PackageInfo
    {
        public override string api  => Protocol.ProtocolBase.GetContactsList; 
        public override int apiId => Protocol.ProtocolBase.GetContactsListCode; 
        public contacts data { get; set; }
        public class contacts
        {
            public int userId { get; set; }
            public string version { get; set; }
            public List<contact> items { get; set; }
        }
       
    }
    
    public class contact
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public int userId { get; set; }
        /// <summary>
        ///  userName 好友昵称
        /// </summary>
        public string userName { get; set; }
       


        /// <summary>
        /// 好友用户ID
        /// </summary>
        public int partnerUserId { get; set; }

        /// <summary>
        /// 好友备注姓名
        /// </summary>
        public string partnerRemark { get; set; }

        /// <summary>
        /// 关系类型(0正常1我拉黑对方2对方拉黑我3双方都已拉黑
        /// </summary>
        public int linkType { get; set; }

        /// <summary>
        /// 置顶聊天时间
        /// </summary>
        public DateTime? friendTopTime { get; set; }

        /// <summary>
        /// 消息免打扰(0 未设置免打扰 1已设置免打扰)
        /// </summary>
        public int doNotDisturb { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime? createTime { get; set; }
        /// <summary>
        /// photo好友头像
        /// </summary>
        public string photo { get; set; }
        /// <summary>
        /// 好友关系状态(0正常好友1我删除对方2对方删除我3双方都已删除
        /// </summary>
        public int linkDelType { get; set; }
        /// <summary>
        /// 省份
        /// </summary>
        public string province { get; set; }

        /// <summary>
        /// 城市
        /// </summary>
        public string city { get; set; }
        /// <summary>
        /// 性别
        /// </summary>
        public byte sex { get; set; }

        public string mobile { get; set; }
        /// <summary>
        /// 可访ID
        /// </summary>
        public string kfId { get; set; }

        /// <summary>
        /// 可访号是否已经修改过(0未修改1已修改过)
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
