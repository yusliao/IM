using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SDKClient.Model;
using Util;
using System.IO;
using SDKClient.P2P;
using static SDKClient.SDKProperty;
using System.Text.RegularExpressions;

namespace SDKClient.DAL
{
    class DALMessageHelper
    {

        static System.Collections.Concurrent.ConcurrentBag<string> fileNameRecord = new System.Collections.Concurrent.ConcurrentBag<string>();
        public static async Task<DB.messageDB> Get(string msgId)
        {
            return await SDKProperty.SQLiteReader.FindAsync<DB.messageDB>(m => m.msgId == msgId);
        }
        public static async Task SendMsgtoDB(MessagePackage package)
        {
            if (package.data.subType.ToLower() == nameof(SDKProperty.MessageType.redenvelopesreceive) || package.data.subType.ToLower() == nameof(SDKProperty.MessageType.redenvelopessendout))//不显示对方接收的红包和自己发送的红包消息
            {
                return;
            }
            if (package.data.subType.ToLower() == nameof(SDKProperty.MessageType.retract))
            {
                string msgId = package.data.body.retractId;

                // SDKProperty.RetractType retractType = (SDKProperty.RetractType)package.data.body.retarctType ?? SDKProperty.RetractType.Normal;
                var retractType = package.data.body.retractType ?? RetractType.Normal;
                var model = await SDKProperty.SQLiteReader.FindAsync<DB.messageDB>(m => m.msgId == msgId);
                if (model == null)
                    return;

                string content;
                if ((RetractType)retractType == RetractType.SourceEndOnlineRetract)
                    content = $"您取消了\"{Path.GetFileName(model.fileName)}\"({model.fileSize.GetFileSizeString()})的发送，文件传输失败。";
                else if ((RetractType)retractType == RetractType.OnlineToOffline)
                // content = $"您取消了\"{Path.GetFileName(model.fileName)}\"({model.fileSize.GetFileSizeString()})的{str}，已转为离线{str}。";
                {
                    await UpdateMsgHidden(msgId);
                    return;
                }
                else if ((RetractType)retractType == RetractType.OfflineToOnline)
                {
                    await UpdateMsgHidden(msgId);
                    return;
                }
                else if ((RetractType)retractType == RetractType.TargetEndOnlineRetract)
                {
                    content = $"您取消了\"{Path.GetFileName(model.fileName)}\"({model.fileSize.GetFileSizeString()})的接收，文件传输失败。";
                }
                else
                    content = "您撤回了一条消息";
                await DAL.DALMessageHelper.UpdateMsgContent(msgId, content, SDKProperty.MessageType.notification, SDKProperty.MessageState.cancel, package);
                return;
            }
            if (package.data.subType.ToLower() == nameof(SDKProperty.MessageType.bigtxt))
            {
                Util.Helpers.Async.Run(async () => await InsertBigTxtMsg(package));
                return;
            }
            DB.messageDB msg = new DB.messageDB()
            {
                from = package.from,
                to = package.to,
                msgTime = DateTime.Now,
                msgId = package.id,
                sessionType = package.data.chatType,
                optionRecord = package.syncMsg == 1 ? 1 : 16,
                roomId = package.to.ToInt(),
                body = Util.Helpers.Json.ToJson(package.data.body),
                SenderName = package.data.senderInfo.userName,
                SenderPhoto = package.data.senderInfo.photo,
                Source = Util.Helpers.Json.ToJson(package)
            };
            if (package.data.type == nameof(chatType.groupChat))
            {
                if (package.data.tokenIds != null && package.data.tokenIds.Any())
                    msg.TokenIds = string.Join(",", package.data.tokenIds);
                msg.roomId = package.data.groupInfo.groupId;
                msg.roomType = 1;
            }
            else
            {
                msg.roomType = 0;
            }

            switch (package.data.subType.ToLower())
            {
                case nameof(SDKProperty.MessageType.txt):
                    string txt = package.data.body.text;
                    //  var content = SDKProperty.stringSearchEx.Replace(txt);
                    msg.content = txt;
                    msg.msgType = nameof(SDKProperty.MessageType.txt);
                    break;
                case nameof(SDKProperty.MessageType.img):
                    msg.resourceId = package.data.body.id;
                    msg.resourcesmallId = package.data.body.smallId;
                    msg.fileName = package.data.body.fileName;
                    msg.msgType = nameof(SDKProperty.MessageType.img);
                    msg.content = "[图片]";
                    break;
                case nameof(SDKProperty.MessageType.onlinefile):
                    msg.msgType = nameof(SDKProperty.MessageType.onlinefile);
                    msg.fileName = package.data.body.fileName;
                    msg.resourceId = package.data.body.id;
                    msg.fileSize = package.data.body.fileSize;
                    msg.content = "[文件]";
                    break;
                case nameof(SDKProperty.MessageType.file):
                    msg.msgType = nameof(SDKProperty.MessageType.file);
                    msg.fileName = package.data.body.fileName;
                    msg.resourcesmallId = package.data.body.img;
                    msg.resourceId = package.data.body.id;
                    msg.fileSize = package.data.body.fileSize;
                    msg.content = "[文件]";
                    msg.fileState = (int)SDKProperty.ResourceState.IsCompleted;
                    break;
                case nameof(SDKProperty.MessageType.audio):
                    msg.msgType = nameof(SDKProperty.MessageType.audio);
                    msg.content = "对方发送语音消息，请在手机端查看";
                    break;
                case nameof(SDKProperty.MessageType.video):
                    msg.msgType = nameof(SDKProperty.MessageType.video);
                    msg.content = "对方发送视频消息，请在手机端查看";


                    msg.fileName = package.data.body.fileName;
                    msg.resourceId = package.data.body.id;
                    msg.fileSize = package.data.body.fileSize;
                    msg.content = "[视频]";
                    msg.fileState = (int)SDKProperty.ResourceState.IsCompleted;

                    break;
                case nameof(SDKProperty.MessageType.smallvideo):
                    msg.msgType = nameof(SDKProperty.MessageType.smallvideo);
                    msg.resourceId = package.data.body.id;
                    msg.content = "[小视频]";
                    msg.fileSize = package.data.body.fileSize;


                    msg.fileName = package.data.body.fileName ?? package.data.body.id;
                    msg.fileState = (int)SDKProperty.ResourceState.IsCompleted;
                    break;
                case nameof(SDKProperty.MessageType.redenvelopessendout):
                    msg.msgType = nameof(SDKProperty.MessageType.redenvelopessendout);
                    msg.content = "[您有新红包，请在手机上查看]";
                    break;
                case nameof(SDKProperty.MessageType.redenvelopesreceive):
                    msg.msgType = nameof(SDKProperty.MessageType.redenvelopesreceive);
                    msg.content = "[有人领取了你的红包，请在手机端查看]";
                    break;
                case nameof(SDKProperty.MessageType.eval):
                    msg.msgType = nameof(SDKProperty.MessageType.eval);
                    msg.content = "[发送评价]";

                    break;
                case nameof(SDKProperty.MessageType.goods):
                    msg.msgType = nameof(SDKProperty.MessageType.goods);
                    msg.content = "[商品链接]";

                    break;
                case nameof(SDKProperty.MessageType.order):
                    msg.msgType = nameof(SDKProperty.MessageType.order);
                    msg.content = "[商品链接]";

                    break;
                case nameof(SDKProperty.MessageType.custom):
                    msg.msgType = nameof(SDKProperty.MessageType.custom);
                    msg.content = "[商品链接]";

                    break;
                case nameof(SDKProperty.MessageType.addgroupnotice):
                    msg.msgType = nameof(SDKProperty.MessageType.addgroupnotice);
                    string content = package.data.body.content;
                    msg.content = content;
                    msg.noticeId = package.data.body.noticeId;

                    break;
                case nameof(SDKProperty.MessageType.deletegroupnotice):
                    msg.msgType = nameof(SDKProperty.MessageType.deletegroupnotice);
                    msg.content = "[群公告]";
                    int noticeId = package.data.body.noticeId;
                    await DAL.DALMessageHelper.UpdateNoticeMsgCancel(noticeId);

                    return;//删除公告，只更新新增公告消息，不新增一条消息
                case nameof(SDKProperty.MessageType.usercard):
                    msg.msgType = nameof(SDKProperty.MessageType.usercard);
                    msg.content = "[个人名片]";
                    break;
                case nameof(SDKProperty.MessageType.foreigndyn):
                    msg.msgType = nameof(SDKProperty.MessageType.foreigndyn);
                    string dynContent = package.data.body.text;
                    string dynImgUrl = package.data.body.img;
                    if (!string.IsNullOrEmpty(dynImgUrl) && IsUrlRegex(dynImgUrl))
                    {
                        msg.resourcesmallId = dynImgUrl;
                    }
                    msg.content = "[链接]" + dynContent;
                    break;
                default:
                    break;
            }
            try
            {
                await SDKProperty.SQLiteConn.InsertAsync(msg);
            }
            catch (Exception ex)
            {
                SDKClient.logger.Error($"消息处理异常：error:{ex.Message},stack:{ex.StackTrace};\r\n" + "消息内容：" + msg);
            }

        }


        public static async Task SendMsgtoDB(CustomServicePackage package)
        {
            if (package.data.type == 3)
                return;
            DB.messageDB msg = new DB.messageDB()
            {
                from = package.from,
                to = package.to,
                msgTime = DateTime.Now,
                msgId = package.id,
                msgType = nameof(SDKProperty.MessageType.notification),

                optionRecord = 1,
                roomId = package.to.ToInt(),
                roomType = 0,
                content = Util.Helpers.Enum.GetDescription<SDKProperty.customOption>(package.data.type),
                Source = Util.Helpers.Json.ToJson(package)
            };
            try
            {

                await SDKProperty.SQLiteConn.InsertAsync(msg);
            }
            catch (Exception ex)
            {
                SDKClient.logger.Error($"消息处理异常：error:{ex.Message},stack:{ex.StackTrace};\r\n");

            }


        }

