using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Reflection;
using SuperSocket.ClientEngine;
using System.ComponentModel.Composition;
using System.Net;
using System.ComponentModel.Composition.Hosting;
using SDKClient.Model;

using SDKClient.Protocol;
using System.IO;

using NLog;
using Util.ImageOptimizer;
using Util;
using SDKClient.WebAPI;
using SDKClient.P2P;
using static SDKClient.SDKProperty;

using SDKClient.DTO;
using SuperSocket.ProtoBase;
using SDKClient.DB;
using System.Configuration;
using System.Threading;
using SDKClient.Controllers;

namespace SDKClient
{
    /// <summary>
    /// 服务功能模型
    /// </summary>
    public class SDKClient
    {
        [ImportMany(typeof(CommandBase))]
        internal IEnumerable<CommandBase> CommmandSet { get; set; }  //命令集合
        [ImportMany(typeof(Util.Dependency.ConfigBase))]
        private IEnumerable<Util.Dependency.ConfigBase> EntityConfigs { get; set; }
        internal  EasyClient<PackageInfo> ec = new EasyClient<PackageInfo>();//通讯接口对象

        internal static Util.Logs.ILog logger = Util.Logs.Log.GetLog(typeof(SDKClient));//日志对象
        public event EventHandler<PackageInfo> NewDataRecv; //转发服务端数据
        public event EventHandler<P2PPackage> P2PDataRecv; //p2p消息处理
        public event EventHandler<OfflineMessageContext> OffLineMessageEventHandle; //转发离线聊天消息
       
        private static bool needStop = false;
        public readonly SDKProperty property = new SDKProperty();//SDK挂载的属性集对象
        public static readonly SDKClient Instance  = new SDKClient();
        
        //心跳定时器
        public System.Threading.Timer timer = null;
        /// <summary>
        /// 是否连接
        /// </summary>
        public bool IsConnected => ec.IsConnected && System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable();

        /// <summary>
        /// 消息发送失败处理事件
        /// </summary>
        public event EventHandler<(int roomId, string msgId)> SendFaile;
        internal void OnSendFaile(int roomId, string msgId)
        {
            Instance.SendFaile?.BeginInvoke(this, (roomId, msgId), null, null);
        }
        /// <summary>
        /// 消息回包确认事件
        /// </summary>
        public event EventHandler<(int roomId, MessagePackage package, DateTime sendTime)> SendConfirm;
        /// <summary>
        /// 消息回包确认
        /// </summary>
        /// <param name="roomId"></param>
        /// <param name="isgroup"></param>
        /// <param name="msgId"></param>
        internal void OnSendConfirm(int roomId, MessagePackage package, DateTime dateTime)
        {
            Instance.SendConfirm?.BeginInvoke(this, (roomId, package, dateTime), null, null);
        }
        internal void OnOffLineMessageEventHandle(OfflineMessageContext context)
        {
            Instance.OffLineMessageEventHandle?.BeginInvoke(this, context, null, null);
        }
        /// <summary>
        /// 处理待发送的协议包
        /// </summary>
        /// <param name="packageInfo"></param>
        internal void OnSendCommand(PackageInfo packageInfo)
        {
            if (property.CanSendMsg)
            {
                var cmd = CommmandSet.FirstOrDefault(c => c.Name == packageInfo.api) ?? new CommandBase();
                try
                {
                    cmd?.SendCommand(ec, packageInfo);//日志及入库操作
                }
                catch (Exception ex)
                {
                    logger.Error($"SendCommand失败 session:{SDKClient.Instance.property.CurrentAccount.Session},error:{ex.Message} \r\n{Util.Helpers.Json.ToJson(packageInfo)}");
                }
            }
            else
            {
                switch (packageInfo.apiId)
                {
                    case ProtocolBase.loginCode:
                    case ProtocolBase.HeartMsgCode:
                    case ProtocolBase.authCode:
                    case ProtocolBase.QRCancelCode:
                    case ProtocolBase.QRConfirmCode:
                    case ProtocolBase.QRExpiredCode:
                    case ProtocolBase.QRScanCode:
                    case ProtocolBase.LogoutCode:
                    case ProtocolBase.ForceExitCode:
                    case ProtocolBase.GetClientIDCode:
                    case ProtocolBase.PCAutoLoginApplyCode:
                    case ProtocolBase.GetLoginQRCodeCode: //连接请求包
                        var cmd = CommmandSet.FirstOrDefault(c => c.Name == packageInfo.api) ?? new CommandBase();
                        try
                        {
                            cmd?.SendCommand(ec, packageInfo);//日志及入库操作
                        }
                        catch (Exception ex)
                        {
                            logger.Error($"SendCommand失败 session:{SDKClient.Instance.property.CurrentAccount.Session},error:{ex.Message} \r\n{Util.Helpers.Json.ToJson(packageInfo)}");
                        }
                        break;
                    case ProtocolBase.messageCode:
                        MessagePackage messagePackage = packageInfo as MessagePackage;
                        if (messagePackage.data.type == nameof(SDKProperty.chatType.chat))
                            SDKClient.Instance.OnSendFaile(packageInfo.to.ToInt(), packageInfo.id);
                        else
                        {
                            SDKClient.Instance.OnSendFaile(messagePackage.data.groupInfo.groupId, packageInfo.id);
                        }
                        break;
                    default:
                        logger.Error($"断网下无效请求： SendCommand失败 session:{SDKClient.Instance.property.CurrentAccount.Session} \r\n{Util.Helpers.Json.ToJson(packageInfo)}");
                        break;
                }
            }
        }
        /// <summary>
        ///连接状态 true:success;false:failed
        ///通知UI底层通讯状态
        /// </summary>
        public event EventHandler<bool> ConnState;
        internal void OnSendConnState(bool isSuccess)
        {
            SDKClient.Instance.ConnState?.BeginInvoke(this, isSuccess, null, null);
            property.CanSendMsg = isSuccess;
#if CUSTOMSERVER
            property.CanHandleMsg = 2;

#endif

            SendCachePackage(isSuccess);
            RecvCachePackage(isSuccess);
        }
        /// <summary>
        /// 发送缓存的消息包
        /// </summary>
        /// <param name="isSuccess"></param>
        private void SendCachePackage(bool isSuccess)
        {
            if (isSuccess)
            {
                if (property.SendPackageCache.Any())
                {
                    var array = property.SendPackageCache.ToArray();
                    property.SendPackageCache.Clear();

                    foreach (var item in array)
                    {
                        item.Send(ec);
                    }
                }
            }
        }
        private void RecvCachePackage(bool isSuccess)
        {
            if (isSuccess)
            {
                if (property.PackageCache.Any())
                {
                    var array = property.PackageCache.ToArray();
                    property.PackageCache.Clear();

                    foreach (var item in array)
                    {
                        OnNewDataRecv(item);
                    }
                }
            }
        }

