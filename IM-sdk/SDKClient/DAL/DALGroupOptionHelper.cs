using SDKClient.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Util;
using static SDKClient.SDKProperty;

namespace SDKClient.DAL
{
    class DALGroupOptionHelper
    {
        public static async Task<DB.groupListDB> GetGroupListPackage()
        {
            return await SDKProperty.SQLiteConn.Table<DB.groupListDB>().FirstOrDefaultAsync();
        }
        public static async Task InsertGroupInfo(Model.GetGroupListPackage getGroupListPackage)
        {
            List<DB.GroupInfo> lst = new List<DB.GroupInfo>();
            var glist = await SDKProperty.SQLiteConn.Table<DB.GroupInfo>().ToListAsync();

            foreach (var group in getGroupListPackage.data.items.adminGroup)
            {
                var model = glist.Find(g => g.groupId == group.groupId);
                if (model != null)
                    continue;
                GetGroupPackage package = new GetGroupPackage()
                {
                    data = new Model.GetGroupPackage.Data()
                    {
                        groupId = group.groupId,
                        item = group
                    }
                };
                DB.GroupInfo info = new DB.GroupInfo()
                {
                    getGroupPackage = Util.Helpers.Json.ToJson(package),
                    groupId = group.groupId
                };
                lst.Add(info);
            }
            foreach (var group in getGroupListPackage.data.items.joinGroup)
            {
                var model = glist.Find(g => g.groupId == group.groupId);
                if (model != null)
                    continue;
                GetGroupPackage package = new GetGroupPackage()
                {
                    data = new Model.GetGroupPackage.Data()
                    {
                        groupId = group.groupId,
                        item = group
                    }
                };
                DB.GroupInfo info = new DB.GroupInfo()
                {
                    getGroupPackage = Util.Helpers.Json.ToJson(package),
                    groupId = group.groupId
                };


                lst.Add(info);
            }
            foreach (var group in getGroupListPackage.data.items.ownerGroup)
            {
                var model = glist.Find(g => g.groupId == group.groupId);
                if (model != null)
                    continue;
                GetGroupPackage package = new GetGroupPackage()
                {
                    data = new Model.GetGroupPackage.Data()
                    {
                        groupId = group.groupId,
                        item = group
                    }
                };
                DB.GroupInfo info = new DB.GroupInfo()
                {
                    getGroupPackage = Util.Helpers.Json.ToJson(package),
                    groupId = group.groupId
                };


                lst.Add(info);
            }
            try
            {
                await SDKProperty.SQLiteConn.InsertAllAsync(lst);
            }
            catch (Exception)
            {
            }

        }
        public static async Task InsertGroupInfo(Model.CreateGroupComponsePackage package)
        {
            GetGroupPackage p = new GetGroupPackage()
            {
                data = new GetGroupPackage.Data
                {
                    groupId = package.data.groupId,
                    item = package.data
                }
            };

            DB.GroupInfo info = new DB.GroupInfo()
            {
                getGroupPackage = Util.Helpers.Json.ToJson(p),
                groupId = package.data.groupId
            };

            try
            {
                await SDKProperty.SQLiteConn.InsertAsync(info);
            }
            catch (Exception)
            {
            }

        }
        public static async Task UpdateGroupInfo(Model.GetGroupListPackage getGroupListPackage)
        {
            await DeleteGroupListPackage();
            await InsertGroupInfo(getGroupListPackage);
        }
        public static async Task<DB.GroupInfo> GetGroupInfo(int roomId)
        {
            return await SDKProperty.SQLiteConn.FindAsync<DB.GroupInfo>(roomId);
        }
        public static async Task<DB.GroupMemberInfo> GetGroupMemberInfo(int userid, int groupId)
        {
            return await SDKProperty.SQLiteConn.Table<DB.GroupMemberInfo>().Where(g => g.groupId == groupId && g.userId == userid).FirstOrDefaultAsync();

        }
        public static async Task InsertGroupMemberInfo(Model.GetGroupMemberListPackage getGroupMemberListPackage)
        {

            System.Collections.Concurrent.ConcurrentDictionary<int, DB.GroupMemberInfo> dic = new System.Collections.Concurrent.ConcurrentDictionary<int, DB.GroupMemberInfo>();
            var array = getGroupMemberListPackage.data.items.ToArray();
            foreach (var item in array)
            {


                DB.GroupMemberInfo info = new DB.GroupMemberInfo()
                {
                    groupId = item.groupId,
                    memoInGroup = item.memoInGroup,//群昵称
                    userId = item.userId,
                    userName = item.userName,
                    auditUserId = item.auditUserId,
                    city = item.city,
                    createTime = item.createTime,
                    mobile = item.mobile,
                    photo = item.photo,
                    province = item.province,
                    recommendByUser = item.recommendByUser,
                    sex = item.sex

                };
                dic.TryAdd(info.userId, info);
            }
            try
            {

                await SDKProperty.SQLiteConn.ExecuteAsync($"delete from GroupMemberInfo where groupId ={getGroupMemberListPackage.data.groupId}");

                await SDKProperty.SQLiteConn.InsertAllAsync(dic.Values);
            }
            catch (Exception)
            {
                await SDKProperty.SQLiteConn.ExecuteAsync($"delete from GroupMemberInfo where groupId ={getGroupMemberListPackage.data.groupId}");

            }

        }