        public static async Task<DB.messageDB> SendMsgtoDB(string msgId, string from, string to, string content, int roomId, int userId, SDKProperty.MessageType messageType, SDKProperty.MessageState messageState = MessageState.isRead, SDKProperty.chatType chatType = chatType.chat)
        {
            DB.messageDB msg = new DB.messageDB()
            {
                from = from,
                to = to,
                msgTime = DateTime.Now,
                msgId = msgId,
                msgType = messageType.ToString(),

                optionRecord = (int)messageState,
                roomId = roomId,
                roomType = (int)chatType,
                content = content,
                Source = userId != 0 ? userId.ToString() : string.Empty,

            };
            try
            {
                await SDKProperty.SQLiteConn.InsertAsync(msg);
                return msg;
            }
            catch (Exception ex)
            {
                SDKClient.logger.Error($"消息处理异常：error:{ex.Message},stack:{ex.StackTrace};\r\n");
                return null;
            }

        }
        public static async Task RecvMsgtoDB(CustomServicePackage package)
        {
            DB.messageDB msg = new DB.messageDB()
            {
                from = package.from.ToString(),
                to = package.to.ToString(),
                msgTime = package.time.Value,
                msgId = package.id,
                msgType = nameof(SDKProperty.MessageType.notification),
                SenderName = package.data.mobile,
                SenderPhoto = package.data.photo,

                optionRecord = (int)SDKProperty.MessageState.isRead,
                roomId = package.from.ToInt(),
                roomType = 0,
                content = Util.Helpers.Enum.GetDescription<SDKProperty.customOption>(package.data.type),
                Source = Util.Helpers.Json.ToJson(package)
            };
            try
            {
                await SDKProperty.SQLiteConn.InsertAsync(msg);
            }
            catch (Exception ex)
            {
                SDKClient.logger.Error($"消息处理异常：error:{ex.Message},stack:{ex.StackTrace};\r\n");

            }

        }
        public static async Task SendMsgtoDB(NotificatonPackage package)
        {
            DB.messageDB msg = new DB.messageDB()
            {
                from = package.From.ToString(),
                to = package.To.ToString(),
                msgTime = DateTime.Now,
                msgId = package.MsgId,
                msgType = nameof(SDKProperty.MessageType.notification),
                fileName = package.FileName,
                fileSize = package.FileSize,

                optionRecord = 1,
                roomId = package.To,
                roomType = 0,
                content = package.Content,
                Source = Util.Helpers.Json.ToJson(package)
            };
            try
            {
                await SDKProperty.SQLiteConn.InsertAsync(msg);
            }
            catch (Exception ex)
            {
                SDKClient.logger.Error($"消息处理异常：error:{ex.Message},stack:{ex.StackTrace};\r\n");

            }

        }
        public static async Task CancelOfflineFileRecv(string msgId)
        {
            var model = await SDKProperty.SQLiteReader.FindAsync<DB.messageDB>(m => m.msgId == msgId);
            if (model == null)
            {
                return;
            }
            string str = model.from == SDKClient.Instance.property.CurrentAccount.userID.ToString() ? "发送" : "接收";
            string content = $"您取消了\"{Path.GetFileName(model.fileName)}\"({model.fileSize.GetFileSizeString()})的{str}，文件传输失败。";
            try
            {
                await DAL.DALMessageHelper.UpdateMsgContent(msgId, content, SDKProperty.MessageType.notification, SDKProperty.MessageState.cancel);
                return;
            }
            catch (Exception ex)
            {
                SDKClient.logger.Error($"消息处理异常：error:{ex.Message},stack:{ex.StackTrace};\r\n");

            }


        }
        public static async Task UpdateSendMsgTime(PackageInfo package)
        {
            var item = await SDKProperty.SQLiteReader.FindAsync<DB.messageDB>(m => m.msgId == package.id);

            if (item != null)
            {
                item.msgTime = package.time.Value;
                item.optionRecord = 1;
                await SDKProperty.SQLiteConn.UpdateAsync(item);
            }
        }
        public static async Task UpdateNoticeMsgCancel(int noticeId)
        {
            var item = await SDKProperty.SQLiteReader.FindAsync<DB.messageDB>(m => m.noticeId == noticeId);

            if (item != null)
            {
                if ((item.optionRecord & (int)SDKProperty.MessageState.cancel) == (int)SDKProperty.MessageState.cancel)
                {
                    item.optionRecord += (int)SDKProperty.MessageState.cancel;
                    await SDKProperty.SQLiteConn.UpdateAsync(item);
                }
            }
        }
        public static async Task UpdateMsgHidden(string msgId)
        {
            await SDKProperty.SQLiteConn.ExecuteAsync($"delete from messageDB where msgId='{msgId}'");
            //if (string.IsNullOrEmpty(msgId))
            //    return;
            //var item = await SDKProperty.SQLiteConn.FindAsync<DB.messageDB>(m => m.msgId == msgId);
            //if (item != null&&item.Id!=0)
            //{

            //    if ((item.optionRecord & (int)SDKProperty.MessageState.deleted) == 0)
            //    {
            //        lock (item)
            //        {
            //            if ((item.optionRecord & (int)SDKProperty.MessageState.deleted) == 0)
            //            {
            //                item.optionRecord += (int)SDKProperty.MessageState.deleted;
            //            }
            //        }
            //        await SDKProperty.SQLiteConn.UpdateAsync(item);
            //    }
            //}
        }
        public static async Task<DB.messageDB> UpdateMsgContent(string msgId, string content, SDKProperty.MessageType messageType, SDKProperty.MessageState msgState = SDKProperty.MessageState.None, MessagePackage message = null)
        {
            if (string.IsNullOrEmpty(msgId))
                return null;
            var item = await SDKProperty.SQLiteReader.FindAsync<DB.messageDB>(m => m.msgId == msgId);
            if (item != null && item.Id != 0)
            {
                if (messageType == MessageType.retract)
                {
                    if (message.time != null)
                        item.msgTime = message.time.Value;
                    item.msgId = message.id;
                }

                item.content = content;
                item.msgType = messageType.ToString();
                if (message != null)
                    item.body = Util.Helpers.Json.ToJson(message.data.body);
                if (msgState != SDKProperty.MessageState.None)
                {
                    int i = item.optionRecord & (int)msgState;
                    if (i != (int)msgState)
                        item.optionRecord += (int)msgState;
                    else
                        return item;
                }
                try
                {
                    int w = await SDKProperty.SQLiteConn.UpdateAsync(item);
                }
                catch (Exception ex)
                {
                    SDKClient.logger.Error($"消息处理异常：error:{ex.Message},stack:{ex.StackTrace};\r\n" + "消息id：{ msgId}");
                }
              
            }
            return item;
        }
        public static async Task<bool> UpdateMsgSessionTypeToCommon(int roomId)
        {
            var i = await SDKProperty.SQLiteConn.ExecuteAsync($"update messageDB set sessionType={(int)SDKProperty.SessionType.CommonChat} where roomId={roomId} and roomType={(int)SDKProperty.chatType.chat} ");
            return i > 0 ? true : false;
        }
        public static async Task<DB.messageDB> ReceiveMsgtoDB(MessagePackage package)
        {
            if (package.data.subType.ToLower() == nameof(SDKProperty.MessageType.redenvelopesreceive))
            {
                return null;
            }
            if (package.data != null &&
                ((package.data.type != nameof(SDKProperty.chatType.chat) && package.data.type != nameof(SDKProperty.chatType.groupChat))
                || package.data.subType == nameof(SDKProperty.MessageType.shareDyn)
                || package.data.subType == nameof(SDKProperty.MessageType.shareSpace)))
                return null;
            if (package.data.subType.ToLower() == nameof(SDKProperty.MessageType.retract))
            {

                string msgId = package.data.body.retractId;
                if (package.syncMsg == 1)
                {
                    var foo = await UpdateMsgContent(msgId, "您撤回了一条消息", SDKProperty.MessageType.retract, SDKProperty.MessageState.cancel, package);
                    if (foo == null) return null;
                    foo.msgId = package.id;
                    return foo;
                }
                var retractType = package.data.body.retractType ?? SDKProperty.RetractType.Normal;

                if ((RetractType)retractType != RetractType.Normal)
                {
                    var model = await SDKProperty.SQLiteReader.FindAsync<DB.messageDB>(m => m.msgId == msgId);
                    string c = string.Empty;
                    if (model == null || model.Id == 0)
                    {
                        return null;
                    }
                    switch ((RetractType)retractType)
                    {
                        case RetractType.SourceEndOnlineRetract:
                            c = $"对方取消了\"{Path.GetFileName(model.fileName)}\"({model.fileSize.GetFileSizeString()})的发送，文件传输失败。";
                            break;
                        case RetractType.OnlineToOffline:
                            c = $"对方已取消在线文件\"{Path.GetFileName(model.fileName)}\"({model.fileSize.GetFileSizeString()})的发送，已转为离线发送。";
                            break;
                        case RetractType.TargetEndOnlineRetract:
                            c = $"对方取消了\"{Path.GetFileName(model.fileName)}\"({model.fileSize.GetFileSizeString()})的接收，文件传输失败。";
                            break;
                        default:
                            break;
                    }
                    var foo = await DAL.DALMessageHelper.UpdateMsgContent(msgId, c, SDKProperty.MessageType.retract, SDKProperty.MessageState.cancel, package);
                    if (foo == null) return null;
                    foo.msgId = package.id;
                    return foo;

                }
                if (package.data.type == nameof(chatType.groupChat))
                {

                    var obj = await DAL.DALUserInfoHelper.Get(package.from.ToInt());
                    var obj1 = await DAL.DALGroupOptionHelper.GetGroupMemberInfo(package.from.ToInt(), package.data.groupInfo.groupId);
                    if (obj == null || obj.UserId == 0 || obj.State == 0)//没有这个好友
                    {

                        if (obj1 == null || obj1.userId == 0)//群中也没找到成员信息
                        {
                            return await UpdateMsgContent(msgId, $"[{package.data.senderInfo.userName}] 撤回了一条消息", SDKProperty.MessageType.notification, SDKProperty.MessageState.cancel, package);
                        }
                        else
                        {
                            var name = string.IsNullOrEmpty(obj1.memoInGroup) == true ? obj1.userName : obj1.memoInGroup;
                            return await UpdateMsgContent(msgId, $"[{name}] 撤回了一条消息", SDKProperty.MessageType.retract, SDKProperty.MessageState.cancel, package);
                        }
                    }
                    else//是好友关系
                    {
                        if (obj1 == null || obj1.userId == 0)//没有找到群成员对象
                        {
                            var name = string.IsNullOrEmpty(obj.Remark) == true ? obj.NickName : obj.Remark;
                            return await UpdateMsgContent(msgId, $"[{name}] 撤回了一条消息", SDKProperty.MessageType.retract, SDKProperty.MessageState.cancel, package);
                        }
                        else
                        {
                            var name = string.IsNullOrEmpty(obj.Remark) == true ? (string.IsNullOrEmpty(obj1.memoInGroup) == true ? obj1.userName : obj1.memoInGroup) : obj.Remark;
                            return await UpdateMsgContent(msgId, $"[{name}] 撤回了一条消息", SDKProperty.MessageType.retract, SDKProperty.MessageState.cancel, package);
                        }

                    }

                }
                else
                {
                    var foo = await UpdateMsgContent(msgId, "对方撤回了一条消息", SDKProperty.MessageType.retract, SDKProperty.MessageState.cancel, package);
                    if (foo == null) return null;
                    foo.msgId = package.id;
                    return foo;
                }

            }
            if (package.data.subType == nameof(SDKProperty.MessageType.bigtxt))
            {
                return await InsertBigTxtMsg(package);

            }

            DB.messageDB msg = new DB.messageDB()
            {
                from = package.from,
                to = package.to,
                body = Util.Helpers.Json.ToJson(package.data.body),
                msgTime = package.time.Value,
                msgId = package.id,
                SenderPhoto = package.data.senderInfo.photo,
                SenderName = package.data.senderInfo.userName,
                sessionType = package.data.chatType,
                optionRecord = package.read == 0 ? 0 : 1,
                roomId = package.syncMsg == 1 ? package.to.ToInt() : package.from.ToInt(),
                Source = Util.Helpers.Json.ToJson(package)
            };
            if (package.data.type == nameof(chatType.groupChat))
            {
                if (package.data.tokenIds != null && package.data.tokenIds.Any())
                    msg.TokenIds = string.Join(",", package.data.tokenIds);
                msg.roomId = package.data.groupInfo.groupId;
                msg.roomType = 1;

            }
            else
            {
                msg.roomType = 0;
            }
            if (package != null)
            {
                switch (package.data.subType.ToLower())
                {
                    case nameof(SDKProperty.MessageType.txt):
                        // string cc = package.data.body.text;
                        // var content = SDKProperty.stringSearchEx.Replace(cc);
                        msg.content = package.data.body.text;
                        msg.msgType = nameof(SDKProperty.MessageType.txt);
                        break;
                    case nameof(SDKProperty.MessageType.img):
                        msg.resourceId = package.data.body.id;
                        msg.resourcesmallId = package.data.body.smallId;
                        msg.msgType = nameof(SDKProperty.MessageType.img);
                        msg.fileName = package.data.body.fileName;
                        msg.content = "[图片]";
                        break;
                    case nameof(SDKProperty.MessageType.onlinefile):
                        msg.msgType = nameof(SDKProperty.MessageType.onlinefile);
                        msg.resourceId = package.data.body.id;
                        msg.fileName = Path.GetFileName($"{package.data.body.fileName}");
                        string onlinePath = Path.Combine(SDKClient.Instance.property.CurrentAccount.filePath, msg.fileName);
                        msg.fileName = GetFileName(onlinePath, 1);
                        msg.fileSize = package.data.body.fileSize;
                        msg.content = "[文件]";
                        break;
                    case nameof(SDKProperty.MessageType.file):
                        msg.msgType = nameof(SDKProperty.MessageType.file);
                        msg.resourceId = package.data.body.id;
                        msg.fileName = Path.GetFileName($"{package.data.body.fileName}" ?? $"{package.data.body.id}");

                        string combinePath = Path.Combine(SDKClient.Instance.property.CurrentAccount.filePath, msg.fileName);
                        msg.fileName = GetFileName(combinePath, 1);
                        msg.fileSize = package.data.body.fileSize;
                        msg.content = "[文件]";
                        msg.fileState = (int)ResourceState.NoStart;
                        break;
                    case nameof(SDKProperty.MessageType.audio):
                        msg.msgType = nameof(SDKProperty.MessageType.audio);
                        msg.content = "对方发送语音消息，请在手机端查看";
                        break;
                    case nameof(SDKProperty.MessageType.video):
                        msg.msgType = nameof(SDKProperty.MessageType.video);
                        msg.resourceId = package.data.body.id;
                        msg.content = "[视频]";
                        msg.fileSize = package.data.body.fileSize;
                        msg.fileName = Path.GetFileName($"{package.data.body.fileName}" ?? $"{package.data.body.id}");

                        //   string videoPath = Path.Combine(SDKClient.Instance.property.CurrentAccount.filePath, msg.fileName);
                        //   msg.fileName = GetFileName(videoPath, 1);
                        break;
                    case nameof(SDKProperty.MessageType.smallvideo):
                        msg.msgType = nameof(SDKProperty.MessageType.smallvideo);
                        msg.resourceId = package.data.body.id;
                        msg.content = "[小视频]";
                        msg.fileSize = package.data.body.fileSize;

                        string fileName = package.data.body.fileName;
                        msg.fileName = Path.GetFileName(fileName ?? msg.resourceId);

                        //   string smallvideoPath = Path.Combine(SDKClient.Instance.property.CurrentAccount.filePath, msg.fileName);
                        //   msg.fileName = GetFileName(smallvideoPath, 1);
                        break;
                    case nameof(SDKProperty.MessageType.retract):
                        msg.msgType = nameof(SDKProperty.MessageType.retract);
                        msg.content = "[撤回消息]";
                        msg.content = package.data.body.retractId;
                        break;
                    //case "bigtxt":
                    //    msg.msgType = "bigtxt";
                    //    msg.content = package.data.body.partName;
                    //    Util.Helpers.Async.Run(async () => await InsertBigTxtMsg(package));
                    //    break;
                    case nameof(SDKProperty.MessageType.redenvelopessendout):
                        msg.msgType = nameof(SDKProperty.MessageType.redenvelopessendout);
                        msg.content = "[您有新红包，请在手机上查看]";
                        break;
                    case nameof(SDKProperty.MessageType.redenvelopesreceive):
                        msg.msgType = nameof(SDKProperty.MessageType.redenvelopesreceive);
                        msg.content = "[有人领取了你的红包，请在手机端查看]";
                        break;
                    case nameof(SDKProperty.MessageType.eval):
                        msg.msgType = nameof(SDKProperty.MessageType.eval);
                        msg.content = "[对方已评价]";
                        msg.body = Util.Helpers.Json.ToJson(package.data.body);
                        break;
                    case nameof(SDKProperty.MessageType.goods):
                        msg.msgType = nameof(SDKProperty.MessageType.goods);
                        msg.content = "[商品链接]";

                        break;
                    case nameof(SDKProperty.MessageType.order):
                        msg.msgType = nameof(SDKProperty.MessageType.order);
                        msg.content = "[商品链接]";

                        break;
                    case nameof(SDKProperty.MessageType.custom):
                        msg.msgType = nameof(SDKProperty.MessageType.custom);
                        msg.content = "[商品链接]";

                        break;
                    case nameof(SDKProperty.MessageType.addgroupnotice):
                        msg.msgType = nameof(SDKProperty.MessageType.addgroupnotice);
                        string content = package.data.body.content;
                        msg.content = content;
                        msg.noticeId = package.data.body.noticeId;

                        break;
                    case nameof(SDKProperty.MessageType.deletegroupnotice):
                        msg.msgType = nameof(SDKProperty.MessageType.deletegroupnotice);
                        msg.content = "[群公告]";
                        int noticeId = package.data.body.noticeId;
                        await DAL.DALMessageHelper.UpdateNoticeMsgCancel(noticeId);

                        return msg;
                    case nameof(SDKProperty.MessageType.usercard):
                        msg.msgType = nameof(SDKProperty.MessageType.usercard);
                        msg.content = "[个人名片]";
                        break;
                    case nameof(SDKProperty.MessageType.foreigndyn):
                        msg.msgType = nameof(SDKProperty.MessageType.foreigndyn);
                        string dynContent = package.data.body.text;
                        string dynImgUrl = package.data.body.img;
                        if (!string.IsNullOrEmpty(dynImgUrl) && IsUrlRegex(dynImgUrl))
                        {
                            msg.resourcesmallId = dynImgUrl;
                        }
                        msg.content = "[链接]" + dynContent;
                        //msg.content = "分享链接消息，请在手机端查看";
                        break;
                    default:
                        break;
                }
                if (string.IsNullOrEmpty(msg.msgType))
                    return null;
                try
                {
                    int i = await SDKProperty.SQLiteConn.InsertAsync(msg);
                    return msg;
                }
                catch (Exception ex)
                {
                    return null;
                }

            }
            return null;
        }

