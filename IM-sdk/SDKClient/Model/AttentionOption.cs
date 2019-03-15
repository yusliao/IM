using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDKClient.Model
{

    public class AddAttentionPackage:PackageInfo
    {
        public override string api => Protocol.ProtocolBase.AddAttention;
        public override int apiId => Protocol.ProtocolBase.AddAttentionCode;
        public Data data { get; set; }
        public class Data
        {
            /// <summary>
            /// 关注ID
            /// </summary>
            public int strangerLinkId { get; set; }
            /// <summary>
            /// 陌生人ID
            /// </summary>
            public int strangerId { get; set; }
            /// <summary>
            /// 用户ID
            /// </summary>
            public int userId { get; set; }
            /// 性别
            /// </summary>
            public byte strangerSex { get; set; }

            /// <summary>
            /// 昵称
            /// </summary>
            public string strangerName { get; set; }

            /// <summary>
            /// 头像
            /// </summary>
            public string strangerPhoto { get; set; }
            /// <summary>
            /// 省份
            /// </summary>
            public string strangerProvince { get; set; }

            /// <summary>
            /// 城市
            /// </summary>
            public string strangerCity { get; set; }




        }
    }
    public class GetAttentionListPackage :PackageInfo
    {
        public override string api => Protocol.ProtocolBase.GetAttentionList;
        public override int apiId => Protocol.ProtocolBase.GetAttentionListCode;
        public Data data { get; set; }
        public class Data
        {
            public int min { get; set; }
            public int max { get; set; }
            public int userId { get; set; }
            public int total { get; set; }
            public List<AttentionUser> items { get; set; }

        }


    }
    public class AttentionUser
    {
        /// <summary>
        /// 
        /// </summary>
        public int strangerLinkId { get; set; }
        /// <summary>
        /// 用户ID
        /// </summary>
        public int userId { get; set; }

        /// <summary>
        /// 陌生人昵称
        /// </summary>
        public string strangerName { get; set; }

        /// <summary>
        /// 陌生人头像
        /// </summary>
        public string strangerPhoto { get; set; }

        /// <summary>
        /// 陌生人ID
        /// </summary>
        public int strangerId { get; set; }

        /// <summary>
        /// 关注状态 0未关注1已关注
        /// </summary>
        public int attentionStatus { get; set; }

        /// <summary>
        /// 过往状态 0非过往 1过往的
        /// </summary>
        public int pastStatus { get; set; }

        /// <summary>
        /// 关注置顶的时间，空表示未置顶
        /// </summary>
        public DateTime? attentionTopTime { get; set; }

        /// <summary>
        /// 过往置顶的时间，空表示未置顶
        /// </summary>
        public DateTime? pastTopTime { get; set; }

        /// <summary>
        /// 结束聊天
        /// </summary>
        public bool endChat { get; set; }

        /// <summary>
        /// 回复聊天时间
        /// </summary>
        public DateTime? recoverChatTime { get; set; }

        /// <summary>
        /// 有无聊天
        /// </summary>
        public bool haveChat { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime? createTime { get; set; }
        /// <summary>
        /// 省份
        /// </summary>
        public string province { get; set; }

        /// <summary>
        /// 城市
        /// </summary>
        public string city { get; set; }
    }
    public class TopAttentionUserPackage:PackageInfo
    {
        public override string api => Protocol.ProtocolBase.TopAttentionUser;
        public override int apiId => Protocol.ProtocolBase.TopAttentionUserCode;
        public Data data { get; set; }
        public class Data
        {
            public int strangerLinkId { get; set; }
           
            public int userId { get; set; }
            /// <summary>
            /// 操作类型：setTop置顶；cancelTop取消置顶
            /// </summary>
            public string oprationType { get; set; }

        }
    }
    public class DeleteAttentionUserPackage : PackageInfo
    {
        public override string api => Protocol.ProtocolBase.DeleteAttentionUser;
        public override int apiId => Protocol.ProtocolBase.DeleteAttentionUserCode;
        public Data data { get; set; }
        public class Data
        {
            public int strangerLinkId { get; set; }

            public int userId { get; set; }

        }
    }


}