        private SDKClient()
        {
            #region MEF配置
            MyComposePart();
            #endregion
            Init();
            #region 关键字过滤
            try
            {
                List<string> list = new List<string>();
                using (StreamReader sw = new StreamReader(File.OpenRead("BadWord.txt"), Encoding.UTF8))
                {
                    string key = sw.ReadLine();
                    while (key != null)
                    {
                        if (key != string.Empty)
                        {
                            list.Add(key);
                        }
                        key = sw.ReadLine();
                    }
                }
                SDKProperty.stringSearchEx.SetKeywords(list);
            }
            catch (Exception ex)
            {

            }
            #endregion

        }
        private void Init()
        {
            #region AOP设置 调试太卡放弃使用
            //  Util.Helpers.Ioc.Register(EntityConfigs.ToArray());

            #endregion
            #region 通讯组件配置

            InitSocketAsync();

            #endregion

        }

        private async void InitSocketAsync()
        {

            if (ec != null)
            {
                ec.NewPackageReceived -= Ec_NewPackageReceived;
                ec.Error -= ec_Error;
                ec.Connected -= ec_Connected;
                ec.Closed -= ec_Closed;
                if (ec.IsConnected)
                    await ec.Close();
            }

            ec = null;
            ec = new EasyClient<PackageInfo>();
            // ec.Initialize(Util.Helpers.Ioc.Create<FixedHeaderReceiveFilter<Model.PackageInfo>>());
            ec.Initialize(new RecvFilter2());
            ec.NewPackageReceived += Ec_NewPackageReceived;
            ec.Error += ec_Error;
            ec.Connected += ec_Connected;
            ec.Closed += ec_Closed;
        }

        void MyComposePart()
        {
            var catalog = new AssemblyCatalog(Assembly.GetExecutingAssembly());
            var container = new CompositionContainer(catalog);
            //将部件（part）和宿主程序添加到组合容器
            container.ComposeParts(this);
        }
        /// <summary>
        /// socket已关闭
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ec_Closed(object sender, EventArgs e)
        {
            // property.RaiseConnEvent = false;
            // System.Threading.Interlocked.Exchange(ref property.ConnState, 0);
            logger.Info("连接被关闭");
            ConnState?.BeginInvoke(this, false, null, null);
            StartReConn();
        }
        /// <summary>
        /// 重连，分QR服务器重连和IM服务器重连，通过property.State识别
        /// </summary>
        public async void StartReConn()
        {
            if (!needStop && property.RaiseConnEvent)
            {
                //没有开始连接，发送连接请求,过滤掉重复请求
                if (System.Threading.Interlocked.CompareExchange(ref property.ConnState, 1, 0) == 0)
                {
                    ConnState?.BeginInvoke(this, false, null, null);
                    property.RaiseConnEvent = false;
                    if (ec.IsConnected)//已连接断开重新连接
                        SendLogout(LogoutModel.Logout_self);
                    if (property.remotePoint == null)
                    {
                        if (property.State > ServerState.NotStarted)
                        {

                            property.remotePoint = new IPEndPoint(property.IMServerIP, ProtocolBase.IMPort);

                        }
                        else
                        {
                            property.remotePoint = new System.Net.IPEndPoint(property.QrServerIP, ProtocolBase.QrLoginPort);
                        }
                    }
                    logger.Info($"开始重连: {property.remotePoint.ToString()}");
                    try
                    {
                        InitSocketAsync();
                        bool success = false;
                        do
                        {
                            success = await ec.ConnectAsync(property.remotePoint).ConfigureAwait(false);
                            if (!success)//连接失败
                            {
                                logger.Error($"连接失败:{property.CurrentAccount.Session}");
                                //延迟10秒自动重连
                                await Task.Delay(10 * 1000);
                            }
                        } while (!success && !needStop);
                        System.Threading.Interlocked.Exchange(ref property.ConnState, 0);
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex.Message);
                        System.Threading.Interlocked.Exchange(ref property.ConnState, 0);
                        property.RaiseConnEvent = true;
                    }
                }
             
            }

        }
       