        public static async Task<DB.messageDB> GetMessageDB(MessagePackage package, List<DB.messageDB> lst)
        {
            if (package.data.subType.ToLower() == nameof(SDKProperty.MessageType.redenvelopesreceive))
            {
                return null;
            }
            if (package.data != null &&
                ((package.data.type != nameof(SDKProperty.chatType.chat) && package.data.type != nameof(SDKProperty.chatType.groupChat))
                || package.data.subType == nameof(SDKProperty.MessageType.shareDyn)
                || package.data.subType == nameof(SDKProperty.MessageType.shareSpace)))
                return null;
            if (package.data.subType.ToLower() == nameof(SDKProperty.MessageType.retract))
            {

                string msgId = package.data.body.retractId;
                if (package.syncMsg == 1)
                {
                    var foo = await UpdateMsgContent(msgId, "您撤回了一条消息", SDKProperty.MessageType.retract, SDKProperty.MessageState.cancel, package);
                    if (foo == null) return null;
                    foo.msgId = package.id;
                    return foo;
                }
                var retractType = package.data.body.retractType ?? SDKProperty.RetractType.Normal;

                if ((RetractType)retractType != RetractType.Normal)
                {
                    var model = await SDKProperty.SQLiteReader.FindAsync<DB.messageDB>(m => m.msgId == msgId);
                    string c = string.Empty;
                    if (model == null || model.Id == 0)
                    {
                        return null;
                    }
                    switch ((RetractType)retractType)
                    {
                        case RetractType.SourceEndOnlineRetract:
                            c = $"对方取消了\"{Path.GetFileName(model.fileName)}\"({model.fileSize.GetFileSizeString()})的发送，文件传输失败。";
                            break;
                        case RetractType.OnlineToOffline:
                            c = $"对方已取消在线文件\"{Path.GetFileName(model.fileName)}\"({model.fileSize.GetFileSizeString()})的发送，已转为离线发送。";
                            break;
                        case RetractType.TargetEndOnlineRetract:
                            c = $"对方取消了\"{Path.GetFileName(model.fileName)}\"({model.fileSize.GetFileSizeString()})的接收，文件传输失败。";
                            break;
                        default:
                            break;
                    }
                    var foo = await DAL.DALMessageHelper.UpdateMsgContent(msgId, c, SDKProperty.MessageType.retract, SDKProperty.MessageState.cancel, package);
                    if (foo == null) return null;
                    foo.msgId = package.id;
                    return foo;

                }
                if (package.data.type == nameof(chatType.groupChat))
                {

                    var obj = await DAL.DALUserInfoHelper.Get(package.from.ToInt());
                    var obj1 = await DAL.DALGroupOptionHelper.GetGroupMemberInfo(package.from.ToInt(), package.data.groupInfo.groupId);
                    if (obj == null || obj.UserId == 0 || obj.State == 0)//没有这个好友
                    {

                        if (obj1 == null || obj1.userId == 0)//群中也没找到成员信息
                        {
                            return await UpdateMsgContent(msgId, $"[{package.data.senderInfo.userName}] 撤回了一条消息", SDKProperty.MessageType.notification, SDKProperty.MessageState.cancel, package);
                        }
                        else
                        {
                            var name = string.IsNullOrEmpty(obj1.memoInGroup) == true ? obj1.userName : obj1.memoInGroup;
                            return await UpdateMsgContent(msgId, $"[{name}] 撤回了一条消息", SDKProperty.MessageType.retract, SDKProperty.MessageState.cancel, package);
                        }
                    }
                    else//是好友关系
                    {
                        if (obj1 == null || obj1.userId == 0)//没有找到群成员对象
                        {
                            var name = string.IsNullOrEmpty(obj.Remark) == true ? obj.NickName : obj.Remark;
                            return await UpdateMsgContent(msgId, $"[{name}] 撤回了一条消息", SDKProperty.MessageType.retract, SDKProperty.MessageState.cancel, package);
                        }
                        else
                        {
                            var name = string.IsNullOrEmpty(obj.Remark) == true ? (string.IsNullOrEmpty(obj1.memoInGroup) == true ? obj1.userName : obj1.memoInGroup) : obj.Remark;
                            return await UpdateMsgContent(msgId, $"[{name}] 撤回了一条消息", SDKProperty.MessageType.retract, SDKProperty.MessageState.cancel, package);
                        }

                    }

                }
                else
                {
                    var foo = await UpdateMsgContent(msgId, "对方撤回了一条消息", SDKProperty.MessageType.retract, SDKProperty.MessageState.cancel, package);
                    if (foo == null) return null;
                    foo.msgId = package.id;
                    return foo;
                }

            }
            if (package.data.subType == nameof(SDKProperty.MessageType.bigtxt))
            {
                return await InsertBigTxtMsg(package);

            }

            DB.messageDB msg = new DB.messageDB()
            {
                from = package.from,
                to = package.to,
                body = Util.Helpers.Json.ToJson(package.data.body),
                msgTime = package.time.Value,
                msgId = package.id,
                SenderPhoto = package.data.senderInfo.photo,
                SenderName = package.data.senderInfo.userName,
                sessionType = package.data.chatType,
                optionRecord = package.read == 0 ? 0 : 1,
                roomId = package.syncMsg == 1 ? package.to.ToInt() : package.from.ToInt(),
                Source = Util.Helpers.Json.ToJson(package)
            };
            if (package.data.type == nameof(chatType.groupChat))
            {
                if (package.data.tokenIds != null && package.data.tokenIds.Any())
                    msg.TokenIds = string.Join(",", package.data.tokenIds);
                msg.roomId = package.data.groupInfo.groupId;
                msg.roomType = 1;

            }
            else
            {
                msg.roomType = 0;
            }
            if (package != null)
            {
                switch (package.data.subType.ToLower())
                {
                    case nameof(SDKProperty.MessageType.txt):
                        // string cc = package.data.body.text;
                        // var content = SDKProperty.stringSearchEx.Replace(cc);
                        msg.content = package.data.body.text;
                        msg.msgType = nameof(SDKProperty.MessageType.txt);
                        break;
                    case nameof(SDKProperty.MessageType.img):
                        msg.resourceId = package.data.body.id;
                        msg.resourcesmallId = package.data.body.smallId;
                        msg.msgType = nameof(SDKProperty.MessageType.img);
                        msg.fileName = package.data.body.fileName;
                        msg.content = "[图片]";
                        break;
                    case nameof(SDKProperty.MessageType.onlinefile):
                        msg.msgType = nameof(SDKProperty.MessageType.onlinefile);
                        msg.resourceId = package.data.body.id;
                        msg.fileName = Path.GetFileName($"{package.data.body.fileName}");
                        string onlinePath = Path.Combine(SDKClient.Instance.property.CurrentAccount.filePath, msg.fileName);
                        msg.fileName = GetFileName(onlinePath, 1);
                        msg.fileSize = package.data.body.fileSize;
                        msg.content = "[文件]";
                        break;
                    case nameof(SDKProperty.MessageType.file):
                        msg.msgType = nameof(SDKProperty.MessageType.file);
                        msg.resourceId = package.data.body.id;
                        msg.fileName = Path.GetFileName($"{package.data.body.fileName}" ?? $"{package.data.body.id}");

                        string combinePath = Path.Combine(SDKClient.Instance.property.CurrentAccount.filePath, msg.fileName);
                        msg.fileName = GetFileName(combinePath, 1);
                        msg.fileSize = package.data.body.fileSize;
                        msg.content = "[文件]";
                        msg.fileState = (int)ResourceState.NoStart;
                        break;
                    case nameof(SDKProperty.MessageType.audio):
                        msg.msgType = nameof(SDKProperty.MessageType.audio);
                        msg.content = "对方发送语音消息，请在手机端查看";
                        break;
                    case nameof(SDKProperty.MessageType.video):
                        msg.msgType = nameof(SDKProperty.MessageType.video);
                        msg.resourceId = package.data.body.id;
                        msg.content = "[视频]";
                        msg.fileSize = package.data.body.fileSize;
                        msg.fileName = Path.GetFileName($"{package.data.body.fileName}" ?? $"{package.data.body.id}");

                        //   string videoPath = Path.Combine(SDKClient.Instance.property.CurrentAccount.filePath, msg.fileName);
                        //   msg.fileName = GetFileName(videoPath, 1);
                        break;
                    case nameof(SDKProperty.MessageType.smallvideo):
                        msg.msgType = nameof(SDKProperty.MessageType.smallvideo);
                        msg.resourceId = package.data.body.id;
                        msg.content = "[小视频]";
                        msg.fileSize = package.data.body.fileSize;

                        string fileName = package.data.body.fileName;
                        msg.fileName = Path.GetFileName(fileName ?? msg.resourceId);

                        //   string smallvideoPath = Path.Combine(SDKClient.Instance.property.CurrentAccount.filePath, msg.fileName);
                        //   msg.fileName = GetFileName(smallvideoPath, 1);
                        break;
                    case nameof(SDKProperty.MessageType.retract):
                        msg.msgType = nameof(SDKProperty.MessageType.retract);
                        msg.content = "[撤回消息]";
                        msg.content = package.data.body.retractId;
                        break;
                    //case "bigtxt":
                    //    msg.msgType = "bigtxt";
                    //    msg.content = package.data.body.partName;
                    //    Util.Helpers.Async.Run(async () => await InsertBigTxtMsg(package));
                    //    break;
                    case nameof(SDKProperty.MessageType.redenvelopessendout):
                        msg.msgType = nameof(SDKProperty.MessageType.redenvelopessendout);
                        msg.content = "[您有新红包，请在手机上查看]";
                        break;
                    case nameof(SDKProperty.MessageType.redenvelopesreceive):
                        msg.msgType = nameof(SDKProperty.MessageType.redenvelopesreceive);
                        msg.content = "[有人领取了你的红包，请在手机端查看]";
                        break;
                    case nameof(SDKProperty.MessageType.eval):
                        msg.msgType = nameof(SDKProperty.MessageType.eval);
                        msg.content = "[对方已评价]";
                        msg.body = Util.Helpers.Json.ToJson(package.data.body);
                        break;
                    case nameof(SDKProperty.MessageType.goods):
                        msg.msgType = nameof(SDKProperty.MessageType.goods);
                        msg.content = "[商品链接]";

                        break;
                    case nameof(SDKProperty.MessageType.order):
                        msg.msgType = nameof(SDKProperty.MessageType.order);
                        msg.content = "[商品链接]";

                        break;
                    case nameof(SDKProperty.MessageType.custom):
                        msg.msgType = nameof(SDKProperty.MessageType.custom);
                        msg.content = "[商品链接]";

                        break;
                    case nameof(SDKProperty.MessageType.addgroupnotice):
                        msg.msgType = nameof(SDKProperty.MessageType.addgroupnotice);
                        string content = package.data.body.content;
                        msg.content = content;
                        msg.noticeId = package.data.body.noticeId;

                        break;
                    case nameof(SDKProperty.MessageType.deletegroupnotice):
                        msg.msgType = nameof(SDKProperty.MessageType.deletegroupnotice);
                        msg.content = "[群公告]";
                        int noticeId = package.data.body.noticeId;
                        await DAL.DALMessageHelper.UpdateNoticeMsgCancel(noticeId);

                        return msg;
                    case nameof(SDKProperty.MessageType.usercard):
                        msg.msgType = nameof(SDKProperty.MessageType.usercard);
                        msg.content = "[个人名片]";
                        break;
                    case nameof(SDKProperty.MessageType.foreigndyn):
                        msg.msgType = nameof(SDKProperty.MessageType.foreigndyn);
                        string dynContent = package.data.body.text;
                        string dynImgUrl = package.data.body.img;
                        if (!string.IsNullOrEmpty(dynImgUrl) && IsUrlRegex(dynImgUrl))
                        {
                            msg.resourcesmallId = dynImgUrl;
                        }
                        msg.content = "[链接]" + dynContent;
                        //msg.content = "分享链接消息，请在手机端查看";
                        break;
                    default:
                        break;
                }
                if (string.IsNullOrEmpty(msg.msgType))
                    return null;


            }
            lst.Add(msg);
            return msg;
        }
        /// <summary>
        /// 验证地址是否是超链接
        /// </summary>
        /// <returns></returns>
        public static bool IsUrlRegex(string url)
        {
            string Pattern = @"^(http|https):\/\/[\w\-_]+(\.[\w\-_]+)+([\w\-\.,@?^=%&amp;:/~\+#]*[\w\-\@?^=%&amp;/~\+#])?$";
            Regex r = new Regex(Pattern);
            Match m = r.Match(url);
            return m.Success;
        }
        public static async Task<DB.messageDB> InsertBigTxtMsg(MessagePackage package)
        {
            string msgID = package.data.body != null ? package.data.body.partName : string.Empty;
            if (string.IsNullOrEmpty(msgID))
            {
                msgID = package.id;
            }
            //插入BigTxtPackageDB表
            DB.BigTxtPackageDB bigTxtPackageDB = new DB.BigTxtPackageDB()
            {
                msgId = package.id,
                partName = package.data.body.partName,
                partOrder = package.data.body.partOrder,
                partTotal = package.data.body.partTotal,
                Source = Util.Helpers.Json.ToJson(package),
                text = package.data.body.text,
                time = package.time
            };
            try
            {
                await SDKProperty.SQLiteConn.InsertAsync(bigTxtPackageDB);
            }
            catch (Exception ex)
            {
                SDKClient.logger.Error($"消息处理异常：error:{ex.Message},stack:{ex.StackTrace};\r\n" + "消息内容：" + bigTxtPackageDB);
            }

            //插入messageDB表
            DB.messageDB msg = null;
            msg = new DB.messageDB()
            {
                from = package.from,
                to = package.to,
                msgTime = package.time.Value,
                msgId = msgID,
                SenderPhoto = package.data.senderInfo.photo,
                SenderName = package.data.senderInfo.userName,
                optionRecord = package.syncMsg == 1 ? 1 : 0,
                roomId = package.syncMsg == 1 ? package.to.ToInt() : package.from.ToInt(),
                body = Util.Helpers.Json.ToJson(package.data.body),
                Source = Util.Helpers.Json.ToJson(package)
            };
            if (package.data.type == nameof(chatType.groupChat))
            {
                if (package.data.tokenIds != null && package.data.tokenIds.Any())
                    msg.TokenIds = string.Join(",", package.data.tokenIds);
                msg.roomId = package.data.groupInfo.groupId;
                msg.roomType = 1;

            }
            else
            {

                msg.roomType = 0;
            }
            if (bigTxtPackageDB.partOrder == 0)
            {
                msg.msgType = nameof(SDKProperty.MessageType.bigtxt);
                msg.content = package.data.body.text;
                try
                {
                    await SDKProperty.SQLiteConn.InsertAsync(msg);
                }
                catch (Exception ex)
                {
                    SDKClient.logger.Error($"消息处理异常：error:{ex.Message},stack:{ex.StackTrace};\r\n");

                }


            }


            //合并更新messageDB表
            BigBody body = new BigBody()
            {

                partName = package.data.body.partName,
                partOrder = package.data.body.partOrder,
                partTotal = package.data.body.partTotal,

                text = package.data.body.text,

            };
            int index = bigTxtPackageDB.partOrder;
            if (index == 0)
            {
                BigtxtHelper.AddBigtxtMsg(body, async s =>
                {
                    await UpdateBigTxtMsg(msgID, s);
                });
                return msg;
            }
            else
            {
                BigtxtHelper.AddBigtxtMsg(body, null);
                return null;
            }


        }
        public static async Task UpdateBigTxtMsg(string msgId, string content)
        {
            var item = await SDKProperty.SQLiteReader.FindAsync<DB.messageDB>(m => m.msgId == msgId);
            if (item == null)
                return;
            item.content = content;
            item.msgType = nameof(SDKProperty.MessageType.txt);
            await SDKProperty.SQLiteConn.UpdateAsync(item);
        }


