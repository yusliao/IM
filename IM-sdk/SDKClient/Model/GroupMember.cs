using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDKClient.Model
{
    /// <summary>
    /// 单个群成员
    /// </summary>
    public class GetGroupMemberPackage : PackageInfo
    {
        public override string api  => Protocol.ProtocolBase.GetgroupMember; 
        public override int apiId  => Protocol.ProtocolBase.GetgroupMemberCode; 
        public Data data { get; set; }
        public class Data
        {
            public int groupId { get; set; }
            /// <summary>
            /// 查看着ID
            /// </summary>
            public int userId { get; set; }
            /// <summary>
            /// 被查看着ID
            /// </summary>
            public int partnerId { get; set; }
            public groupmemberInfo user { get; set; }
        }
    }
    /// <summary>
    /// 群成员列表
    /// </summary>
    public class GetGroupMemberListPackage:PackageInfo
    {
        public override string api { get => Protocol.ProtocolBase.GetgroupMemberList; }
        public override int apiId { get => Protocol.ProtocolBase.GetgroupMemberListCode; }
        public groupmemberlist data { get; set; }
    }
    public class GetImDataListIncrPackage : PackageInfo
    {
        public override string api { get => Protocol.ProtocolBase.GetgroupMemberList; }
        public override int apiId { get => Protocol.ProtocolBase.GetgroupMemberListCode; }
        public Data data { get; set; }
        public class Data
        {
         
            public int userId { get; set; }
            public int myGroupIncrAll { get; set; }
            public int contactIncrAll { get; set; }
            public int strangerIncrAll { get; set; }
            public int myBlackIncr { get; set; }
            public string myGroupGetTime { get; set; }
            public string contactGetTime { get; set; }
            public string strangerGetTime { get; set; }
            public string myBlackGetTime { get; set; }
            public List<groupmemberInfo> myGroupItem { get; set; }
            public List<contact> contactsItem { get; set; }
            public List<AttentionUser> strangersItem { get; set; }
            public List<Defriend> myBlackItem { get; set; }
        }
    }
    public class groupmemberlist
    {
        public int min { get; set; }
        public int max { get; set; }
        public int groupId { get; set; }
        public string version { get; set; }
        public int total { get; set; }
        public List<groupmemberInfo> items { get; set; }
    }
    public class groupmemberInfo
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public int userId { get; set; }
        public string userName { get; set; }

        public string photo { get; set; }

        /// <summary>
        /// 群ID
        /// </summary>
        public int groupId { get; set; }

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
        /// 在群中的权限 0 普通用户 1 管理员 2 群主
        /// </summary>
        public byte userPower { get; set; }

        /// <summary>
        /// 群被置顶时间，空表示未置顶
        /// </summary>
        public DateTime? groupTopTime { get; set; }

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

        public string kfId { get; set; }

        public override string ToString()
        {
            return string.Format("{0}:{1}",this.userName,this.createTime);
        }
    }
}
