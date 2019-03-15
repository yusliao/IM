using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDKClient.Protocol
{
    /// <summary>
    /// 返回消息响应码
    /// </summary>
    public enum StatusCode : int
    {

        [Description("请求(或处理)成功")]
        Success = 0,

        [Description("登录成功后才能执行该操作")]
        NoAuth = 300,

        [Description("请求超时")]
        RequestTimeout = 301,

        [Description("用户名或密码不正确")]
        AuthParameterError = 400,

        [Description("无效的令牌")]
        InvalidToken = 401,

        [Description("重复登录")]
        RepeatLogin = 402,

        [Description("单点登录下线通知")]
        KickAway = 403,
        [Description("请求无响应")]
        NoFound = 404,

        [Description("当前用户已有一个会话，不能重复登录")]
        ExistsSession = 405,

        [Description("会话已被禁用")]
        SessionForbid = 406,

        [Description("音视频通话鉴权未通过")]
        CallAuthError = 480,

        [Description("内部请求出错")]
        ServerInnerError = 500,

        [Description("无效的用户")]
        InvalidUser = 501,

        [Description("操作数据库时发生异常")]
        DatabaseExcept = 502,
        [Description("用户名或密码错误")]
        LoginError = -502,

        [Description("数据校验失败")]
        ValidateDataExcept = 582,

        [Description("传入数据不存在")]
        DataNoExists = 600,

        [Description("存储了非法数据")]
        StoredInvalidData = 601,

        [Description("不能频繁添加同一个好友")]
        AddFriendInvalid = 602,

        [Description("好友备注含有敏感词")]
        InvalidFriendRemark = 603,

        [Description("敏感词校验失败")]
        InvalidKeyword = 604,

        [Description("参数错误")]
        ParameterError = 605,

        [Description("好友申请不存在，或者已被审核")]
        AuditFriendApplyError = 606,

        [Description("指定的参数必须是好友")]
        NotFriend = 607,

        [Description("好友未被拉黑")]
        NoBlack = 608,

        [Description("好友已经被拉黑")]
        AlreadyBlacked = 609,

        [Description("好友不存在")]
        FriendNoExists = 610,

        [Description("用户不存在")]
        UserNoExists = 611,

        [Description("超出了最大用户限制")]
        GroupUserTooMany = 612,

        [Description("群主不存在")]
        GroupOwnerNoExists = 613,

        [Description("群组不存在")]
        GroupNoExists = 614,

        [Description("群已冻结")]
        GroupForbid = 615,

        [Description("一次邀请超过了群允许的最大用户数")]
        InviteUserTooMany = 616,

        [Description("用户已经加入了该群")]
        UserIsGroupMember = 617,

        [Description("只有管理员或者群主才能直接邀请其他用户加群")]
        NoPowerToInviteUser = 618,

        [Description("您申请太多次啦！")]
        JoinGroupApplyTooMany = 619,

        [Description("审核员不是群成员")]
        AuditorIsNotGroupMember = 620,

        [Description("只有管理员或者群主才能审核用户的加群申请")]
        NoPowerToAuditJoinGroup = 621,

        [Description("已被其他管理员或群主审核，或者用户未申请加群")]
        AlreadyCompleted = 622,

        [Description("当前用户不是群主，不能添加管理员")]
        NoPowerToAddGroupAdmin = 623,

        [Description("请检查所有待添加管理员的用户id是否正确，或者是否全部为普通成员")]
        NotCommonUser = 624,

        [Description("管理员人数达到群管理员的最大限额")]
        ToManyAdmin = 625,

        [Description("可能存在用户被移出群，或者已是管理员")]
        GroupMemberNoExists = 626,

        [Description("操作者必须为管理员或者群主")]
        UserIsNotGroupAdmin = 627,

        [Description("成员不存在，或者操作员权限不足")]
        NoPowerToExitGroup = 628,

        [Description("用户已被删除，或者有用户已被提权")]
        UserNoExistsOrIsAdmin = 629,

        [Description("无效的区域")]
        InvalidArea = 630,

        [Description("无效的手机号")]
        InvalidMobileNo = 631,

        [Description("无效的邮件地址")]
        InvalidEMail = 632,

        [Description("字符串中含有敏感词")]
        HasInvalidFilterKeywords = 633,

        [Description("对方已经把我拉黑，或已是好友，不能申请加对方为好友")]
        HeDefriendMe = 634,

        [Description("陌生人聊天消息不能超过3条")]
        StrangerMessageToMany = 635,

        [Description("已经是好友")]
        AlreadyBecomeFriend = 636,

        [Description("用户未关注该陌生人")]
        NoAttentionThisStranger = 637,

        [Description("不能添加自己为好友")]
        NotAddFriendBySelf = 638,

        [Description("排序值冲突")]
        RankError = 639,

        [Description("排序值超出范围")]
        RankOutOfRange = 640,

        [Description("旧密码不正确")]
        OldPasswordError = 641,

        [Description("临时聊天已结束")]
        TempChatOver = 642,

        [Description("对方拒绝接收粉丝留言")]
        NotRecAnonymousMsg = 643,

        [Description("群组不存在,或权限不足")]
        GroupNoExistsOrNoPower = 644,
        [Description("对方已把你拉黑")]
        BeDefriend = 648
    }

}