        /// <summary>
        /// 获取条目上下文
        /// </summary>
        /// <returns></returns>
        //public async static Task<List<DTO.MessageContext>> GetRoomContext()
        //{
        //    List<DTO.MessageContext> result = new List<DTO.MessageContext>();
        //    var lst = await SDKProperty.SQLiteConn.Table<DB.messageDB>().ToListAsync();//.GroupBy(m => m.roomId);
        //    //根据条目类型分组
        //    var glst = lst.GroupBy(m => m.roomType);
        //    foreach (var c in glst)
        //    {
        //        //根据条目ID分组
        //        var rooms = c.GroupBy(m => m.roomId).ToList();

        //        foreach (var item in rooms)
        //        {
        //            //根据时间和流水ID倒叙排列
        //            var db = item.OrderByDescending(m => m.msgTime).ThenByDescending(m => m.Id).ToList();
        //            if (db != null && db.Count() > 0)
        //            {
        //                DTO.MessageContext messageContext = new DTO.MessageContext();
        //                //预先装载最新的1屏的消息
        //                var preloadmsgs = db.Take(6);
        //                if (db[0].roomType == (int)chatType.groupChat)
        //                {
        //                    messageContext.ChatType = SDKProperty.chatType.groupChat;
        //                    //填充未读消息集合
        //                    messageContext.UnReadCount = db.Where(m => m.optionRecord == (int)SDKProperty.MessageState.noRead).Count();
        //                    var messages = db.Where(m => m.optionRecord == (int)SDKProperty.MessageState.noRead).Take(100).ToList();

        //                    foreach (var m in messages)
        //                    {
        //                        DTO.MessageEntity entity = new DTO.MessageEntity() { db = m };
        //                        messageContext.UnReadList.Add(entity);
        //                        if (m.msgType != nameof(MessageType.retract) && !string.IsNullOrEmpty(m.TokenIds) && (m.TokenIds.IndexOf(SDKClient.Instance.property.CurrentAccount.userID.ToString()) != -1 || m.TokenIds == "-1"))
        //                        {
        //                            messageContext.IsCallMe = true;
        //                        }
        //                    }


        //                }
        //                else//单聊或者陌生人聊天
        //                {
        //                    //填充粉丝留言集合
        //                    if (db[0].sessionType == (int)SDKProperty.SessionType.ReceiverLeavingChat)
        //                    {
        //                        var strangemsgs = db.Where(m => m.sessionType == (int)SDKProperty.SessionType.ReceiverLeavingChat).ToList();
        //                        foreach (var m in strangemsgs)
        //                        {
        //                            DTO.MessageEntity entity = new DTO.MessageEntity() { db = m };
        //                            messageContext.StrangerMsgList.Add(entity);
        //                        }
        //                    }
        //                    else//填充未读消息集合
        //                    {

        //                        messageContext.UnReadCount = db.Where(m => m.optionRecord == (int)SDKProperty.MessageState.noRead).Count();
        //                        var messages = db.Where(m => m.optionRecord == (int)SDKProperty.MessageState.noRead).Take(100).ToList();
        //                        foreach (var m in messages)
        //                        {
        //                            DTO.MessageEntity entity = new DTO.MessageEntity() { db = m };
        //                            messageContext.UnReadList.Add(entity);
        //                            if (m.msgType != nameof(MessageType.retract) && !string.IsNullOrEmpty(m.TokenIds) && (m.TokenIds.IndexOf(SDKClient.Instance.property.CurrentAccount.userID.ToString()) != -1 || m.TokenIds == "-1"))
        //                            {
        //                                messageContext.IsCallMe = true;
        //                            }
        //                        }
        //                    }

        //                    messageContext.ChatType = SDKProperty.chatType.chat;
        //                }
        //                foreach (var p in preloadmsgs)
        //                {
        //                    DTO.MessageEntity entity = new DTO.MessageEntity() { db = p };
        //                    messageContext.PreloadLists.Add(entity);
        //                    if (p.msgType != nameof(MessageType.retract) && !string.IsNullOrEmpty(p.TokenIds) && (p.TokenIds.IndexOf(SDKClient.Instance.property.CurrentAccount.userID.ToString()) != -1 || p.TokenIds == "-1"))
        //                    {
        //                        messageContext.IsCallMe = true;
        //                    }
        //                }
        //                messageContext.RoomId = item.Key;

        //                messageContext.LastMessage = new DTO.MessageEntity() { db = db[0] };

        //                result.Add(messageContext);
        //            }
        //        }

        //    }

        //    return result;
        //}

