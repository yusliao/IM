using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDKClient.Model
{
    public class GetFriendPackage : PackageInfo
    {
        public override string api => Protocol.ProtocolBase.GetFriend;
        public override int apiId => Protocol.ProtocolBase.GetFriendCode;
        public Data data { get; set; }
        public class Data
        {
            public int userId { get; set; }
            public int friendId { get; set; }
            public contact item { get; set; }
        }
    }
    public class AddFriendPackage:PackageInfo
    {
        public override string api => Protocol.ProtocolBase.AddFriend;
        public override int apiId => Protocol.ProtocolBase.AddFriendCode;
        public addfriend data { get; set; }
    }
    public class addfriend
    {
       public int friendApplyId { get; set; }
        public string photo { get; set; }
        public  string userName { get; set; }
        public string remark { get; set; }
        /// <summary>
        /// 申请好友需验证的信息
        /// </summary>
        public string applyRemark { get; set; }

        /// <summary>
        /// 好友名称备注
        /// </summary>
        public string friendMemo { get; set; }
        public int friendId { get; set; }
       
        public int userId { get; set; }
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
    public class AddFriendAcceptedPackage:PackageInfo
    {
        public override string api => Protocol.ProtocolBase.AddFriendAccepted;
        public override int apiId => Protocol.ProtocolBase.AddFriendAcceptedCode;
        public addFriendAccepted data { get; set; }
        public class addFriendAccepted
        {
            public string partnerPhoto { get; set; }
            public string partnerName { get; set; }
            public int userId { get; set; }
            public int friendId { get; set; }
            public int auditStatus { get; set; }
            public string auditRemark { get; set; }
            /// <summary>
            /// 好友备注
            /// </summary>
            public string friendMemo { get; set; }
            /// <summary>
            /// 0-正常 1-代发
            /// </summary>
            public int type { get; set; }

        }

    }
    /// <summary>
    /// 20.修改好友的关系类型（拉黑、恢复正常等）
    /// </summary>
    public class UpdateFriendRelationPackage:PackageInfo
    {
        public override string api => Protocol.ProtocolBase.UpdateFriendRelation;
        public override int apiId => Protocol.ProtocolBase.UpdateFriendRelationCode;
        public Data data { get; set; }
        public class Data
        {
            /// <summary>
            /// 好友的关系类型 0 正常 1 我拉黑对方，2被拉黑，3双方拉黑
            /// </summary>
            public int relationType { get; set; }
            public int userId { get; set; }
            public int friendId { get; set; }
        }
    }
    public class UpdateFriendSetPackage : PackageInfo
    {
        public override string api => Protocol.ProtocolBase.UpdateFriendSet;
        public override int apiId => Protocol.ProtocolBase.UpdateFriendSetCode;
        public Data data { get; set; }
        public class Data
        {
            public int userId { get; set; }
            public int friendId { get; set; }
            public int setType { get; set; }
            public string content { get; set; }
        }
        public enum FriendSetOption
        {
            None=0,
            设置是否消息置顶,//（内容传入0表示取消置顶/1设置置顶）
            设置免打扰, //（内容传入0表示取消免打扰/1设置免打扰)
            设置好友备注
        }
    }
    public class DeleteFriendPackage:PackageInfo
    {
        public override string api => Protocol.ProtocolBase.DeleteFriend;
        public override int apiId => Protocol.ProtocolBase.DeleteFriendCode;
        public class Data
        {
            public int userId { get;set; }
            /// <summary>
            /// 好友ID
            /// </summary>
            public int friendId { get; set; }
            /// <summary>
            /// type : 0-用户人工删除 1-系统自动删除
            /// </summary>
            public int type { get; set; }
        } 
        public Data data { get; set; }
    }
    public class GetFriendApplyListPackage:PackageInfo
    {
        public override string api => Protocol.ProtocolBase.GetFriendApplyList;
        public override int apiId => Protocol.ProtocolBase.GetFriendApplyListCode;
        public class Data
        {
            public int userId { get; set; }
            public int min { get; set; }
            public int max { get; set; }
            public int total { get; set; }
            public List<FriendApply> items { get; set; }
        }
        public Data data { get; set; }
    }

    //public class GetFriendApplyPackage:PackageInfo
    //{
    //    public override string api => Protocol.ProtocolBase.GetFriend;
    //    public override int apiId => Protocol.ProtocolBase.GetFriendCode;
    //    public class Data {
    //        public int userId { get; set; }
    //        public int friendId { get; set; }
    //        public FriendApply item { get; set; }
    //    }
    //}
    
    public class FriendApply
    {

        /// <summary>
        /// 用户ID
        /// </summary>
        public int userId { get; set; }
        /// <summary>
        /// 申请用户ID
        /// </summary>
        public int applyUserId { get; set; }

        /// <summary>
        /// 申请用户名
        /// </summary>
        public string applyUserName { get; set; }

        /// <summary>
        /// 申请用户头像
        /// </summary>
        public string applyUserPhoto { get; set; }

        /// <summary>
        /// 审核状态（0未审核/1已通过/2已拒绝/3已忽略） 
        /// </summary>
        public byte auditStatus { get; set; }

        /// <summary>
        /// 申请时间
        /// </summary>
        public DateTime? applyTime { get; set; }
        /// <summary>
        /// 好友备注
        /// </summary>
        public string friend_memo { get; set; }
    }

    public class GetFriendApplyStatus : PackageInfo
    {

    }
    public class GetBlackListPackage:PackageInfo
    {
        public override string api => Protocol.ProtocolBase.GetBlackList;
        public override int apiId => Protocol.ProtocolBase.GetBlackListCode;
        public class Data
        {
            public int userId { get; set; }
            public int min { get; set; }
            public int max { get; set; }
            public List<Defriend> items { get; set; }
            public int total { get; set; }

        }
        public Data data { get; set; }
    }
    public class DeleteFriendApplyPackage : PackageInfo
    {
        public override string api => Protocol.ProtocolBase.DeleteAttentionUser;
        public override int apiId => Protocol.ProtocolBase.DeleteFriendApplyCode;
        public Data data { get; set; }
        public class Data
        {
            public List<int> friendIds { get; set; }

            public int userId { get; set; }

        }
    }
    /// 黑名单
    /// </summary>
    public class Defriend
    {
        /// <summary>
        /// 拉黑好友ID
        /// </summary>
        public int userId { get; set; }

        /// <summary>
        /// 昵称
        /// </summary>
        public string userName { get; set; }

        /// <summary>
        /// 头像
        /// </summary>
        public string photo { get; set; }
    }

    public enum AuditStatus
    {
        未审核=0,
        已通过,
        已拒绝,
        已忽略
    }

    


}
