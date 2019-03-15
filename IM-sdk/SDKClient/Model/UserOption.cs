using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDKClient.Model
{
    public class GetUserPrivacySettingPackage : PackageInfo
    {
        public override string api => Protocol.ProtocolBase.GetUserPrivacySetting;
        public override int apiId => Protocol.ProtocolBase.GetUserPrivacySettingCode;
        public Data data { get; set; }
        public class Data
        {
            public int userId { get; set; }

            public UserPrivacySetting item { get; set; }
        }
        /// <summary>
        /// 用户隐私设置
        /// </summary>
        public class UserPrivacySetting
        {
            /// <summary>
            /// 用户ID
            /// </summary>
            public int userId { get; set; }

            /// <summary>
            /// 加好友是否需要验证
            /// </summary>
            public bool verifyFriendApply { get; set; }

            /// <summary>
            /// 允许通过手机找到我
            /// </summary>
            public bool searchMeByMobile { get; set; }

            /// <summary>
            /// 允许通过昵称找到我
            /// </summary>
            public bool searchMeByName { get; set; }

            /// <summary>
            /// 允许通过模糊位置找到我
            /// </summary>
            public bool searchMeByLocation { get; set; }

            /// <summary>
            /// 是否接收陌生信息
            /// </summary>
            public bool receiveAnonymousMsg { get; set; }

            /// <summary>
            /// 陌生人查看动态的条数(-1表示全部,0表示不允许,10表示10条)
            /// </summary>
            public int dynamicNumForAnonymous { get; set; }

            /// <summary>
            /// 朋友动态更新是否提醒
            /// </summary>
            public bool newDynamicNotice { get; set; }
        }
    }
    public class GetUserPackage : PackageInfo
    {
        public override string api => Protocol.ProtocolBase.GetUser;
        public override int apiId => Protocol.ProtocolBase.GetUserCode;
        public Data data { get; set; }
        public class Data
        {
            public int userId { get; set; }
            public user user { get; set; }
        }
    }
    public class UpdateUserDetailPackage : PackageInfo
    {
        public override string api => Protocol.ProtocolBase.UpdateUserDetail;
        public override int apiId => Protocol.ProtocolBase.UpdateUserDetailCode;
        public Data data { get; set; }
        public class Data
        {
            public int userId { get; set; }
            public string userName { get; set; }
            public string photo { get; set; }
            /// <summary>
            ///  0女  1男
            /// </summary>
            public int sex { get; set; }
            public DateTime? birthday { get; set; }
            public string locationAreaId { get; set; }
        }
    }

    public class UpdateuserPackage : PackageInfo
    {
        public override string api => Protocol.ProtocolBase.UpdateUser;
        public override int apiId => Protocol.ProtocolBase.UpdateUserCode;
        public Data data { get; set; }
        public class Data
        {
            public int userId { get; set; }
            public int updateType { get; set; }
            public string content { get; set; }
        }
    }



    public class SearchNewFriendPackage : PackageInfo
    {
        public override string api => Protocol.ProtocolBase.SearchNewFriend;
        public override int apiId => Protocol.ProtocolBase.SearchNewFriendCode;
        public Data data { get; set; }
        public class Data
        {
            public int userId { get; set; }
            public string keyword { get; set; }
            public int min { get; set; }
            public int max { get; set; }
            public int total { get; set; }
            /// <summary>
            /// 只有 userid,photo,userName 可用
            /// </summary>
            public List<Stranger> items { get; set; }
            public class Stranger
            {
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


            }
        }
    }



    public class user
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public int userId { get; set; }

        /// <summary>
        /// 满金店用户ID
        /// </summary>
        public int mjdUserId { get; set; }

        /// <summary>
        /// 手机号
        /// </summary>
        public string mobile { get; set; }
        /// <summary>
        /// 性别
        /// </summary>
        public byte sex { get; set; }

        /// <summary>
        /// 昵称
        /// </summary>
        public string userName { get; set; }

        /// <summary>
        /// 头像
        /// </summary>
        public string photo { get; set; }

        /// <summary>
        /// 标签
        /// </summary>
        public string label { get; set; }

        /// <summary>
        /// 签名留言
        /// </summary>
        public string userSign { get; set; }
        /// <summary>
        /// 所在行业
        /// </summary>
        public int trade { get; set; }

        /// <summary>
        /// 所在地ID
        /// </summary>
        public int locationAreaId { get; set; }

        /// <summary>
        /// 生日
        /// </summary>
        public DateTime? birthday { get; set; }

        /// <summary>
        /// 生肖
        /// </summary>
        public int zodiac { get; set; }
        /// <summary>
        /// 星座
        /// </summary>
        public int constellation { get; set; }

        /// <summary>
        /// 用户等级
        /// </summary>
        public int userGrade { get; set; }

        /// <summary>
        /// 全局禁言设置
        /// </summary>
        public string gagGlag { get; set; }

        /// <summary>
        /// 解禁时间
        /// </summary>
        public DateTime? removeGagTime { get; set; }

        /// <summary>
        /// 注册来源系统，参见代码类型REGIST_SOURCE
        /// </summary>
        public int registSource { get; set; }

        /// <summary>
        /// 用户来源，参见代码类型USER_COME_FROM
        /// </summary>
        public int userComeFrom { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime? createTime { get; set; }

        //二维码
        public string qrCode { get; set; }

        /// <summary>
        /// 省份ID
        /// </summary>
        public int areaAID { get; set; }
        /// <summary>
        /// 地市ID
        /// </summary>
        public int areaBID { get; set; }

        /// <summary>
        /// 省份名称
        /// </summary>
        public string areaAName { get; set; }
        /// <summary>
        /// 地市名称
        /// </summary>
        public string areaBName { get; set; }
        //public string partnerRemark { get; set; }
        /// <summary>
        /// 上次头像
        /// </summary>
        public string prePhoto { get; set; }
        /// <summary>
        /// 相册风格
        /// </summary>
        public int photoStyle { get; set; }
        /// <summary>
        /// 可访号
        /// </summary>
        public string kfId { get; set; }
        /// <summary>
        /// 可访号是否已经修改过(0未修改1已修改过)
        /// </summary>
        public int haveModifiedKfid { get; set; }
        /// <summary>
        /// 好友备注
        /// </summary>
        public string partnerRemark { get; set; }
    }
    public enum UpdateUserOption
    {
        修改昵称,
        修改绑定的手机号码,
        修改安全邮箱,
        修改安全密码,
        修改登录口令,
        修改生日,
        修改性别,
        修改签名,
        修改所在地 = 9,
        修改头像 = 11,
        接收新消息通知,//：接收新消息通知 1是0否
        显示语音及视频聊天的邀请弹窗,//：显示语音及视频聊天的邀请弹窗 1是0否
        修改语音及视频聊天铃声,//：语音及视频聊天铃声 1是0否
        修改通知显示详细信息,//：通知显示详细信息1是0否
        修改启用消息提示声音,//：启用消息提示声音1是0否
        修改启用消息提示振动,//：启用消息提示振动1是0否
        修改加好友是否需要验证,//：加好友是否需要验证 1是0否
        修改允许通过手机找到我,//：是否允许通过手机找到我 1是0否
        修改是否接收陌生信息,//：是否接收陌生信息 1是 0否
        修改陌生人查看动态的条数,//：陌生人查看动态的条数(-1表示全部,0表示不允许,10表示10条)
        修改是否允许通过昵称找到我,//：是否允许通过昵称找到我 1是0否
        修改是否允许通过模糊位置找到我,//：是否允许通过模糊位置找到我 1是0否
        修改可访号 = 28,
    }




}