        public static async Task UpdateGroupMemberInfo(GetGroupMemberPackage memberPackage)
        {
            var model = await GetGroupMemberInfo(memberPackage.data.partnerId, memberPackage.data.groupId);
            if (model == null)
            {
                DB.GroupMemberInfo info = new DB.GroupMemberInfo()
                {
                    groupId = memberPackage.data.user.groupId,
                    memoInGroup = memberPackage.data.user.memoInGroup,//群昵称
                    userId = memberPackage.data.user.userId,
                    userName = memberPackage.data.user.userName,
                    auditUserId = memberPackage.data.user.auditUserId,
                    city = memberPackage.data.user.city,
                    createTime = memberPackage.data.user.createTime,
                    mobile = memberPackage.data.user.mobile,
                    photo = memberPackage.data.user.photo,
                    province = memberPackage.data.user.province,
                    recommendByUser = memberPackage.data.user.recommendByUser,
                    sex = memberPackage.data.user.sex

                };
                try
                {
                    await SDKProperty.SQLiteConn.InsertAsync(info);
                }
                catch (Exception ex)
                {
                    SDKClient.logger.Error($"消息处理异常：error:{ex.Message},stack:{ex.StackTrace};\r\n GroupMemberInfo:{Util.Helpers.Json.ToJson(info)}");
                    
                }

            }
            //else
            //{

            //    await SDKProperty.SQLiteConn.DeleteAsync(model);
            //    DB.GroupMemberInfo info = new DB.GroupMemberInfo()
            //    {
            //        groupId = memberPackage.data.user.groupId,
            //        memoInGroup = memberPackage.data.user.memoInGroup,//群昵称
            //        userId = memberPackage.data.user.userId,
            //        userName = memberPackage.data.user.userName,
            //        auditUserId = memberPackage.data.user.auditUserId,
            //        city = memberPackage.data.user.city,
            //        createTime = memberPackage.data.user.createTime,
            //        mobile = memberPackage.data.user.mobile,
            //        photo = memberPackage.data.user.photo,
            //        province = memberPackage.data.user.province,
            //        recommendByUser = memberPackage.data.user.recommendByUser,
            //        sex = memberPackage.data.user.sex

            //    };
            //    await SDKProperty.SQLiteConn.InsertAsync(info);
            //}


        }
        public static async Task<bool> UpdateGroupMemberInfo(DB.GroupMemberInfo groupMemberInfo)
        {
            var model = await SDKProperty.SQLiteConn.Table<DB.GroupMemberInfo>().Where(g => g.groupId == groupMemberInfo.groupId && g.userId == groupMemberInfo.userId).FirstOrDefaultAsync();

            await SDKProperty.SQLiteConn.DeleteAsync(model);
            try
            {
                int i = await SDKProperty.SQLiteConn.InsertAsync(groupMemberInfo);
                return i > 0 ? true : false;
            }
            catch (Exception ex)
            {
                SDKClient.logger.Error($"消息处理异常：error:{ex.Message},stack:{ex.StackTrace};\r\n");
                return false;
            }

        }
        public static async Task DeleteGroupListPackage()
        {
            await SDKProperty.SQLiteConn.DropTableAsync<DB.groupListDB>();
            await SDKProperty.SQLiteConn.CreateTableAsync<DB.groupListDB>();
        }