        /// <summary>
        /// socket已连接
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ec_Connected(object sender, EventArgs e)
        {
            logger.Info("连接成功");

            var obj = sender as EasyClientBase;
            if (SDKProperty.P2PServer.ServerState == SuperSocket.SocketBase.ServerState.NotInitialized)
            {
                var ipa = obj.LocalEndPoint as IPEndPoint;
                if (ipa.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    SDKProperty.P2PServer.Start(ipa.Address);
                }
                else if (ipa.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                {
                    IPAddress iPAddress = ipa.Address.MapToIPv4();
                    SDKProperty.P2PServer.Start(iPAddress);
                }

            }
            SendConn();

            if (timer == null)
            {
                timer = new System.Threading.Timer(o =>
                {
                    if (ec.IsConnected && !needStop)
                    {
                        if (SDKClient.Instance.property.RaiseConnEvent)
                        {
                            OnSendCommand(new HeartMsgPackage());
                        }

                        timer?.Change(30 * 1000, System.Threading.Timeout.Infinite);

                    }
                    else
                    {
                        logger.Error($"心跳检测连接断开,session:{property.CurrentAccount.Session}");
                        StartReConn();

                    }

                }, null, 0, System.Threading.Timeout.Infinite);

            }
            else
            {
                timer?.Change(30 * 1000, System.Threading.Timeout.Infinite);
            }
        }
        /// <summary>
        /// sokcket通讯出错
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ec_Error(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
        {
            logger.Error($"{e.Exception.Message}");
        }
        /// <summary>
        /// 开始连接
        /// </summary>
        /// <param name="endPoint"></param>
        /// <returns></returns>
        public async Task<bool> StartAsync(string name, string pwd, LoginMode loginMode, SDKProperty.userType userType = SDKProperty.userType.imcustomer)
        {

            property.CurUserType = userType;
            property.CurrentAccount.loginId = name;
            property.CurrentAccount.userPass = pwd;
            property.CurrentAccount.LoginMode = loginMode;
            property.m_StateCode = ServerStateConst.Starting;
            try
            {
#if DEBUG
                property.IMServerIP = IPAddress.Parse(ProtocolBase.IMIP);
#else

                IPHostEntry iPHostEntry = Dns.GetHostEntry(ProtocolBase.IMIP);
                property.IMServerIP = iPHostEntry.AddressList[0];
#endif
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
            }

            var endPoint = new IPEndPoint(property.IMServerIP, ProtocolBase.IMPort);
            if (ec != null && ec.IsConnected)
            {

                if (endPoint.ToString() == property.remotePoint.ToString())
                {
                    SendConn();
                }
                else
                {
                    //从其他方式切过来，关闭重连信号
                    property.RaiseConnEvent = false;
                    await ec.Close();
                    return await CreateConn(endPoint);
                }
                return true;
            }
            else
            {
                return await CreateConn(endPoint);

            }
        }

        internal async Task<bool> CreateConn(EndPoint endPoint)
        {
            var result = await ec.ConnectAsync(endPoint);
            property.remotePoint = endPoint;
            return result;
        }
        public async Task<bool> CreateConn()
        {
            //进入IM服务器连接阶段
            if (property.State > ServerState.NotStarted)
            {
                property.RaiseConnEvent = false;

                IPHostEntry iPHostEntry = Dns.GetHostEntry(ProtocolBase.IMIP);
                property.IMServerIP = iPHostEntry.AddressList[0];

                property.remotePoint = new IPEndPoint(property.IMServerIP, ProtocolBase.IMPort);
            }
            else//扫码连接阶段
            {
                try
                {
                    IPHostEntry iPHostEntry = Dns.GetHostEntry(ProtocolBase.QrLoginIP);
                    property.QrServerIP = iPHostEntry.AddressList[0];
                }
                catch (Exception ex)
                {
                    logger.Error(ex.Message);
                }
                property.RaiseConnEvent = true;
                property.remotePoint = new System.Net.IPEndPoint(property.QrServerIP, ProtocolBase.QrLoginPort);
            }

            var result = await ec.ConnectAsync(property.remotePoint);
            return result;
        }

        bool _isQuickLogon; //当前登陆
        string _token; //登陆的token
        /// <summary>
        /// 扫码登陆
        /// </summary>
        /// <param name="isQuickLogon">是否快速登陆</param>
        /// <param name="token">快速登陆用户的token</param>
        /// <returns></returns>
        public async Task<bool> StartQRLoginAsync(bool isQuickLogon = false, string token = "")
        {
            _isQuickLogon = isQuickLogon;
            _token = token;
            try
            {
                IPHostEntry iPHostEntry = Dns.GetHostEntry(ProtocolBase.QrLoginIP);
                property.QrServerIP = iPHostEntry.AddressList[0];

            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return false;
            }

            System.Net.IPEndPoint iPEndPoint = new System.Net.IPEndPoint(property.QrServerIP, ProtocolBase.QrLoginPort);
            property.CurrentAccount.LoginMode = LoginMode.Scan;
            property.m_StateCode = ServerStateConst.Initializing;
            property.RaiseConnEvent = false;//显示的关闭重连开关
            if (ec.IsConnected)
            {
                if (iPEndPoint.ToString() == property.remotePoint.ToString())
                    SendConn();
                else
                {
                    InitSocketAsync();
                    return await CreateConn(iPEndPoint);
                }
                return true;
            }
            else
            {
                return await CreateConn(iPEndPoint);
            }
        }

        /// <summary>
        /// 开始处理聊天消息
        /// </summary>
        public void StartMsgProcess()
        {
            property.CanHandleMsg = 2;
            logger.Info("CanHandleMsg 值修改为:2");
        }
        /// <summary>
        /// 关闭连接
        /// </summary>
        /// <returns></returns>
        public async Task<bool> StopAsync()
        {
            logger.Info($"停止通讯-{property.CurrentAccount.Session}");
            needStop = true;
            return await ec.Close();

            //TODO: 关闭处理
        }

        /// <summary>
        /// 消息包回调处理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Ec_NewPackageReceived(object sender, PackageEventArgs<PackageInfo> e)
        {

            if (e.Package.apiId == ProtocolBase.HeartMsgCode || e.Package.apiId == ProtocolBase.NoHandlePackageCode)
                return;
            if (e.Package.apiId == ProtocolBase.ErrorPackageCode)
            {

                logger.Error($"解析出错-{property.CurrentAccount.Session}\r\n包内容:\t{e.Package.ToString()}");
                SendErrorToCloud(e.Package, 4);
                StartReConn();
                return;
            }
            switch (Util.Helpers.Enum.Parse<StatusCode>(e.Package.code))
            {
                case StatusCode.NoAuth:
                case StatusCode.SessionForbid:
                    logger.Error($"通讯不符合规定，即将重新建立连接。消息包内容：{e.Package.ToString()}");

                    StartReConn();
                    return;
                default:
                    break;
            }
            //过滤重复消息
            if (SDKClient.Instance.property.MsgDic.TryAdd(e.Package.id, e.Package))
            {
                var cmd = CommmandSet.FirstOrDefault(c => c.Name == e.Package.api);
                try
                {
                    cmd?.ExecuteCommand(ec, e.Package);//日志及入库操作
                    if (cmd == null)
                        CommmandSet.FirstOrDefault(c => c.Name == "common")?.ExecuteCommand(ec, e.Package);
                }
                catch (Exception ex)
                {
                    logger.Error($"消息处理异常：error:{ex.Message},stack:{ex.StackTrace};\r\ncontent:{Util.Helpers.Json.ToJson(e.Package)}");
                    SendErrorToCloud(e.Package, 4);
                    if (cmd != null && cmd.Name == Protocol.ProtocolBase.GetOfflineMessageList)
                    {
                        System.Threading.Interlocked.CompareExchange(ref SDKClient.Instance.property.CanHandleMsg, 2, 1);
                        logger.Info("CanHandleMsg 值修改为:2");
                    }
                }

            }
            else
            {
                GetOfflineMessageListPackage package = e.Package as GetOfflineMessageListPackage;
                if (package != null)
                {
                    try
                    {
                        var cmd = CommmandSet.FirstOrDefault(c => c.Name == e.Package.api);
                        cmd?.ExecuteCommand(ec, e.Package);//日志及入库操作

                    }
                    catch (Exception ex)
                    {
                        logger.Error($"消息处理异常：error:{ex.Message},stack:{ex.StackTrace};\r\ncontent:{Util.Helpers.Json.ToJson(e.Package)}");

                        System.Threading.Interlocked.CompareExchange(ref SDKClient.Instance.property.CanHandleMsg, 2, 1);
                        logger.Info("CanHandleMsg 值修改为:2");
                    }
                }
                else
                    logger.Error($"session:{property.CurrentAccount.Session}\r\n重复消息:{Util.Helpers.Json.ToJson(e.Package)}");

            }


        }

        private void SendErrorToCloud(PackageInfo packageInfo, int msgType)
        {
            WebAPI.ErrorPackage errorPackage = new WebAPI.ErrorPackage()
            {
                content = Util.Helpers.Json.ToJson(packageInfo),
                msgId = packageInfo.id,
                msgType = msgType,
                receiverId = packageInfo.to.ToInt(),
                senderId = packageInfo.from.ToInt()
            };
            Task.Run(() => WebAPICallBack.SendErrorToCloud(errorPackage));
        }

        internal void OnNewDataRecv(PackageInfo info)
        {
            //  logger.Info($"msg=> ui,content:{info.ToString()}");
            NewDataRecv?.BeginInvoke(this, info, null, null);
        }
        public void OnP2PPackagePush(P2PPackage info)
        {
            P2PDataRecv?.BeginInvoke(this, info, null, null);
        }

        /// <summary>
        /// 发送连接请求
        /// </summary>
        private void SendConn()
        {
            if (property.m_StateCode > ServerStateConst.NotStarted)
            {
                LoginPackage package = new LoginPackage();
                package.ComposeHead(null, null);

                package.data = new Model.login()
                {
                    deviceId = Guid.NewGuid().ToString(),
                    time = DateTime.Now,
                    version = "1.0"

                };
                package.Send(ec);
            }
            else if (_isQuickLogon)
            {
                QRController.QuickLogonMsg();
            }
            else
                QRController.GetLoginQRCode();
        }

      
 
        #region 公开的功能
  
        /// <summary>
        /// 扫描最新版本
        /// </summary>
        /// <returns></returns>
        public bool ScanNewVersion(out string newVersion)
        {
            var detail = WebAPICallBack.GetLatestVersionNum();
            newVersion = detail.VersionName;
            var curnum = System.Configuration.ConfigurationManager.AppSettings["version"] ?? "1";
            var config = System.Configuration.ConfigurationManager.OpenExeConfiguration("IMUI.exe");
            if (config.AppSettings.Settings["externalversion"] != null && config.AppSettings.Settings["externalversion"].Value != newVersion)
            {
                config.AppSettings.Settings["externalversion"].Value = newVersion;
                config.Save();
            }
            else if (config.AppSettings.Settings["externalversion"] == null)
            {
                config.AppSettings.Settings.Add("externalversion", newVersion);
                config.Save();
            }
            if (detail.VersionNum > curnum.ToInt())
                return true;
            else
            {
                return false;
            }
        }
        /// <summary>
        /// 扫描更新程序是否有更新
        /// </summary>
        /// <returns></returns>
        public (bool isUpdate, string newVersion) ScanNewVersion()
        {
            var detail = WebAPICallBack.GetLatestVersionNum();
            if (detail == null) return (false, "");
            string newVersion = detail.VersionName;
            var curnum = System.Configuration.ConfigurationManager.AppSettings["updateversion"] ?? "1";
            var str = detail.IsSubUpgrade ? "是" : "否";
            SDKClient.logger.Info($"GetLatestVersionNum_升级包版本号：" + curnum + "是否需要升级：" + str);
            //var curnum = System.Configuration.ConfigurationManager.AppSettings["updateversion"] ?? "";
            return (detail.IsSubUpgrade, newVersion);
        }

        /// <summary>
        /// 获取短信验证码
        /// </summary>
        /// <param name="userMobile"></param>
        /// <returns></returns>
        public async Task<string> GetSmsMessage(string userMobile)
        {
            return await new Util.Webs.Clients.WebClient().Post(Protocol.ProtocolBase.SmsUri)
                .Data("userMobile", userMobile)
                .ContentType(Util.Webs.Clients.HttpContentType.Json)
                .ResultAsync();

        }


#region CURD DB_historyAccount
        /// <summary>
        /// 获取历史账户列表
        /// </summary>
        /// <returns></returns>
        //public async Task<List<DB.historyAccountDB>> GetAccountListDESC()
        //{
        //    return await GetData(() =>
        //    {
        //        //return Util.Helpers.Async.Run(async()=>await DAL.DALAccount.GetAccountListDESC());
        //        return DAL.DALAccount.GetAccountListDESC().Result;
        //        // return DAL.DALAccount.GetAccountListDESC().ConfigureAwait(false).GetAwaiter().GetResult();
        //    }).ConfigureAwait(false);
        //   // return await DAL.DALAccount.GetAccountListDESC().ConfigureAwait(false);

        //}
        public async Task<List<DB.historyAccountDB>> GetAccountListDESC()
        {
            //return await GetData(async () =>
            //{
            return await DAL.DALAccount.GetAccountListDESC();
            //});
            //return await DAL.DALAccount.GetAccountListDESC();

        }
        public void UpdateAccountLoginModel(Model.LoginMode loginModel)
        {
            DAL.DALAccount.UpdateAccountLoginModel(loginModel);
        }
        /// <summary>
        /// 更新置顶时间
        /// </summary>
        /// <param name="topMostTime"></param>
        public void UpdateAccountTopMostTime(DateTime? topMostTime)
        {
            DAL.DALAccount.UpdateAccountTopMostTime(topMostTime);
        }
        public Task<bool> DeleteHistoryAccount(string account)
        {
            return DAL.DALAccount.DeleteAccount(account);
        }

#endregion

        /// <summary>
        /// 发送好友申请
        /// </summary>
        /// <param name="toUserId">好友ID</param>
        /// <param name="remark">申请信息</param>
        /// <param name="photo">自己的照片</param>
        /// <returns></returns>
        public string AddFriend(int toUserId, string applyRemark, int friendSource = 0, string sourceGroup = "",string sourceGroupName="", string friendMemo = "")
        {
            AddFriendPackage package = new AddFriendPackage();
            package.ComposeHead(toUserId.ToString(), property.CurrentAccount.userID.ToString());
            //var result = Task.Run(async()=> await FindResource(photo).ConfigureAwait(false)).GetAwaiter().GetResult();
            //if(!result.existed)
            //{
            //    UpLoadResource(photo, null, null);
            //}
            package.data = new addfriend()
            {
                userId = property.CurrentAccount.userID,
                remark = "",
                applyRemark = applyRemark,
                friendMemo = friendMemo,
                friendId = toUserId,
                userName = property.CurrentAccount.userName,
                photo = property.CurrentAccount.photo,
                province = property.CurrentAccount.Province,
                city = property.CurrentAccount.City,
                sex = property.CurrentAccount.Sex,
                sourceGroup = sourceGroup,
                friendSource = friendSource,
                sourceGroupName= sourceGroupName
            };
            package.Send(ec);
            return package.id;
        }
        /// <summary>
        /// 添加关注
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="strangerId">陌生人ID</param>
        /// <returns></returns>
        public string AddAttention(int userId, int strangerId)
        {
            AddAttentionPackage package = new AddAttentionPackage();
            package.ComposeHead(strangerId.ToString(), property.CurrentAccount.userID.ToString());

            package.data = new AddAttentionPackage.Data()
            {
                userId = property.CurrentAccount.userID,
                strangerId = strangerId

            };
            package.Send(ec);
            return package.id;
        }
        /// <summary>
        /// 意见反馈
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public async Task<bool> AddFeedBack(string content)
        {
            return await IMRequest.AddFeedBack(content);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="friendId"></param>
        /// <param name="type">type : 0-用户人工删除 1-系统自动删除</param>
        /// <returns></returns>
        public string DeleteFriend(int friendId, int type = 0)
        {
            DeleteFriendPackage package = new DeleteFriendPackage();
            package.ComposeHead(friendId.ToString(), property.CurrentAccount.userID.ToString());
            package.data = new DeleteFriendPackage.Data()
            {
                userId = property.CurrentAccount.userID,
                friendId = friendId,
                type = type
            };
            return package.Send(ec).id;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="toUserId"></param>
        /// <param name="status"></param>
        /// <param name="partnerName"></param>
        /// <param name="auditRemark"></param>
        /// <param name="partnerPhoto"></param>
        /// <returns></returns>
        public string AddFriendAccepted(int toUserId, AuditStatus status, string partnerName, string auditRemark, string partnerPhoto)
        {
            AddFriendAcceptedPackage package = new AddFriendAcceptedPackage();
            package.ComposeHead(toUserId.ToString(), property.CurrentAccount.userID.ToString());

            package.data = new AddFriendAcceptedPackage.addFriendAccepted()
            {
                userId = property.CurrentAccount.userID,
                auditStatus = (int)status,
                friendId = toUserId,
                auditRemark = "",
                partnerName = property.CurrentAccount.userName,
                partnerPhoto = property.CurrentAccount.photo,
                friendMemo = auditRemark
            };
            package.Send(ec);
            return package.id;
        }

        public void AddNotice(string title, string content, int groupId, string groupName, Action<(bool, int, string)> HandleCompleteCallBack, SDKProperty.NoticeType noticeType = NoticeType.Common)
        {
            /*
             * 发送HTTP请求
             * 收到请求，CB给UI
             * 发送公告消息到IM服务器
             */
            Task.Run(async () =>
            {
                var resp = await IMRequest.AddNotice(groupId, title, content, noticeType);
                if (resp != null && resp.success)
                {
                    int noticeId = resp.noticeId;

                    MessagePackage package = new MessagePackage();
                    package.ComposeHead(null, property.CurrentAccount.userID.ToString());

                    package.data = new message()
                    {
                        body = new addGroupNoticeBody()
                        {
                            content = content,
                            noticeId = noticeId,
                            publishTime = DateTime.Now,
                            title = title,
                            groupId = groupId,

                            type = (int)noticeType
                        },
                        groupInfo = new message.msgGroup()
                        {
                            groupId = groupId,
                            groupName = groupName
                        },
                        senderInfo = new message.SenderInfo()
                        {
                            photo = property.CurrentAccount.photo,
                            userName = property.CurrentAccount.userName
                        },
                        subType = Util.Helpers.Enum.GetDescription<SDKProperty.MessageType>(SDKProperty.MessageType.addgroupnotice),
                        type = SDKProperty.chatType.groupChat.ToString()
                    };
                    package.Send(ec);
                    if (HandleCompleteCallBack != null)
                        HandleCompleteCallBack((true, resp.noticeId, package.id));
                }
                else
                {
                    if (HandleCompleteCallBack != null)
                        HandleCompleteCallBack.Invoke((false, 0, null));
                }
            });


        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="groupId"></param>
        /// <param name="noticeId"></param>
        /// <param name="type">操作意图：0-初始化获取，1-取最新的公告，2-取历史公告</param>
        /// <param name="datetime"></param>
        /// <param name="count"></param>
        /// <param name="HandleCompleteCallBack"></param>
        /// <returns></returns>
        public async Task GetNoticeList_DESC(int groupId, int noticeId, int type, DateTime? datetime, int count = 20, Action<(bool success, IList<NoticeEntity> datas)> HandleCompleteCallBack = null)
        {
            IList<NoticeEntity> lst = new List<NoticeEntity>();

            //从服务器拉取指定公告
            var resp = await IMRequest.GetNoticeList(groupId, type, count, datetime);
            if (resp != null && resp.success)
            {
                lst = new List<NoticeEntity>();
                foreach (var item in resp.noticeList)
                {

                    NoticeEntity noticeEntity = new NoticeEntity()
                    {
                        db = new GetNoticeListResponse.NoticeInfo()
                        {
                            content = item.content,
                            groupId = item.groupId,

                            noticeId = item.noticeId,
                            title = item.title,
                            type = item.type,
                            publishTime = item.publishTime
                        }
                    };
                    lst.Add(noticeEntity);
                }

                if (HandleCompleteCallBack != null)
                    HandleCompleteCallBack((true, lst));
            }
            else
            {
                if (HandleCompleteCallBack != null)
                    HandleCompleteCallBack((false, lst));
            }
        }

        public void DeleteNotice(int noticeId, int groupId, string groupName, string title, Action<(bool, int, string)> HandleCompleteCallBack, SDKProperty.NoticeType noticeType = NoticeType.Common)
        {
            Task.Run(async () =>
            {
                var resp = await IMRequest.DeleteNotice(noticeId);
                if (resp.code == -101)//服务器已经删除该公告
                {

                    if (HandleCompleteCallBack != null)
                        HandleCompleteCallBack((true, noticeId, null));
                    return;
                }
                if (resp != null && resp.success)
                {
                    MessagePackage package = new MessagePackage();
                    package.ComposeHead(null, property.CurrentAccount.userID.ToString());
                    package.data = new message()
                    {
                        body = new deleteGroupNoticeBody()
                        {

                            noticeId = noticeId,
                            publishTime = DateTime.Now,
                            title = title,
                            type = (int)noticeType
                        },
                        groupInfo = new message.msgGroup()
                        {
                            groupId = groupId,
                            groupName = groupName
                        },
                        senderInfo = new message.SenderInfo()
                        {
                            photo = property.CurrentAccount.photo,
                            userName = property.CurrentAccount.userName
                        },
                        subType = Util.Helpers.Enum.GetDescription<SDKProperty.MessageType>(SDKProperty.MessageType.deletegroupnotice),
                        type = SDKProperty.chatType.groupChat.ToString()
                    };
                    package.Send(ec);
                    if (HandleCompleteCallBack != null)
                        HandleCompleteCallBack((resp.success, noticeId, package.id));
                }
                else
                {
                    if (HandleCompleteCallBack != null)
                        HandleCompleteCallBack((false, noticeId, null));
                }
            });

        }

        /// <summary>
        /// 获取入群须知
        /// </summary>
        /// <param name="groupId">群ID</param>
        /// <returns></returns>
        public async Task<NoticeEntity> GetJoinGroupNotice(int groupId)
        {
            var item = await IMRequest.GetJoinGroupNotice(groupId);
            if (item != null && item.success && item.noticeList.Any())
            {
                var db = item.noticeList[0];
                if (db != null)
                    return new NoticeEntity() { db = db };
            }
            return null;
        }
        public async Task<NoticeEntity> GetGroupNotice(int noticeId)
        {
            return await IMRequest.GetNotice(noticeId);
        }
#region CURD DB_friend
        public void UpdateFriendApplyIsRead()
        {
            Task.Run(async () => await DAL.DALFriendApplyListHelper.UpdateTableIsRead());
        }


#endregion
#region DB
        /// <summary>
        /// 历史消息
        /// </summary>
        /// <param name="roomId">聊天窗ID</param>
        /// <param name="loadCount">加载数量</param>
        /// <returns></returns>
        public List<DB.messageDB> GetHistoryMsg(int roomId, int loadCount = 6, DateTime? dateTime = null, SDKProperty.MessageType messageType = SDKProperty.MessageType.all, chatType chatType = chatType.chat)
        {
            string mt = messageType.ToString();
            //if (dateTime != null)
            //{
            //    dateTime = dateTime.Value.AddDays(1);
            //}
            return Util.Helpers.Async.Run(async () => await DAL.DALMessageHelper.GetLatestMsgs(roomId, loadCount, dateTime, mt, chatType));
        }
        /// <summary>
        /// 历史消息
        /// </summary>
        /// <param name="roomId">聊天窗ID</param>
        /// <param name="loadCount">加载数量</param>
        /// <returns></returns>
        public List<DTO.MessageEntity> GetHistoryMsgEntity(int roomId, int loadCount = 6, DateTime? dateTime = null, SDKProperty.MessageType messageType = SDKProperty.MessageType.all, bool showDelMsg = false)
        {
            string mt = messageType.ToString();
            var lst = Util.Helpers.Async.Run(async () => await DAL.DALMessageHelper.GetMsgEntity(roomId, loadCount, dateTime, mt, showDelMsg));
            return lst;
        }
        /// <summary>
        /// 历史消息，用于显示指定消息之前的消息记录
        /// </summary>
        /// <param name="roomId">聊天窗ID</param>
        /// <param name="msgId">指定消息ID</param>
        /// <param name="loadCount">显示数量</param>
        /// <returns></returns>
        public List<DB.messageDB> GetHistoryMsg(int roomId, string msgId, int loadCount = 20, DateTime? dateTime = null, SDKProperty.MessageType messageType = SDKProperty.MessageType.all, bool isForword = true, chatType chatType = chatType.chat)
        {
            string mt = messageType.ToString();
            return Util.Helpers.Async.Run(async () => await DAL.DALMessageHelper.GetMsg_DESC(roomId, msgId, loadCount, mt, dateTime, isForword, chatType));

        }
        /// <summary>
        /// 历史消息，用于显示指定消息之前的消息记录
        /// </summary>
        /// <param name="roomId">聊天窗ID</param>
        /// <param name="msgId">指定消息ID</param>
        /// <param name="loadCount">显示数量</param>
        /// <returns></returns>
        public List<DTO.MessageEntity> GetHistoryMsgEntity(int roomId, string msgId, int loadCount = 20, DateTime? dateTime = null, SDKProperty.MessageType messageType = SDKProperty.MessageType.all, bool isForword = true, bool showDelMsg = false)
        {

#if CUSTOMSERVER

            return WebAPICallBack.GetHistoryMessageList(dateTime ?? DateTime.Now.AddDays(1), roomId);

#else
            string mt = messageType.ToString();
            return Util.Helpers.Async.Run(async () => await DAL.DALMessageHelper.GetMsgEntity(roomId, msgId, loadCount, mt, dateTime, isForword, showDelMsg).ConfigureAwait(false));
#endif
        }
     
      

       
      
        
        /// <summary>
        /// 接收端取消接收离线消息，接收端调用
        /// </summary>
        /// <param name="msgId"></param>
        public void CancelOfflineFileRecv(string msgId)
        {
            Util.Helpers.Async.Run(async () => await DAL.DALMessageHelper.CancelOfflineFileRecv(msgId));
        }

        /// <summary>
        /// 设置条目是否可见
        /// </summary>
        /// <param name="roomId">窗体ID</param>
        /// <param name="roomType">窗体类型，0：chat;1:groupchat</param>
        /// <param name="visibility">是否可见，true:可见;false:隐藏</param>
        public void UpdateChatRoomVisibility(int roomId, int roomType, bool visibility)
        {
            ThreadPool.QueueUserWorkItem(m =>
            {
                if (!visibility)
                    Util.Helpers.Async.Run(async () => await DAL.DALChatRoomConfigHelper.SetRoomHiddenAsync(roomId));
                else
                    Util.Helpers.Async.Run(async () => await DAL.DALChatRoomConfigHelper.SetRoomVisiableAsync(roomId));
            });
        }
        /// <summary>
        /// 清空历史聊天记录
        /// </summary>
        /// <returns></returns>
        public void DeleteHistoryMsg()
        {

            property.CanHandleMsg = 1;
            logger.Info("CanHandleMsg 值修改为:1");
            Util.Helpers.Async.Run(async () => await DAL.DALMessageHelper.DeleteHistoryMsg().ConfigureAwait(false));
            this.StartMsgProcess();


        }
        /// <summary>
        /// 清空单个聊天室消息
        /// </summary>
        /// <param name="roomId">聊天室ID</param>
        /// <returns></returns>
        public async Task DeleteHistoryMsg(int roomId, SDKProperty.chatType chatType)
        {
            await DAL.DALMessageHelper.DeleteHistoryMsg(roomId, chatType);
        }
        public async Task DeleteJoinGroupRecord(int groupId, int userId)
        {
            await DAL.DALJoinGroupHelper.DeleteJoinGroupItem(groupId, userId);
        }

#endregion

        public string SearchNewFriend(string keyword, int pageIndex = 1)
        {
            SearchNewFriendPackage package = new SearchNewFriendPackage();
            package.ComposeHead(string.Empty, property.CurrentAccount.userID.ToString());
            package.data = new SearchNewFriendPackage.Data()
            {
                userId = property.CurrentAccount.userID,
                keyword = keyword,
                min = 1,
                max = 50
            };
            package.Send(ec);
            return package.id;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="keyWord"></param>
        /// <param name="searchType">1:UserName,2:MobilePhone</param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public SearchResult SearchQuery(string keyWord, string searchType = "1,2,3", int pageIndex = 1, int pageSize = 30)
        {
            if (string.IsNullOrEmpty(SDKClient.Instance.property.CurrentAccount.token))
            {
                var res = WebAPI.WebAPICallBack.GetAuthByUserPassword();
                SDKClient.Instance.property.CurrentAccount.token = res.token;
                logger.Error($"获取token：{res.token}");
            }

            SearchResult result = WebAPICallBack.GetSearchResult(keyWord, searchType, pageIndex, pageSize);
            logger.Info(Util.Helpers.Json.ToJson(result));
            return result;

        }
       

       
        public string InviteJoinGroup(InviteJoinGroupPackage.Data data, bool isFoward = false)
        {
            InviteJoinGroupPackage package = new InviteJoinGroupPackage();
            package.ComposeHead(property.ServerJID, property.CurrentAccount.userID.ToString());
            package.data = new InviteJoinGroupPackage.Data()
            {
                groupId = data.groupId,
                userIds = data.userIds,
                InviteUserId = isFoward ? data.InviteUserId : property.CurrentAccount.userID,
                groupIntroduction = data.groupIntroduction,
                groupName = data.groupName,
                groupPhoto = data.groupPhoto,
                inviteUserName = data.inviteUserName,
                inviteUserPhoto = data.inviteUserPhoto,
                targetGroupIds = data.targetGroupIds,
                targetGroupId = data.targetGroupId

            };
            package.Send(ec);
            return package.id;
        }
        public string SendLogout(SDKProperty.LogoutModel logoutModel)
        {
            LogoutPackage package = new LogoutPackage();
            package.ComposeHead(property.ServerJID, property.CurrentAccount.userID.ToString());
            package.data = new LogoutPackage.Data()
            {
                status = (int)logoutModel,
                userId = property.CurrentAccount.userID
            };
            package.Send(ec);
            return package.id;
        }
        /// <summary>
        /// 通过入群申请
        /// </summary>
        /// <param name="memberId"></param>
        /// <param name="groupId"></param>
        /// <param name="auditStatus"> 1 审核通过 2 拒绝加群 3 忽略加群申请</param>
        /// <param name="auditRemark"></param>
        /// <returns></returns>
        public string JoinGroupAccepted(JoinGroupAcceptedPackage.Data data)
        {
            JoinGroupAcceptedPackage package = new JoinGroupAcceptedPackage();
            package.ComposeHead(property.ServerJID, property.CurrentAccount.userID.ToString());
            package.data = new JoinGroupAcceptedPackage.Data()
            {
                groupId = data.groupId,
                memberId = data.memberId,
                auditStatus = data.auditStatus,
                userName = data.userName,
                photo = data.photo,
                auditRemark = data.auditRemark,
                adminId = property.CurrentAccount.userID
            };
            package.Send(ec);
            return package.id;
        }
        public string JoinGroup(JoinGroupPackage.Data data)
        {
            JoinGroupPackage package = new JoinGroupPackage();
            package.ComposeHead(property.ServerJID, property.CurrentAccount.userID.ToString());
            package.data = new JoinGroupPackage.Data()
            {
                groupId = data.groupId,
                remark = data.remark,
                userId = property.CurrentAccount.userID,
                userName = data.userName,
                InviteUserId = data.InviteUserId,
                photo = data.photo
            };
            package.Send(ec);
            return package.id;
        }
        /// <summary>
        /// 退群
        /// </summary>
        /// <param name="userIds">退群的对象列表</param>
        /// <param name="adminId">管理员ID</param>
        /// <param name="groupId">群ID</param>
        /// <returns></returns>
        public string ExitGroup(List<int> userIds, int adminId, int groupId, List<string> userNames = null)
        {
            ExitGroupPackage package = new ExitGroupPackage();
            package.ComposeHead(property.ServerJID, property.CurrentAccount.userID.ToString());
            package.data = new ExitGroupPackage.Data()
            {
                adminId = adminId,
                userIds = userIds,
                userNames = userNames,
                groupId = groupId
            };
            package.Send(ec);
            return package.id;
        }

        public string GetGroup(int groupId)
        {
            GetGroupPackage package = new GetGroupPackage();
            package.ComposeHead(property.ServerJID, property.CurrentAccount.userID.ToString());
            package.data = new GetGroupPackage.Data()
            { groupId = groupId };
            package.Send(ec);
            return package.id;
        }

        public string DismissGroup(int groupId)
        {
            DismissGroupPackage package = new DismissGroupPackage();
            package.ComposeHead(null, property.CurrentAccount.userID.ToString());
            package.data = new DismissGroupPackage.Data()
            {
                groupId = groupId,
                ownerId = property.CurrentAccount.userID
            };
            return package.Send(ec).id;
        }
        /// <summary>
        /// 更新群个人设置
        /// </summary>
        /// <param name="groupId"></param>
        /// <param name="setType">1我的群昵称 2 设置置顶 3 是否免打扰（1设置免打扰0不设置免打扰）</param>
        /// <param name="content"></param>
        /// <returns></returns>
        public string UpdateUserSetsInGroup(int groupId, int setType, string content)
        {
            UpdateUserSetsInGroupPackage package = new UpdateUserSetsInGroupPackage();
            package.ComposeHead(null, property.CurrentAccount.userID.ToString());
            package.data = new UpdateUserSetsInGroupPackage.Data()
            {
                userId = property.CurrentAccount.userID,
                content = content,
                setType = setType,
                groupId = groupId
            };
            return package.Send(ec).id;
        }
        /// <summary>
        /// 更新好友关系
        /// </summary>
        /// <param name="relationType"> 0 正常 1 我拉黑对方，2被拉黑，3双方拉黑</param>
        /// <param name="friendId"></param>
        /// <returns></returns>
        public string UpdateFriendRelation(int relationType, int friendId)
        {
            UpdateFriendRelationPackage package = new UpdateFriendRelationPackage();
            package.ComposeHead(friendId.ToString(), property.CurrentAccount.userID.ToString());
            package.data = new UpdateFriendRelationPackage.Data()
            {
                userId = property.CurrentAccount.userID,
                friendId = friendId,
                relationType = relationType
            };
            return package.Send(ec).id;
        }

        /// <summary>
        /// 获取关注人列表
        /// </summary>
        /// <returns></returns>
        public string GetAttentionList()
        {
            GetAttentionListPackage package = new GetAttentionListPackage();
            package.ComposeHead(null, property.CurrentAccount.userID.ToString());
            package.data = new GetAttentionListPackage.Data()
            {
                userId = property.CurrentAccount.userID,
                min = 1,
                max = 100
            };
            var obj = IMRequest.GetAttentionList(package);
            if (obj != null && obj.code == 0)
            {
                var attentionPackage = obj;
                if (attentionPackage != null)
                {
                    try
                    {
                        var cmd = CommmandSet.FirstOrDefault(c => c.Name == attentionPackage.api);
                        cmd?.ExecuteCommand(ec, attentionPackage);//日志及入库操作

                    }
                    catch (Exception ex)
                    {
                        logger.Error($"获取关注列表数据处理异常：error:{ex.Message},stack:{ex.StackTrace};\r\ncontent:{Util.Helpers.Json.ToJson(attentionPackage)}");

                        System.Threading.Interlocked.CompareExchange(ref SDKClient.Instance.property.CanHandleMsg, 2, 1);
                        logger.Info("CanHandleMsg 值修改为:2");
                    }
                }
            }
            else
            {

            }
            //package.Send(ec).id;
            return package.id;
        }
        public string DeleteAttentionUser(int strangerLinkId)
        {
            DeleteAttentionUserPackage package = new DeleteAttentionUserPackage();
            package.ComposeHead(null, property.CurrentAccount.userID.ToString());
            package.data = new DeleteAttentionUserPackage.Data();
            package.data.strangerLinkId = strangerLinkId;
            package.data.userId = property.CurrentAccount.userID;
            return package.Send(ec).id;
        }
        /// <summary>
        /// 关注列表置顶与取消置顶操作
        /// </summary>
        /// <param name="strangerId">陌生人ID</param>
        /// <param name="isTop">是否置顶</param>
        /// <returns>消息ID</returns>
        public string TopAttentionUser(int strangerId, bool isTop = true)
        {
            TopAttentionUserPackage package = new TopAttentionUserPackage();
            package.ComposeHead(null, property.CurrentAccount.userID.ToString());
            package.data = new TopAttentionUserPackage.Data();
            package.data.strangerLinkId = strangerId;
            package.data.oprationType = isTop == true ? "setTop" : "cancelTop";
            return package.Send(ec).id;
        }
        public async Task<List<DTO.MessageContext>> GetRoomContextList()
        {

            var lst = await DAL.DALMessageHelper.GetRoomContext();
            var filter = await DAL.DALChatRoomConfigHelper.GetListAsync().ContinueWith(t =>
            {
                if (t.IsCompleted)
                {
                    return t.Result.Where(c => c.Visibility == false);
                }
                else
                    return null;
            });
            if (filter != null)
            {
                foreach (var item in filter)
                {
                    var temp = lst.Find(m => m.RoomId == item.RoomId);
                    if (temp != null)
                        lst.Remove(temp);
                }
            }
            return lst;

        }
        /// <summary>
        /// 获取基础数据上下文
        /// </summary>
        /// <returns></returns>
        public async Task<DTO.InfrastructureContext> GetInfrastructureContext()
        {
            DTO.InfrastructureContext infrastructureContext = new InfrastructureContext();
            var groupList = await DAL.DALGroupOptionHelper.GetGroupListPackage();
            if (groupList != null)
            {
                var package = Util.Helpers.Json.ToObject<GetGroupListPackage>(groupList.getGroupListPackage);
                infrastructureContext.GroupListPackage = package;
            }
            var contactList = Util.Helpers.Async.Run(async () => await DAL.DALContactListHelper.GetCurrentContactListPackage());
            if (contactList != null)
            {
                var package = Util.Helpers.Json.ToObject<GetContactsListPackage>(contactList.getContactsListPackage);

                infrastructureContext.FriendListPackage = package;

            }
            return infrastructureContext;

        }
        /// <summary>
        /// 使用TaskCompletionSource类型处理Task，防止UI与后台线程死锁
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dataProcess"></param>
        /// <returns></returns>
        private async Task<T> GetData<T>(Func<Task<T>> dataProcess)
        {
            TaskCompletionSource<T> task = new TaskCompletionSource<T>();

            TaskCompletionSource<T> ComposeData()
            {
                TaskCompletionSource<T> ts = new TaskCompletionSource<T>();
                Task.Run(async () =>
                {
                    var lst = await dataProcess();
                    ts.SetResult(lst);
                    return ts;
                });
                return ts;

            }
            task = ComposeData();

            return await task.Task.ConfigureAwait(false);
        }
        private async Task<T> GetData<T>(Func<T> dataProcess)
        {
            TaskCompletionSource<T> task = new TaskCompletionSource<T>();

            TaskCompletionSource<T> ComposeData()
            {
                TaskCompletionSource<T> ts = new TaskCompletionSource<T>();
                Task.Run(() =>
                {
                    var lst = dataProcess();
                    ts.SetResult(lst);
                    return ts;
                });
                return ts;

            }
            task = ComposeData();

            return await task.Task.ConfigureAwait(false);
        }
       

#endregion


    }
}
