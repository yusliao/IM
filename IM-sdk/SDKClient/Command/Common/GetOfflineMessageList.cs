using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SDKClient.Model;
using SuperSocket.ClientEngine;
using System.ComponentModel.Composition;
using SDKClient.Protocol;
using SDKClient.DTO;
using SDKClient.WebAPI;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using SDKClient.DB;

namespace SDKClient.Command
{
    [Export(typeof(CommandBase))]
    class GetOfflineMessageList_cmd : CommandBase
    {

        public override string Name => Protocol.ProtocolBase.GetOfflineMessageList;
        static OfflineMessageContext offlineMessage = new OfflineMessageContext();
        public override void ExecuteCommand(EasyClientBase session, PackageInfo packageInfo)
        {

            GetOfflineMessageListPackage package = packageInfo as GetOfflineMessageListPackage;
            List<DB.messageDB> lst = new List<DB.messageDB>();

            if (packageInfo.code == 0)
            {

                //离线消息入库
                if (package != null)
                {
                    package.RECVLog();
                    string log = Util.Helpers.CodeTimer.Time("ReceiveOffLineMessage", 1, () =>
                     {
                         lst = Util.Helpers.Async.Run(async () => await DAL.DALMessageHelper.ReceiveOffLineMessage(package));
                       
                         if (lst != null)
                         {
                             var noticemsgs_del = lst.Where(m => m.noticeId != 0 && m.msgType == nameof(SDKProperty.MessageType.deletegroupnotice));
                             var noticemsgs_add = lst.Where(m => m.noticeId != 0 && m.msgType == nameof(SDKProperty.MessageType.addgroupnotice));

                             foreach (var item in noticemsgs_add)
                             {
                                 if (noticemsgs_del.Any(m => m.noticeId == item.noticeId))
                                     item.optionRecord += (int)SDKProperty.MessageState.cancel;
                             }
                             foreach (var item in lst)
                             {
                                 var isgroup = item.roomType == 1 ? true : false;
                                 IList<MessageEntity> msglst = new List<MessageEntity>();
                                 //  if (offlineMessage.context.TryAdd((item.roomId, isgroup), msglst))
                                 if (offlineMessage.context.TryAdd(new Tuple<int, bool>(item.roomId,isgroup), msglst))
                                 {

                                     MessageEntity messageEntity = new MessageEntity() { db = item };
                                     msglst.Add(messageEntity);

                                 }
                                 else
                                 {
                                     // if (offlineMessage.context.TryGetValue((item.roomId, isgroup), out msglst))
                                     if (offlineMessage.context.TryGetValue(new Tuple<int, bool>(item.roomId, isgroup), out msglst))
                                     {
                                         MessageEntity messageEntity = new MessageEntity() { db = item };
                                         var target = msglst.FirstOrDefault(m => m.MsgId == item.msgId);
                                         if (target == null)
                                         {
                                             msglst.Add(messageEntity);
                                         }
                                         else
                                         {
                                             msglst.Remove(target);
                                             msglst.Add(messageEntity);
                                         }
                                     }
                                 }

                             }

                           //  SDKClient.Instance.OnOffLineMessageEventHandle(offlineMessage);
                         }
                     });
                    logger.Info(log);
                }

                if (package.data.count == 1000)//继续发送请求
                {
                    GetOfflineMessageListPackage p = new GetOfflineMessageListPackage();

                    p.data = new GetOfflineMessageListPackage.Data();

                    p.from = SDKClient.Instance.property.CurrentAccount.userID.ToString();
                    p.id = package.id;
                    p.data.count = package.data.count;
                    p.data.time = package.data.time;
                    ThreadPool.QueueUserWorkItem(m =>
                    {
                       // SDKClient.Instance.GetOfflineMessageList(p);
                    });
                    //p.Send(session);

                    PushOfflineMessage(package, lst);
                    //logger.Info("离线消息msg=>ui: " + package.id);
                }
                else
                {

                    Task.Run(() =>
                    {
                        try
                        {
                            PushOfflineMessage(package, lst);

                            
                            using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
                            {
                                BinaryFormatter binaryFormatter = new BinaryFormatter();
                                
                                binaryFormatter.Serialize(ms, offlineMessage);
                                ms.Position = 0;
                                object temp = binaryFormatter.Deserialize(ms);
                                SDKClient.Instance.OnOffLineMessageEventHandle((OfflineMessageContext)temp);
                            }
                            offlineMessage.context.Clear();
                            logger.Info("离线消息msg=>ui: " + package.id);

                        }
                        catch (Exception ex)
                        {
                            logger.Error($"PushOfflineMessage 处理出错 error:{ex.Message}; source : {package.ToString()}");
                        }
                        System.Threading.Interlocked.CompareExchange(ref SDKClient.Instance.property.CanHandleMsg, 2, 1);
                        logger.Info("CanHandleMsg 值修改为:2");
                        //foreach (var item in SDKClient.Instance.property.PackageCache)
                        //{
                        //    SDKClient.Instance.OnNewDataRecv(item);
                        //}
                        //发送离线消息已收确认请求
                        GetOfflineMessageListPackage p = new GetOfflineMessageListPackage();
                        p.data = new GetOfflineMessageListPackage.Data();
                        p.from = SDKClient.Instance.property.CurrentAccount.userID.ToString();
                        p.id = package.id;
                        p.data.count = 0;
                        p.data.time = package.data.time;
                        ThreadPool.QueueUserWorkItem(m =>
                        {
                           // SDKClient.Instance.GetOfflineMessageList(p,false);
                            logger.Info("OfflineMessageList 发送离线消息全部已收确认");
                        });

                    });
                }
            }
            base.ExecuteCommand(session, packageInfo);
        }
        public void PushOfflineMessage(GetOfflineMessageListPackage package, List<DB.messageDB> filters)
        {

            if (package == null || package.code != 0 || package.data == null || package.data.items == null)
            {
                return;
            }

            foreach (var item in package.data.items)
            {
                var obj = Util.Helpers.Json.ToObject<PackageInfo>(Util.Helpers.Json.ToJson(item));
                switch (obj.apiId)
                {
                    case ProtocolBase.AddFriendCode:
                        obj = Newtonsoft.Json.JsonConvert.DeserializeObject<AddFriendPackage>(Util.Helpers.Json.ToJson(item));
                        SDKClient.Instance.OnNewDataRecv(obj);
                        break;
                    case ProtocolBase.AddFriendAcceptedCode:
                        obj = Newtonsoft.Json.JsonConvert.DeserializeObject<AddFriendAcceptedPackage>(Util.Helpers.Json.ToJson(item));
                        SDKClient.Instance.OnNewDataRecv(obj);
                        break;
                    case ProtocolBase.DismissGroupCode:
                        if (filters.Any(m => m.msgId == obj.id))
                        {
                            obj = Newtonsoft.Json.JsonConvert.DeserializeObject<DismissGroupPackage>(Util.Helpers.Json.ToJson(item));

                            SDKClient.Instance.OnNewDataRecv(obj);
                        }
                        break;
                    case ProtocolBase.ExitGroupCode:
                        if (filters.Any(m => m.msgId == obj.id))
                        {
                            obj = Newtonsoft.Json.JsonConvert.DeserializeObject<ExitGroupPackage>(Util.Helpers.Json.ToJson(item));

                            SDKClient.Instance.OnNewDataRecv(obj);
                        }
                        break;
                    case ProtocolBase.InviteJoinGroupCode:
                        if (filters.Any(m => m.msgId == obj.id))
                        {
                            obj = Newtonsoft.Json.JsonConvert.DeserializeObject<InviteJoinGroupPackage>(Util.Helpers.Json.ToJson(item));

                            SDKClient.Instance.OnNewDataRecv(obj);
                        }
                        break;
                    case ProtocolBase.JoinGroupCode:
                        if (filters.Any(m => m.msgId == obj.id))
                        {
                            obj = Newtonsoft.Json.JsonConvert.DeserializeObject<JoinGroupPackage>(Util.Helpers.Json.ToJson(item));

                            SDKClient.Instance.OnNewDataRecv(obj);
                        }
                        break;
                    case ProtocolBase.UpdateGroupCode:
                        obj = Newtonsoft.Json.JsonConvert.DeserializeObject<UpdateGroupPackage>(Util.Helpers.Json.ToJson(item));
                        if (filters.Any(m => m.msgId == obj.id))
                            SDKClient.Instance.OnNewDataRecv(obj);
                        break;
                    case ProtocolBase.JoinGroupAcceptedCode:
                        if (filters.Any(m => m.msgId == obj.id))
                        {
                            obj = Newtonsoft.Json.JsonConvert.DeserializeObject<JoinGroupAcceptedPackage>(Util.Helpers.Json.ToJson(item));
                            SDKClient.Instance.OnNewDataRecv(obj);
                        }
                        break;
                    case ProtocolBase.DeleteFriendCode:
                        //if (filters.Any(m => m.msgId == obj.id))
                        //{
                        obj = Newtonsoft.Json.JsonConvert.DeserializeObject<DeleteFriendPackage>(Util.Helpers.Json.ToJson(item));
                        //SDKClient.Instance.OnNewDataRecv(obj);
                        //}
                        break;
                    case ProtocolBase.UpdateFriendRelationCode:
                        if (filters.Any(m => m.msgId == obj.id))
                        {
                            obj = Newtonsoft.Json.JsonConvert.DeserializeObject<UpdateFriendRelationPackage>(Util.Helpers.Json.ToJson(item));
                            SDKClient.Instance.OnNewDataRecv(obj);
                        }
                        break;
                    case ProtocolBase.SetMemberPowerCode:
                        if (filters.Any(m => m.msgId == obj.id))
                        {
                            obj = Newtonsoft.Json.JsonConvert.DeserializeObject<SetMemberPowerPackage>(Util.Helpers.Json.ToJson(item));
                            SDKClient.Instance.OnNewDataRecv(obj);
                        }
                        break;
                    case ProtocolBase.SyncMsgStatusCode:
                       
                        obj = Newtonsoft.Json.JsonConvert.DeserializeObject<SyncMsgStatusPackage>(Util.Helpers.Json.ToJson(item));
                        SDKClient.Instance.OnNewDataRecv(obj);
                        break;
                }

            }

        }
        
       
        
    }
}