        public async static Task<List<DTO.MessageContext>> GetRoomContext()
        {
            List<DTO.MessageContext> result = new List<DTO.MessageContext>();
            var lst = await SDKProperty.SQLiteConn.Table<DB.messageDB>().ToListAsync();//.GroupBy(m => m.roomId);
            //根据条目类型分组
            var glst = lst.GroupBy(m => m.roomType);
            foreach (var c in glst)
            {
                //根据条目ID分组
                var rooms = c.GroupBy(m => m.roomId).ToList();

                foreach (var item in rooms)
                {
                    //根据时间和流水ID倒叙排列
                    var db = item.OrderByDescending(m => m.msgTime).ThenByDescending(m => m.Id).ToList();
                    if (db != null && db.Count() > 0)
                    {
                        DTO.MessageContext messageContext = new DTO.MessageContext();
                        //预先装载最新的1屏的消息
                        var preloadmsgs = db.Take(6);
                        if (db[0].roomType == (int)chatType.groupChat)
                        {
                            messageContext.ChatType = SDKProperty.chatType.groupChat;
                            //填充未读消息集合
                            messageContext.UnReadCount = db.Where(m => m.optionRecord == (int)SDKProperty.MessageState.noRead).Count();
                            var messages = db.Where(m => m.optionRecord == (int)SDKProperty.MessageState.noRead).Take(100).ToList();

                            foreach (var m in messages)
                            {
                                DTO.MessageEntity entity = new DTO.MessageEntity() { db = m };
                                messageContext.UnReadList.Add(entity);
                                if (m.msgType != nameof(MessageType.retract) && !string.IsNullOrEmpty(m.TokenIds) && (m.TokenIds.IndexOf(SDKClient.Instance.property.CurrentAccount.userID.ToString()) != -1 || m.TokenIds == "-1"))
                                {
                                    messageContext.IsCallMe = true;
                                }
                            }


                        }
                        else//单聊或者陌生人聊天
                        {
                            //填充粉丝留言集合
                            if (db[0].sessionType == (int)SDKProperty.SessionType.ReceiverLeavingChat)
                            {
                                var strangemsgs = db.Where(m => m.sessionType == (int)SDKProperty.SessionType.ReceiverLeavingChat).ToList();
                                foreach (var m in strangemsgs)
                                {
                                    DTO.MessageEntity entity = new DTO.MessageEntity() { db = m };
                                    messageContext.StrangerMsgList.Add(entity);
                                }
                            }
                            else//填充未读消息集合
                            {

                                messageContext.UnReadCount = db.Where(m => m.optionRecord == (int)SDKProperty.MessageState.noRead).Count();
                                var messages = db.Where(m => m.optionRecord == (int)SDKProperty.MessageState.noRead).Take(100).ToList();
                                foreach (var m in messages)
                                {
                                    DTO.MessageEntity entity = new DTO.MessageEntity() { db = m };
                                    messageContext.UnReadList.Add(entity);
                                    if (m.msgType != nameof(MessageType.retract) && !string.IsNullOrEmpty(m.TokenIds) && (m.TokenIds.IndexOf(SDKClient.Instance.property.CurrentAccount.userID.ToString()) != -1 || m.TokenIds == "-1"))
                                    {
                                        messageContext.IsCallMe = true;
                                    }
                                }
                            }

                            messageContext.ChatType = SDKProperty.chatType.chat;
                        }
                        foreach (var p in preloadmsgs)
                        {
                            DTO.MessageEntity entity = new DTO.MessageEntity() { db = p };
                            messageContext.PreloadLists.Add(entity);
                            if (p.msgType != nameof(MessageType.retract) && !string.IsNullOrEmpty(p.TokenIds) && (p.TokenIds.IndexOf(SDKClient.Instance.property.CurrentAccount.userID.ToString()) != -1 || p.TokenIds == "-1"))
                            {
                                messageContext.IsCallMe = true;
                            }
                        }
                        messageContext.RoomId = item.Key;

                        messageContext.LastMessage = new DTO.MessageEntity() { db = db[0] };

                        result.Add(messageContext);
                    }
                }

            }

            return result;
        }
        /// <summary>
        /// 清空历史聊天消息
        /// </summary>
        /// <returns></returns>
        public static async Task DeleteHistoryMsg()
        {
            await SDKProperty.SQLiteConn.DropTableAsync<DB.messageDB>();
            await SDKProperty.SQLiteConn.CreateTableAsync<DB.messageDB>();
        }
        public static async Task DeleteHistoryMsg(int roomId, SDKProperty.chatType chatType = chatType.chat)
        {
            int i = await SDKProperty.SQLiteConn.ExecuteAsync($"DELETE FROM  messageDB  WHERE roomId ={roomId} and roomType={(int)chatType} ");


            //var lst = await SDKProperty.SQLiteConn.Table<DB.messageDB>().Where(m => m.roomId == roomId).ToListAsync();
            //foreach (var item in lst)
            //{
            //    await SDKProperty.SQLiteConn.DeleteAsync(item);
            //}
        }
        public static async Task DeleteHistoryMsg(string msgId)
        {
            var item = await SDKProperty.SQLiteConn.Table<DB.messageDB>().Where(m => m.msgId == msgId).FirstOrDefaultAsync();
            if (item != null)
                await SDKProperty.SQLiteConn.DeleteAsync(item);
        }
        public static async Task UpdateMsgSendFailed(string msgId)
        {
            var item = await SDKProperty.SQLiteReader.FindAsync<DB.messageDB>(m => m.msgId == msgId);
            if (item != null)
            {

                if ((item.optionRecord & (int)SDKProperty.MessageState.sendfaile) == 0)
                    item.optionRecord += (int)SDKProperty.MessageState.sendfaile;
                await SDKProperty.SQLiteConn.ExecuteAsync($"update messageDB set optionRecord={item.optionRecord} where msgId='{msgId}' ");
            }
        }
        /// <summary>
        /// 获取聊天窗消息
        /// </summary>
        /// <param name="roomId">窗体ID</param>
        /// <param name="loadCount">加载条数</param>
        ///  <param name="datetime">查询时间</param>
        ///   <param name="msgtype">查询类型：img,file,all</param>
        /// <returns></returns>
        public static async Task<List<DB.messageDB>> GetLatestMsgs(int roomId, int loadCount = 6, DateTime? datetime = null, string msgtype = null, chatType chatType = chatType.chat)
        {
            //首次进入
            if (datetime == null || datetime == DateTime.MinValue)
            {
                if (string.IsNullOrEmpty(msgtype) || msgtype.Equals(nameof(SDKProperty.MessageType.all)))
                {


                    System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
                    stopwatch.Start();
                    //var lst = await SDKProperty.SQLiteConn.Table<DB.messageDB>()
                    //    .Where(s => s.roomId == roomId 
                    //    && s.roomType == (int)chatType)
                    //    .OrderByDescending(m => m.msgTime)
                    //    .ThenByDescending(m => m.Id)
                    //    .Take(loadCount).ToListAsync();
                    var lst = await SDKProperty.SQLiteReader.QueryAsync<DB.messageDB>($"select * from messageDB indexed by msgtime where roomId={roomId} and roomType={(int)chatType}  order by msgTime desc,Id desc limit {loadCount} ");
                    stopwatch.Stop();
                    System.Diagnostics.Debug.WriteLine($"查询条目最新消息耗时：{stopwatch.ElapsedMilliseconds}毫秒");

                    lst.Reverse();
                    return lst;

                }
                else
                {
                    if (msgtype == nameof(SDKProperty.MessageType.file) || msgtype == nameof(SDKProperty.MessageType.onlinefile))
                    {

                        // var lst = await SDKProperty.SQLiteConn.Table<DB.messageDB>().Where(s => s.roomId == roomId && s.roomType == (int)chatType && (s.msgType.ToLower() == nameof(SDKProperty.MessageType.file) || s.msgType.ToLower() == nameof(SDKProperty.MessageType.onlinefile))).OrderByDescending(m => m.msgTime).ThenByDescending(m => m.Id).Take(loadCount).ToListAsync();
                        var lst = await SDKProperty.SQLiteReader.QueryAsync<DB.messageDB>($"select * from messageDB indexed by msgtime where roomId={roomId} and roomType={(int)chatType} and (msgType ='{nameof(SDKProperty.MessageType.file)}' or msgType='{nameof(SDKProperty.MessageType.onlinefile)}') order by msgTime desc,Id desc limit {loadCount} ");
                        lst.Reverse();
                        return lst;

                    }
                    else
                    {

                        // var lst = await SDKProperty.SQLiteConn.Table<DB.messageDB>().Where(s => s.roomId == roomId && s.roomType == (int)chatType && s.msgType.ToLower() == msgtype.ToLower()).OrderByDescending(m => m.msgTime).ThenByDescending(m => m.Id).Take(loadCount).ToListAsync();
                        List<DB.messageDB> lst = new List<DB.messageDB>();
                        if (msgtype.ToLower() == nameof(SDKProperty.MessageType.imgandvideo).ToLower())
                            lst = await SDKProperty.SQLiteReader.QueryAsync<DB.messageDB>($"select * from messageDB indexed by msgtime where roomId={roomId} and roomType={(int)chatType} and (msgType ='img' or msgType='smallvideo')order by msgTime desc,Id desc limit {loadCount} ");
                        else
                            lst = await SDKProperty.SQLiteReader.QueryAsync<DB.messageDB>($"select * from messageDB indexed by msgtime where roomId={roomId} and roomType={(int)chatType} and msgType ='{msgtype.ToLower()}' order by msgTime desc,Id desc limit {loadCount} ");
                        lst.Reverse();
                        return lst;

                    }

                }

            }
            else//按时间查询
            {
                if (string.IsNullOrEmpty(msgtype) || msgtype.Equals(nameof(SDKProperty.MessageType.all)))
                {
                    var addDate = datetime.Value.AddDays(1);
                    var lst = await SDKProperty.SQLiteReader.Table<DB.messageDB>().Where(s =>
                    s.roomId == roomId
                    && s.roomType == (int)chatType
                    && (s.msgTime < addDate && s.msgTime >= datetime.Value))
                    .OrderByDescending(m => m.msgTime)
                    .ThenByDescending(m => m.Id)
                    .Take(loadCount).ToListAsync();
                    lst.Reverse();
                    return lst;

                }
                else
                {
                    if (msgtype == nameof(SDKProperty.MessageType.file) || msgtype == nameof(SDKProperty.MessageType.onlinefile))
                    {
                        var addDate = datetime.Value.AddDays(1);
                        var lst = await SDKProperty.SQLiteReader.Table<DB.messageDB>()
                            .Where(s => s.roomId == roomId
                            && s.roomType == (int)chatType
                            && (s.msgTime < addDate && s.msgTime >= datetime.Value)
                            && (s.msgType.ToLower() == nameof(SDKProperty.MessageType.onlinefile) || s.msgType.ToLower() == nameof(SDKProperty.MessageType.file)))
                            .OrderByDescending(m => m.msgTime)
                            .ThenByDescending(m => m.Id)
                            .Take(loadCount)
                            .ToListAsync();
                        lst.Reverse();
                        return lst;

                    }
                    else
                    {
                        var addDate = datetime.Value.AddDays(1);
                        List<DB.messageDB> lst = new List<DB.messageDB>();
                        if (msgtype.ToLower() == nameof(SDKProperty.MessageType.imgandvideo).ToLower())
                            lst = await SDKProperty.SQLiteReader.Table<DB.messageDB>()
                            .Where(s => s.roomId == roomId
                            && s.roomType == (int)chatType
                            && (s.msgTime < addDate && s.msgTime >= datetime.Value)
                            && (s.msgType.ToLower().Equals("img") || s.msgType.ToLower().Equals("smallvideo")))
                            .OrderByDescending(m => m.msgTime)
                            .ThenByDescending(m => m.Id)
                            .Take(loadCount)
                            .ToListAsync();
                        else
                            lst = await SDKProperty.SQLiteReader.Table<DB.messageDB>()
                            .Where(s => s.roomId == roomId
                            && s.roomType == (int)chatType
                              && (s.msgTime < addDate && s.msgTime >= datetime.Value)
                            && s.msgType.ToLower() == msgtype.ToLower())
                            .OrderByDescending(m => m.msgTime)
                            .ThenByDescending(m => m.Id)
                            .Take(loadCount)
                            .ToListAsync();
                        lst.Reverse();
                        return lst;

                    }
                }

            }

        }
        /// <summary>
        /// 获取聊天窗消息
        /// </summary>
        /// <param name="roomId">窗体ID</param>
        /// <param name="loadCount">加载条数</param>
        ///  <param name="datetime">查询时间</param>
        ///   <param name="msgtype">查询类型：img,file,all</param>
        /// <returns></returns>
        public static async Task<List<DTO.MessageEntity>> GetMsgEntity(int roomId, int loadCount = 6, DateTime? datetime = null, string msgtype = null, bool showDelMsg = false)
        {
            if (datetime == null || datetime == DateTime.MinValue)
            {
                if (string.IsNullOrEmpty(msgtype) || msgtype.Equals(nameof(SDKProperty.MessageType.all)))
                {
                    if (showDelMsg)
                    {
                        var lst = await SDKProperty.SQLiteReader.Table<DB.messageDB>().Where(s => s.roomId == roomId).OrderByDescending(m => m.msgTime).ThenByDescending(m => m.Id).Take(loadCount).ToListAsync();
                        lst.Reverse();

                        return lst.Select(m => new DTO.MessageEntity() { db = m }).ToList();
                    }
                    else
                    {
                        var lst = await SDKProperty.SQLiteReader.Table<DB.messageDB>().Where(s => s.roomId == roomId && (s.optionRecord & 2) == 0).OrderByDescending(m => m.msgTime).ThenByDescending(m => m.Id).Take(loadCount).ToListAsync();
                        lst.Reverse();
                        return lst.Select(m => new DTO.MessageEntity() { db = m }).ToList();
                    }
                }
                else
                {
                    if (msgtype == nameof(SDKProperty.MessageType.file) || msgtype == nameof(SDKProperty.MessageType.onlinefile))
                    {
                        if (showDelMsg)
                        {

                            var lst = await SDKProperty.SQLiteReader.Table<DB.messageDB>().Where(s => s.roomId == roomId && (s.msgType.ToLower() == nameof(SDKProperty.MessageType.file) || s.msgType.ToLower() == nameof(SDKProperty.MessageType.onlinefile))).OrderByDescending(m => m.msgTime).ThenByDescending(m => m.Id).Take(loadCount).ToListAsync();
                            lst.Reverse();
                            return lst.Select(m => new DTO.MessageEntity() { db = m }).ToList();

                        }
                        else
                        {
                            var lst = await SDKProperty.SQLiteReader.Table<DB.messageDB>().Where(s => s.roomId == roomId && (s.optionRecord & 2) == 0 && (s.msgType.ToLower() == nameof(SDKProperty.MessageType.file) || s.msgType.ToLower() == nameof(SDKProperty.MessageType.onlinefile))).OrderByDescending(m => m.msgTime).ThenByDescending(m => m.Id).Take(loadCount).ToListAsync();
                            lst.Reverse();
                            return lst.Select(m => new DTO.MessageEntity() { db = m }).ToList();
                        }
                    }
                    else
                    {
                        if (showDelMsg)
                        {

                            var lst = await SDKProperty.SQLiteReader.Table<DB.messageDB>().Where(s => s.roomId == roomId && s.msgType.ToLower() == msgtype.ToLower()).OrderByDescending(m => m.msgTime).ThenByDescending(m => m.Id).Take(loadCount).ToListAsync();
                            lst.Reverse();
                            return lst.Select(m => new DTO.MessageEntity() { db = m }).ToList();

                        }
                        else
                        {
                            var lst = await SDKProperty.SQLiteReader.Table<DB.messageDB>().Where(s => s.roomId == roomId && (s.optionRecord & 2) == 0 && s.msgType.ToLower() == msgtype.ToLower()).OrderByDescending(m => m.msgTime).ThenByDescending(m => m.Id).Take(loadCount).ToListAsync();
                            lst.Reverse();
                            return lst.Select(m => new DTO.MessageEntity() { db = m }).ToList();
                        }
                    }

                }

            }
            else
            {
                if (string.IsNullOrEmpty(msgtype) || msgtype.Equals(nameof(SDKProperty.MessageType.all)))
                {
                    if (showDelMsg)
                    {
                        var lst = await SDKProperty.SQLiteReader.Table<DB.messageDB>().Where(s => s.roomId == roomId).OrderByDescending(m => m.msgTime).ThenByDescending(m => m.Id).ToListAsync();
                        lst = lst.Where(m => m.msgTime.Date == datetime.Value.Date).Take(loadCount).ToList();

                        lst.Reverse();
                        return lst.Select(m => new DTO.MessageEntity() { db = m }).ToList();
                    }
                    else
                    {
                        var lst = await SDKProperty.SQLiteReader.Table<DB.messageDB>().Where(s => s.roomId == roomId && (s.optionRecord & 2) == 0).OrderByDescending(m => m.msgTime).ThenByDescending(m => m.Id).ToListAsync();
                        lst = lst.Where(m => m.msgTime.Date == datetime.Value.Date).Take(loadCount).ToList();

                        lst.Reverse();
                        return lst.Select(m => new DTO.MessageEntity() { db = m }).ToList();
                    }
                }
                else
                {
                    if (msgtype == nameof(SDKProperty.MessageType.file) || msgtype == nameof(SDKProperty.MessageType.onlinefile))
                    {
                        if (showDelMsg)
                        {
                            var lst = await SDKProperty.SQLiteReader.Table<DB.messageDB>().Where(s => s.roomId == roomId && (s.msgType.ToLower() == nameof(SDKProperty.MessageType.onlinefile) || s.msgType.ToLower() == nameof(SDKProperty.MessageType.file))).OrderByDescending(m => m.msgTime).ThenByDescending(m => m.Id).ToListAsync();
                            lst = lst.Where(m => m.msgTime.Date == datetime.Value.Date).Take(loadCount).ToList();
                            lst.Reverse();
                            return lst.Select(m => new DTO.MessageEntity() { db = m }).ToList();
                        }
                        else
                        {
                            var lst = await SDKProperty.SQLiteReader.Table<DB.messageDB>().Where(s => s.roomId == roomId && (s.optionRecord & 2) == 0 && (s.msgType.ToLower() == nameof(SDKProperty.MessageType.onlinefile) || s.msgType.ToLower() == nameof(SDKProperty.MessageType.file))).OrderByDescending(m => m.msgTime).ThenByDescending(m => m.Id).ToListAsync();
                            lst = lst.Where(m => m.msgTime.Date == datetime.Value.Date).Take(loadCount).ToList();
                            lst.Reverse();
                            return lst.Select(m => new DTO.MessageEntity() { db = m }).ToList();
                        }
                    }
                    else
                    {
                        if (showDelMsg)
                        {
                            var lst = await SDKProperty.SQLiteReader.Table<DB.messageDB>().Where(s => s.roomId == roomId && s.msgType.ToLower() == msgtype.ToLower()).OrderByDescending(m => m.msgTime).ThenByDescending(m => m.Id).ToListAsync();
                            lst = lst.Where(m => m.msgTime.Date == datetime.Value.Date).Take(loadCount).ToList();
                            lst.Reverse();
                            return lst.Select(m => new DTO.MessageEntity() { db = m }).ToList();
                        }
                        else
                        {
                            var lst = await SDKProperty.SQLiteReader.Table<DB.messageDB>().Where(s => s.roomId == roomId && (s.optionRecord & 2) == 0 && s.msgType.ToLower() == msgtype.ToLower()).OrderByDescending(m => m.msgTime).ThenByDescending(m => m.Id).ToListAsync();
                            lst = lst.Where(m => m.msgTime.Date == datetime.Value.Date).Take(loadCount).ToList();
                            lst.Reverse();
                            return lst.Select(m => new DTO.MessageEntity() { db = m }).ToList();
                        }
                    }
                }

            }

        }
        /// <summary>
        /// 获取指定消息之前的消息记录项
        /// </summary>
        /// <param name="roomId">窗体ID</param>
        /// <param name="msgId">消息ID</param>
        /// <param name="LoadCount">加载条数</param>
        /// <returns></returns>
        public static async Task<List<DB.messageDB>> GetMsg_DESC(int roomId, string msgId, int LoadCount = 20, string msgtype = null, DateTime? datetime = null, bool isForword = true, chatType chatType = chatType.chat)
        {

            if (datetime == null || datetime == DateTime.MinValue)
            {

                if (string.IsNullOrEmpty(msgtype) || msgtype.Equals(nameof(SDKProperty.MessageType.all)))
                {
                    if (isForword)
                    {
                        var lst = await SDKProperty.SQLiteReader.Table<DB.messageDB>().Where(s => s.roomId == roomId && s.roomType == (int)chatType).OrderByDescending(m => m.msgTime).ThenByDescending(m => m.Id).ToListAsync();
                        int index = lst.FindIndex(m => m.msgId == msgId);
                        index = index + 1;
                        return lst.Skip(index).Take(LoadCount).ToList();

                    }
                    else
                    {

                        var lst = await SDKProperty.SQLiteReader.Table<DB.messageDB>().Where(s => s.roomId == roomId && s.roomType == (int)chatType).OrderBy(m => m.msgTime).ThenBy(m => m.Id).ToListAsync();
                        int index = lst.FindIndex(m => m.msgId == msgId);
                        index = index + 1;
                        return lst.Skip(index).Take(LoadCount).ToList();

                    }
                }
                else
                {
                    if (isForword)
                    {
                        List<DB.messageDB> lst = new List<DB.messageDB>();
                        if (msgtype.ToLower() == nameof(SDKProperty.MessageType.imgandvideo).ToLower())
                            lst = await SDKProperty.SQLiteReader.Table<DB.messageDB>().Where(s => s.roomId == roomId && s.roomType == (int)chatType && (s.msgType.ToLower().Equals("img") || s.msgType.ToLower().Equals("smallvideo"))).OrderByDescending(m => m.msgTime).ThenByDescending(m => m.Id).ToListAsync();

                        else
                            lst = await SDKProperty.SQLiteReader.Table<DB.messageDB>().Where(s => s.roomId == roomId && s.roomType == (int)chatType && s.msgType.ToLower() == msgtype.ToLower()).OrderByDescending(m => m.msgTime).ThenByDescending(m => m.Id).ToListAsync();
                        int index = lst.FindIndex(m => m.msgId == msgId);
                        index = index + 1;
                        return lst.Skip(index).Take(LoadCount).ToList();

                    }
                    else
                    {
                        List<DB.messageDB> lst = new List<DB.messageDB>();
                        if (msgtype.ToLower() == nameof(SDKProperty.MessageType.imgandvideo).ToLower())
                            lst = await SDKProperty.SQLiteReader.Table<DB.messageDB>().Where(s => s.roomId == roomId && s.roomType == (int)chatType && (s.msgType.ToLower().Equals("img") || s.msgType.ToLower().Equals("smallvideo"))).OrderBy(m => m.msgTime).ThenBy(m => m.Id).ToListAsync();
                        else
                            lst = await SDKProperty.SQLiteReader.Table<DB.messageDB>().Where(s => s.roomId == roomId && s.roomType == (int)chatType && s.msgType.ToLower() == msgtype.ToLower()).OrderBy(m => m.msgTime).ThenBy(m => m.Id).ToListAsync();
                        int index = lst.FindIndex(m => m.msgId == msgId);
                        index = index + 1;
                        return lst.Skip(index).Take(LoadCount).ToList();

                    }
                }
            }
            else
            {
                if (string.IsNullOrEmpty(msgtype) || msgtype.Equals(nameof(SDKProperty.MessageType.all)))
                {
                    if (isForword)
                    {

                        var lst = await SDKProperty.SQLiteReader.Table<DB.messageDB>().Where(s => s.roomId == roomId && s.roomType == (int)chatType).OrderByDescending(m => m.msgTime).ThenByDescending(m => m.Id).ToListAsync();
                        lst = lst.Where(m => m.msgTime.Date == datetime.Value.Date).ToList();

                        int index = lst.FindIndex(m => m.msgId == msgId);
                        index = index + 1;
                        return lst.Skip(index).Take(LoadCount).ToList();


                    }
                    else
                    {

                        var lst = await SDKProperty.SQLiteReader.Table<DB.messageDB>().Where(s => s.roomId == roomId && s.roomType == (int)chatType).OrderBy(m => m.msgTime).ThenBy(m => m.Id).ToListAsync();
                        lst = lst.Where(m => m.msgTime.Date == datetime.Value.Date).ToList();

                        int index = lst.FindIndex(m => m.msgId == msgId);
                        index = index + 1;
                        return lst.Skip(index).Take(LoadCount).ToList();

                    }
                }
                else
                {
                    if (isForword)
                    {
                        List<DB.messageDB> lst = new List<DB.messageDB>();
                        if (msgtype.ToLower() == nameof(SDKProperty.MessageType.imgandvideo).ToLower())
                            lst = await SDKProperty.SQLiteReader.Table<DB.messageDB>().Where(s => s.roomId == roomId && s.roomType == (int)chatType && (s.msgType.ToLower().Equals("img") || s.msgType.ToLower().Equals("smallvideo"))).OrderByDescending(m => m.msgTime).ThenByDescending(m => m.Id).ToListAsync();
                        else
                            lst = await SDKProperty.SQLiteReader.Table<DB.messageDB>().Where(s => s.roomId == roomId && s.roomType == (int)chatType && s.msgType.ToLower() == msgtype.ToLower()).OrderByDescending(m => m.msgTime).ThenByDescending(m => m.Id).ToListAsync();
                        lst = lst.Where(m => m.msgTime.Date == datetime.Value.Date).ToList();
                        int index = lst.FindIndex(m => m.msgId == msgId);
                        index = index + 1;
                        return lst.Skip(index).Take(LoadCount).ToList();

                    }
                    else
                    {
                        List<DB.messageDB> lst = new List<DB.messageDB>();
                        if (msgtype.ToLower() == nameof(SDKProperty.MessageType.imgandvideo).ToLower())
                            lst = await SDKProperty.SQLiteReader.Table<DB.messageDB>().Where(s => s.roomId == roomId && s.roomType == (int)chatType && (s.msgType.ToLower().Equals("img") || s.msgType.ToLower().Equals("smallvideo"))).OrderBy(m => m.msgTime).ThenBy(m => m.Id).ToListAsync();
                        else
                            lst = await SDKProperty.SQLiteReader.Table<DB.messageDB>().Where(s => s.roomId == roomId && s.roomType == (int)chatType && s.msgType.ToLower() == msgtype.ToLower()).OrderBy(m => m.msgTime).ThenBy(m => m.Id).ToListAsync();
                        lst = lst.Where(m => m.msgTime.Date == datetime.Value.Date).ToList();
                        int index = lst.FindIndex(m => m.msgId == msgId);
                        index = index + 1;
                        return lst.Skip(index).Take(LoadCount).ToList();

                    }
                }

            }



        }
        /// <summary>
        /// 获取指定消息之前的消息记录项
        /// </summary>
        /// <param name="roomId">窗体ID</param>
        /// <param name="msgId">消息ID</param>
        /// <param name="LoadCount">加载条数</param>
        /// <returns></returns>
        public static async Task<List<DTO.MessageEntity>> GetMsgEntity(int roomId, string msgId, int LoadCount = 20, string msgtype = null, DateTime? datetime = null, bool isForword = true, bool showDelMsg = false)
        {
            System.Diagnostics.Debug.WriteLine($"roomId:{roomId},msgId:{msgId}");
            if (datetime == null || datetime == DateTime.MinValue)
            {
                if (string.IsNullOrEmpty(msgtype) || msgtype.Equals(nameof(SDKProperty.MessageType.all)))
                {
                    if (isForword)
                    {
                        if (showDelMsg)
                        {
                            var lst = await SDKProperty.SQLiteConn.Table<DB.messageDB>().Where(s => s.roomId == roomId).OrderByDescending(m => m.msgTime).ThenByDescending(m => m.Id).ToListAsync();
                            int index = lst.FindIndex(m => m.msgId == msgId);
                            index = index + 1;
                            return lst.Skip(index).Take(LoadCount).Select(m => new DTO.MessageEntity() { db = m }).ToList();
                        }
                        else
                        {
                            var lst = await SDKProperty.SQLiteConn.Table<DB.messageDB>().Where(s => s.roomId == roomId && (s.optionRecord & 2) == 0).OrderByDescending(m => m.msgTime).ThenByDescending(m => m.Id).ToListAsync();
                            int index = lst.FindIndex(m => m.msgId == msgId);
                            index = index + 1;
                            return lst.Skip(index).Take(LoadCount).Select(m => new DTO.MessageEntity() { db = m }).ToList();
                        }
                    }
                    else
                    {
                        if (showDelMsg)
                        {
                            var lst = await SDKProperty.SQLiteConn.Table<DB.messageDB>().Where(s => s.roomId == roomId).OrderBy(m => m.msgTime).ThenBy(m => m.Id).ToListAsync();
                            int index = lst.FindIndex(m => m.msgId == msgId);
                            index = index + 1;
                            return lst.Skip(index).Take(LoadCount).Select(m => new DTO.MessageEntity() { db = m }).ToList();
                        }
                        else
                        {
                            var lst = await SDKProperty.SQLiteConn.Table<DB.messageDB>().Where(s => s.roomId == roomId && (s.optionRecord & 2) == 0).OrderBy(m => m.msgTime).ThenBy(m => m.Id).ToListAsync();
                            int index = lst.FindIndex(m => m.msgId == msgId);
                            index = index + 1;
                            return lst.Skip(index).Take(LoadCount).Select(m => new DTO.MessageEntity() { db = m }).ToList();
                        }
                    }
                }
                else
                {
                    if (isForword)
                    {
                        if (showDelMsg)
                        {
                            var lst = await SDKProperty.SQLiteConn.Table<DB.messageDB>().Where(s => s.roomId == roomId && s.msgType.ToLower() == msgtype.ToLower()).OrderByDescending(m => m.msgTime).ThenByDescending(m => m.Id).ToListAsync();
                            int index = lst.FindIndex(m => m.msgId == msgId);
                            index = index + 1;
                            return lst.Skip(index).Take(LoadCount).Select(m => new DTO.MessageEntity() { db = m }).ToList();
                        }
                        else
                        {
                            var lst = await SDKProperty.SQLiteConn.Table<DB.messageDB>().Where(s => s.roomId == roomId && (s.optionRecord & 2) == 0 && s.msgType.ToLower() == msgtype.ToLower()).OrderByDescending(m => m.msgTime).ThenByDescending(m => m.Id).ToListAsync();
                            int index = lst.FindIndex(m => m.msgId == msgId);
                            index = index + 1;
                            return lst.Skip(index).Take(LoadCount).Select(m => new DTO.MessageEntity() { db = m }).ToList();
                        }
                    }
                    else
                    {
                        if (showDelMsg)
                        {
                            var lst = await SDKProperty.SQLiteConn.Table<DB.messageDB>().Where(s => s.roomId == roomId && s.msgType.ToLower() == msgtype.ToLower()).OrderBy(m => m.msgTime).ThenBy(m => m.Id).ToListAsync();
                            int index = lst.FindIndex(m => m.msgId == msgId);
                            index = index + 1;
                            return lst.Skip(index).Take(LoadCount).Select(m => new DTO.MessageEntity() { db = m }).ToList();
                        }
                        else
                        {
                            var lst = await SDKProperty.SQLiteConn.Table<DB.messageDB>().Where(s => s.roomId == roomId && (s.optionRecord & 2) == 0 && s.msgType.ToLower() == msgtype.ToLower()).OrderBy(m => m.msgTime).ThenBy(m => m.Id).ToListAsync();
                            int index = lst.FindIndex(m => m.msgId == msgId);
                            index = index + 1;
                            return lst.Skip(index).Take(LoadCount).Select(m => new DTO.MessageEntity() { db = m }).ToList();
                        }
                    }
                }
            }
            else
            {
                if (string.IsNullOrEmpty(msgtype) || msgtype.Equals(nameof(SDKProperty.MessageType.all)))
                {
                    if (isForword)
                    {
                        if (showDelMsg)
                        {
                            var lst = await SDKProperty.SQLiteConn.Table<DB.messageDB>().Where(s => s.roomId == roomId).OrderByDescending(m => m.msgTime).ThenByDescending(m => m.Id).ToListAsync();
                            lst = lst.Where(m => m.msgTime.Date == datetime.Value.Date).ToList();

                            int index = lst.FindIndex(m => m.msgId == msgId);
                            index = index + 1;
                            return lst.Skip(index).Take(LoadCount).Select(m => new DTO.MessageEntity() { db = m }).ToList();
                        }
                        else
                        {
                            var lst = await SDKProperty.SQLiteConn.Table<DB.messageDB>().Where(s => s.roomId == roomId && (s.optionRecord & 2) == 0).OrderByDescending(m => m.msgTime).ThenByDescending(m => m.Id).ToListAsync();
                            lst = lst.Where(m => m.msgTime.Date == datetime.Value.Date).ToList();

                            int index = lst.FindIndex(m => m.msgId == msgId);
                            index = index + 1;
                            return lst.Skip(index).Take(LoadCount).Select(m => new DTO.MessageEntity() { db = m }).ToList();
                        }

                    }
                    else
                    {
                        if (showDelMsg)
                        {
                            var lst = await SDKProperty.SQLiteConn.Table<DB.messageDB>().Where(s => s.roomId == roomId).OrderBy(m => m.msgTime).ThenBy(m => m.Id).ToListAsync();
                            lst = lst.Where(m => m.msgTime.Date == datetime.Value.Date).ToList();

                            int index = lst.FindIndex(m => m.msgId == msgId);
                            index = index + 1;
                            return lst.Skip(index).Take(LoadCount).Select(m => new DTO.MessageEntity() { db = m }).ToList();
                        }
                        else
                        {
                            var lst = await SDKProperty.SQLiteConn.Table<DB.messageDB>().Where(s => s.roomId == roomId && (s.optionRecord & 2) == 0).OrderBy(m => m.msgTime).ThenBy(m => m.Id).ToListAsync();
                            lst = lst.Where(m => m.msgTime.Date == datetime.Value.Date).ToList();

                            int index = lst.FindIndex(m => m.msgId == msgId);
                            index = index + 1;
                            return lst.Skip(index).Take(LoadCount).Select(m => new DTO.MessageEntity() { db = m }).ToList();
                        }
                    }
                }
                else
                {
                    if (isForword)
                    {
                        if (showDelMsg)
                        {
                            var lst = await SDKProperty.SQLiteConn.Table<DB.messageDB>().Where(s => s.roomId == roomId && s.msgType.ToLower() == msgtype.ToLower()).OrderByDescending(m => m.msgTime).ThenByDescending(m => m.Id).ToListAsync();
                            lst = lst.Where(m => m.msgTime.Date == datetime.Value.Date).ToList();
                            int index = lst.FindIndex(m => m.msgId == msgId);
                            index = index + 1;
                            return lst.Skip(index).Take(LoadCount).Select(m => new DTO.MessageEntity() { db = m }).ToList();
                        }
                        else
                        {
                            var lst = await SDKProperty.SQLiteConn.Table<DB.messageDB>().Where(s => s.roomId == roomId && (s.optionRecord & 2) == 0 && s.msgType.ToLower() == msgtype.ToLower()).OrderByDescending(m => m.msgTime).ThenByDescending(m => m.Id).ToListAsync();
                            lst = lst.Where(m => m.msgTime.Date == datetime.Value.Date).ToList();
                            int index = lst.FindIndex(m => m.msgId == msgId);
                            index = index + 1;
                            return lst.Skip(index).Take(LoadCount).Select(m => new DTO.MessageEntity() { db = m }).ToList();
                        }
                    }
                    else
                    {
                        if (showDelMsg)
                        {
                            var lst = await SDKProperty.SQLiteConn.Table<DB.messageDB>().Where(s => s.roomId == roomId && s.msgType.ToLower() == msgtype.ToLower()).OrderBy(m => m.msgTime).ThenBy(m => m.Id).ToListAsync();
                            lst = lst.Where(m => m.msgTime.Date == datetime.Value.Date).ToList();
                            int index = lst.FindIndex(m => m.msgId == msgId);
                            index = index + 1;
                            return lst.Skip(index).Take(LoadCount).Select(m => new DTO.MessageEntity() { db = m }).ToList();
                        }
                        else
                        {
                            var lst = await SDKProperty.SQLiteConn.Table<DB.messageDB>().Where(s => s.roomId == roomId && (s.optionRecord & 2) == 0 && s.msgType.ToLower() == msgtype.ToLower()).OrderBy(m => m.msgTime).ThenBy(m => m.Id).ToListAsync();
                            lst = lst.Where(m => m.msgTime.Date == datetime.Value.Date).ToList();
                            int index = lst.FindIndex(m => m.msgId == msgId);
                            index = index + 1;
                            return lst.Skip(index).Take(LoadCount).Select(m => new DTO.MessageEntity() { db = m }).ToList();
                        }
                    }
                }

            }



        }

