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
        private IEnumerable<CommandBase> CommmandSet { get; set; }  //命令集合
        [ImportMany(typeof(Util.Dependency.ConfigBase))]
        private IEnumerable<Util.Dependency.ConfigBase> EntityConfigs { get; set; }
        private EasyClient<PackageInfo> ec = new EasyClient<PackageInfo>();//通讯接口对象

        internal static Util.Logs.ILog logger = Util.Logs.Log.GetLog();//日志对象
        public event EventHandler<PackageInfo> NewDataRecv; //转发服务端数据
        public event EventHandler<P2PPackage> P2PDataRecv; //p2p消息处理
        public event EventHandler<OfflineMessageContext> OffLineMessageEventHandle; //转发离线聊天消息
                                                                                    // public event EventHandler<string> networkMsgRecv; //p2p消息处理
        static readonly Util.ImageOptimizer.Compressor compressor = new Util.ImageOptimizer.Compressor();//图片压缩处理对象
        private static bool needStop = false;
        public readonly SDKProperty property = new SDKProperty();//SDK挂载的属性集对象
        public static SDKClient Instance { get; } = new SDKClient();


        //心跳定时器
        public System.Threading.Timer timer = null;
        /// <summary>
        /// 是否连接
        /// </summary>
        public bool IsConnected => ec.IsConnected && System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable();

        /// <summary>
        /// 消息发送失败处理事件
        /// </summary>
        public event EventHandler<(int roomId, bool isgroup, string msgId)> SendFaile;
        internal void OnSendFaile(int roomId, bool isgroup, string msgId)
        {
            Instance.SendFaile?.BeginInvoke(this, (roomId, isgroup, msgId), null, null);
        }
        /// <summary>
        /// 消息回包确认事件
        /// </summary>
        public event EventHandler<(int roomId, bool isgroup, MessagePackage package, DateTime sendTime)> SendConfirm;
        /// <summary>
        /// 消息回包确认
        /// </summary>
        /// <param name="roomId"></param>
        /// <param name="isgroup"></param>
        /// <param name="msgId"></param>
        internal void OnSendConfirm(int roomId, bool isgroup, MessagePackage package, DateTime dateTime)
        {
            Instance.SendConfirm?.BeginInvoke(this, (roomId, isgroup, package, dateTime), null, null);
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
                            SDKClient.Instance.OnSendFaile(packageInfo.to.ToInt(), false, packageInfo.id);
                        else
                        {
                            SDKClient.Instance.OnSendFaile(messagePackage.data.groupInfo.groupId, true, packageInfo.id);
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

        private string lastSendTime = "2018-08-01 08:12";
        public void GetBadWordEditTime()
        {
            var obj = GetBadWordUpdate(lastSendTime);
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
            if (obj == null)
                return;
            //DateTime dateTime = DateTime.Parse(obj.time);
            //DateTime last = DateTime.Parse(GetLastUpdateTime());
            WriteLatestValueToFile(obj.items);
            //UpdateTimeSubmit(obj.time);
        }

        private void WriteLatestValueToFile(List<WebAPI.GetSensitiveWordsResponse.Keyword> list)
        {
            string path = Environment.CurrentDirectory;
            string filepath = Path.Combine(path, "BadWord.txt");
            try
            {
                using (FileStream fs = new FileStream(filepath, FileMode.Create, FileAccess.Write))
                {
                    using (StreamWriter sw = new StreamWriter(fs))
                    {
                        foreach (var item in list)
                        {
                            sw.WriteLine(item.word);
                        }
                        sw.Close();
                    }
                    fs.Close();
                }
            }
            catch (Exception ex)
            {

                throw;
            }


        }

        private SDKClient()
        {
            //#region 通讯组件配置
            //ec.Initialize(new RecvFilter2());
            //ec.NewPackageReceived += Ec_NewPackageReceived;
            //ec.Error += ec_Error;
            //ec.Connected += ec_Connected;
            //ec.Closed += ec_Closed;

            //#endregion；
            // GetBadWordEditTime();


            #region MEF配置
            MyComposePart();
            #endregion
            Init();


        }
        private void Init()
        {
            #region AOP设置
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
        /// 重连
        /// </summary>
        public async void StartReConn()
        {
            if (!needStop && property.RaiseConnEvent)
            {

                try
                {



                    //没有开始连接，发送连接请求
                    if (System.Threading.Interlocked.CompareExchange(ref property.ConnState, 1, 0) == 0)
                    {

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
                        // timer = null;
                        logger.Info($"开始重连: {property.remotePoint.ToString()}");
                        ConnState?.BeginInvoke(this, false, null, null);
                        InitSocketAsync();
                        var success = await ec.ConnectAsync(property.remotePoint).ConfigureAwait(false);
                        if (!success)//连接失败
                        {
                            logger.Error($"连接失败:{property.CurrentAccount.Session}");
                            //延迟10秒自动重连
                            Task.Delay(10 * 1000).ContinueWith((t) =>
                            {
                                System.Threading.Interlocked.Exchange(ref property.ConnState, 0);
                                StartReConn();
                            });
                        }
                        System.Threading.Interlocked.Exchange(ref property.ConnState, 0);




                    }
                }
                catch (Exception)
                {

                    throw;
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
            //   System.Threading.Interlocked.Exchange(ref property.ConnState, 0);
            logger.Info("连接成功");

#if !CUSTOMSERVER

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
#endif
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
                            //CommandBase.SendHeart(ec);

                        }
#if CUSTOMSERVER
                        timer.Change(5 * 1000, System.Threading.Timeout.Infinite);
#else
                        timer?.Change(30 * 1000, System.Threading.Timeout.Infinite);
#endif
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

            //  StartReConn();


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
#if DEBUG
                property.IMServerIP = IPAddress.Parse(ProtocolBase.IMIP);
#else
                IPHostEntry iPHostEntry = Dns.GetHostEntry(ProtocolBase.IMIP);
                property.IMServerIP = iPHostEntry.AddressList[0];
#endif
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
        bool _isQuickLogon;
        string _token;
        public async Task<bool> StartQRLoginAsync(bool isQuickLogon = false, string token = "")
        {
            _isQuickLogon = isQuickLogon;
            _token = token;
            try
            {
#if DEBUG
                property.QrServerIP = IPAddress.Parse(ProtocolBase.QrLoginIP);
#else
                    IPHostEntry iPHostEntry = Dns.GetHostEntry(ProtocolBase.QrLoginIP);
                    property.QrServerIP = iPHostEntry.AddressList[0];
#endif


            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return false;
            }

            System.Net.IPEndPoint iPEndPoint = new System.Net.IPEndPoint(property.QrServerIP, ProtocolBase.QrLoginPort);
            property.CurrentAccount.LoginMode = LoginMode.Scan;
            property.m_StateCode = ServerStateConst.Initializing;
            property.RaiseConnEvent = true;
            if (ec.IsConnected)
            {
                if (iPEndPoint.ToString() == property.remotePoint.ToString())
                    SendConn();
                else
                {
                    //从其他方式切过来，关闭重连信号
                    property.RaiseConnEvent = false;
                    await ec.Close();
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
            logger.Error($"停止通讯-{property.CurrentAccount.Session}");
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
            //string str = Util.Helpers.Json.ToJson(e.Package);
            //networkMsgRecv?.BeginInvoke(this,str,null,null);

            if (e.Package.apiId == ProtocolBase.HeartMsgCode || e.Package.apiId == ProtocolBase.NoHandlePackageCode)
                return;
            if (e.Package.apiId == ProtocolBase.ErrorPackageCode)
            {

                logger.Error($"解析出错-{property.CurrentAccount.Session}\r\n包内容:\t{e.Package.ToString()}");
                SendPackageToCloud(e.Package, 4);
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
                    SendPackageToCloud(e.Package, 4);
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

        private void SendPackageToCloud(PackageInfo packageInfo, int msgType)
        {
            WebAPI.ErrorPackage errorPackage = new WebAPI.ErrorPackage()
            {
                content = Util.Helpers.Json.ToJson(packageInfo),
                msgId = packageInfo.id,
                msgType = msgType,
                receiverId = packageInfo.to.ToInt(),
                senderId = packageInfo.from.ToInt()
            };
            WebAPICallBack.AddMsgFaceBack(errorPackage);
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
                QuickLogonMsg();
            }
            else
                GetLoginQRCode();
        }
        #region 公开的功能
        /// <summary>
        /// 发送本文消息
        /// </summary>
        /// <param name="content">消息内容</param>
        /// <param name="to">接收者</param>
        /// <param name="userIds">被@对象集合,@ALL定义为值-1</param>
        /// <param name="type">消息类型chat,groupchat</param>
        /// <returns>id</returns>

        public async Task<string> Sendtxt(string content, string to, IList<int> userIds = null, chatType type = chatType.chat, string groupName = null, SDKProperty.SessionType sessionType = SessionType.CommonChat, message.ReceiverInfo recverInfo = null)
        {

            return await GetData(() =>
            {
                MessagePackage package = new MessagePackage();
                package.ComposeHead(to, property.CurrentAccount.userID.ToString());

                package.data = new message()
                {
                    body = new TxtBody()
                    {
                        text = content
                    },
                    senderInfo = new message.SenderInfo()
                    {
                        photo = property.CurrentAccount.photo,
                        userName = property.CurrentAccount.userName ?? property.CurrentAccount.loginId
                    },
                    receiverInfo = recverInfo,
                    chatType = to == property.CurrentAccount.userID.ToString() ? (int)SessionType.FileAssistant : (int)sessionType,
                    subType = "txt",
                    tokenIds = userIds,
                    type = type == chatType.chat ? nameof(chatType.chat) : nameof(chatType.groupChat)
                };
                if (type == chatType.groupChat)
                {
                    package.data.groupInfo = new message.msgGroup()
                    {
                        groupId = to.ToInt(),
                        groupName = groupName
                    };
                }

                package.Send(ec);
                System.Threading.SpinWait spinWait = new System.Threading.SpinWait();
                spinWait.SpinOnce();
                return package.id;

                //Task.Run(() =>
                //    {
                //        if (content.Equals("SHOWMETHEMONEY"))
                //        {
                //            for (int i = 0; i < 100; i++)
                //            {
                //                package.data.body.text = $"{content}{i}";
                //                package.id = SDKProperty.RNGId;
                //                package.Send(ec);

                //            }
                //        }
                //    });

                //return package.id;
            }).ConfigureAwait(false);

        }

        /// <summary>
        /// 发送个人名片
        /// </summary>
        /// <param name="name">发送的目标名片用户名</param>
        /// <param name="photo">发送的目标名片图像</param>
        /// <param name="phone">发送的目标名片电话号码</param>
        /// <param name="userid">发送的目标名片用户Id</param>
        /// <param name="to">接受者Id</param>
        /// <param name="userIds">被@对象集合,@ALL定义为值-1</param>
        /// <param name="type">聊天类型</param>
        /// <param name="groupName">群名称</param>
        /// <param name="sessionType"></param>
        /// <param name="receiverInfo"></param>
        /// <returns>消息Id</returns>
        public async Task<string> SendPersonCard(string name, string photo, string phone, int userid, string to, IList<int> userIds = null, chatType type = chatType.chat, string groupName = null, SDKProperty.SessionType sessionType = SessionType.CommonChat, message.ReceiverInfo receiverInfo = null)
        {
            return await GetData(() =>
            {
                MessagePackage package = new MessagePackage();
                package.ComposeHead(to, property.CurrentAccount.userID.ToString());

                package.data = new message()
                {
                    body = new UserCardBody()
                    {
                        name = name,
                        phone = phone,
                        photo = photo,
                        userId = userid
                    },
                    senderInfo = new message.SenderInfo()
                    {
                        photo = property.CurrentAccount.photo,
                        userName = property.CurrentAccount.userName ?? property.CurrentAccount.loginId
                    },
                    receiverInfo = receiverInfo,
                    chatType = to == property.CurrentAccount.userID.ToString() ? (int)SessionType.FileAssistant : (int)sessionType,
                    subType = "userCard",
                    tokenIds = userIds,
                    type = type == chatType.chat ? nameof(chatType.chat) : nameof(chatType.groupChat)
                };
                if (type == chatType.groupChat)
                {
                    package.data.groupInfo = new message.msgGroup()
                    {
                        groupId = to.ToInt(),
                        groupName = groupName
                    };
                }
                try
                {
                    package.Send(ec);
                }
                catch (Exception ex)
                {
                    logger.Error($"发送失败:{ex.Message}");
                    return null;
                }



                return package.id;
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// 发送手机端分享的消息
        /// </summary>
        /// <param name="foreignDynBody"></param>
        /// <param name="to"></param>
        /// <param name="type"></param>
        /// <param name="groupName"></param>
        /// <param name="sessionType"></param>
        /// <param name="receiverInfo"></param>
        /// <returns></returns>
        public async Task<string> SendForeignDynMsg(object foreignDynBody, string to, chatType type = chatType.chat, string groupName = null, SDKProperty.SessionType sessionType = SessionType.CommonChat, message.ReceiverInfo receiverInfo = null)
        {
            return await GetData(() =>
            {
                MessagePackage package = new MessagePackage();
                package.ComposeHead(to, property.CurrentAccount.userID.ToString());

                package.data = new message()
                {
                    body = foreignDynBody,
                    senderInfo = new message.SenderInfo()
                    {
                        photo = property.CurrentAccount.photo,
                        userName = property.CurrentAccount.userName ?? property.CurrentAccount.loginId
                    },
                    receiverInfo = receiverInfo,
                    chatType = to == property.CurrentAccount.userID.ToString() ? (int)SessionType.FileAssistant : (int)sessionType,
                    subType = "foreignDyn",
                    type = type == chatType.chat ? nameof(chatType.chat) : nameof(chatType.groupChat)
                };
                if (type == chatType.groupChat)
                {
                    package.data.groupInfo = new message.msgGroup()
                    {
                        groupId = to.ToInt(),
                        groupName = groupName
                    };
                }
                try
                {
                    package.Send(ec);
                }
                catch (Exception ex)
                {
                    logger.Error($"发送失败:{ex.Message}");
                    return null;
                }



                return package.id;
            }).ConfigureAwait(false);
        }
        /// <summary>
        /// 设置陌生人免打扰
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="strangerId"></param>
        /// <param name="isNotdisturb">是否消息免打扰 1是 0否</param>
        /// <returns></returns>
        public async Task<bool> SetStrangerDoNotDisturb(int userId, int strangerId, int isNotdisturb)
        {
            return await DAL.DALStrangerOptionHelper.SetStrangerdoNotDisturb(strangerId, isNotdisturb);
            //SetStrangerDoNotDisturbPackage package = new SetStrangerDoNotDisturbPackage();
            //package.ComposeHead(property.ServerJID, property.CurrentAccount.userID.ToString());
            //package.data = new SetStrangerDoNotDisturbPackage.Data()
            //{
            //    userId = userId,
            //    strangerId = strangerId,
            //    isNotdisturb = isNotdisturb
            //};
            //package.Send(ec);
            //return package.id;
        }
        public async Task<IList<StrangerEntity>> GetStrangerList()
        {
            return await DAL.DALStrangerOptionHelper.GetStrangerEntities();
        }
        public async Task<StrangerEntity> GetStranger(int userId)
        {
            return await DAL.DALStrangerOptionHelper.GetStranger(userId);
        }
        public async Task<bool> SetStrangerChatTopTime(int userId, DateTime? datetime)
        {
            return await DAL.DALStrangerOptionHelper.SetStrangerChatTopTime(userId, datetime);
        }
        public async Task<bool> InsertOrUpdateStrangerInfo(int userId, string photo, string name, int sex)
        {
            return await DAL.DALStrangerOptionHelper.InsertOrUpdateStrangerInfo(userId, photo, name, sex);
        }

        public void GetOfflineMessageList(GetOfflineMessageListPackage package = null, bool isPushUI = true)
        {
            if (package == null)
            {
                var a = Util.Helpers.Async.Run(async () => await DAL.DALAccount.GetAccount());

                var msgTime = DateTime.Now.AddDays(-7);
                if (a != null)
                    msgTime = a.GetOffLineMsgTime ?? DateTime.Now.AddDays(-7);


                package = new GetOfflineMessageListPackage();
                package.ComposeHead(property.ServerJID, property.CurrentAccount.userID.ToString());

                package.data = new GetOfflineMessageListPackage.Data()
                {
                    time = msgTime,
                    count = 1000
                };
            }
            logger.Info($"拉取离线消息的时间为：{package.data.time},package:{Util.Helpers.Json.ToJson(package)}");
            var obj = IMRequest.GetOfflineMessageList(package);
            if (isPushUI && obj != null && obj.code == 0)
            {

                var offlinePackage = obj;
                if (offlinePackage != null)
                {
                    try
                    {
                        var cmd = CommmandSet.FirstOrDefault(c => c.Name == offlinePackage.api);
                        cmd?.ExecuteCommand(ec, offlinePackage);//日志及入库操作

                    }
                    catch (Exception ex)
                    {
                        logger.Error($"消息处理异常：error:{ex.Message},stack:{ex.StackTrace};\r\ncontent:{Util.Helpers.Json.ToJson(offlinePackage)}");

                        System.Threading.Interlocked.CompareExchange(ref SDKClient.Instance.property.CanHandleMsg, 2, 1);
                        logger.Info("CanHandleMsg 值修改为:2");
                    }
                }







                //return package.id;
            }
        }
        public void GetThefuckOfflineMessageList()
        {
            Task.Run(async () =>
            {
                //获取汇总
                var r = await IMRequest.GetSummaryInfo().ConfigureAwait(false);
                if (r.code == 0)
                {

                    var optlst = r.operMsgList;
                    if (optlst != null && optlst.Any())
                    {
                        foreach (var opt in optlst)
                        {
                            var c = opt.content;
                            string item = Util.Helpers.Json.ToJson(c);
                            PackageInfo obj = Util.Helpers.Json.ToObject<PackageInfo>(item);
                            if (obj.code != 0)
                                continue;
                         
                            switch (obj.apiId)
                            {
                                case ProtocolBase.DeleteFriendCode:
                                   
                                    obj = Newtonsoft.Json.JsonConvert.DeserializeObject<DeleteFriendPackage>(item);
                                   
                                    break;
                                case ProtocolBase.UpdateFriendRelationCode:

                                    obj = Newtonsoft.Json.JsonConvert.DeserializeObject<UpdateFriendRelationPackage>(item);
                                    SDKClient.Instance.OnNewDataRecv(obj);

                                    break;

                                case Protocol.ProtocolBase.InviteJoinGroupCode:
                                    obj = Newtonsoft.Json.JsonConvert.DeserializeObject<InviteJoinGroupPackage>(item);

                                    await DAL.DALGroupOptionHelper.SendMsgtoDB(Util.Helpers.Json.ToObject<InviteJoinGroupPackage>(item));
                                    break;
                                case Protocol.ProtocolBase.UpdateGroupCode:
                                    obj = Newtonsoft.Json.JsonConvert.DeserializeObject<UpdateGroupPackage>(item);
                                    await DAL.DALGroupOptionHelper.SendMsgtoDB(Util.Helpers.Json.ToObject<UpdateGroupPackage>(item));
                                   
                                    break;
                                case Protocol.ProtocolBase.JoinGroupCode:
                                    obj = Newtonsoft.Json.JsonConvert.DeserializeObject<JoinGroupPackage>(item);
                                    JoinGroupPackage jgp = Util.Helpers.Json.ToObject<JoinGroupPackage>(item);
                                    if (jgp.code == 0)
                                    {
                                        //管理员收到入群申请
                                        if (obj.from != SDKClient.Instance.property.CurrentAccount.userID.ToString())
                                        {
                                            await DAL.DALJoinGroupHelper.RecvJoinGroup(jgp);
                                            await DAL.DALJoinGroupHelper.SendMsgtoDB(jgp);
                                           
                                        }
                                        else
                                        {
                                            if (jgp.data.isAccepted)
                                            {
                                                await DAL.DALJoinGroupHelper.SendMsgtoDB(jgp);
                                                await DAL.DALJoinGroupHelper.DeleteJoinGroupItem(jgp);
                                            }
                                        }
                                    }
                                    else if (jgp.code == (int)Protocol.StatusCode.UserIsGroupMember || jgp.code == (int)Protocol.StatusCode.AlreadyCompleted)
                                    {

                                        await DAL.DALJoinGroupHelper.DeleteJoinGroupItem(jgp);
                                    }

                                    break;
                                case Protocol.ProtocolBase.AddFriendCode:
                                    obj = Newtonsoft.Json.JsonConvert.DeserializeObject<AddFriendPackage>(item);
                                    await DAL.DALFriendApplyListHelper.InsertOrUpdateItem(Util.Helpers.Json.ToObject<AddFriendPackage>(item));
                                    SDKClient.Instance.property.FriendApplyList = Util.Helpers.Async.Run(async () => await DAL.DALFriendApplyListHelper.GetFriendApplyList());
                                    break;
                                case Protocol.ProtocolBase.AddFriendAcceptedCode:
                                    obj = Newtonsoft.Json.JsonConvert.DeserializeObject<AddFriendAcceptedPackage>(item);
                                    AddFriendAcceptedPackage afap = Util.Helpers.Json.ToObject<AddFriendAcceptedPackage>(item);
                                    //接收加好友
                                    if (afap.code == 0 || obj.code == (int)Protocol.StatusCode.AlreadyBecomeFriend)
                                    {
                                        await DAL.DALContactListHelper.DeleteCurrentContactListPackage();
                                        if (afap.data.userId == SDKClient.Instance.property.CurrentAccount.userID)
                                        {
                                            await DAL.DALFriendApplyListHelper.UpdateItemIsChecked(afap.data.friendId);
                                            await DAL.DALUserInfoHelper.UpdateItemIsChecked(afap.data.friendId);
                                            var db = await DAL.DALMessageHelper.SendMsgtoDB(afap.id, afap.from, obj.to, "已经是好朋友，开始聊天吧", afap.data.friendId, afap.data.userId, SDKProperty.MessageType.notification, SDKProperty.MessageState.isRead);
                                           
                                            await DAL.DALMessageHelper.UpdateMsgSessionTypeToCommon(afap.data.friendId);
                                            await DAL.DALStrangerOptionHelper.DeleteStranger(afap.data.friendId);
                                        }
                                        else
                                        {
                                            await DAL.DALFriendApplyListHelper.UpdateItemIsChecked(afap.data.userId);
                                            await DAL.DALUserInfoHelper.UpdateItemIsChecked(afap.data.userId);
                                            if (afap.data.type != 1)//服务器代发的同意消息，不需要添加提示
                                            {
                                                var db = await DAL.DALMessageHelper.SendMsgtoDB(afap.id, afap.from, afap.to, "已经是好朋友，开始聊天吧", afap.data.userId, afap.data.userId, SDKProperty.MessageType.notification, SDKProperty.MessageState.isRead);
                                               
                                            }
                                            await DAL.DALMessageHelper.UpdateMsgSessionTypeToCommon(afap.data.userId);
                                            await DAL.DALStrangerOptionHelper.DeleteStranger(afap.data.userId);
                                        }


                                    }
                                    else if (afap.code == (int)Protocol.StatusCode.AuditFriendApplyError)
                                    {
                                        if (afap.data.userId == SDKClient.Instance.property.CurrentAccount.userID)
                                            await DAL.DALFriendApplyListHelper.DeleteItem(afap.data.friendId);
                                        else
                                            await DAL.DALFriendApplyListHelper.DeleteItem(afap.data.friendId);
                                    }
                                    break;
                                case Protocol.ProtocolBase.SetMemberPowerCode:
                                    obj = Newtonsoft.Json.JsonConvert.DeserializeObject<SetMemberPowerPackage>(item);
                                    SetMemberPowerPackage smpp = Util.Helpers.Json.ToObject<SetMemberPowerPackage>(item);
                                    await DAL.DALGroupOptionHelper.SendMsgtoDB(smpp);
                                 
                                    break;
                                case Protocol.ProtocolBase.ExitGroupCode:
                                    obj = Newtonsoft.Json.JsonConvert.DeserializeObject<ExitGroupPackage>(item);
                                    var egp = Util.Helpers.Json.ToObject<ExitGroupPackage>(item);
                                    if (obj.code == 0)
                                    {
                                        if (egp.data.userIds.Contains(SDKClient.Instance.property.CurrentAccount.userID))//自己退群
                                        {
                                            if (egp.data.adminId == 0)
                                            {
                                                //删除群的聊天记录
                                                await DAL.DALMessageHelper.DeleteHistoryMsg(egp.data.groupId, chatType.groupChat);
                                                await DAL.DALGroupOptionHelper.DeleteGroupListPackage();
                                                await DAL.DALJoinGroupHelper.DeleteJoinGroupItem(egp.data.groupId);//删除该群的入群申请列表
                                                                                                                   //  Util.Helpers.Async.Run(async () => await DAL.DALGroupOptionHelper.DeleteGroupMemberListPackage(package.data.groupId));
                                            }
                                            else//被T出
                                            {
                                                var goh_msg = await DAL.DALGroupOptionHelper.SendMsgtoDB(egp);
                                             
                                                await DAL.DALMessageHelper.UpdateMsgIsRead(egp.data.groupId, 1);
                                                await DAL.DALJoinGroupHelper.DeleteJoinGroupItem(egp.data.groupId);//删除该群的入群申请列表
                                            }
                                        }
                                        else
                                        {
                                            if (egp.data.adminIds != null && egp.data.adminIds.Contains(SDKClient.Instance.property.CurrentAccount.userID))
                                            {
                                                var goh_msg = await DAL.DALGroupOptionHelper.SendMsgtoDB(egp);
                                             
                                            }
                                        }

                                    }

                                    break;
                                case Protocol.ProtocolBase.DismissGroupCode:
                                    obj = Newtonsoft.Json.JsonConvert.DeserializeObject<DismissGroupPackage>(item);
                                    var dgp = Util.Helpers.Json.ToObject<DismissGroupPackage>(item);

                                    if (dgp.code == 0)
                                    {
                                        //群主本人则删除群的聊天记录
                                        if (dgp.data.ownerId == SDKClient.Instance.property.CurrentAccount.userID)
                                            await DAL.DALMessageHelper.DeleteHistoryMsg(dgp.data.groupId, chatType.groupChat);
                                        await DAL.DALGroupOptionHelper.DeleteGroupListPackage();
                                        //Util.Helpers.Async.Run(async () => await DAL.DALGroupOptionHelper.DeleteGroupMemberListPackage(package.data.groupId));
                                        var goh_msg = await DAL.DALGroupOptionHelper.SendMsgtoDB(dgp);
                                    
                                        await DAL.DALJoinGroupHelper.DeleteJoinGroupItem(dgp.data.groupId);//删除该群的入群申请列表
                                        await DAL.DALMessageHelper.UpdateMsgIsRead(dgp.data.groupId, 1);
                                    }
                                    break;
                                case Protocol.ProtocolBase.JoinGroupAcceptedCode:
                                    obj = Newtonsoft.Json.JsonConvert.DeserializeObject<JoinGroupAcceptedPackage>(item);
                                    var jgap = Util.Helpers.Json.ToObject<JoinGroupAcceptedPackage>(item);

                                    if (jgap.code == 0)
                                    {
                                        // Task.Run(async () => await DAL.DALGroupOptionHelper.DeleteGroupMemberListPackage(package.data.groupId));
                                        //TODO:删除群申请列表中申请记录
                                        await DAL.DALJoinGroupHelper.DeleteJoinGroupItem(jgap as JoinGroupAcceptedPackage);
                                        //入群通知消息入库
                                        if (jgap.data.auditStatus == 1)
                                        {
                                            var jgap_msg = await DAL.DALGroupOptionHelper.SendMsgtoDB(jgap);
                                          
                                        }
                                    }
                                    else if (jgap.code == (int)Protocol.StatusCode.UserIsGroupMember || jgap.code == (int)Protocol.StatusCode.AlreadyCompleted)
                                    {
                                        if (!await DAL.DALJoinGroupHelper.DeleteJoinGroupItem(jgap as JoinGroupAcceptedPackage))
                                            SDKClient.logger.Error($"删除入群申请记录失败：{item.ToString()}");

                                    }
                                    break;
                                    //case Protocol.ProtocolBase.SyncMsgStatusCode:
                                    //    var smsp = Util.Helpers.Json.ToObject<SyncMsgStatusPackage>(Util.Helpers.Json.ToJson(item));

                                    //    if (smsp.code == 0)
                                    //    {
                                    //        if (smsp.data.partnerId == 0)
                                    //            await DAL.DALMessageHelper.UpdateMsgIsRead(smsp.data.groupId, (int)SDKProperty.chatType.groupChat);
                                    //        else
                                    //            await DAL.DALMessageHelper.UpdateMsgIsRead(smsp.data.partnerId, (int)SDKProperty.chatType.chat);
                                    //    }

                                    //    break;
                            }

                            this.OnNewDataRecv(obj);
                        }
                    }
                    //获取这次拉取的时间
                    var data = r.entryList;
                    double ts = r.deadTime;
                    var ole = new DateTime(1970, 1, 1).AddMilliseconds(ts);
                    var lh = ole.ToLocalTime();
                    DAL.DALAccount.UpdateAccountOfflineMsgTime(lh);//记录这次拉取的时间值

                    //按条目拉取消息，并且记录未拉取任务
                    data.ForEach(room =>
                    {

                        room.fromTime = ts;
                        if (property.CurrentAccount.GetOfflineMsgTime.HasValue)
                            room.earlyTime = (property.CurrentAccount.GetOfflineMsgTime.Value.ToUniversalTime() - new DateTime(1970, 1, 1)).TotalMilliseconds;
                        else
                        {
                            room.earlyTime = 0;
                        }
                        OffLineMsgController.CreateTask_Offline(room, true);

                    });
                }
            });
            // 开始未完成的任务
            Task.Run(() =>
            {
                OffLineMsgController.RunOffLineTasks();
            });
        }
        /// <summary>
        /// 获取当前用户的登录信息
        /// </summary>
        /// <returns></returns>
        public historyAccountDB GetAccount()
        {
            var a = Util.Helpers.Async.Run(async () => await DAL.DALAccount.GetAccount());
            return a;
        }
        public async Task AppendLocalData_InviteJoinGroupPackage(InviteJoinGroupPackage package, SDKProperty.MessageState messageState = MessageState.sendfaile, bool isFoward = false)
        {
            await GetData(async () =>
            {
                messageState += (int)SDKProperty.MessageState.isRead;
                await DAL.DALGroupOptionHelper.SendMsgtoDB(package, messageState, isFoward);
            }).ConfigureAwait(false);

        }
        /// <summary>
        /// 写入本地消息
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="content"></param>
        /// <param name="roomId"></param>
        /// <param name="messageType"></param>
        /// <param name="messageState"></param>
        /// <param name="chatType"></param>
        /// <returns></returns>
        public async Task AppendLocalData_NotifyMessage(string from, string to, string content, int roomId, SDKProperty.MessageType messageType, SDKProperty.MessageState messageState = MessageState.isRead, SDKProperty.chatType chatType = chatType.chat)
        {
            await DAL.DALMessageHelper.SendMsgtoDB(SDKProperty.RNGId, from, to, content, roomId, 0, messageType, messageState, chatType);

        }


        /// <summary>
        /// 查找资源是否存在
        /// </summary>
        /// <param name="resourceName">资源全路径</param>
        /// <returns></returns>
        public async Task<(bool existed, string resourceId, long fileSize)> FindResource(string resourceFullName)
        {
            try
            {

                //验证资源是否存在

                //  var filedata = File.ReadAllBytes(resourceFullName);
                // var name = Util.Helpers.Encrypt.Md5By32(filedata);
                using (FileStream fs = File.OpenRead(resourceFullName))
                {
                    string md5 = Util.Helpers.Encrypt.Md5By32(fs);

                    var t = await WebAPICallBack.FindResource($"{md5}{Path.GetExtension(resourceFullName)}");

                    if (t.isExist)
                        return (t.isExist, $"{md5}{Path.GetExtension(resourceFullName)}", fs.Length);
                    else
                        return (false, $"{md5}{Path.GetExtension(resourceFullName)}", fs.Length);
                }
            }
            catch (Exception)
            {

                return (false, null, 0);
            }
        }
        /// <summary>
        /// 查找资源是否存在
        /// </summary>
        /// <param name="resourceName">资源全路径</param>
        /// <returns></returns>
        public async Task<fileInfo> IsFileExist(string resourceFullName)
        {
            try
            {

                //验证资源是否存在

                //  var filedata = File.ReadAllBytes(resourceFullName);
                // var name = Util.Helpers.Encrypt.Md5By32(filedata);
                using (FileStream fs = File.OpenRead(resourceFullName))
                {
                    long len = fs.Length;
                    string md5 = Util.Helpers.Encrypt.Md5By32(fs);

                    var file = await WebAPICallBack.FindResource($"{md5}{Path.GetExtension(resourceFullName)}");
                    if(file==null||!file.isExist)
                    {
                        file.fileSize = len;
                        file.fileCode = $"{md5}{Path.GetExtension(resourceFullName)}";
                        
                    }
                    return file;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return null;
            }
        }
        /// <summary>
        /// 上传资源
        /// </summary>
        /// <param name="resourceName">资源全路径名称</param>
        /// <param name="UploadProgressChanged">上传进度</param>
        /// <param name="UploadDataCompleted"></param>
        /// <returns></returns>
        public void UpLoadResource( string resourceFullName, Action<long> UploadProgressChanged, Action<bool, string, SDKProperty.ErrorState> UploadDataCompleted,
            System.Threading.CancellationToken? cancellationToken = null)
        {

            //上传资源
            if (File.Exists(resourceFullName))
            {
                FileInfo info = new FileInfo(resourceFullName);
                if (info.Length > 500 * 1000 * 1000)
                {
                    UploadDataCompleted?.Invoke(false, null, SDKProperty.ErrorState.OutOftheControl);
                    return;
                }
                byte[] filedata = null;
                try
                {
                    filedata = File.ReadAllBytes(resourceFullName);
                }
                catch (Exception ex)
                {
                    UploadDataCompleted?.Invoke(false, resourceFullName, SDKProperty.ErrorState.AppError);
                    logger.Error($"UploadError: filename:{resourceFullName},ex:{ex.Message}");
                    return;
                }

#if !CUSTOMSERVER

                string flag = DateTime.Now.Ticks.ToString("x");//不能删除
                var boundary = "---------------------------" + flag;

                string httpRow = $"--{boundary}\r\nContent-Disposition: form-data; name=\"uploadfile\"; filename=\"{Path.GetFileName(resourceFullName)}\"\r\nContent-Type: application/octet-stream\r\n\r\n";
                var datas = Encoding.UTF8.GetBytes(httpRow);
                datas = datas.Concat(filedata)
                    .Concat(Encoding.UTF8.GetBytes("\r\n"))
                    .Concat(Encoding.UTF8.GetBytes($"--{ boundary}--\r\n")).ToArray();


                WebClient webClient = new WebClient();
                webClient.Headers.Add("Content-Type", "multipart/form-data; boundary=" + boundary);
                webClient.UploadProgressChanged += (s, e) =>
                {
                    if (cancellationToken == null || !cancellationToken.Value.IsCancellationRequested)
                        UploadProgressChanged?.Invoke(e.BytesSent);
                    else
                        webClient.CancelAsync();

                };
                webClient.UploadDataCompleted += (s, e) =>
                {

                    if (e.Error == null)
                    {
                        UploadResult webapiResult = Util.Helpers.Json.ToObject<WebAPI.UploadResult>(Encoding.UTF8.GetString(e.Result));
                        if (webapiResult.code == 0)
                        {
                            UploadDataCompleted?.Invoke(true, webapiResult.uploadResult.First().fileCode, SDKProperty.ErrorState.None);
                            var fileFullName = Path.Combine(property.CurrentAccount.filePath, Path.GetFileName(resourceFullName));
                            //if(fileType== FileType.file)
                            //{
                            //    lock (obj_lock)
                            //    {

                            //        File.WriteAllBytes(fileFullName, filedata);
                            //    }
                            //}
                            //else
                            //{
                            //    if (!File.Exists(fileFullName))
                            //    {
                            //        lock (obj_lock)
                            //        {
                            //            if (!File.Exists(fileFullName))
                            //                File.WriteAllBytes(fileFullName, filedata);
                            //        }
                            //    }
                            //}
                        }
                        else
                        {
                            UploadDataCompleted?.Invoke(false, webapiResult.uploadResult.First().fileCode, SDKProperty.ErrorState.ServerException);
                            logger.Error($"UploadError: code:{webapiResult.code},error:{webapiResult.error},id:{webapiResult.uploadResult.First().fileCode}");
                        }
                    }
                    else
                    {
                        if (e.Cancelled)
                        {
                            UploadDataCompleted?.Invoke(false, resourceFullName, SDKProperty.ErrorState.Cancel);
                            logger.Info($"UploadCancel: filename:{resourceFullName}");
                        }
                        else
                        {
                            UploadDataCompleted?.Invoke(false, resourceFullName, SDKProperty.ErrorState.NetworkException);
                            logger.Error($"UploadError: filename:{resourceFullName},ex:{e.Error.Message}");
                        }
                    }
                };

                webClient.UploadDataAsync(new Uri(Protocol.ProtocolBase.uploadresource), datas);
#else
                WebClient webClient = new WebClient();
                webClient.UploadFileCompleted += (s, e) =>
                {
                    if (e.Error == null)
                    {
                        dynamic d = Util.Helpers.Json.ToObject<dynamic>(Encoding.UTF8.GetString(e.Result));
                        if (d.code != 1)
                        {
                            UploadDataCompleted?.Invoke(false, d.message, SDKProperty.ErrorState.ServerException);
                            logger.Error($"UploadError: code:{d.code},error:{d.message},id:{resourceFullName}");
                        }
                        else
                        {
                            UploadDataCompleted?.Invoke(true, $"{d.data.originalphoto},{d.data.thumbnailphoto}", SDKProperty.ErrorState.None);
                        }
                    }
                    else
                    {
                        if (e.Cancelled)
                        {
                            UploadDataCompleted?.Invoke(false, resourceFullName, SDKProperty.ErrorState.Cancel);
                            logger.Info($"UploadCancel: filename:{resourceFullName}");
                        }
                        else
                        {
                            UploadDataCompleted?.Invoke(false, resourceFullName, SDKProperty.ErrorState.NetworkException);
                            logger.Error($"UploadError: filename:{resourceFullName},ex:{e.Error.Message}");
                        }
                    }
                };
                webClient.UploadProgressChanged += (s, e) =>
                {
                    if (cancellationToken == null || !cancellationToken.Value.IsCancellationRequested)
                        UploadProgressChanged?.Invoke(e.BytesSent);
                    else
                        webClient.CancelAsync();
                };
                string strtime = DateTime.Now.ToString("yyyyMMddHHmmss");
                string param = $"upload{strtime}{CustomServerURL.CSKEY}";
                string signatureresult = MJD.Utility.UtilityCrypto.Encrypt(param, MJD.Utility.CryptoProvider.MD5).ToLower();

                webClient.Headers.Add("signature", signatureresult);
                webClient.Headers.Add("action", "upload");
                webClient.Headers.Add("time", strtime);
                var source = new System.Drawing.Bitmap(resourceFullName);
                webClient.UploadFileAsync(new Uri($"{WebAPI.CustomServerURL.UploadIMG}?width={source.Width}&height={source.Height}"), resourceFullName);
#endif
                return;
            }
            else
                return;
        }
        //UpLoadResource//ResumeUpload
        public async void ResumeUpload(string resourceFullName,string md5,Action<long> UploadProgressChanged, Action<bool, string, SDKProperty.ErrorState> UploadDataCompleted,
           List<int> blockNum,  System.Threading.CancellationToken? cancellationToken = null)
        {
            
            int blocklen = 1024 * 1024 * 2;
            using (FileStream fs = new FileStream(resourceFullName, FileMode.Open))
            {
               
                long totalCount = fs.Length;
                long totalnum = totalCount / blocklen;
                if (totalCount % blocklen != 0)
                    totalnum += 1;
                fs.Seek(0, SeekOrigin.Begin);
                bool Isfaile = false;
                for (int i = 1; i < (totalnum+1); i++)
                {
                    if (blockNum!=null&&blockNum.Contains(i))
                        continue;
                    if (cancellationToken.HasValue && cancellationToken.Value.IsCancellationRequested)
                        break;
                    byte[] buff = new byte[blocklen];
                    int len = fs.Read(buff, 0, buff.Length);
                    if (len == blocklen)
                    {
                        await WebAPICallBack.ResumeUpload(buff, i, blocklen, md5, totalCount, totalnum).ContinueWith(async t =>
                        {
                            if(t.IsFaulted)//服务不可用
                            {
                                //TODO:
                               // UploadDataCompleted?.Invoke(false, md5, ErrorState.NetworkException);
                                Isfaile = true;
                            }
                            else
                            {
                                var obj = t.Result;
                                if (!obj.Success)
                                {
                                    if (obj.code == "-999")
                                    {
                                        Isfaile = true;
                                        return;
                                    }
                                    bool isOk = false;
                                    for (int j = 0; j < 5; j++)
                                    {
                                       var r = await WebAPICallBack.ResumeUpload(buff, i, blocklen, md5, totalCount, totalnum);
                                        isOk = r.Success;
                                        if (isOk)
                                            break;
                                            
                                    }
                                    Isfaile = isOk ? false : true;
                                    
                                }
                                else
                                {
                                    UploadProgressChanged?.Invoke(len);
                                }
                            }
                        });

                    }
                    else
                    {
                        byte[] temp = new byte[len];
                        Buffer.BlockCopy(buff, 0, temp, 0, len);
                        await WebAPICallBack.ResumeUpload(temp, i, len, md5, totalCount, totalnum).ContinueWith(async t =>
                        {
                            if (t.IsFaulted)
                            {
                                //TODO:
                                Isfaile = true;
                            }
                            else
                            {
                                var obj = t.Result;
                                if (!obj.Success)
                                {

                                    if (obj.code == "-999")
                                    {
                                        Isfaile = true;
                                        return;
                                    }
                                    bool isOk = false;
                                    for (int j = 0; j < 5; j++)
                                    {
                                        var r = await WebAPICallBack.ResumeUpload(buff, i, blocklen, md5, totalCount, totalnum);
                                        isOk = r.Success;
                                        if (isOk)
                                            break;

                                    }
                                    Isfaile = isOk ? false : true;

                                }
                                else
                                {
                                    UploadProgressChanged?.Invoke(len);
                                }
                            }

                        });
                    }


                }
                if(Isfaile)
                {
                    UploadDataCompleted?.Invoke(false, md5, ErrorState.ServerException);
                }
                else
                {
                    UploadDataCompleted?.Invoke(true, md5, ErrorState.None);
                }

            }
        }



        private static object obj_lock = new object();
        //DownloadFileWithResume
        public void DownLoadResource(string resourceName, string fileName, FileType fileType, Action<long> downloadProgressChanged, 
            Action<bool> downloadDataCompleted, string msgId, System.Threading.CancellationToken? cancellationToken = null)
        {
            void UpdateFileState(DB.messageDB message, int fileState)
            {
                if (message == null)
                    return;
                int i = Util.Helpers.Async.Run(async () => await SDKProperty.SQLiteConn.ExecuteAsync($"update messageDB set fileState={fileState} where Id='{message.Id}'"));
            }
            var m = Util.Helpers.Async.Run(async () => await DAL.DALMessageHelper.Get(msgId));
#if !CUSTOMSERVER

            WebClient webClient = new WebClient();

            UpdateFileState(m, (int)ResourceState.Working);
            webClient.DownloadProgressChanged += (s, e) =>
            {
                if (cancellationToken == null || !cancellationToken.Value.IsCancellationRequested)
                    downloadProgressChanged?.Invoke(e.BytesReceived);
                else
                {
                    webClient.CancelAsync();
                    UpdateFileState(m, (int)ResourceState.IsCancelled);

                }
            };

            webClient.DownloadDataCompleted += (s, e) =>
            {
                try
                {
                    // if (((WebClient)s).ResponseHeaders.GetValue("Content-Type") != "application/json")
                    if (!((WebClient)s).ResponseHeaders.GetValue("Content-Type").ToLower().Contains("application/json"))
                    {
                        if (e.Error == null && e.Cancelled == false && e.Result.Length > 0)
                        {
                            try
                            {
                                if (e.Result.Length < 100)
                                {
                                    logger.Error($"下载资源失败;message:{Encoding.UTF8.GetString(e.Result)},name:{resourceName}");
                                    downloadDataCompleted?.Invoke(false);
                                    return;
                                }
                                var data = e.Result;
                                string basePath = null;

                                switch (fileType)
                                {
                                    case FileType.img:
                                        basePath = Path.Combine(SDKProperty.imgPath, property.CurrentAccount.loginId);
                                        if (!Directory.Exists(basePath))
                                            Directory.CreateDirectory(basePath);
                                        break;
                                    case FileType.file:
                                        basePath = Path.Combine(SDKProperty.filePath, property.CurrentAccount.loginId);
                                        if (!Directory.Exists(basePath))
                                            Directory.CreateDirectory(basePath);
                                        break;
                                    default:
                                        break;
                                }
                                if (Path.IsPathRooted(fileName))
                                {
                                    var dir = Path.GetDirectoryName(fileName);
                                    if (!Directory.Exists(dir))
                                        Directory.CreateDirectory(dir);
                                    if (!File.Exists(fileName))
                                    {
                                        lock (obj_lock)
                                        {
                                            if (!File.Exists(fileName))
                                            {
                                                File.WriteAllBytes(fileName, data);
                                                // Util.Helpers.Async.Run(async () => await DAL.DALMessageHelper.UpdateMsgNewFileName(msgId, fileName));
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (!File.Exists(Path.Combine(basePath, fileName)))
                                    {
                                        lock (obj_lock)
                                        {
                                            if (!File.Exists(Path.Combine(basePath, fileName)))
                                            {
                                                File.WriteAllBytes(Path.Combine(basePath, fileName), data);
                                                //  Util.Helpers.Async.Run(async () => await DAL.DALMessageHelper.UpdateMsgNewFileName(msgId, Path.Combine(basePath, fileName)));
                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {

                                logger.Error($"下载资源失败;message:{ex.Message},name:{resourceName}");
                                UpdateFileState(m, (int)ResourceState.NoStart);
                                downloadDataCompleted?.Invoke(false);
                                return;
                            }
                            UpdateFileState(m, (int)ResourceState.IsCompleted);
                            downloadDataCompleted?.Invoke(true);
                        }
                        else
                        {
                            logger.Error($"下载资源失败;message:{Encoding.UTF8.GetString(e.Result)},name:{resourceName}");
                            UpdateFileState(m, (int)ResourceState.NoStart);
                            downloadDataCompleted?.Invoke(false);
                        }
                    }
                    else
                    {
                        UpdateFileState(m, (int)ResourceState.NoStart);
                        downloadDataCompleted?.Invoke(false);
                        if (e.Error == null)
                            logger.Error($"下载资源失败;message:{Encoding.UTF8.GetString(e.Result)},name:{resourceName}");
                        else
                            logger.Error($"下载资源失败;message:{e.Error.Message},name:{resourceName}");
                    }
                }
                catch (Exception ex)
                {
                    logger.Error($"下载资源失败;message:{ex.Message},name:{resourceName}");
                    UpdateFileState(m, (int)ResourceState.NoStart);
                    downloadDataCompleted?.Invoke(false);
                }
            };
            logger.Info($"开始下载资源：{resourceName}");
            webClient.DownloadDataAsync(new Uri(string.Format("{0}{1}", Protocol.ProtocolBase.downLoadResource, resourceName)));

#else

            if (Path.IsPathRooted(fileName))
            {
                var dir = Path.GetDirectoryName(fileName);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                Util.Helpers.Async.Run(async () => await DAL.DALMessageHelper.UpdateMsgNewFileName(msgId, fileName));

            }
            else
            {
                fileName = Path.Combine(property.CurrentAccount.imgPath, fileName);

                Util.Helpers.Async.Run(async () => await DAL.DALMessageHelper.UpdateMsgNewFileName(msgId, fileName));

            }
            Task.Run(() =>
            {
                try
                {
                    var stream = WebRequest.Create(resourceName).GetResponse().GetResponseStream();

                    using (FileStream fs = new FileStream(fileName, FileMode.OpenOrCreate))
                    {
                        byte[] buff = new byte[4096];
                        while (true)
                        {
                            int i = stream.Read(buff, 0, 4096);
                            fs.Write(buff, 0, i);

                            if (i == 0)
                                break;
                        }
                        fs.Flush();

                    }
                }
                catch (Exception e)
                {

                    downloadDataCompleted?.Invoke(false);

                    logger.Error($"下载资源失败;message:{e.Message},name:{resourceName}");
                }
                UpdateFileState(m, (int)ResourceState.IsCompleted);
                downloadDataCompleted?.Invoke(true);
            });
#endif


        }
        /// <summary>
        /// 断点续传
        /// </summary>
        /// <param name="resourceName"></param>
        /// <param name="fileName"></param>
        /// <param name="fileType"></param>
        /// <param name="msgId"></param>
        /// <param name="InitProgress"></param>
        /// <param name="downloadProgressChanged"></param>
        /// <param name="downloadDataCompleted"></param>
        /// <param name="cancellationToken"></param>
        public void DownloadFileWithResume(string resourceName, string fileName, FileType fileType, Action<long> downloadProgressChanged,
            Action<bool> downloadDataCompleted, string msgId, Action<long, long> InitProgress=null, System.Threading.CancellationToken? cancellationToken = null)
        {
            void UpdateFileState(DB.messageDB message, int fileState)
            {
                if (message == null)
                    return;
                int i = Util.Helpers.Async.Run(async () => await SDKProperty.SQLiteConn.ExecuteAsync($"update messageDB set fileState={fileState} where Id='{message.Id}'"));
            }
            var m = Util.Helpers.Async.Run(async () => await DAL.DALMessageHelper.Get(msgId));


            UpdateFileState(m, (int)ResourceState.Working);
            string sourceUrl = string.Format("{0}{1}", Protocol.ProtocolBase.DownloadFileWithResume, resourceName);
            string basePath = null;
            
            if (!Path.IsPathRooted(fileName))
            {
                switch (fileType)
                {
                    case FileType.img:
                        basePath = Path.Combine(SDKProperty.imgPath, property.CurrentAccount.loginId);
                        if (!Directory.Exists(basePath))
                            Directory.CreateDirectory(basePath);
                        break;
                    case FileType.file:
                        basePath = Path.Combine(SDKProperty.filePath, property.CurrentAccount.loginId);
                        if (!Directory.Exists(basePath))
                            Directory.CreateDirectory(basePath);
                        break;
                    default:
                        break;
                }
                fileName = Path.Combine(basePath, fileName);
            }
            IMRequest.DownloadFileWithResume(sourceUrl, fileName, InitProgress, downloadProgressChanged, (b)=> {
                if (b)
                {
                    UpdateFileState(m, (int)ResourceState.IsCompleted);
                }
                else
                    UpdateFileState(m, (int)ResourceState.IsCancelled);
                downloadDataCompleted?.Invoke(b);

            }, cancellationToken);

        }

        public void GetLaunchFile()
        {
            try
            {

                System.Net.WebClient client = new System.Net.WebClient();
                //client.UploadProgressChanged += Client_UploadProgressChanged; ;
                client.DownloadDataCompleted += Client_DownloadDataCompleted;
                client.Headers.Add(System.Net.HttpRequestHeader.ContentType, "application/json");
                client.DownloadDataAsync(new Uri(ProtocolBase.downLoadUpdateFile));
            }
            catch
            {

            }
        }

        private static void Client_DownloadDataCompleted(object s, System.Net.DownloadDataCompletedEventArgs e)
        {
            try
            {
                if (((WebClient)s).ResponseHeaders.GetValue("Content-Type") != "application/json")
                {
                    if (e.Error == null && e.Cancelled == false && e.Result.Length > 0)
                    {
                        var version = ((WebClient)s).ResponseHeaders.GetValue("subgradeVersion");
                        var data = e.Result;
                        var fileName = "IMLaunch.exe";
                        var basePath = AppDomain.CurrentDomain.BaseDirectory;
                        if (File.Exists(basePath + fileName))
                        {
                            var lst = System.Diagnostics.Process.GetProcessesByName("IMLaunch");
                            foreach (var item in lst)
                            {
                                item.Kill();
                            }
                            File.Delete(basePath + fileName);
                            File.WriteAllBytes(Path.Combine(basePath, fileName), data);
                        }
                        else
                        {
                            File.WriteAllBytes(Path.Combine(basePath, fileName), data);
                        }
                        var config = System.Configuration.ConfigurationManager.OpenExeConfiguration("IMUI.exe");
                        //string version = "0";
                        if (config.AppSettings.Settings["updateversion"] != null)
                        {
                            logger.Info($"升级包版本号：" + config.AppSettings.Settings["updateversion"].Value);
                            config.AppSettings.Settings["updateversion"].Value = version;
                            config.Save();
                        }
                        else
                        {
                            config.AppSettings.Settings.Add("updateversion", version);
                            config.Save();
                        }

                        ConfigurationManager.RefreshSection("appSettings");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error($"下载升级文件失败;message:{ex.Message}");
            }
        }
        public string SendOnlineFile(int to, string fileFullName, Action<long> SetProgressSize, Action<(int isSuccess, string imgMD5, string imgId, NotificatonPackage notifyPackage)> SendComplete, Action<long> ProgressChanged,
            System.Threading.CancellationToken? cancellationToken = null)
        {

            string MD5 = string.Empty;
            long fileSize = 0;
            FileInfo info = new FileInfo(fileFullName);
            fileSize = info.Length;
            //发送在线文件消息给对方
            if (cancellationToken != null && !cancellationToken.Value.IsCancellationRequested)
            {
                string id = SendOnlineFileMessage(fileFullName, to.ToString(), MD5, fileSize, SDKProperty.P2PServer.GetLocalIP(), SDKProperty.P2PServer.GetLocalPort());
                P2P.P2PClient p2PHelper = new P2P.P2PClient()
                {
                    CancellationToken = cancellationToken,
                    FileName = fileFullName,
                    MD5 = MD5,
                    MsgId = id,
                    From = property.CurrentAccount.userID,
                    To = to,
                    FileSize = fileSize
                };
                p2PHelper.SendComplete += SendComplete;
                p2PHelper.SetProgressSize += SetProgressSize;
                p2PHelper.ProgressChanged += ProgressChanged;
                P2P.P2PClient.FileCache.Add(id, p2PHelper);
                return id;
            }
            else
                return null;

        }


        /// <summary>
        /// 接收在线文件
        /// </summary>
        /// <param name="msgId"></param>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="to"></param>
        /// <param name="fileName"></param>
        /// <param name="resourceId"></param>
        /// <param name="SetProgressSize"></param>
        /// <param name="SendComplete"></param>
        /// <param name="ProgressChanged"></param>
        /// <param name="cancellationToken"></param>
        public bool RecvOnlineFile(string msgId, string ip, int port, int to, long fileSize, string fileName, string resourceId, Action<long> SetProgressSize, Action<(int isSuccess, string imgMD5, string imgId, NotificatonPackage notifyPackage)> SendComplete, Action<long> ProgressChanged,
         System.Threading.CancellationToken? cancellationToken = null)
        {
            P2P.P2PClient p2PHelper = new P2P.P2PClient()
            {
                CancellationToken = cancellationToken,
                FileName = fileName,
                RemotePort = port,
                RemoteIP = IPAddress.Parse(ip),
                From = to,
                To = property.CurrentAccount.userID,
                MD5 = resourceId,
                MsgId = msgId,
                FileSize = fileSize
            };
            p2PHelper.SendComplete += SendComplete;
            p2PHelper.SetProgressSize += SetProgressSize;
            p2PHelper.ProgressChanged += ProgressChanged;
            property.SendP2PList.Add(p2PHelper);
            if (p2PHelper.TryConnect())
            {
                p2PHelper.SendHeader();

                return true;
            }
            else
            {

                return false;
            }

        }

        private static object face_lock = new object();
        private static object myimg_lock = new object();
        public void DownLoadFacePhoto(string resourceName, Action ErrorCallBack = null, Action SuccessCallBack = null)
        {
#if !CUSTOMSERVER
            System.Threading.CancellationTokenSource source = new System.Threading.CancellationTokenSource();

            WebClient webClient = new WebClient();
            webClient.DownloadDataCompleted += (s, e) =>
            {

                source.Cancel();
                if (!((WebClient)s).ResponseHeaders.GetValue("Content-Type").ToLower().Contains("application/json"))
                {
                    if (e.Error == null && e.Cancelled == false && e.Result.Length > 0)
                    {
                        lock (face_lock)
                        {
                            var data = e.Result;
                            //var str = Encoding.UTF8.GetString(e.Result);
                            var filename = Path.Combine(property.CurrentAccount.facePath, resourceName);
                            if (File.Exists(filename))
                            {
                                SuccessCallBack?.Invoke();
                            }
                            else if (File.Exists(filename))
                            {
                                SuccessCallBack?.Invoke();
                            }
                            else
                            {
                                File.WriteAllBytes(Path.Combine(property.CurrentAccount.facePath, resourceName), data);
                                SuccessCallBack?.Invoke();
                            }
                        }

                    }
                }
                else
                {
                    ErrorCallBack?.Invoke();
                    if (e.Error == null)
                        logger.Error($"下载头像失败;message:{Encoding.UTF8.GetString(e.Result)},name:{resourceName}");
                    else
                        logger.Error($"下载头像失败;message:{e.Error.Message},name:{resourceName}");

                }

            };
            Task.Run(() =>
            {
                string uri = $"{Protocol.ProtocolBase.downLoadResource}{resourceName}";
                webClient.DownloadDataAsync(new Uri(uri));
            }).ContinueWith(task =>
            {
                Task.Delay(32 * 1000, source.Token).ContinueWith(t =>
                {
                    if (t.IsCanceled)
                    {
                        //   logger.Error($"图片下载超时检测被取消;{resoureceName}");
                        return;

                    }
                    else
                    {
                        ErrorCallBack?.Invoke();
                        logger.Error($"下载头像超时;{resourceName}");
                    }
                });
            });
#else
            Task.Run(() =>
            {
                try
                {
                    string fileName = Path.GetFileName(resourceName);
                    fileName = Path.Combine(property.CurrentAccount.imgPath, fileName);
                    var stream = WebRequest.Create(resourceName).GetResponse().GetResponseStream();

                    using (FileStream fs = new FileStream(fileName, FileMode.OpenOrCreate))
                    {
                        byte[] buff = new byte[4096];
                        while (true)
                        {
                            int i = stream.Read(buff, 0, 4096);
                            fs.Write(buff, 0, i);

                            if (i == 0)
                                break;
                        }
                        fs.Flush();

                    }
                }
                catch (Exception e)
                {

                    ErrorCallBack?.Invoke();

                    logger.Error($"下载资源失败;message:{e.Message},name:{resourceName}");
                }

                SuccessCallBack?.Invoke();
            });
#endif

        }
        public async Task SendImgMessage(string imgFullName, Action<(int isSuccess, string imgMD5, string msgId, string smallId,
            SDKProperty.ErrorState)> SendComplete,
            string to, chatType type = chatType.chat, int groupId = 0,
            System.Threading.CancellationToken? cancellationToken = null, string groupName = null)
        {
            System.Threading.CancellationToken token;
            if (cancellationToken.HasValue)
                token = cancellationToken.Value;

            string sourcefile = imgFullName;
            CompressionResult imgResult = null;
            if (Compressor.IsFileSupported(imgFullName))
            {

                imgResult = compressor.CompressFileAsync(imgFullName, false);
                if (!string.IsNullOrEmpty(imgResult.ResultFileName))
                {
                    imgFullName = imgResult.ResultFileName;
                }

            }
#if CUSTOMSERVER
            Action<long> uploadProgressChanged = (x) =>
            {

            };
            Action<bool, string, SDKProperty.ErrorState> uploadDataCompleted = (b, s, e) =>
            {
                if (b)
                {
                    string imgId = Instance.SendImgMessage(imgFullName, to, s.Split(new char[] { ',' })[0], s.Split(new char[] { ',' })[1], type, cancellationToken);

                    SendComplete?.Invoke((1, s, imgId, null, e));

                }
                else
                {
                    SendComplete((0, imgFullName, null, null, e));
                }

            };
            await Task.Run(() => Instance.UpLoadResource(imgFullName, uploadProgressChanged, uploadDataCompleted, cancellationToken)).ConfigureAwait(false);
#else
            await UploadImg(imgFullName, async result =>
            {
                string imgId;
                if (result.isSuccess)
                {
                    if (Path.GetExtension(imgFullName).ToLower() == ".gif")//GIF原图发送
                    {
                        imgId = SDKProperty.RNGId;
                        SendComplete?.Invoke((1, result.imgMD5, imgId, null, SDKProperty.ErrorState.None));
                        imgId = Instance.SendImgMessage(imgFullName, to, result.imgMD5, result.imgMD5, type, cancellationToken, groupName, SessionType.CommonChat, imgId);
                    }
                    else
                    {

                        var bitImgFile = Path.Combine(property.CurrentAccount.imgPath, $"my{result.imgMD5}");

                        if (!File.Exists(bitImgFile))//小图本地不存在
                        {
                            var source = new System.Drawing.Bitmap(imgFullName);

                            var With = (int)Math.Min(source.Width, 300);
                            var h = With * source.Height / source.Width;
                            var bmp = Util.ImageProcess.GetThumbnail(imgFullName, With, h);
                            using (MemoryStream ms = new MemoryStream())
                            {
                                bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);

                                ms.Seek(0, SeekOrigin.Begin);
                                var bmpArray = ms.ToArray();
                                lock (myimg_lock)
                                {
                                    if (!File.Exists(bitImgFile))
                                        File.WriteAllBytes(bitImgFile, bmpArray);
                                }

                            }

                        }


                        // var smallresult = Util.Helpers.Async.Run(async () => await Instance.FindResource(bitImgFile));
                        await UploadImg(bitImgFile, smallresult =>
                        {
                            if (smallresult.isSuccess)
                            {
                                imgId = SDKProperty.RNGId;
                                SendComplete((1, result.imgMD5, imgId, smallresult.imgMD5, SDKProperty.ErrorState.None));
                                imgId = Instance.SendImgMessage(imgFullName, to, result.imgMD5, smallresult.imgMD5, type, cancellationToken, groupName, SessionType.CommonChat, imgId);
                            }
                            else
                            {
                                SendComplete((0, smallresult.imgMD5, null, null, smallresult.errorState));
                            }
                        }, token);

                    }
                }
                else
                {

                    SendComplete((0, result.imgMD5, null, null, result.errorState));

                }
                var fullname = Path.Combine(Instance.property.CurrentAccount.imgPath, result.imgMD5);
                if (!File.Exists(fullname))
                {
                    var filedata = File.ReadAllBytes(imgFullName);
                    File.WriteAllBytes(fullname, filedata);

                }
            }, token);
#endif

        }




        public string SendImgMessage(string path, string to, string resourceId, string smallresourceId,
            chatType type = chatType.chat, System.Threading.CancellationToken? cancellationToken = null, string groupName = null, SDKProperty.SessionType sessionType = SessionType.CommonChat, string imgId = "")
        {
            MessagePackage package = new MessagePackage();
            package.ComposeHead(to, property.CurrentAccount.userID.ToString());
            if (!string.IsNullOrEmpty(imgId))
                package.id = imgId;
            string width = string.Empty, height = string.Empty;
            try
            {
                using (var bmp = new System.Drawing.Bitmap(path))
                {
                    width = bmp.Width.ToString();
                    height = bmp.Height.ToString();
                }
            }
            catch (Exception ex)
            {
                SDKClient.logger.Error($"发送图片消息提取图片宽高：error:{ex.Message},stack:{ex.StackTrace};\r\n");
            }
            if (to == property.CurrentAccount.userID.ToString())
            {

            }
            package.data = new message()
            {
                body = new ImgBody()
                {
                    fileName = path,
                    id = resourceId,
                    smallId = smallresourceId,
                    width = width,
                    height = height
                },
                senderInfo = new message.SenderInfo()
                {
                    photo = property.CurrentAccount.photo,
                    userName = property.CurrentAccount.userName ?? property.CurrentAccount.loginId
                },
                subType = "img",
                chatType = to == property.CurrentAccount.userID.ToString() ? (int)SessionType.FileAssistant : (int)sessionType,
                type = type == chatType.chat ? nameof(chatType.chat) : nameof(chatType.groupChat)
            };
            if (type == chatType.groupChat)
            {
                package.data.groupInfo = new message.msgGroup()
                {
                    groupId = to.ToInt(),
                    groupName = groupName
                };
            }

            if (cancellationToken != null && cancellationToken.HasValue)
            {
                if (!cancellationToken.Value.IsCancellationRequested)
                    package.Send(ec);
            }
            else
                package.Send(ec);
            return package.id;
        }

        public string SendFileMessage(string path, string to, string resourceId, long fileSize, chatType type = chatType.chat, string groupName = null, int width = 0, int height = 0, string imgMD5 = null, SDKProperty.SessionType sessionType = SessionType.CommonChat, string msgId = "")
        {
            MessagePackage package = new MessagePackage()
            {
                from = property.CurrentAccount.userID.ToString(),
                to = to,
                id = string.IsNullOrEmpty(msgId) ? SDKProperty.RNGId : msgId
            };
            package.data = new message()
            {
                body = new fileBody()
                {
                    fileSize = fileSize,
                    fileName = path,
                    id = resourceId,
                    width = width,
                    height = height,
                    img = imgMD5
                },
                chatType = to == property.CurrentAccount.userID.ToString() ? (int)SessionType.FileAssistant : (int)sessionType,
                subType = "file",
                senderInfo = new message.SenderInfo()
                {
                    photo = property.CurrentAccount.photo,
                    userName = property.CurrentAccount.userName
                },
                type = type == chatType.chat ? nameof(chatType.chat) : nameof(chatType.groupChat)
            };

            if (type == chatType.groupChat)
            {
                package.data.groupInfo = new message.msgGroup()
                {
                    groupId = to.ToInt(),
                    groupName = groupName
                };
            }

            package.Send(ec);
            return package.id;
        }
        public string SendFiletoDB(string path, string to, string resourceId, long fileSize, chatType type = chatType.chat, string groupName = null, int width = 0, int height = 0, string imgMD5 = null, SDKProperty.SessionType sessionType = SessionType.CommonChat, string msgId = "")
        {
            MessagePackage package = new MessagePackage()
            {
                from = property.CurrentAccount.userID.ToString(),
                to = to,
                id = string.IsNullOrEmpty(msgId) ? SDKProperty.RNGId : msgId
            };
            package.data = new message()
            {
                body = new fileBody()
                {
                    fileSize = fileSize,
                    fileName = Path.GetFileName(path),
                    id = resourceId,
                    width = width,
                    height = height,
                    img = imgMD5
                },
              
                chatType = to == property.CurrentAccount.userID.ToString() ? (int)SessionType.FileAssistant : (int)sessionType,
                subType = "file",
                senderInfo = new message.SenderInfo()
                {
                    photo = property.CurrentAccount.photo,
                    userName = property.CurrentAccount.userName
                },
                type = type == chatType.chat ? nameof(chatType.chat) : nameof(chatType.groupChat)
            };

            if (type == chatType.groupChat)
            {
                package.data.groupInfo = new message.msgGroup()
                {
                    groupId = to.ToInt(),
                    groupName = groupName
                };
            }
            Util.Helpers.Async.Run(async () => await DAL.DALMessageHelper.SendMsgtoDB(package));

            return package.id;
        }
        public string SendSmallVideoMessage(string path, string to, string recordTime, string resourceId, string previewId, int width, int height, long fileSize, chatType type = chatType.chat, string groupName = null, SDKProperty.SessionType sessionType = SessionType.CommonChat, string msgId = "")
        {
            MessagePackage package = new MessagePackage()
            {
                from = property.CurrentAccount.userID.ToString(),
                to = to,
                id = string.IsNullOrEmpty(msgId) ? SDKProperty.RNGId : msgId,
            };
            package.data = new message()
            {
                body = new smallVideoBody()
                {
                    fileSize = fileSize,
                    fileName = path,
                    id = resourceId,
                    previewId = previewId,
                    width = width,
                    height = height,

                    recordTime = recordTime

                },
                subType = Util.Helpers.Enum.GetDescription<SDKProperty.MessageType>(SDKProperty.MessageType.smallvideo),
                chatType = to == property.CurrentAccount.userID.ToString() ? (int)SessionType.FileAssistant : (int)sessionType,
                senderInfo = new message.SenderInfo()
                {
                    photo = property.CurrentAccount.photo,
                    userName = property.CurrentAccount.userName
                },
                type = type == chatType.chat ? nameof(chatType.chat) : nameof(chatType.groupChat)
            };

            if (type == chatType.groupChat)
            {
                package.data.groupInfo = new message.msgGroup()
                {
                    groupId = to.ToInt(),
                    groupName = groupName
                };
            }

            package.Send(ec);
            return package.id;
        }
        /// <summary>
        /// 设置群组的管理
        /// </summary>
        /// <param name="type">CancelManager:取消管理员 SetManager：设置管理员</param>
        /// <param name="userID">被设置者ID</param>
        /// <param name="groupID">群组ID</param>
        /// <returns></returns>
        public string SetGroupMemberAdmin(SetCancelGroupPowerOption type, int userID, int groupID)
        {
            SetMemberPowerPackage groupPackage = new SetMemberPowerPackage();
            groupPackage.ComposeHead(null, property.CurrentAccount.userID.ToString());
            groupPackage.data = new SetMemberPowerPackage.Data()
            {
                type = type == SetCancelGroupPowerOption.SetManager ? "admin" : "unAdmin",
                userIds = new List<int>(),
                groupId = groupID,
                adminId = property.CurrentAccount.userID
            };
            groupPackage.data.userIds.Add(userID);
            groupPackage.Send(ec);
            return groupPackage.id;
        }
        private string SendOnlineFileMessage(string path, string to, string resourceId, long fileSize, string ip, int port, SDKProperty.SessionType sessionType = SessionType.CommonChat)
        {
            string id;
            MessagePackage package = new MessagePackage()
            {
                from = property.CurrentAccount.userID.ToString(),
                to = to,
                id = SDKProperty.RNGId
            };
            id = package.id;
            Task.Run(() =>
            {
                package.data = new message()
                {
                    body = new OnlineFileBody()
                    {
                        fileSize = fileSize,
                        fileName = path,
                        id = resourceId,
                        IP = ip,
                        Port = port
                    },
                    subType = nameof(SDKProperty.MessageType.onlinefile),
                    chatType = (int)sessionType,
                    senderInfo = new message.SenderInfo()
                    {
                        photo = property.CurrentAccount.photo,
                        userName = property.CurrentAccount.userName
                    },
                    type = nameof(chatType.chat)
                };
                package.Send(ec);
            });
            return id;
        }

        /// <summary>
        /// 发送文件消息
        /// </summary>
        /// <param name="fileFullName">文件全路径</param>
        /// <param name="SetProgressSize">设置进度条大小</param>
        /// <param name="SendComplete">发送成功CB</param>
        /// <param name="ProgressChanged">进度条变化CB</param>
        /// <param name="to">目标ID</param>
        /// <param name="type">聊天类型<see cref="SDKProperty.chatType"/></param>
        /// <param name="cancellationToken">取消结构体对象</param>
        /// <param name="messageType">消息类型<see cref="SDKProperty.MessageType"/></param>
        /// <param name="groupName">如果type=[<see cref="SDKProperty.chatType.groupChat"/>]，需要提供群名称</param>
        /// <param name="imgFullName">缩略图全路径</param>
        /// <returns></returns>
        public async Task SendFileMessage(string fileFullName, Action<long,long> SetProgressSize,
            Action<(int isSuccess, string fileMD5, string msgId, string imgId, SDKProperty.ErrorState errorState, Func<string> func)> SendComplete, Action<long> ProgressChanged, string to,
            chatType type = chatType.chat, System.Threading.CancellationToken? cancellationToken = null, MessageType messageType = MessageType.file, string groupName = null, string imgFullName = null,string msgId=null)
        {
            
            await UploadFile(fileFullName, async result =>
            {
                if (result.isSuccess)
                {
                    if (!string.IsNullOrEmpty(imgFullName))
                    {
                        if (fileFullName.Equals(imgFullName))//图片文件
                        {
                            var bitImgFile = Path.Combine(property.CurrentAccount.imgPath, $"my{result.fileMD5}");
                            if (!File.Exists(bitImgFile))//小图本地不存在
                            {
                                var source = new System.Drawing.Bitmap(imgFullName);
                                var With = (int)Math.Min(source.Width, 300);
                                var h = With * source.Height / source.Width;
                                var bmp = Util.ImageProcess.GetThumbnail(imgFullName, With, h);
                                using (MemoryStream ms = new MemoryStream())
                                {
                                    bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);

                                    ms.Seek(0, SeekOrigin.Begin);
                                    var bmpArray = ms.ToArray();
                                    File.WriteAllBytes(bitImgFile, bmpArray);
                                }
                            }

                            await UploadImg(bitImgFile, imgresult =>
                            {
                                if (imgresult.isSuccess)
                                {
                                    using (var bmp = new System.Drawing.Bitmap(bitImgFile))
                                    {
                                        if (string.IsNullOrEmpty(msgId))
                                            msgId = SDKProperty.RNGId;
                                        SendComplete?.Invoke((1, result.fileMD5, null, imgresult.imgMD5, SDKProperty.ErrorState.None, () =>
                                          msgId
                                        ));
                                        SDKClient.Instance.SendFileMessage(fileFullName, to, result.fileMD5, result.fileSize, type, groupName, bmp.Width, bmp.Height, imgresult.imgMD5, SessionType.CommonChat, msgId);
                                    }
                                    var videoimg = Path.Combine(property.CurrentAccount.imgPath, imgresult.imgMD5);
                                    if (!File.Exists(videoimg))
                                    {
                                        var filedata = File.ReadAllBytes(imgFullName);
                                        File.WriteAllBytes(videoimg, filedata);
                                    }

                                }
                                else
                                {
                                    SendComplete?.Invoke((0, result.fileMD5, null, null, result.errorState, null));
                                }

                            }, cancellationToken);

                        }
                        else
                        {

                            //视频缩略图
                            if (Compressor.IsFileSupported(imgFullName))
                            {
                                CompressionResult imgResult = null;
                                imgResult = compressor.CompressFileAsync(imgFullName, false);
                                if (!string.IsNullOrEmpty(imgResult.ResultFileName))
                                {
                                    imgFullName = imgResult.ResultFileName;
                                }

                            }
                            await UploadImg(imgFullName, imgresult =>
                            {
                                if (imgresult.isSuccess)
                                {
                                    using (var bmp = new System.Drawing.Bitmap(imgFullName))
                                    {
                                        // string msgId = SDKClient.Instance.SendFileMessage(fileFullName, to, result.fileMD5, result.fileSize, type, groupName, bmp.Width, bmp.Height, imgresult.imgMD5);

                                        //SendComplete?.Invoke((1, result.fileMD5, null, imgresult.imgMD5, SDKProperty.ErrorState.None, () =>
                                        // SDKClient.Instance.SendFileMessage(fileFullName, to, result.fileMD5, result.fileSize, type, groupName, bmp.Width, bmp.Height, imgresult.imgMD5)
                                        //));
                                        if (string.IsNullOrEmpty(msgId))
                                            msgId = SDKProperty.RNGId;
                                        SendComplete?.Invoke((1, result.fileMD5, null, imgresult.imgMD5, SDKProperty.ErrorState.None, () =>
                                          msgId
                                        ));
                                        SDKClient.Instance.SendFileMessage(fileFullName, to, result.fileMD5, result.fileSize, type, groupName, bmp.Width, bmp.Height, imgresult.imgMD5, SessionType.CommonChat, msgId);
                                    }

                                    var videoimg = Path.Combine(property.CurrentAccount.imgPath, imgresult.imgMD5);
                                    if (!File.Exists(videoimg))
                                    {
                                        var filedata = File.ReadAllBytes(imgFullName);
                                        File.WriteAllBytes(videoimg, filedata);
                                    }


                                }
                                else
                                {
                                    SendComplete?.Invoke((0, result.fileMD5, null, null, result.errorState, null));
                                }

                            }, cancellationToken);
                        }
                    }
                    else
                    {
                        // string msgId = SDKClient.Instance.SendFileMessage(fileFullName, to, result.fileMD5, result.fileSize, type, groupName);
                        if (string.IsNullOrEmpty(msgId))
                            msgId = SDKProperty.RNGId;
                        SendComplete?.Invoke((1, result.fileMD5, null, null, SDKProperty.ErrorState.None, () =>
                          msgId
                        ));
                        
                        //SendComplete?.Invoke((1, result.fileMD5, null, null, SDKProperty.ErrorState.None, () =>
                        SDKClient.Instance.SendFileMessage(fileFullName, to, result.fileMD5, result.fileSize, type, groupName, 0, 0, "", SessionType.CommonChat, msgId);
                        //));

                    }
                }
                else
                {
                    SendComplete?.Invoke((0, result.fileMD5, null, null, result.errorState, null));
                    if (string.IsNullOrEmpty(msgId))
                        msgId = SDKProperty.RNGId;
                    SDKClient.Instance.SendFiletoDB(fileFullName, to, result.fileMD5, result.fileSize, type, groupName, 0, 0, "", SessionType.CommonChat, msgId);
                }
            },(f,t) =>
            {
                SetProgressSize?.Invoke(f,t);
            }, c =>
            {
                ProgressChanged?.Invoke(c);
            }
            , cancellationToken);
        }
        /// <summary>
        /// 发送小视频消息
        /// </summary>
        /// <param name="fileFullName">视频文件全路径</param>
        /// <param name="recordTime">时长</param>
        /// <param name="imgFullName">缩略图全路径</param>
        /// <param name="SetProgressSize">设置文件大小</param>
        /// <param name="SendComplete">发送完毕CB</param>
        /// <param name="ProgressChanged">进度条CB</param>
        /// <param name="to">目标ID</param>
        /// <param name="type">聊天类型<see cref="SDKProperty.chatType"/></param>
        /// <param name="cancellationToken">取消结构体对象</param>
        /// <param name="messageType">消息类型<see cref="SDKProperty.MessageType"/></param>
        /// <param name="groupName">如果type=[<see cref="SDKProperty.chatType.groupChat"/>]，需要提供群名称</param>
        /// <returns></returns>
        public async Task SendSmallVideoMessage(string fileFullName, string recordTime, string imgFullName, Action<long,long> SetProgressSize, Action<(int isSuccess, string videoMD5, string msgId, string imgId, SDKProperty.ErrorState errorState)> SendComplete, Action<long> ProgressChanged, string to,
           chatType type = chatType.chat, System.Threading.CancellationToken? cancellationToken = null, MessageType messageType = MessageType.smallvideo, string groupName = null)
        {

            await UploadFile(fileFullName, async result =>
            {

                if (result.isSuccess)
                {
                    await UploadImg(imgFullName, imgresult =>
                    {
                        if (imgresult.isSuccess)
                        {
                            using (var source = new System.Drawing.Bitmap(imgFullName))
                            {
                                string msgId = SDKProperty.RNGId;
                                SendComplete?.Invoke((1, result.fileMD5, msgId, imgresult.imgMD5, SDKProperty.ErrorState.None));
                                msgId = SDKClient.Instance.SendSmallVideoMessage(fileFullName, to, recordTime,
                                    result.fileMD5, imgresult.imgMD5, source.Width, source.Height, result.fileSize, type, groupName, SessionType.CommonChat, msgId);
                            }
                            var videoimg = Path.Combine(property.CurrentAccount.imgPath, imgresult.imgMD5);
                            if (!File.Exists(videoimg))
                            {
                                var filedata = File.ReadAllBytes(imgFullName);
                                File.WriteAllBytes(videoimg, filedata);

                            }


                        }
                        else
                        {
                            SendComplete?.Invoke((0, result.fileMD5, null, null, result.errorState));
                        }
                    }, cancellationToken);

                }
                else
                {
                    SendComplete?.Invoke((0, result.fileMD5, null, null, result.errorState));
                }
            }, (from,t) => SetProgressSize?.Invoke(from,t), c => ProgressChanged?.Invoke(c), cancellationToken);

        }


        public static async Task UploadImg(string imgFullName,
           Action<(bool isSuccess, string imgMD5, ErrorState errorState)> SendComplete,
            System.Threading.CancellationToken? cancellationToken)
        {

            var imgresult = await SDKClient.Instance.FindResource(imgFullName);
            if (imgresult.existed)
            {

                SendComplete?.Invoke((true, imgresult.resourceId, SDKProperty.ErrorState.None));
            }
            else
            {

                Action<bool, string, SDKProperty.ErrorState> uploadDataCompleted = (b, s, e) =>
                {
                    if (!b)
                    {
                        SendComplete?.Invoke((false, s, e));

                    }
                    else
                    {

                        SendComplete?.Invoke((true, s, SDKProperty.ErrorState.None));

                    }
                };

                SDKClient.Instance.UpLoadResource(imgFullName, null, uploadDataCompleted, cancellationToken);
            }

        }
        public static async Task UploadFile(string fileFullName,
          Action<(bool isSuccess, string fileMD5, long fileSize, ErrorState errorState)> SendComplete,
           Action<long,long> SetProgressSize, Action<long> UploadProgressChanged,
           System.Threading.CancellationToken? cancellationToken)
        {

            //var result = await SDKClient.Instance.FindResource(fileFullName);
            var result = await SDKClient.Instance.IsFileExist(fileFullName);
            if (result.isExist)
            {
                SendComplete?.Invoke((true, result.fileCode, result.fileSize, SDKProperty.ErrorState.None));
            }
            else
            {
                ResourceManifest resourceManifest = new ResourceManifest()
                {
                    MD5 = result.fileCode,
                    Size = result.fileSize,
                    State = (int)SDKProperty.ResourceState.Working
                };
                await DAL.DALResourceManifestHelper.InsertOrUpdateItem(resourceManifest);
                Action<bool, string, SDKProperty.ErrorState> uploadDataCompleted = async (b, s, e) =>
                {
                    if (!b)
                    {
#if !CUSTOMSERVER

                        await DAL.DALResourceManifestHelper.UpdateResourceState(s, ResourceState.NoStart);
#endif

                        SendComplete?.Invoke((false, s, 0, e));

                    }
                    else
                    {
#if !CUSTOMSERVER
                        await DAL.DALResourceManifestHelper.UpdateResourceState(s, ResourceState.IsCompleted);
#endif
                        FileInfo info = new FileInfo(fileFullName);
                        SendComplete?.Invoke((true, s, info.Length, SDKProperty.ErrorState.None));

                    }
                };
#if CUSTOMSERVER
                SDKClient.Instance.UpLoadResource(fileFullName, UploadProgressChanged, uploadDataCompleted, cancellationToken);
#else
                
                
              
                if (result.blocks != null&&result.blocks.Any())
                {
                    SetProgressSize?.Invoke(result.blocks.Count*result.blocks[0].blockSize, result.fileSize);
                    SDKClient.Instance.ResumeUpload(fileFullName, result.fileCode, UploadProgressChanged, uploadDataCompleted, result.blocks.Select(b => b.blockNum).ToList(), cancellationToken);
                }
                else
                {
                    SetProgressSize?.Invoke(0, result.fileSize);
                    SDKClient.Instance.ResumeUpload(fileFullName, result.fileCode, UploadProgressChanged, uploadDataCompleted, null, cancellationToken);
                }

#endif

            }

        }
        public async void UpdateMsgFileName(string msgId, string fileName)
        {
            await DAL.DALMessageHelper.UpdateMsgNewFileName(msgId, fileName);
        }
        public string SendRetractMessage(string msgId, string to, chatType type = chatType.chat, int groupId = 0, SDKProperty.RetractType retractType = RetractType.Normal, SDKProperty.SessionType sessionType = SessionType.CommonChat, message.ReceiverInfo recverInfo = null)
        {
            MessagePackage package = new MessagePackage()
            {
                from = property.CurrentAccount.userID.ToString(),
                to = to,
                id = SDKProperty.RNGId
            };
            package.data = new message()
            {
                body = new retractBody()
                {
                    retractId = msgId,
                    retractType = (int)retractType

                },
                senderInfo = new message.SenderInfo()
                {
                    photo = property.CurrentAccount.photo,
                    userName = property.CurrentAccount.userName
                },
                receiverInfo = recverInfo,
                subType = "retract",
                chatType = to == property.CurrentAccount.userID.ToString() ? (int)SessionType.FileAssistant : (int)sessionType,
                type = type == chatType.chat ? nameof(chatType.chat) : nameof(chatType.groupChat)
            };
            if (type == chatType.groupChat)
            {
                package.data.groupInfo = new message.msgGroup()
                {
                    groupId = groupId
                };
            }

            package.Send(ec);
            return package.id;
        }
        public async Task<string> SendSyncMsgStatus(int roomId, int readNum, string lastMsgID, SDKProperty.chatType chatType)
        {
            return await GetData<string>(() =>
            {
                SyncMsgStatusPackage package = new SyncMsgStatusPackage();
                package.ComposeHead(roomId.ToString(), property.CurrentAccount.userID.ToString());
                package.data = new SyncMsgStatusPackage.Data()
                {
                    lastMsgID = lastMsgID,
                    readNum = readNum
                };
                if (chatType == chatType.chat)
                    package.data.partnerId = roomId;
                else
                    package.data.groupId = roomId;

                package.Send(ec);
                return package.id;

            }).ConfigureAwait(false);
        }
        public string GetUser(int userId)
        {

            GetUserPackage package = new GetUserPackage();
            package.ComposeHead(property.ServerJID, property.CurrentAccount.userID.ToString());
            package.data = new GetUserPackage.Data()
            {
                userId = userId,
                user = new user()
                {
                    userId = userId
                }
            };
            //ThreadPool.QueueUserWorkItem(m =>
            //{
            var _loadHisTask = Task.Run(() =>
            {
                var userPackage = IMRequest.GetUser(package);
                if (userPackage != null && userPackage.code == 0)
                {
                    try
                    {
                        var cmd = CommmandSet.FirstOrDefault(c => c.Name == userPackage.api);
                        cmd?.ExecuteCommand(ec, userPackage);//日志及入库操作

                    }
                    catch (Exception ex)
                    {
                        logger.Error($"获取用户信息数据处理异常：error:{ex.Message},stack:{ex.StackTrace};\r\ncontent:{Util.Helpers.Json.ToJson(userPackage)}");

                        System.Threading.Interlocked.CompareExchange(ref SDKClient.Instance.property.CanHandleMsg, 2, 1);
                        logger.Info("CanHandleMsg 值修改为:2");
                    }
                }
            }).ContinueWith((t) =>
            {

            });
            //});
            //package.Send(ec).id
            return package.id;
        }
        /// <summary>
        /// 获取好友
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="friendId"></param>
        /// <returns></returns>
        public GetFriendPackage GetFriend(int friendId)
        {
            GetFriendPackage package = new GetFriendPackage();
            package.ComposeHead(property.ServerJID, property.CurrentAccount.userID.ToString());
            package.data = new GetFriendPackage.Data()
            {
                userId = property.CurrentAccount.userID,
                friendId = friendId,
                //item = new user()
                //{
                //    userId = userId
                //}
            };
            var userPackage = IMRequest.GetFreind(package);
            return userPackage;
        }
        public async Task<bool> GetUserPcOnlineInfo(int userId)
        {
            return await GetData<bool>(async () =>
            {
                return await WebAPICallBack.CheckUserIsOnline(userId);
            });
        }
        public GetUserPrivacySettingPackage GetUserPrivacySetting(int userId, bool isImmediately = false)
        {
            GetUserPrivacySettingPackage package = new GetUserPrivacySettingPackage();
            package.ComposeHead(property.ServerJID, property.CurrentAccount.userID.ToString());
            package.data = new GetUserPrivacySettingPackage.Data()
            {
                userId = userId
            };
            var userPrivacyPackage = IMRequest.GetUserPrivacySettingPackage(package);
            if (userPrivacyPackage != null && !isImmediately && userPrivacyPackage.code == 0)
            {
                try
                {
                    var cmd = CommmandSet.FirstOrDefault(c => c.Name == userPrivacyPackage.api);
                    cmd?.ExecuteCommand(ec, userPrivacyPackage);//日志及入库操作

                }
                catch (Exception ex)
                {
                    logger.Error($"获取用户隐私信息数据处理异常：error:{ex.Message},stack:{ex.StackTrace};\r\ncontent:{Util.Helpers.Json.ToJson(userPrivacyPackage)}");

                    System.Threading.Interlocked.CompareExchange(ref SDKClient.Instance.property.CanHandleMsg, 2, 1);
                    logger.Info("CanHandleMsg 值修改为:2");
                }
            }
            //package.Send(ec).id;
            return userPrivacyPackage;
        }
        /// <summary>
        /// 更新用户设置
        /// </summary>
        /// <param name="option"><see cref="UpdateUserOption"/>用户设置项</param>
        /// <param name="content">设置项的值</param>
        /// <returns></returns>
        public string UpdateMyself(UpdateUserOption option, string content)
        {

            var t = Task.Run(async () =>
            {
                UpdateuserPackage package = new UpdateuserPackage();
                package.ComposeHead(property.ServerJID, property.CurrentAccount.userID.ToString());
                package.data = new UpdateuserPackage.Data()
                {
                    userId = property.CurrentAccount.userID,
                    updateType = (int)option,

                    content = content

                };
                if (option == UpdateUserOption.修改头像)
                {
                    var result = await FindResource(content);
                    if (!string.IsNullOrEmpty(content))
                    {
                        package.data.content = result.resourceId;
                        if (!result.existed)
                        {
                            UpLoadResource(content, null, null);
                        }

                    }
                }
                return package.Send(ec).id;
            }).ConfigureAwait(false);
            return t.GetAwaiter().GetResult();
        }
        /// <summary>
        /// 修改个人资料多项
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public string UpdateUserDetail(UpdateUserDetailPackage.Data data)
        {
            var t = Task.Run(async () =>
            {
                UpdateUserDetailPackage package = new UpdateUserDetailPackage();
                package.ComposeHead(property.ServerJID, property.CurrentAccount.userID.ToString());
                package.data = data;

                if (!string.IsNullOrEmpty(data.photo))
                {
                    var result = await FindResource(data.photo);

                    package.data.photo = result.resourceId;
                    if (!result.existed)
                    {
                        UpLoadResource(data.photo, null, null);
                    }

                }

                return package.Send(ec).id;
            }).ConfigureAwait(false);
            return t.GetAwaiter().GetResult();
        }
        /// <summary>
        /// 设置好友备注
        /// </summary>
        /// <param name="option"></param>
        /// <param name="content"></param>
        /// <param name="friendId"></param>
        /// <returns></returns>
        public string UpdateFriendSet(UpdateFriendSetPackage.FriendSetOption option, string content, int friendId)
        {

            UpdateFriendSetPackage package = new UpdateFriendSetPackage();
            package.ComposeHead(property.ServerJID, property.CurrentAccount.userID.ToString());
            package.data = new UpdateFriendSetPackage.Data()
            {
                userId = property.CurrentAccount.userID,
                friendId = friendId,
                setType = (int)option,
                content = content
            };

            return package.Send(ec).id;
        }
        /// <summary>
        /// 修改用户可访号
        /// </summary>
        /// <param name="kfId"></param>
        /// <returns></returns>
        public string UpdateUserKfId(string kfId)
        {
            UpdateuserPackage package = new UpdateuserPackage();
            package.ComposeHead(property.ServerJID, property.CurrentAccount.userID.ToString());
            package.data = new UpdateuserPackage.Data
            {
                content = kfId,
                updateType = 28,
                userId = property.CurrentAccount.userID

            };
            package.Send(ec);
            return package.id;
        }


        public string GetContactsList()
        {
            var card = SDKClient.Instance.property.CurrentAccount.imei ?? System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(N => N.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback)?.GetPhysicalAddress().ToString();
            if (SDKClient.Instance.property.CurrentAccount.preimei == card)
            {
                var dbobj = Util.Helpers.Async.Run(async () => await DAL.DALContactListHelper.GetCurrentContactListPackage());
                if (dbobj != null && dbobj.getContactsListPackage != null)
                {
                    var localContactPackage = Util.Helpers.Json.ToObject<GetContactsListPackage>(dbobj.getContactsListPackage);
                    SDKClient.Instance.OnNewDataRecv(localContactPackage);
                }
            }
            GetContactsListPackage package = new GetContactsListPackage();
            package.ComposeHead(property.ServerJID, property.CurrentAccount.userID.ToString());
            package.data = new GetContactsListPackage.contacts();
            package.data.userId = property.CurrentAccount.userID;
            //var _loadHisTask = Task.Run(() =>
            //{
            var obj = IMRequest.GetContactsList(package);
            if (obj != null)
            {
                var contactsPackage = obj;
                if (contactsPackage != null && contactsPackage.code == 0)
                {
                    try
                    {
                        var cmd = CommmandSet.FirstOrDefault(c => c.Name == contactsPackage.api);
                        cmd?.ExecuteCommand(ec, contactsPackage);//日志及入库操作

                    }
                    catch (Exception ex)
                    {
                        logger.Error($"获取好友列表数据处理异常：error:{ex.Message},stack:{ex.StackTrace};\r\ncontent:{Util.Helpers.Json.ToJson(contactsPackage)}");

                        System.Threading.Interlocked.CompareExchange(ref SDKClient.Instance.property.CanHandleMsg, 2, 1);
                        logger.Info("CanHandleMsg 值修改为:2");
                    }
                }
            }
            else
            {

            }
            //}).ContinueWith((t)=>{
            //    return package.id;
            //});
            //package.Send(ec);

            return package.id;
        }
        public int GetFriendApplyStatus(int friendApplyId)
        {
            var obj = IMRequest.GetFriendApplyStatus(friendApplyId);
            return obj;
        }
        public string GetBlackList()
        {
            GetBlackListPackage package = new GetBlackListPackage();
            package.ComposeHead(null, property.CurrentAccount.userID.ToString());
            package.data = new GetBlackListPackage.Data()
            {
                min = 1,
                max = 100,
                userId = property.CurrentAccount.userID

            };
            var obj = IMRequest.GetBlackList(package);
            if (obj != null && obj.code == 0)
            {
                var blackPackage = obj;
                if (blackPackage != null)
                {
                    try
                    {
                        var cmd = CommmandSet.FirstOrDefault(c => c.Name == blackPackage.api);
                        cmd?.ExecuteCommand(ec, blackPackage);//日志及入库操作

                    }
                    catch (Exception ex)
                    {
                        logger.Error($"获取黑名单列表数据处理异常：error:{ex.Message},stack:{ex.StackTrace};\r\ncontent:{Util.Helpers.Json.ToJson(blackPackage)}");

                        System.Threading.Interlocked.CompareExchange(ref SDKClient.Instance.property.CanHandleMsg, 2, 1);
                        logger.Info("CanHandleMsg 值修改为:2");
                    }
                }
            }
            else
            {

            }
            //package.Send(ec).id
            return package.id;

        }
        /// <summary>
        /// 获取入群申请列表
        /// </summary>
        /// <param name="groupId"></param>
        public void GetJoinGroupList(int groupId)
        {
            var lst = Util.Helpers.Async.Run(async () => await DAL.DALJoinGroupHelper.GetJoinGroupList(groupId).ConfigureAwait(false));
            foreach (var item in lst)
            {
                var obj = Util.Helpers.Json.ToObject<JoinGroupPackage>(item.JoinGroupPackage);
                SDKClient.Instance.OnNewDataRecv(obj);
            }
        }
        public string GetGroupList()
        {
            var card = SDKClient.Instance.property.CurrentAccount.imei ?? System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(N => N.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback)?.GetPhysicalAddress().ToString();

            if (SDKClient.Instance.property.CurrentAccount.preimei == card)
            {
                var dbobj = Util.Helpers.Async.Run(async () => await DAL.DALGroupOptionHelper.GetGroupListPackage());
                if (dbobj != null && dbobj.getGroupListPackage != null)
                {
                    var groupListPackage = Util.Helpers.Json.ToObject<GetGroupListPackage>(dbobj.getGroupListPackage);
                    SDKClient.Instance.OnNewDataRecv(groupListPackage);
                }

            }
            GetGroupListPackage package = new GetGroupListPackage();
            package.ComposeHead(property.ServerJID, property.CurrentAccount.userID.ToString());
            package.data = new grouplist()
            {
                max = 100,
                min = 1,
                groupType = 0, //普通群
                userId = property.CurrentAccount.userID

            };
            var obj = IMRequest.GetGroupListPackage(package);
            if (obj != null && obj.code == 0)
            {
                var groupListPackage = obj;
                if (groupListPackage != null)
                {
                    try
                    {
                        var cmd = CommmandSet.FirstOrDefault(c => c.Name == groupListPackage.api);
                        cmd?.ExecuteCommand(ec, groupListPackage);//日志及入库操作

                    }
                    catch (Exception ex)
                    {
                        logger.Error($"获取群列表数据处理异常：error:{ex.Message},stack:{ex.StackTrace};\r\ncontent:{Util.Helpers.Json.ToJson(groupListPackage)}");

                        System.Threading.Interlocked.CompareExchange(ref SDKClient.Instance.property.CanHandleMsg, 2, 1);
                        logger.Info("CanHandleMsg 值修改为:2");
                    }
                }
            }
            else
            {

            }
            //package.Send(ec);
            return package.id;
        }
        /// <summary>
        /// 获取单个群成员信息
        /// </summary>
        /// <param name="userId">查看着ID</param>
        /// <param name="groupId">群ID</param>
        /// <param name="partnerId">被查看着ID</param>
        /// <returns></returns>
        public string GetGroupMember(int userId, int groupId, int partnerId)
        {

            GetGroupMemberPackage package = new GetGroupMemberPackage();
            package.ComposeHead(property.ServerJID, property.CurrentAccount.userID.ToString());

            package.data = new GetGroupMemberPackage.Data
            {
                groupId = groupId,
                userId = userId,
                partnerId = partnerId
            };
            package.Send(ec);
            return package.id;
        }
        public string GetGroupMemberList(int groupId, bool isLoaclData = false)
        {

            //var dbobj = Util.Helpers.Async.Run(async () => await DAL.DALGroupOptionHelper.GetGroupListPackage());

            var card = SDKClient.Instance.property.CurrentAccount.imei ?? System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(N => N.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback)?.GetPhysicalAddress().ToString();


            if (isLoaclData && SDKClient.Instance.property.CurrentAccount.preimei == card)
            {
                var dbobj = Util.Helpers.Async.Run(async () => await DAL.DALGroupOptionHelper.GetGroupMemberListPackage(groupId));
                if (dbobj != null && dbobj.getGroupMemberListPackage != null)
                {
                    var groupListPackage = Util.Helpers.Json.ToObject<GetGroupMemberListPackage>(dbobj.getGroupMemberListPackage);
                    SDKClient.Instance.OnNewDataRecv(groupListPackage);
                }
            }
            GetGroupMemberListPackage package = new GetGroupMemberListPackage();
            package.ComposeHead(property.ServerJID, property.CurrentAccount.userID.ToString());
            package.data = new groupmemberlist()
            {
                max = 100,
                min = 1,
                groupId = groupId //普通群
            };
            var obj = IMRequest.GetMemberList(package);
            if (obj != null)
            {
                var groupMemberListPackage = obj;
                if (groupMemberListPackage != null)
                {
                    try
                    {
                        var cmd = CommmandSet.FirstOrDefault(c => c.Name == groupMemberListPackage.api);
                        cmd?.ExecuteCommand(ec, groupMemberListPackage);//日志及入库操作

                    }
                    catch (Exception ex)
                    {
                        logger.Error($"获取群成员数据处理异常：error:{ex.Message},stack:{ex.StackTrace};\r\ncontent:{Util.Helpers.Json.ToJson(groupMemberListPackage)}");

                        System.Threading.Interlocked.CompareExchange(ref SDKClient.Instance.property.CanHandleMsg, 2, 1);
                        logger.Info("CanHandleMsg 值修改为:2");
                    }
                }
            }
            else
            {

            }
            //package.Send(ec).id
            return package.id;

        }
        public async void GetImDataListIncr()
        {
            var items = await IMRequest.GetImDataListIncr();
            if (items == null || items.data == null)
                return;
            
            if (items.data.contactsItem != null && items.data.contactsItem.Any())
            {
                GetContactsListPackage cp = new GetContactsListPackage();
                cp.data = new GetContactsListPackage.contacts();
                cp.data.items = items.data.contactsItem;
                cp.data.userId = items.data.userId;
                OnNewDataRecv(cp);
            }
            if (items.data.myBlackItem != null && items.data.myBlackItem.Any())
            {
                GetBlackListPackage bp = new GetBlackListPackage();
                bp.data = new GetBlackListPackage.Data();
                bp.data.items = items.data.myBlackItem;
                bp.data.userId = items.data.userId;
                OnNewDataRecv(bp);
            }
            
            if (items.data.strangersItem != null && items.data.strangersItem.Any())
            {
                GetAttentionListPackage ap = new GetAttentionListPackage();
                ap.data = new GetAttentionListPackage.Data();
                ap.data.items = items.data.strangersItem;
                ap.data.userId = items.data.userId;
                OnNewDataRecv(ap);
            }
          


        }

        /// <summary>
        /// 获取二维码图片
        /// </summary>
        /// <param name="Id">个人或者群组编号</param>
        /// <param name="userOrgroup">1:个人；2：群</param>
        /// <returns></returns>
        public string GetQrCodeImg(string Id, string userOrgroup)
        {
            if (string.IsNullOrEmpty(SDKClient.Instance.property.CurrentAccount.token))
            {
                var res = WebAPICallBack.Getfuck();
                SDKClient.Instance.property.CurrentAccount.token = res.token;

                logger.Error($"获取token：token:{res.token}");
            }
            //获取二维码
            var server = Util.Tools.QrCode.QrCodeFactory.Create(property.CurrentAccount.qrCodePath);
            var response = WebAPICallBack.GetQrCode(Id, userOrgroup);
            if (response.success)
            {
                return server.Size(Util.Tools.QrCode.QrSize.Middle).Save(response.qrCode);
            }
            else
            {
                logger.Error($"获取二维码错误：imei:{property.CurrentAccount.imei},token:{property.CurrentAccount.token},signature:{Util.Helpers.Encrypt.Md5By32(property.CurrentAccount.lastlastLoginTime.Value.Ticks + ProtocolBase.ImLinkSignUri)},timeStamp:{property.CurrentAccount.lastlastLoginTime.Value.Ticks}，code:{response.code}");
                logger.Error($"获取二维码错误：{response.error}，code:{response.code}");
                return null;
            }
        }
        public static string GetLoginQrCodeImg(string session)
        {

            //获取二维码
            var server = Util.Tools.QrCode.QrCodeFactory.Create(SDKProperty.QrCodePath);

            return server.Size(Util.Tools.QrCode.QrSize.Middle).Save(session);

        }
        public string GetLoginQRCode()
        {
            GetLoginQRCodePackage package = new GetLoginQRCodePackage();
            package.data = new GetLoginQRCodePackage.Data();

            package.id = SDKProperty.RNGId;
            package.Send(ec);
            return package.id;
        }
        public string QuickLogonMsg()
        {
            PCAutoLoginApplyPackage package = new PCAutoLoginApplyPackage();
            package.data = new PCAutoLoginApplyPackage.Data();
            package.data.token = _token;
            package.id = SDKProperty.RNGId;
            package.Send(ec);
            return package.id;
        }

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
        /// 更新消息为已读
        /// </summary>
        /// <param name="roomId">窗体ID</param>
        /// <param name="roomType">窗体类型，0：chat;1:groupchat</param>
        /// <param name="isRead">已读标志：true:已读；false:未读</param>
        public async void UpdateHistoryMsgIsReaded(int roomId, int roomType, bool isRead = true, int unReadCount = 0, bool sameToOther = true)
        {
            if (unReadCount > 0)
                await DAL.DALMessageHelper.UpdateMsgIsRead(roomId, roomType);

            await Instance.SendSyncMsgStatus(roomId, 2, string.Empty,
                    roomType == 1 ? SDKProperty.chatType.groupChat : SDKProperty.chatType.chat);

        }

        public void UpdateHistoryMsgIsReaded(string msgId)
        {
            Util.Helpers.Async.Run(async () => await DAL.DALMessageHelper.UpdateMsgIsRead(msgId));
        }
        public void UpdateHistoryMsgIsHidden(string msgId)
        {
            Util.Helpers.Async.Run(async () => await DAL.DALMessageHelper.UpdateMsgHidden(msgId));
        }
        public void UpdateHistoryMsgContent(string msgId, string content, SDKProperty.MessageType messageType = SDKProperty.MessageType.notification)
        {
            Util.Helpers.Async.Run(async () => await DAL.DALMessageHelper.UpdateMsgContent(msgId, content, messageType));
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
        public string CreateGroup(List<int> items, string photo)
        {
            var t = Task.Run(async () =>
            {
                var result = await FindResource(photo).ConfigureAwait(false);
                CreateGroupPackage package = new CreateGroupPackage();
                package.ComposeHead(property.ServerJID, property.CurrentAccount.userID.ToString());
                package.data = new CreateGroupPackage.Data()
                {
                    items = items,
                    groupPhoto = result.resourceId
                };
                try
                {
                    if (!result.existed)
                    {
                        UpLoadResource(photo, null, (b, s, e) =>
                        {
                            package.Send(ec);

                        });

                    }
                    else
                    {
                        package.Send(ec);

                    }
                }
                catch (Exception ex)
                {

                    logger.Error(ex.Message);
                }
                return package.id;

            }).ConfigureAwait(false);
            return t.GetAwaiter().GetResult();

        }

        public string UpdateGroup(int groupId, SetGroupOption option, string content)
        {
            var t = Task.Run(async () =>
            {
                UpdateGroupPackage package = new UpdateGroupPackage();
                package.ComposeHead(property.ServerJID, property.CurrentAccount.userID.ToString());
                package.data = new UpdateGroupPackage.Data()
                {
                    userId = property.CurrentAccount.userID,
                    groupId = groupId,
                    setType = (int)option,
                    content = content

                };
                if (option == SetGroupOption.修改群头像)
                {
                    var result = await FindResource(content);
                    if (!string.IsNullOrEmpty(content))
                    {
                        package.data.content = result.resourceId;
                        if (!result.existed)
                        {
                            UpLoadResource(content, null, null);
                        }

                    }
                }
                return package.Send(ec).id;
            }).ConfigureAwait(false);
            return t.GetAwaiter().GetResult();

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
        public async void RetrySendMessageByMsgId(string msgId)
        {
            var db = await DAL.DALMessageHelper.Get(msgId);
            if (db != null)
            {
                
                MessagePackage package = Util.Helpers.Json.ToObject<MessagePackage>(db.Source);
                package.Send(ec);
            }
        }

#endregion
#region 客服接口

        public async Task<string> SendCustiomServerMsg(string to, string sessionId, SDKProperty.customOption customOption = customOption.over)
        {
            if (string.IsNullOrEmpty(sessionId))
                return null;
            return await GetData<string>(() =>
            {
                switch (customOption)
                {
                    case customOption.conn:
                        return null;
                    case customOption.over:
                        CustomServicePackage customServicePackage = new CustomServicePackage();
                        customServicePackage.ComposeHead(to, property.CurrentAccount.userID.ToString());
                        customServicePackage.data = new CustomServicePackage.Data()
                        {
                            type = (int)customOption,
                            sessionId = sessionId
                        };
                        customServicePackage.Send(ec);

                        logger.Info($"会话结束\t：sessionId：{sessionId}");
                        WebAPICallBack.DiminishingSessionItem(sessionId);

                        return customServicePackage.id;
                    case customOption.requestappraisal:
                        MessagePackage messagePackage = new MessagePackage();
                        messagePackage.ComposeHead(to, property.CurrentAccount.userID.ToString());
                        messagePackage.data = new message()
                        {
                            body = new EvalBody()
                            {
                                id = sessionId
                            },
                            subType = nameof(SDKProperty.MessageType.eval),
                            senderInfo = new message.SenderInfo()
                            {
                                photo = property.CurrentAccount.photo,
                                userName = property.CurrentAccount.userName
                            },
                            type = nameof(SDKProperty.chatType.chat)


                        };
                        messagePackage.Send(ec);
                        return messagePackage.id;

                    case customOption.responseappraisal:
                        break;

                    default:
                        break;
                }
                return null;

            }).ConfigureAwait(false);
        }
        /// <summary>
        /// 客服转接
        /// </summary>
        /// <param name="to">用户ID</param>
        /// <param name="exchangeId">新客服服务ID</param>
        /// <returns></returns>
        public async Task<bool> SendCustiomExchangeMsg(string to, int exchangeId)
        {

            if (exchangeId == 0)
                exchangeId = SDKClient.Instance.property.CurrentAccount.CustomProperty.ServicerId ?? 0;

            if (exchangeId == 0 || exchangeId == SDKClient.Instance.property.CurrentAccount.CustomProperty.ServicerId)
            {
                exchangeId = SDKClient.Instance.property.CurrentAccount.CustomProperty.ServicerId ?? 0;
                var resut = await WebAPICallBack.CustomExchange(to.ToInt(), exchangeId).ConfigureAwait(false);
                if (resut.code == 1)
                {
                    CustomExchangePackage customExchangePackage = new CustomExchangePackage();
                    customExchangePackage.ComposeHead(to, property.CurrentAccount.userID.ToString());
                    customExchangePackage.data = new CustomExchangePackage.Data()
                    {
                        photo = null,
                        sessionId = resut.data.sessionid,
                        userId = resut.data.imopenid.ToInt()
                    };
                    customExchangePackage.Send(ec);


                    logger.Info($"申请会话\t：新的客服:{exchangeId},userId:{resut.data.imopenid},新的sessionId：{resut.data.sessionid}");
                    return true;
                }
                else
                {
                    logger.Info($"申请会话失败\t：to:{to},exchangeId:{exchangeId},code:{resut.code}");
                    return false;
                }
            }
            else
            {
                var resut = await WebAPICallBack.CustomExchange(to.ToInt(), exchangeId, 1).ConfigureAwait(false);
                if (resut.code == 1)
                {
                    CustomExchangePackage customExchangePackage = new CustomExchangePackage();
                    customExchangePackage.ComposeHead(to, property.CurrentAccount.userID.ToString());
                    customExchangePackage.data = new CustomExchangePackage.Data()
                    {
                        photo = null,
                        sessionId = resut.data.sessionid,
                        userId = resut.data.imopenid.ToInt()
                    };
                    customExchangePackage.Send(ec);

                    logger.Info($"会话转移\t：新的客服:{exchangeId},userId:{resut.data.imopenid},新的sessionId：{resut.data.sessionid}");
                    return true;
                }
                else
                {
                    logger.Info($"会话转移失败\t：to:{resut.data.imopenid},exchangeId:{exchangeId},code:{resut.code}");
                    return false;
                }
            }
        }
        public bool SetCustiomServerState(SDKProperty.customState customState)
        {

            logger.Info($"设置客服状态\t：状态为：{Util.Helpers.Enum.GetDescription<SDKProperty.customState>(customState)}");
            var response = WebAPICallBack.PostCustomServerState(customState);
            if (response.code == 1)
            {
                return true;
            }
            else
            {
                return false;
            }

        }
        public async Task<List<DTO.CSRoomListEntity.entity>> GetCSRoomlist()
        {

            var response = await WebAPICallBack.GetCSRoomlist().ConfigureAwait(false);
            SDKClient.logger.Info($"GetCSRoomlist: {Util.Helpers.Json.ToJson(response)}");
            if (response != null && response.data != null && response.data.Count > 0)
            {
                return response.data.Select(e => new DTO.CSRoomListEntity.entity()
                {
                    userId = e.imopenid,
                    photo = e.photo,
                    mobile = e.mobile,
                    shopBackUrl = e.shopBackUrl,
                    shopId = e.shopId,
                    shopName = e.shopName,
                    sessionType = e.sessionType,
                    sessionId = e.sesssionId
                }).ToList();
            }
            else
                return new List<DTO.CSRoomListEntity.entity>();

        }
        public async Task<List<DTO.CSTempCustomItem>> GetTempCSRoomlist()
        {

            var response = await IMRequest.GetTempCSRoomlist().ConfigureAwait(false);
            logger.Info(Util.Helpers.Json.ToJson(response));
            if (response != null && response.entryList != null && response.entryList.Count > 0)
            {
                return response.entryList.Select(e =>
                {
                    string msg = "";
                    if (!string.IsNullOrEmpty(e.lastMessage))
                    {
                        MessagePackage mp = Util.Helpers.Json.ToObject<MessagePackage>(e.lastMessage);
                        if (mp != null)
                        {
                            switch (mp.data.subType.ToLower())
                            {
                                case nameof(SDKProperty.MessageType.txt):
                                    string txt = mp.data.body.text;

                                    msg = txt;
                                    break;
                                case nameof(SDKProperty.MessageType.img):

                                    msg = "[图片]";
                                    break;
                                case nameof(SDKProperty.MessageType.retract):

                                    msg = "消息撤回";
                                    break;
                                case nameof(SDKProperty.MessageType.eval):

                                    msg = "[对方已评价]";

                                    break;
                                case nameof(SDKProperty.MessageType.goods):

                                    msg = "[商品链接]";

                                    break;
                                case nameof(SDKProperty.MessageType.order):

                                    msg = "[订单链接]";

                                    break;
                                case nameof(SDKProperty.MessageType.custom):

                                    msg = "[商品链接]";

                                    break;
                                default:
                                    msg = "";
                                    break;
                            }
                            return new DTO.CSTempCustomItem()
                            {
                                userId = e.userId,
                                photo = e.photo,
                                userName = e.userName,
                                message = msg,
                                UnreadCount = e.count,
                                msgTime = mp.time ?? DateTime.Now

                            };
                        }
                        else
                        {
                            return new DTO.CSTempCustomItem()
                            {
                                userId = e.userId,
                                photo = e.photo,
                                userName = e.userName,
                                message = msg,
                                UnreadCount = e.count,
                                msgTime = DateTime.Now

                            };
                        }

                    }
                    else
                    {
                        return new DTO.CSTempCustomItem()
                        {
                            userId = e.userId,
                            photo = e.photo,
                            userName = e.userName,
                            message = msg,
                            UnreadCount = e.count,
                            msgTime = DateTime.Now

                        };
                    }



                }).ToList();
            }
            else
                return new List<DTO.CSTempCustomItem>();

        }
        /// <summary>
        /// 获取快速回复类别集合
        /// </summary>
        /// <param name="cateType">类型类别 1 公共 2 个人</param>
        /// <returns></returns>
        public QuickReplycategory GetQuickReplycategory(int cateType)
        {
            return WebAPICallBack.GetQuickReplycategory(cateType);

        }
        /// <summary>
        /// 获取快速回复上下文
        /// </summary>
        /// <param name="cateType">快捷回复类型类别 1 公共快捷 2 个人快捷</param>
        /// <param name="actionType"></param>
        /// <returns></returns>
        public async Task<QuickReplycontent> GetQuickReplycontext(int cateType)
        {
            return await WebAPICallBack.GetQuickReplycontext(cateType);
        }
        /// <summary>
        /// 获取客服系统配置
        /// </summary>

        /// <returns></returns>
        public async Task<CSSysConfig> GetSysConfig()
        {
            return await WebAPICallBack.GetOnLineServicerSysConfig();
        }
        public async Task<OnlineStatusEntity> GetfreeServicers()
        {
            return await WebAPICallBack.GetfreeServicers().ConfigureAwait(false);
        }
        /// <summary>
        /// 根据日期 获取会话信息
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        public async Task<CSSysConfig> GetSessionByDate(int userId, DateTime date)
        {
            return await WebAPICallBack.GetSessionDate(date, userId);
        }
        /// <summary>
        /// 获取指定类型的记录项集合
        /// </summary>
        /// <param name="cateId">类型ID</param>
        /// <returns></returns>
        public QuickReplyCategorycontents GetQuickReplyCategorycontents(int cateId)
        {
            return WebAPICallBack.GetQuickReplyCategorycontents(cateId);
        }
        /// <summary>
        /// CURD类型信息
        /// </summary>
        /// <param name="editType">1新增，2修改，3删除</param>
        /// <param name="categoryId">类型ID</param>
        /// <param name="categoryName">类型名称</param>
        /// <param name="categoryType">1:公共，2:个人</param>
        /// <returns>success:是否成功，id:内容项ID</returns>
        public (bool success, int id) PostQuickReplyCategoryedit(int editType, int categoryId, string categoryName, int categoryType)
        {
            return WebAPICallBack.PostQuickReplyCategoryedit(editType, categoryId, categoryName, categoryType);
        }
        /// <summary>
        /// CURD具体记录项的信息
        /// </summary>
        /// <param name="editType">1新增，2修改，3删除</param>
        /// <param name="contentId">记录项ID</param>
        /// <param name="content">内容</param>
        /// <param name="categoryId">类型ID</param>
        /// <returns> success:是否成功，id:内容项ID</returns>
        public (bool success, int id) PostQuickReplyContentedit(int editType, int contentId, string content, int categoryId)
        {
            return WebAPICallBack.PostQuickReplyContentedit(editType, contentId, content, categoryId);
        }
        /// <summary>
        /// 通过手机号查找指定满金店用户
        /// </summary>
        /// <param name="mobile"></param>
        /// <returns></returns>
        public (bool success, CSRoomListEntity.entity entity) GetUserInfoByMobile(string mobile)
        {
            return WebAPICallBack.GetUserInfoByMobile(mobile);
        }
        /// <summary>
        /// 通过手机号查找指定满金店用户
        /// </summary>
        /// <param name="mobile"></param>
        /// <returns></returns>
        public (bool success, CSRoomListEntity.entity entity) QueryuserByMobile(string mobile)
        {
            return WebAPICallBack.QueryuserByMobile(mobile);
        }
        public static async Task<HistoryRecordListResp> Getuserhistorylist(int PageIndex = 0)
        {
            return await WebAPICallBack.Getuserhistorylist(PageIndex);
        }
        public async Task<string> GetAddressByIP(string ip)
        {
            var obj = await WebAPICallBack.GetAddressByIP(ip).ConfigureAwait(false);
            logger.Info($"根据IP获取地址信息\t：ip为：{ip},address:{obj.data}");
            if (string.IsNullOrEmpty(obj.data))
                return null;
            else
                return obj.data.TrimEnd(',');
        }
        /// <summary>
        /// 领取任务
        /// </summary>
        /// <param name="userId">用户的ID</param>
        /// <returns></returns>
        public string SendCSSyncMsgStatus(int userId, string photo, string nickName)
        {
            CSSyncMsgStatusPackage package = new CSSyncMsgStatusPackage();
            package.ComposeHead(null, property.CurrentAccount.userID.ToString());
            package.data = new CSSyncMsgStatusPackage.Data();
            package.data.userId = userId;
            package.data.photo = photo;
            package.data.userName = nickName;
            return package.Send(ec).id;
        }
#endregion

        public GetSensitiveWordsResponse GetBadWordUpdate(string lastTime)
        {
            return WebAPICallBack.GetBadWordUpdate(lastTime);
        }

    }
}