        public static async Task<DB.groupMemberListDB> GetGroupMemberListPackage(int groupId)
        {
            var lst = await SDKProperty.SQLiteConn.Table<DB.groupMemberListDB>().Where(g => g.groupId == groupId).ToListAsync();
            if (lst.Any())
                return lst[0];
            else
                return null;

        }

        public static async Task<DB.messageDB> SendMsgtoDB(Model.InviteJoinGroupPackage package, SDKProperty.MessageState state = SDKProperty.MessageState.noRead, bool isFoward = false)
        {
            var msgId = string.Empty;
            var isGroup = false;
            if (package.data.targetGroupId == 0)
            {
                //var userId = isFoward ? package.to : package.from;
                msgId = package.id + package.to + "single";
            }
            else
            {
                isGroup = true;
                msgId = package.id + package.data.targetGroupId + "group";
            }
            int roomId = 0;
            if (isFoward)
                int.TryParse(package.to, out roomId);
            else
                int.TryParse(package.from, out roomId);
            DB.messageDB msg = new DB.messageDB()
            {
                from = package.from,
                to = package.to,
                msgTime = package.time.Value,
                msgId = msgId,
                body = Util.Helpers.Json.ToJson(package.data),
                optionRecord = (int)state,//收到群名片消息，消息为未读
                roomId = isGroup ? package.data.targetGroupId : roomId,
                Source = Util.Helpers.Json.ToJson(package),
                roomType = isGroup ? 1 : 0//单聊窗体显示
            };
            // msg.content = $"{package.data.inviteUserName}\t邀请:{string.Join<int>(",",package.data.userIds)}\t入群:{package.data.groupName}";
            msg.content = "群名片";
            msg.msgType = nameof(SDKProperty.MessageType.invitejoingroup);
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


        public static async Task<List<DB.messageDB>> SendMsgtoDB(Model.SetMemberPowerPackage package)
        {
            int index = 0;
            List<DB.messageDB> lst = new List<DB.messageDB>();
            try
            {
                foreach (var item in package.data.userIds)
                {
                    DB.messageDB msg = new DB.messageDB()
                    {
                        from = package.from,
                        to = package.to,
                        msgTime = package.time.Value,
                        msgId = SDKProperty.RNGId,
                        body = Util.Helpers.Json.ToJson(package.data),
                        optionRecord = 1,
                        roomId = package.data.groupId,
                        Source = Util.Helpers.Json.ToJson(package),
                        roomType = 1//群聊窗体显示
                    };
                    msg.msgType = nameof(SDKProperty.MessageType.notification);
                    string info = string.Empty;
                    if (package.data.userNames == null)
                    {
                        if (package.data.type == "admin")
                        {
                            if (item == SDKClient.Instance.property.CurrentAccount.userID)
                            {
                                info = "你成为群管理员";

                            }
                            else
                            {
                                info = $"[{package.data.userNames[index]}] 成为群管理员";
                            }
                        }
                        else
                        {
                            if (item == SDKClient.Instance.property.CurrentAccount.userID)
                            {
                                info = "你被取消群管理员";
                                Util.Helpers.Async.Run(async () => await DAL.DALJoinGroupHelper.DeleteJoinGroupItem(package.data.groupId));//删除该群的入群申请列表
                            }
                            else
                            {
                                info = $"[{package.data.userNames[index]}] 被取消群管理员";
                            }
                        }
                    }
                    else
                    {
                        if (package.data.type == "admin")
                        {
                            if (item == SDKClient.Instance.property.CurrentAccount.userID)
                            {
                                info = "你成为群管理员";

                            }
                            else
                            {
                                info = $"[{package.data.userNames[index]}] 成为群管理员";
                            }
                        }
                        else
                        {
                            if (item == SDKClient.Instance.property.CurrentAccount.userID)
                            {
                                info = "你被取消群管理员";
                                Util.Helpers.Async.Run(async () => await DAL.DALJoinGroupHelper.DeleteJoinGroupItem(package.data.groupId));//删除该群的入群申请列表
                            }
                            else
                            {
                                info = $"[{package.data.userNames[index]}] 被取消群管理员";
                            }
                        }
                    }

                    msg.content = info;
                    try
                    {
                        await SDKProperty.SQLiteConn.InsertAsync(msg);
                        lst.Add(msg);
                    }
                    catch (Exception ex)
                    {
                        SDKClient.logger.Error($"InsertAsync异常：error:{ex.Message},stack:{ex.StackTrace};\r\n");
                    }
                    index++;
                }
                return lst;
            }
            catch (Exception ex)
            {
                Util.Logs.Log.GetLog().Error(ex.Message);
                return null;
            }

        }
        public static async Task<DB.messageDB> SendMsgtoDB(Model.UpdateGroupPackage package)
        {
            DB.messageDB msg = new DB.messageDB()
            {
                from = package.from,
                to = package.to,
                msgTime = package.time.Value,
                msgId = package.id,
                body = Util.Helpers.Json.ToJson(package.data),
                optionRecord = 1,
                roomId = package.data.groupId,
                Source = Util.Helpers.Json.ToJson(package),
                roomType = 1//群聊窗体显示
            };
            // msg.content = $"{package.data.inviteUserName}\t邀请:{string.Join<int>(",",package.data.userIds)}\t入群:{package.data.groupName}";
            switch (package.data.setType)
            {
                case (int)Model.SetGroupOption.修改群名称:
                    msg.content = $"群名称修改为：[{package.data.content}]";
                    break;
                case (int)Model.SetGroupOption.修改群头像:
                    msg.content = "群头像已修改";
                    return null; //不需要显示 该条信息
                case (int)Model.SetGroupOption.修改群简介:
                    msg.content = $"修改群简介：[{package.data.content}]";
                    break;
                case (int)Model.SetGroupOption.设置入群验证方式:
                    switch (package.data.content)
                    {
                        case "1":
                            msg.content = "入群方式修改为: [管理员审批入群]";
                            break;
                        case "2":
                            msg.content = "移动端入群方式修改为: [自由入群]";
                            break;
                        case "3":
                            msg.content = "移动端入群方式修改为: [密码入群]";
                            break;
                        default:
                            break;
                    }
                    break;

                default:
                    break;
            }

            msg.msgType = nameof(SDKProperty.MessageType.notification);
            try
            {
                await SDKProperty.SQLiteConn.InsertAsync(msg);
                return msg;
            }
            catch (Exception ex)
            {
                SDKClient.logger.Error($"入库异常：error:{ex.Message},stack:{ex.StackTrace};\r\n");
                return null;
            }


        }
        public static async Task<DB.messageDB> SendMsgtoDB(Model.JoinGroupAcceptedPackage package)
        {
            DB.messageDB msg = new DB.messageDB()
            {
                from = package.from,
                to = package.to,
                msgTime = package.time.Value,
                msgId = package.id,
                body = Util.Helpers.Json.ToJson(package.data),
                optionRecord = 1,
                roomId = package.data.groupId,
                Source = Util.Helpers.Json.ToJson(package),
                roomType = 1//群聊窗体显示
            };
            // msg.content = $"{package.data.inviteUserName}\t邀请:{string.Join<int>(",",package.data.userIds)}\t入群:{package.data.groupName}";
            if (package.data.userName == SDKClient.Instance.property.CurrentAccount.userName)
            {
                msg.content = $"你进入群聊";
            }
            else
            {
                msg.content = $"[{package.data.userName}] 进入群聊";
            }
            msg.msgType = nameof(SDKProperty.MessageType.notification);
            try
            {
                await SDKProperty.SQLiteConn.InsertAsync(msg);
                return msg;
            }
            catch (Exception)
            {
                return null;
            }


        }
        public static async Task<DB.messageDB> SendMsgtoDB(Model.ExitGroupPackage package)
        {
            DB.messageDB msg = new DB.messageDB()
            {
                from = package.from,
                to = package.to,
                msgTime = package.time.Value,
                msgId = package.id,
                body = Util.Helpers.Json.ToJson(package.data),
                optionRecord = 1,
                roomId = package.data.groupId,
                Source = Util.Helpers.Json.ToJson(package),
                roomType = 1//群聊窗体显示
            };

            string prefix = string.Empty;
            string suffix = package.data.adminId == 0 ? "退出群聊" : "被移出群聊";
            string tip = string.Empty;

            //if (package.data.userIds.Count == 1 && package.data.userIds[0] == SDKClient.Instance.property.CurrentAccount.userID)
            if (package.data.userIds.Contains(SDKClient.Instance.property.CurrentAccount.userID))
            {

                prefix = package.data.adminId == 0 ? "我" : "你";
                tip = string.Format("{0}{1}", prefix, suffix);
            }
            else
            {

                foreach (var item in package.data.userNames)
                {
                    prefix += item + "、";
                }
                prefix = prefix.TrimEnd('、');
                tip = string.Format("[{0}] {1}", prefix, suffix);

            }

            msg.content = tip;

            msg.msgType = nameof(SDKProperty.MessageType.notification);
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
        public static async Task<DB.messageDB> SendMsgtoDB(Model.DismissGroupPackage package)
        {
            DB.messageDB msg = new DB.messageDB()
            {
                from = package.from,
                to = package.to,
                msgTime = package.time.Value,
                msgId = package.id,
                body = Util.Helpers.Json.ToJson(package.data),
                optionRecord = 1,
                roomId = package.data.groupId,
                Source = Util.Helpers.Json.ToJson(package),
                roomType = 1//群聊窗体显示
            };

            msg.content = $"该群已经被解散！";

            msg.msgType = nameof(SDKProperty.MessageType.notification);
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
        public static async Task<DB.messageDB> SendMsgtoDB(Model.CreateGroupComponsePackage package)
        {
            DB.messageDB msg = new DB.messageDB()
            {
                from = package.from,
                to = package.to,
                msgTime = package.time.Value,
                msgId = package.id,
                body = Util.Helpers.Json.ToJson(package.data),
                optionRecord = 1,
                roomId = package.data.groupId,
                Source = Util.Helpers.Json.ToJson(package),
                roomType = 1//群聊窗体显示
            };
            StringBuilder sb = new StringBuilder();
            foreach (var item in package.data.items)
            {
                sb.Append(item.userName);
                sb.Append("、");
            }
            sb.Remove(sb.Length - 1, 1);
            // msg.content = $"{package.data.inviteUserName}\t邀请:{string.Join<int>(",",package.data.userIds)}\t入群:{package.data.groupName}";
            msg.content = $"[{sb.ToString()}] 进入群聊";

            msg.msgType = nameof(SDKProperty.MessageType.notification);
            try
            {
                await SDKProperty.SQLiteConn.InsertAsync(msg);
                await DAL.DALGroupOptionHelper.InsertGroupInfo(package);
                return msg;
            }
            catch (Exception ex)
            {
                SDKClient.logger.Error($"消息处理异常：error:{ex.Message},stack:{ex.StackTrace};\r\n");
                return null;
            }


        }

    }
}
