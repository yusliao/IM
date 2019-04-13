using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SDKClient.Model;
using System.Net;
using ToolGood.Words;
using System.Security.Cryptography;
using SuperSocket.ClientEngine;
using System.ComponentModel;
using AspectCore.DynamicProxy;
namespace SDKClient
{
    /// <summary>
    /// 服务属性模型
    /// </summary>
    public class SDKProperty
    {
        public List<DB.messageDB> RoomList { get; set; } = new List<DB.messageDB>();
        public List<DB.friendApplyItem> FriendApplyList { get; set; } = new List<DB.friendApplyItem>();
        public System.Collections.Concurrent.ConcurrentDictionary<string, PackageInfo> MsgDic = new System.Collections.Concurrent.ConcurrentDictionary<string, PackageInfo>();
        public List<P2P.P2PClient> SendP2PList { get; set; } = new List<P2P.P2PClient>();
        private readonly Account account = new Account();
        
        internal static P2P.P2PServer P2PServer = new P2P.P2PServer();
        internal int m_StateCode = ServerStateConst.NotInitialized;
        public IPAddress IMServerIP { get; set; }
        public IPAddress QrServerIP { get; set; } 
        /// <summary>
        /// true: AUTH通过，可以发送消息到服务器
        /// </summary>
        public bool CanSendMsg { get; set; }

        /// <summary>
        /// Gets the current state of the work item.
        /// </summary>
        /// <value>
        /// The state.
        /// </value>
        public ServerState State
        {
            get
            {
                return (ServerState)m_StateCode;
            }
        }
       
        /// <summary>
        /// <summary>
        /// 当前登录账户
        /// </summary>
        public Account CurrentAccount
        {
            get
            {
                return account;
            }
        }
      
        /// <summary>
        /// 缓存的消息集合
        /// </summary>
        public List<PackageInfo> PackageCache { get; set; } = new List<PackageInfo>();
        /// <summary>
        /// 缓存的待发送的离线消息
        /// </summary>
        public Queue<PackageInfo> SendPackageCache = new Queue<PackageInfo>();
      
        /// <summary>
        /// 消息处理控制，控制消息的接收处理 
        /// 0 初始值 1 可以处理离线消息2 可以处理其他消息
        /// </summary>
        public int CanHandleMsg  = 1;

        /// <summary>
        /// 服务端JID
        /// </summary>
        public string ServerJID { get; set; }

        /// <summary>
        /// 关键字过滤服务对象，提供脏字检索、替换功能
        /// </summary>

        public static readonly StringSearchEx stringSearchEx = new StringSearchEx();

        /// <summary>
        /// 服务器端点信息
        /// </summary>
        public EndPoint remotePoint { get; set; }
        /// <summary>
        /// 重连信号,true说明可以进行重连，false 不可以进行重连
        /// </summary>
        public bool RaiseConnEvent { get; set; }
        public userType CurUserType { get; set; }

        #region 存储相关属性

#if CUSTOMSERVER
        /// <summary>
        /// 根资源路径，统一放置下载的图片、文件包、音频、视频
        /// </summary>
        public static string rootPath => System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "csmanjinba");
#elif HUIDU
        public static string rootPath => System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "hudumanjinba");
#elif CHECK
        public static string rootPath => System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "test");
#else
        /// <summary>
        /// 根资源路径，统一放置下载的图片、文件包、音频、视频
        /// </summary>
        public static string rootPath => System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "manjinba");
#endif
        /// <summary>
        /// 数据库路径
        /// </summary>
        public static string dbPath => System.IO.Path.Combine(rootPath, "db");
       
        /// <summary>
        /// 图片文件夹路径
        /// </summary>
        public static string imgPath => System.IO.Path.Combine(rootPath, "img");
        /// <summary>
        /// 头像文件夹路径
        /// </summary>
        public static string facePath => System.IO.Path.Combine(rootPath, "face");
        /// <summary>
        /// 文件存放路径
        /// </summary>
        public static string filePath => System.IO.Path.Combine(rootPath, "file");
        /// <summary>
        /// 我的收藏路径
        /// </summary>
        public static string mystorePath => System.IO.Path.Combine(rootPath, "mystore");
        /// <summary>
        /// 二维码路径
        /// </summary>
        public static string QrCodePath => System.IO.Path.Combine(rootPath, "QrCode");
        /// <summary>
        /// 具体个人业务数据库操作对象
        /// </summary>
        public static SQLite.SQLiteAsyncConnection SQLiteConn = null;
        /// <summary>
        /// 具体个人业务数据库操作对象
        /// </summary>
        public static SQLite.SQLiteAsyncConnection SQLiteReader = null;
        /// <summary>
        /// 公共业务数据库操作对象
        /// </summary>
        private static SQLite.SQLiteAsyncConnection _sqliteComConn;

        public static SQLite.SQLiteAsyncConnection SQLiteComConn
        {
            get
            {
                if(_sqliteComConn==null)
                    _sqliteComConn = Util.Helpers.Async.Run(async()=> await DAL.DALSqliteHelper.CreateCommDB());
                return _sqliteComConn;
            }
           
        }

        /// <summary>
        /// 重连状态   0表示未开始连接;1表示正在连接
        /// </summary>
        public int ConnState = 0;
        /// <summary>
        /// 启动对象
        /// </summary>
        public static string LaunchObj => "IMLaunch.exe";

