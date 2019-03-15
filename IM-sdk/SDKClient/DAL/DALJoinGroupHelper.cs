using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Util.Logs.Extensions;

namespace SDKClient.DAL
{
    class DALJoinGroupHelper
    {
        /// <summary>
        /// 接收到好友申请
        /// </summary>
        /// <param name="joinGroupPackage"></param>
        /// <returns></returns>
        public static async Task RecvJoinGroup(Model.JoinGroupPackage joinGroupPackage)
        {
            
          //  var lst = await SDKProperty.SQLiteConn.Table<DB.JoinGroupDB>().Where(j => j.groupId == joinGroup.groupId && j.userId == joinGroup.userId).ToListAsync();
            var item = await SDKProperty.SQLiteConn.FindAsync<DB.JoinGroupDB>(j => j.groupId == joinGroupPackage.data.groupId && j.userId == joinGroupPackage.data.userId);
            if (item!=null)
                return;
            else
            {
                DB.JoinGroupDB joinGroup = new DB.JoinGroupDB()
                {
                    msgId = joinGroupPackage.id,
                    JoinGroupPackage = Util.Helpers.Json.ToJson(joinGroupPackage),
                    userId = joinGroupPackage.data.userId,
                    groupId = joinGroupPackage.data.groupId

                };
                try
                {
                    await SDKProperty.SQLiteConn.InsertAsync(joinGroup);
                }
                catch (Exception ex)
                {
                    
                    SDKClient.logger.Content("接收好友申请").Error($"消息处理异常：error:{ex.Message},stack:{ex.StackTrace};\r\n");
                }
                
            }
        }
        public static async Task<bool> DeleteJoinGroupItem(Model.JoinGroupAcceptedPackage acceptedPackage)
        {
            int i = await SDKProperty.SQLiteConn.ExecuteAsync($"delete from JoinGroupDB where groupId={acceptedPackage.data.groupId} and userId={acceptedPackage.data.memberId} ");
            return i > 0 ? true : false;
            //var lst = await SDKProperty.SQLiteConn.Table<DB.JoinGroupDB>().Where(j => j.groupId == acceptedPackage.data.groupId && j.userId == acceptedPackage.data.memberId).ToListAsync();
            //foreach (var item in lst)
            //{
            //    await SDKProperty.SQLiteConn.DeleteAsync(item);
            //}

        }
        public static async Task<bool> DeleteJoinGroupItem(Model.JoinGroupPackage Package)
        {
            int i = await SDKProperty.SQLiteConn.ExecuteAsync($"delete from JoinGroupDB where groupId={Package.data.groupId} and userId={Package.data.userId} ");
            return i > 0 ? true : false;
            //var lst = await SDKProperty.SQLiteConn.Table<DB.JoinGroupDB>().Where(j => j.groupId == Package.data.groupId && j.userId == Package.data.userId).ToListAsync();
            //foreach (var item in lst)
            //{
            //    await SDKProperty.SQLiteConn.DeleteAsync(item);
            //}

        }
        public static async Task DeleteJoinGroupItem(int groupId,int userId)
        {
            var lst = await SDKProperty.SQLiteConn.Table<DB.JoinGroupDB>().Where(j => j.groupId == groupId && j.userId == userId).ToListAsync();
            foreach (var item in lst)
            {
                await SDKProperty.SQLiteConn.DeleteAsync(item);
            }

        }
        public static async Task<List<DB.JoinGroupDB>> GetJoinGroupList(int groupId)
        {
            return await SDKProperty.SQLiteConn.Table<DB.JoinGroupDB>().Where(g=>g.groupId==groupId).ToListAsync();
        }
        public static async Task DeleteJoinGroupItem(int groupId)
        {
            
            await SDKProperty.SQLiteConn.ExecuteAsync($" delete from JoinGroupDB where groupId={groupId}");
            

        }
        public static async Task<DB.messageDB> SendMsgtoDB(Model.JoinGroupPackage package)
        {
            DB.messageDB msg = new DB.messageDB()
            {
                from = package.from,
                to = package.to,
                msgTime = package.time.Value,
                msgId = package.id,
                body = Util.Helpers.Json.ToJson(package.data),
                optionRecord = package.data.isAccepted==true?1:0,//
                roomId = package.data.groupId,
                Source = Util.Helpers.Json.ToJson(package),
                roomType = 1
            };
            if (!package.data.isAccepted)
            {
                msg.content = $"[{package.data.userName}] 申请进入群聊";
            }
            else
            {
                if(package.data.userId!=SDKClient.Instance.property.CurrentAccount.userID)
                    msg.content = $"[{package.data.userName}] 进入群聊";
                else
                    msg.content = $"你进入群聊";
            }

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
       


    }       
}