        public static async Task UpdateMsgIsRead(int roomId, int roomType)
        {
            var i = await SDKProperty.SQLiteConn.ExecuteAsync($"update messageDB set optionRecord=1 where roomId={roomId} and roomType={roomType}");

            // //当前不判断消息类型
            // var lst = await SDKProperty.SQLiteConn.Table<DB.messageDB>().Where(s => s.roomId == roomId&& s.roomType== roomType&& ((s.optionRecord&(int)SDKProperty.MessageState.isRead)==(int)SDKProperty.MessageState.noRead)).ToListAsync();
            // lst.ForEach(m => m.optionRecord += (int)SDKProperty.MessageState.isRead);
            //// if(lst.Count>0)
            //     await SDKProperty.SQLiteConn.UpdateAllAsync(lst);
        }
        public static async Task UpdateMsgIsRead(string msgId)
        {
            await SDKProperty.SQLiteConn.ExecuteAsync($"update messageDB set optionRecord=1 where msgId='{msgId}' ");

            //var item = await SDKProperty.SQLiteConn.FindAsync<DB.messageDB>(m => m.msgId == msgId);
            //if (item == null)
            //    return;
            //item.optionRecord = 1;
            //await SDKProperty.SQLiteConn.UpdateAsync(item);
        }
        public static async Task UpdateMsgNewFileName(string msgId, string fileName)
        {
            var item = await SDKProperty.SQLiteConn.FindAsync<DB.messageDB>(m => m.msgId == msgId);
            if (item == null)
                return;
            int i = await SDKProperty.SQLiteConn.ExecuteAsync($"update messageDB set fileName='{fileName}' where Id={item.Id}");

        }