#endregion
#region 获取随机字符串


        private static readonly RNGCryptoServiceProvider RNG = new RNGCryptoServiceProvider();
        
        /// <summary>
        /// 生成随机值，用于消息的ID
        /// </summary>
        public static string RNGId => GetID();
        private static string GetID()
        {
            //byte[] buf = new byte[12];
            //RNG.GetBytes(buf);
            //StringBuilder sb = new StringBuilder();
            //foreach (byte b in buf)
            //{
            //    sb.Append(b.ToString("x2"));
            //}

            //string id1 = Util.Helpers.Id.ObjectId();
            return Util.Helpers.Id.Guid();

           // return sb.ToString();

        }
#endregion
        
        public enum MessageType
        {
            txt = 0,
            img,
            file,
            audio,
            video,
            retract,//撤回
            notification,//通知 
            bigtxt,

            invitejoingroup,//邀请加入群

            exitgroup,//退群
            setmemberpower,//设置成员权限
            dismissgroup,
            redenvelopessendout,

            redenvelopesreceive,
            shareDyn,// 从广场分享的动态消息
            shareSpace,//从广场分享的空间消息
            onlinefile,
            eval,
            goods,
            order,
            custom,
            foreigndyn,
            

            [Description("smallVideo")]
            smallvideo,
            [Description("addGroupNotice")]
            addgroupnotice,
            [Description("deleteGroupNotice")]
            deletegroupnotice,
            [Description("个人名片")]
            usercard,
            addfriendaccepted,
            all =999,
            imgandvideo,
            

        }
        [Flags]
        public enum MessageState
        {
            noRead=0x00,
            isRead=0x01,
            
            cancel=0x04,
            sendfaile=0x08,
            sending=0x10,
            None= 0xff
        }
        public enum ErrorState
        {
            /// <summary>
            /// 成功
            /// </summary>
            [Description("成功")]
            None =0,
            /// <summary>
            /// 网络异常
            /// </summary>
            [Description("网络异常")]
            NetworkException,
            /// <summary>
            /// 服务器异常
            /// </summary>
            [Description("服务器异常")]
            ServerException,
            /// <summary>
            /// 用户取消
            /// </summary>
            [Description("取消操作")]
            Cancel,
            /// <summary>
            /// 超过限制
            /// </summary>
            [Description("超过限制")]
            OutOftheControl,
            /// <summary>
            /// 程序异常
            /// </summary>
            [Description("程序异常")]
            AppError

        }
        /// <summary>
        /// 用户类型
        /// </summary>
        public enum userType
        {
            /// <summary>
            /// IM客户
            /// </summary>
            [Description("IM客户")]
            imcustomer=0,
            /// <summary>
            /// 客户
            /// </summary>
            [Description("满金店客户")]
            mjdcustomer = 1,
            /// <summary>
            /// 客服
            /// </summary>
            [Description("客服")]
            customserver=2
        }
        /// <summary>
        /// (1很满意; 2满意; 3一般; 4不满意)
        /// </summary>
        public enum Satisfaction
        {
            very_satisfied= 1,
            satisfied = 2,
            just_so_so=3,
            not_satisfied
        }
        /// <summary>
        /// 聊天类型
        /// </summary>
        public enum chatType
        {
            [Description("个人聊天")]
            chat = 0,
            [Description("群聊")]
            groupChat = 1
        }
        /// <summary>
        /// 会话模式
        /// </summary>
        public enum SessionType
        {
            /// <summary>
            /// 正常聊天模式
            /// </summary>
            CommonChat = 0,
            /// <summary>
            /// 留言聊天模式
            /// </summary>
            ReceiverLeavingChat = 1,
            /// <summary>
            /// 临时聊天模式
            /// </summary>
            temporaryChat = 2,
            /// <summary>
            /// 文件小助手
            /// </summary>
            FileAssistant=3,
            /// <summary>
            /// 主动发起临时会话聊天模式
            /// </summary>
            SenderLeavingChat = 4


        }
        /// <summary>
        /// 1:发起请求2：结束会话3：发起评价4：回复评价
        /// </summary>
        public enum customOption
        {
            /// <summary>
            /// 开始会话
            /// </summary>
            [Description("接入聊天")]
            conn =1,
            /// <summary>
            /// 结束会话
            /// </summary>
            [Description("结束聊天")]
            over =2,
            /// <summary>
            /// 发起评价
            /// </summary>
            [Description("发起评价")]
            requestappraisal = 3,
            /// <summary>
            /// 回复评价
            /// </summary>
            [Description("回复评价")]
            responseappraisal = 4
           
        }
        public enum customState
        {
            /// <summary>
            /// 正常
            /// </summary>
            [Description("正常")]
            working = 1,
            /// <summary>
            /// 繁忙
            /// </summary>
            [Description("繁忙")]
            business = 2,
            /// <summary>
            /// 离开
            /// </summary>
            [Description("离开")]
            left = 3,
            /// <summary>
            /// 退出
            /// </summary>
            [Description("退出")]
            quit = 4
        }
        public enum ResourceState
        {
            NoStart =0,
            Working=1,
            IsCompleted=2,
            IsCancelled=3,
            Failed
        }
        public enum MessageDisturbOption
        {
            /// <summary>
            /// 关闭
            /// </summary>
            Off=0,
            /// <summary>
            /// 打开
            /// </summary>
            On=1
        }
        public enum BlackSettingOption
        {
            /// <summary>
            /// 拉黑对方
            /// </summary>
            lahei  = 0,
            /// <summary>
            /// 我被拉黑
            /// </summary>
            beilahei = 1,
            /// <summary>
            /// 双方互黑
            /// </summary>
            huxianglahei
        }
        public enum RetractType
        {
            /// <summary>
            /// 普通消息撤回
            /// </summary>
            Normal=0,
            /// <summary>
            /// 发送方在线文件撤回
            /// </summary>
            SourceEndOnlineRetract=1,
            /// <summary>
            /// 在线转离线
            /// </summary>
            OnlineToOffline=2,
            /// <summary>
            /// 离线转在线
            /// </summary>
            OfflineToOnline=3,
            /// <summary>
            /// 接收方在线文件撤回
            /// </summary>
            TargetEndOnlineRetract=4
        }
        /// <summary>
        /// 好友关系状态(0正常好友1我删除对方2对方删除我3双方都已删除
        /// </summary>
        public enum linkDelType
        {
            好友,
            我删除对方,
            对方删除我,
            双方互相删除

        }
        public enum ServerState : int
        {
            /// <summary>
            /// Not initialized
            /// </summary>
            NotInitialized = ServerStateConst.NotInitialized,

            /// <summary>
            /// In initializing
            /// </summary>
            Initializing = ServerStateConst.Initializing,

            /// <summary>
            /// Has been initialized, but not started
            /// </summary>
            NotStarted = ServerStateConst.NotStarted,

            /// <summary>
            /// In starting
            /// </summary>
            Starting = ServerStateConst.Starting,

            /// <summary>
            /// In running
            /// </summary>
            Running = ServerStateConst.Running,

            /// <summary>
            /// In stopping
            /// </summary>
            Stopping = ServerStateConst.Stopping,
        }
        public enum LogoutModel
        {
            Logout_self=1,
            Logout_kickout
        }
        /// <summary>
        /// 公告类型
        /// </summary>
        public enum NoticeType
        {
            /// <summary>
            /// 普通公告
            /// </summary>
            Common = 0,
            /// <summary>
            /// 入群须知
            /// </summary>
            JoinGroupNotice
        }


    }

    internal class ServerStateConst
    {
        public const int NotInitialized = 0;//初始状态

        public const int Initializing = 1; //QR连接

        public const int NotStarted = 2; //QR连接完毕

        public const int Starting = 3; //IM服务器连接

        public const int Running = 4; //IM服务器连接成功

        public const int Stopping = 5;
    }


}
