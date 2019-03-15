using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SDKClient.Model;
using SuperSocket.ClientEngine;
using SQLite;
using SDKClient.Protocol;
using System.IO;
using System.Collections.Concurrent;
using Util;
using Autofac;
using Util.Dependency;

namespace SDKClient.Command
{
    [Export(typeof(CommandBase))]
    class CSMessage_cmd:CommandBase
    {
        protected System.Collections.Concurrent.ConcurrentDictionary<string, (System.Threading.CancellationTokenSource, PackageInfo)> SendDic = new ConcurrentDictionary<string, (System.Threading.CancellationTokenSource, PackageInfo)>();

        public override string Name => Protocol.ProtocolBase.CSMessage;
       
        public override void ExecuteCommand(EasyClientBase session, PackageInfo packageInfo)
        {
            var package = packageInfo as CSMessagePackage;
            bool CanSendtoUI = false;
            //以下类型消息不处理。
            if (package.data != null &&
                ((package.data.type != nameof(SDKProperty.chatType.chat) && package.data.type != nameof(SDKProperty.chatType.groupChat))
                || package.data.subType == nameof(SDKProperty.MessageType.shareDyn)
                || package.data.subType == nameof(SDKProperty.MessageType.shareSpace)
                ||(package.data.type == nameof(SDKProperty.chatType.groupChat) && package.data.groupInfo.groupType== 1)
                ))
            {
                base.ExecuteCommand(session, packageInfo);
                return;
            }
            if (package.code == 0)
            {
                //我发送的消息
                if (packageInfo.from == SDKClient.Instance.property.CurrentAccount.userID.ToString())
                {
                    //非同步消息
                    if ((package.syncMsg == null || package.syncMsg == 0) && package.reply == 1)
                    {
                        (System.Threading.CancellationTokenSource, PackageInfo) source;
                        if (SendDic.TryRemove(packageInfo.id, out source))
                        {
                            source.Item1.Cancel();
                            var message = source.Item2 as MessagePackage;
                            if (message.data.type == nameof(SDKProperty.chatType.chat))
                                SDKClient.Instance.OnSendConfirm(message.to.ToInt(), false, package,package.time.Value);
                            else
                            {
                                SDKClient.Instance.OnSendConfirm(message.data.groupInfo.groupId, true, package,package.time.Value);
                            }
                        }
                        Util.Helpers.Async.Run(async () => await DAL.DALMessageHelper.UpdateSendMsgTime(package));
                        CanSendtoUI = false;
                    }
                    else//同步消息
                    {
                        Util.Helpers.Async.Run(async () => await DAL.DALMessageHelper.SendMsgtoDB(package));
                        CanSendtoUI = true;
                    }
                   

                }
                else
                {
                    var o = Util.Helpers.Async.Run(async () => await DAL.DALMessageHelper.ReceiveMsgtoDB(package));
                    if (o != null)
                        CanSendtoUI = true;
                }
            }
            else 
            {
                CanSendtoUI = true;
                switch (package.code)
                {
                    case (int)Protocol.StatusCode.StrangerMessageToMany:
                    case (int)Protocol.StatusCode.NotRecAnonymousMsg:
                    case (int)Protocol.StatusCode.BeDefriend:
                        
                        Util.Helpers.Async.Run(async () => await DAL.DALMessageHelper.DeleteHistoryMsg(package.id));
                        break;
                    default:
                        break;
                }
            }

            //不根据SDKClient.Instance.property.CanHandleMsg 来判断是否可显示消息。
            if (CanSendtoUI)
            {
                logger.Info($"msg=>ui: {package.id}");
                SDKClient.Instance.OnNewDataRecv(package);
            }

            //if (SDKClient.Instance.property.CanHandleMsg > 1&& CanSendtoUI&&SDKClient.Instance.property.CanSendMsg)
            //{
            //    SDKClient.Instance.OnNewDataRecv(package);
            //}
            //else
            //{
            //    if (CanSendtoUI)
            //    {
            //        logger.Info($"msg=>cache: {package.id}");
            //        SDKClient.Instance.property.PackageCache.Add(package);
            //    }
            //}

            base.ExecuteCommand(session, packageInfo);
            
            
                
        }
        public override void SendCommand(EasyClientBase session, PackageInfo packageinfo)
        {
            MessagePackage p = packageinfo as MessagePackage;
            //判定是否被禁言
            if(SDKClient.Instance.property.CurrentAccount.Isforbidden)
            {
                p.code = -1;
                
                long days = SDKClient.Instance.property.CurrentAccount.removeGagTime / (24 * 60 * 60);
                long hours = (SDKClient.Instance.property.CurrentAccount.removeGagTime % (24 * 60 * 60)) / (60 * 60);
                long minutes = (SDKClient.Instance.property.CurrentAccount.removeGagTime % (24 * 60 * 60)) / 60;
                //long hours = (SDKClient.Instance.property.CurrentAccount.removeGagTime % (24 * 60 * 60)) / (60 * 60);
                //long minutes = ((SDKClient.Instance.property.CurrentAccount.removeGagTime % (24 * 60 * 60)) % (60 * 60)) / 60;
                if (days > 0)
                    p.error = string.Format("发消息权限已被封停，剩余解封时间：{0}天{1}小时，若有疑问请联系客服：{2}", days, hours, GlobalConfig.CompanyPhone);
                else if (days == 0 && hours == 0 && minutes == 0)
                    SDKClient.Instance.property.CurrentAccount.Isforbidden = false;
                else
                    p.error = string.Format("发消息权限已被封停，剩余解封时间：{0}小时{1}分钟，若有疑问请联系客服：{2}", hours, minutes, GlobalConfig.CompanyPhone);




                //p.error = $"发消息权限已被封停，剩余解封时间：{days}天{hours}小时{minutes}分钟，若有疑问请联系客服：{GlobalConfig.CompanyPhone}";
                if (p.data.type == nameof(SDKProperty.chatType.chat))
                    SDKClient.Instance.OnSendConfirm(p.to.ToInt(), false, p,DateTime.Now);
                else
                {
                    SDKClient.Instance.OnSendConfirm(p.to.ToInt(), true, p, DateTime.Now);
                }
                return;
            }
            Util.Helpers.Async.Run(async () => await DAL.DALMessageHelper.SendMsgtoDB(p));
            switch (p.data.subType.ToLower())
            {
                case nameof(SDKProperty.MessageType.img):
                case nameof(SDKProperty.MessageType.file):
                case nameof(SDKProperty.MessageType.onlinefile):
                case nameof(SDKProperty.MessageType.smallvideo):
                    string filename = p.data.body.fileName;
                    p.data.body.fileName = Path.GetFileName(filename);
                    break;
                default:
                    break;
            }
            if (p.data.chatType == (int)SDKProperty.SessionType.SenderLeavingChat)
            {
                
                p.data.chatType = (int)SDKProperty.SessionType.temporaryChat;
            }
            base.SendCommand(session, p);
        }
        protected override void SendCompletedCallBack(bool isRanCompleted, PackageInfo package, EasyClientBase easyClientBase, int expires = 8)
        {
            if(isRanCompleted)
            {
                var source = new System.Threading.CancellationTokenSource();
                if (SendDic.TryAdd(package.id, (source,package)))
                {
                    Task.Delay(1 * expires * 1000,source.Token).ContinueWith(task =>
                    {
                        MessagePackage messagePackage = (MessagePackage)package;
                        if (messagePackage != null)
                        {
                            Task.Run(async () => await DAL.DALMessageHelper.UpdateMsgSendFailed(package.id));
                            if (messagePackage.data.type == nameof(SDKProperty.chatType.chat))
                                SDKClient.Instance.OnSendFaile(package.to.ToInt(),false, package.id);
                            else
                            {
                                SDKClient.Instance.OnSendFaile(messagePackage.data.groupInfo.groupId,true, package.id);
                            }
                        }
                    },TaskContinuationOptions.OnlyOnRanToCompletion);
                }
            }
            else
            {
                (System.Threading.CancellationTokenSource, PackageInfo) source;
                if(SendDic.TryRemove(package.id, out source))
                {
                    source.Item1.Cancel();
                }
                MessagePackage messagePackage = (MessagePackage)package;
                if (messagePackage != null)
                {
                  
                    Task.Run(async () => await DAL.DALMessageHelper.UpdateMsgSendFailed(package.id));
                    System.Threading.Thread.Sleep(1000);//竞态情况下，让Send先出栈
                    if (messagePackage.data.type == nameof(SDKProperty.chatType.chat))
                        SDKClient.Instance.OnSendFaile(package.to.ToInt(),false, package.id);
                    else
                    {
                        SDKClient.Instance.OnSendFaile(messagePackage.data.groupInfo.groupId,true, package.id);
                    }
                }
            }
            base.SendCompletedCallBack(isRanCompleted, package, easyClientBase, expires);
        }

    }
 
}