        public static async Task<List<DB.messageDB>> ReceiveOffLineMessage(GetOfflineMessageListPackage package)
        {
            List<DB.messageDB> lst = new List<DB.messageDB>();
            List<DB.messageDB> msglist = new List<DB.messageDB>();
            if (package.data.items != null && package.data.items.Count > 0)
            {
                try
                {
                    foreach (var item in package.data.items)
                    {
                        PackageInfo obj = Util.Helpers.Json.ToObject<PackageInfo>(Util.Helpers.Json.ToJson(item));

                        switch (obj.apiId)
                        {
                            case Protocol.ProtocolBase.messageCode:
                                MessagePackage m = Util.Helpers.Json.ToObject<MessagePackage>(Util.Helpers.Json.ToJson(item));
                                var msg = await ReceiveMsgtoDB(m);
                                //当离线消息是撤回消息时，检查缓存中是否有原始消息，有则移除。
                                if (m.data.subType == nameof(SDKProperty.MessageType.retract))
                                {
                                    string msgid = m.data.body.retractId;
                                    var foo = lst.FirstOrDefault(i => i.msgId == msgid);
                                    if (foo != null)
                                        lst.Remove(foo);
                                }
                                if (msg != null)
                                    lst.Add(msg);
                                break;
                            case Protocol.ProtocolBase.InviteJoinGroupCode:
                                var ijgc_msg = await DAL.DALGroupOptionHelper.SendMsgtoDB(Util.Helpers.Json.ToObject<InviteJoinGroupPackage>(Util.Helpers.Json.ToJson(item)));
                                if (ijgc_msg != null)
                                    lst.Add(ijgc_msg);
                                break;
                            case Protocol.ProtocolBase.UpdateGroupCode:
                                var ugc_msg = await DAL.DALGroupOptionHelper.SendMsgtoDB(Util.Helpers.Json.ToObject<UpdateGroupPackage>(Util.Helpers.Json.ToJson(item)));
                                if (ugc_msg != null)
                                    lst.Add(ugc_msg);
                                break;
                            case Protocol.ProtocolBase.JoinGroupCode:
                                JoinGroupPackage jgp = Util.Helpers.Json.ToObject<JoinGroupPackage>(Util.Helpers.Json.ToJson(item));
                                if (jgp.code == 0)
                                {
                                    //管理员收到入群申请
                                    if (obj.from != SDKClient.Instance.property.CurrentAccount.userID.ToString())
                                    {
                                        await DAL.DALJoinGroupHelper.RecvJoinGroup(jgp);
                                        var msg_jgp = await DAL.DALJoinGroupHelper.SendMsgtoDB(jgp);
                                        if (msg_jgp != null)
                                            lst.Add(msg_jgp);
                                    }
                                    else
                                    {
                                        if (jgp.data.isAccepted)
                                        {
                                            var jgp_msg = await DAL.DALJoinGroupHelper.SendMsgtoDB(jgp);
                                            if (jgp_msg != null)
                                                lst.Add(jgp_msg);
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
                                await DAL.DALFriendApplyListHelper.InsertOrUpdateItem(Util.Helpers.Json.ToObject<AddFriendPackage>(Util.Helpers.Json.ToJson(item)));
                                SDKClient.Instance.property.FriendApplyList = Util.Helpers.Async.Run(async () => await DAL.DALFriendApplyListHelper.GetFriendApplyList());
                                break;
                            case Protocol.ProtocolBase.AddFriendAcceptedCode:
                                AddFriendAcceptedPackage afap = Util.Helpers.Json.ToObject<AddFriendAcceptedPackage>(Util.Helpers.Json.ToJson(item));
                                //接收加好友
                                if (afap.code == 0 || package.code == (int)Protocol.StatusCode.AlreadyBecomeFriend)
                                {
                                    await DAL.DALContactListHelper.DeleteCurrentContactListPackage();
                                    if (afap.data.userId == SDKClient.Instance.property.CurrentAccount.userID)
                                    {
                                        await DAL.DALFriendApplyListHelper.UpdateItemIsChecked(afap.data.friendId);
                                        await DAL.DALUserInfoHelper.UpdateItemIsChecked(afap.data.friendId);
                                        var db = await DAL.DALMessageHelper.SendMsgtoDB(afap.id, afap.from, package.to, "已经是好朋友，开始聊天吧", afap.data.friendId, afap.data.userId, SDKProperty.MessageType.notification, SDKProperty.MessageState.isRead);
                                        if (db != null)
                                            lst.Add(db);
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
                                            if (db != null)
                                                lst.Add(db);
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
                                SetMemberPowerPackage smpp = Util.Helpers.Json.ToObject<SetMemberPowerPackage>(Util.Helpers.Json.ToJson(item));
                                var smpp_dbs = await DAL.DALGroupOptionHelper.SendMsgtoDB(smpp);
                                if (smpp_dbs != null && smpp_dbs.Any())
                                    lst.AddRange(smpp_dbs);
                                break;
                            case Protocol.ProtocolBase.ExitGroupCode:
                                var egp = Util.Helpers.Json.ToObject<ExitGroupPackage>(Util.Helpers.Json.ToJson(item));
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
                                            if (goh_msg != null)
                                                lst.Add(goh_msg);
                                            await DAL.DALMessageHelper.UpdateMsgIsRead(egp.data.groupId, 1);
                                            await DAL.DALJoinGroupHelper.DeleteJoinGroupItem(egp.data.groupId);//删除该群的入群申请列表
                                        }
                                    }
                                    else
                                    {
                                        if (egp.data.adminIds != null && egp.data.adminIds.Contains(SDKClient.Instance.property.CurrentAccount.userID))
                                        {
                                            var goh_msg = await DAL.DALGroupOptionHelper.SendMsgtoDB(egp);
                                            if (goh_msg != null)
                                                lst.Add(goh_msg);
                                        }
                                    }

                                }

                                break;
                            case Protocol.ProtocolBase.DismissGroupCode:
                                var dgp = Util.Helpers.Json.ToObject<DismissGroupPackage>(Util.Helpers.Json.ToJson(item));

                                if (dgp.code == 0)
                                {
                                    //群主本人则删除群的聊天记录
                                    if (dgp.data.ownerId == SDKClient.Instance.property.CurrentAccount.userID)
                                        await DAL.DALMessageHelper.DeleteHistoryMsg(dgp.data.groupId, chatType.groupChat);
                                    await DAL.DALGroupOptionHelper.DeleteGroupListPackage();
                                    //Util.Helpers.Async.Run(async () => await DAL.DALGroupOptionHelper.DeleteGroupMemberListPackage(package.data.groupId));
                                    var goh_msg = await DAL.DALGroupOptionHelper.SendMsgtoDB(dgp);
                                    if (goh_msg != null)
                                        lst.Add(goh_msg);
                                    await DAL.DALJoinGroupHelper.DeleteJoinGroupItem(dgp.data.groupId);//删除该群的入群申请列表
                                    await DAL.DALMessageHelper.UpdateMsgIsRead(dgp.data.groupId, 1);
                                }
                                break;
                            case Protocol.ProtocolBase.JoinGroupAcceptedCode:
                                var jgap = Util.Helpers.Json.ToObject<JoinGroupAcceptedPackage>(Util.Helpers.Json.ToJson(item));

                                if (jgap.code == 0)
                                {
                                    // Task.Run(async () => await DAL.DALGroupOptionHelper.DeleteGroupMemberListPackage(package.data.groupId));
                                    //TODO:删除群申请列表中申请记录
                                    await DAL.DALJoinGroupHelper.DeleteJoinGroupItem(jgap as JoinGroupAcceptedPackage);
                                    //入群通知消息入库
                                    if (jgap.data.auditStatus == 1)
                                    {
                                        var jgap_msg = await DAL.DALGroupOptionHelper.SendMsgtoDB(jgap);
                                        if (jgap_msg != null)
                                            lst.Add(jgap_msg);
                                    }
                                }
                                else if (jgap.code == (int)Protocol.StatusCode.UserIsGroupMember || jgap.code == (int)Protocol.StatusCode.AlreadyCompleted)
                                {
                                    if (!await DAL.DALJoinGroupHelper.DeleteJoinGroupItem(jgap as JoinGroupAcceptedPackage))
                                        SDKClient.logger.Error($"删除入群申请记录失败：{package.ToString()}");

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
                    }


                }
                catch (Exception ex)
                {
                    SDKClient.logger.Error($"消息处理异常：error:{ex.Message},stack:{ex.StackTrace};\r\n");
                }
                DateTime? t = package.data.items.Last().time;
                DAL.DALAccount.UpdateAccountOfflineMsgTime(t);
                SDKClient.Instance.property.CurrentAccount.GetOfflineMsgTime = t;
                SDKClient.logger.Error($"本次拉取离线消息最后一条消息的时间为：{t}");
            }
            return lst;


        }

      

        /// <summary>
        /// 获取文件名
        /// </summary>
        /// <param name="combinePath">原始文件名</param>
        /// <param name="index">新文件序列号默认从1开始</param>
        /// <returns></returns>
        private static string GetFileName(string combinePath, int index)
        {
            //新文件名称
            var filename = $"{Path.GetFileNameWithoutExtension(combinePath)}({index}){Path.GetExtension(combinePath)}";

            if (index == 1)//首次
            {
                if (File.Exists(combinePath))//原始文件存在
                {
                    var temp = $"{Path.Combine(Path.GetDirectoryName(combinePath), filename)}";
                    if (fileNameRecord.Any(s => s.Equals(filename)) || File.Exists(temp))//新文件已经存在
                    {
                        index += 1;
                        return GetFileName(combinePath, index);//递增序号
                    }
                    else
                    {
                        fileNameRecord.Add(filename);
                        return temp;
                    }
                }
                else//原始文件不存在
                {

                    if (fileNameRecord.Any(s => s.Equals(combinePath)))//原始文件内存中已经存在
                    {
                        var temp = $"{Path.Combine(Path.GetDirectoryName(combinePath), filename)}";
                        if (File.Exists(temp) || fileNameRecord.Any(s => s == filename))
                        {
                            index += 1;
                            return GetFileName(combinePath, index);//递增序号
                        }
                        else
                        {
                            fileNameRecord.Add(filename);
                            return temp;

                        }

                    }
                    else
                    {
                        fileNameRecord.Add(combinePath);
                        return combinePath;
                    }
                }
            }
            else
            {
                var temp = $"{Path.Combine(Path.GetDirectoryName(combinePath), filename)}";
                if (fileNameRecord.Any(s => s.Equals(filename)) || File.Exists(temp))//新文件已经存在
                {
                    index += 1;
                    return GetFileName(combinePath, index);//递增序号
                }
                else
                {
                    fileNameRecord.Add(filename);
                    return temp;
                }

            }

        }

    }

}
