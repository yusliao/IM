using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDKClient.Model
{
    public class AddNoticePackage:PackageInfo
    {
        public override string api => Protocol.ProtocolBase.AddNotice;
        public override int apiId => Protocol.ProtocolBase.AddNoticeCode;
        public Data data { get; set; }
        public class Data
        {
            public int groupId { get; set; }
            public string groupName { get; set; }
            public string title { get; set; }
            public string content { get; set; }
            public int noticeId { get; set; }
            /// <summary>
            /// 公告类型 0：普通公告，1：入群须知
            /// </summary>
            public int type { get; set; }
            public DateTime? publishTime { get; set; }
        }

    }
    public class RetractNoticePackage : PackageInfo
    {
        public override string api => Protocol.ProtocolBase.RetreatNotice;
        public override int apiId => Protocol.ProtocolBase.RetreatNoticeCode;
        public Data data { get; set; }
        public class Data
        {
            public int groupId { get; set; }
            public string groupName { get; set; }
            public string title { get; set; }
            public string content { get; set; }
            public int noticeId { get; set; }
        }

    }
    public class SetMemberPowerPackage:PackageInfo
    {
        public override string api => Protocol.ProtocolBase.SetMemberPower;
        public override int apiId => Protocol.ProtocolBase.SetMemberPowerCode;
        public Data data { get; set; }
        public class Data
        {
            /// <summary>
            /// admin时为提升管理员unAdmin时为取消管理员
            /// </summary>
            public string type { get; set; }
            public int adminId { get; set; }
            public List<int> userIds { get; set; }
            public List<string> userNames { get; set; }
            public int groupId { get; set; }
        }
    }
    public class GetGroupListPackage : PackageInfo
    {
        public override string api  => Protocol.ProtocolBase.GetgroupList; 
        public override int apiId  => Protocol.ProtocolBase.GetgroupListCode; 
        public grouplist data { get; set; }
    }
    
    public class grouplist
    {
        public int userId { get; set; }
        public int min { get; set; }
        public int max { get; set; }
        public int groupType { get; set; }
        public string version { get; set; }
        public int total { get; set; }
        public myGroupList items { get; set; }
    }
    public class myGroupList
    {
        public List<group> ownerGroup { get; set; }
        public List<group> adminGroup { get; set; }
        public List<group> joinGroup { get; set; }
    }
    public class ExitGroupPackage : PackageInfo
    {
        public override string api => Protocol.ProtocolBase.ExitGroup;
        public override int apiId => Protocol.ProtocolBase.ExitGroupCode;
        public Data data { get; set; }
        public class Data
        {
            public List<int> userIds { get; set; }
            public List<string> userNames { get; set; }
            public int adminId { get; set; }
            public List<int> adminIds { get; set; }
            public int groupId { get; set; }
        }
    }
    public class CreateGroupPackage : PackageInfo
    {
        public override string api => Protocol.ProtocolBase.CreateGroup;
        public override int apiId => Protocol.ProtocolBase.CreateGroupCode;
        public Data data { get; set; }
        public class Data
        {
            public List<int> items { get; set; }
            public int groupId { get; set; }
        
            public string groupPhoto { get; set; }

            public string groupName { get; set; }
        }
    }
    public class CreateGroupComponsePackage: PackageInfo
    {
        public override string api => Protocol.ProtocolBase.CreateGroup;
        public override int apiId => Protocol.ProtocolBase.CreateGroupCode;
        public group data { get; set; }
       

    }
    /// <summary>
    /// 解散群
    /// </summary>
    public class DismissGroupPackage:PackageInfo
    {
        public override string api => Protocol.ProtocolBase.DismissGroup;
        public override int apiId => Protocol.ProtocolBase.DismissGroupCode;
        public Data data { get; set; }
        public class Data
        {
            public int ownerId { get; set; }
            public int groupId { get; set; }
        }
    }
    /// <summary>
    /// 邀请入群
    /// </summary>
    public class InviteJoinGroupPackage:PackageInfo
    {
        public override string api => Protocol.ProtocolBase.InviteJoinGroup;
        public override int apiId => Protocol.ProtocolBase.InviteJoinGroupCode;
        public Data data { get; set; }
        public class Data
        {
            public List<int> userIds { get; set; }
            public List<int> targetGroupIds { get; set; }

            public int targetGroupId { get; set; }
            public int groupId { get; set; }
            public int InviteUserId { get; set; }
            public string groupPhoto { get; set; }

            public string groupName { get; set; }
            /// <summary>
            /// 群简介
            /// </summary>
            public string groupIntroduction { get; set; }
            /// <summary>
            /// 邀请人头像
            /// </summary>
            public string inviteUserPhoto { get; set; }
            /// <summary>
            /// 邀请人昵称
            /// </summary>
            public string inviteUserName { get; set; }
        }
    }
    public class JoinGroupPackage:PackageInfo
    {
        public override string api => Protocol.ProtocolBase.JoinGroup;
        public override int apiId => Protocol.ProtocolBase.JoinGroupCode;
        public Data data { get; set; }
        public class Data
        {
            /// <summary>
            /// 用户ID
            /// </summary>
            public int userId { get; set; }
            /// <summary>
            /// 群ID
            /// </summary>
            public int groupId { get; set; }
            /// <summary>
            /// 用户昵称
            /// </summary>
            public string userName { get; set; }
            /// <summary>
            /// 用户头像
            /// </summary>
            public string photo { get; set; }
            /// <summary>
            /// 邀请人ID
            /// </summary>
            public int InviteUserId { get; set; }
            /// <summary>
            /// 申请备注
            /// </summary>
            public string remark { get; set; }
            /// <summary>
            /// 是否接受
            /// </summary>
            public bool isAccepted { get; set; }
        }
    }
    public class JoinGroupAcceptedPackage:PackageInfo
    {
        public override string api => Protocol.ProtocolBase.JoinGroupAccepted;
        public override int apiId => Protocol.ProtocolBase.JoinGroupAcceptedCode;
        public Data data { get; set; }
        public class Data
        {
            /// <summary>
            /// 管理员ID
            /// </summary>
            public int adminId { get; set; }
            /// <summary>
            /// 被审核用户ID
            /// </summary>
            public int memberId { get; set; }
            /// <summary>
            /// 群组ID
            /// </summary>
            public int groupId { get; set; }
            /// <summary>
            /// 审核备注
            /// </summary>
            public string auditRemark { get; set; }
            /// <summary>
            /// 用户昵称
            /// </summary>
            public string userName { get; set; }
            /// <summary>
            /// 用户头像
            /// </summary>
            public string photo { get; set; }
            /// <summary>
            /// 审核状态 1 审核通过 2 拒绝加群 3 忽略加群申请
            /// </summary>
            public int auditStatus { get; set; }
        }
    }
    public class UpdateUserSetsInGroupPackage:PackageInfo
    {
        public override string api => Protocol.ProtocolBase.UpdateUserSetsInGroup;
        public override int apiId => Protocol.ProtocolBase.UpdateUserSetsInGroupCode;
        public Data data { get; set; }
        public class Data
        {
            public int  userId { get; set; }
            public int groupId { get; set; }
            /// <summary>
            /// 1我的群昵称 2 设置置顶 3 是否免打扰（1设置免打扰0不设置免打扰）
            /// </summary>
            public int  setType { get; set; }
            public string content { get; set; }

        }
    }
    public class GetGroupPackage:PackageInfo
    {
        public override string api => Protocol.ProtocolBase.GetGroup;
        public override int apiId => Protocol.ProtocolBase.GetGroupCode;
        public Data data { get; set; }
        public class Data
        {
            public int groupId { get; set; }
            public group item { get; set; }
        }

    }
    public class UpdateGroupPackage:PackageInfo
    {
        public override string api => Protocol.ProtocolBase.UpdateGroup;
        public override int apiId => Protocol.ProtocolBase.UpdateGroupCode;
        public Data data { get; set; }
        public class Data
        {
            public int userId { get; set; }
            public int groupId { get; set; }
           
            public int setType { get; set; }
            public string content { get; set; }

        }

    }
    /// <summary>
    /// 修改群资料 当setType=4时：
   // 1- 管理员审批入群 2- 自由入群 3- 密码入群(数字+逗号+密码)
    /// </summary>
    public enum SetGroupOption
    {
        None=0,
        修改群名称,
        修改群简介,
        修改群头像,
        /// <summary>
        /// 1- 管理员审批入群 2- 自由入群 3- 密码入群(数字+逗号+密码)
        /// </summary>
        设置入群验证方式
    }
    /// <summary>
    /// 设置群成员权限
    /// </summary>
    public enum SetCancelGroupPowerOption
    {
        SetManager,
        CancelManager
    }
   
    public class group
    {
      
        public int groupId { get; set; }

        /// <summary>
        /// 群主ID
        /// </summary>
        public int groupOwnerId { get; set; }

        /// <summary>
        /// 群创建者ID
        /// </summary>
        public int groupCreator { get; set; }

        /// <summary>
        /// 群类型(0普通群1官方群)
        /// </summary>
        public int groupType { get; set; } = 0;

        /// <summary>
        /// 群名
        /// </summary>
        public string groupName { get; set; }

        /// <summary>
        /// 群头像
        /// </summary>
        public string groupPhoto { get; set; }

        /// <summary>
        /// 群简介
        /// </summary>
        public string groupIntroduction { get; set; }

        /// <summary>
        /// 群标签
        /// </summary>
        public string groupLabel { get; set; }

        /// <summary>
        /// 群人数
        /// </summary>
        public int groupNumMember { get; set; }

        /// <summary>
        /// 群冻结状态(0未冻结1已冻结)
        /// </summary>
        public string groupFrozenStatus { get; set; }

        /// <summary>
        /// 群解冻时间
        /// </summary>
        public string groupUnfrozenTime { get; set; }

        /// <summary>
        /// 加群密钥
        /// </summary>
        public string groupSecurityKey { get; set; }

        /// <summary>
        /// 群创建时间
        /// </summary>
        public DateTime? groupCreateTime { get; set; }
        

        public List<groupmemberInfo> items { get; set; }
        /// <summary>
        /// 群置顶时间
        /// </summary>
        public DateTime? groupTopTime { get; set; }
        /// <summary>
        /// 消息免打扰
        /// </summary>
        public bool doNotDisturb { get; set; }
        /// <summary>
        /// 群编码
        /// </summary>
        public int groupCode { get; set; }
        /// <summary>
        /// 入群验证方式: 1 管理员审批入群 2 自由入群 3 密码入群
        /// </summary>
        public byte joinAuthType { get; set; }
      

    }

}
