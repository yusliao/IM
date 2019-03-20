using SDKClient.DB;
using SDKClient.DTO;
using SDKClient.WebAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDKClient.Controllers
{
    public class OffLineMsgController
    {
       
        /// <summary>
        /// 创建拉取任务,从近到远
        /// </summary>
        /// <param name="room"></param>
        internal static async void CreateTask_Offline(RoomInfo room,bool needShow=false)
        {

            room.pullType = 1;//从近到远
            room.pageNum = 100;
            bool isGroup = room.entryType == 2 ? true : false;

            Task head = Task.Run(async () =>
            {
                await SDKProperty.SQLiteReader.FindAsync<OffLineMsgTask>(o => o.roomId == room.entryId && o.isGroup == isGroup && o.earlyTime == room.earlyTime)
           .ContinueWith(async t =>
           {
               if (t.IsFaulted)
               {
                   OffLineMsgTask firstTask = new OffLineMsgTask()
                   {
                       deadTime = room.fromTime,
                       isGroup = room.entryType == 2 ? true : false,
                       roomId = room.entryId,
                       earlyTime = room.earlyTime
                   };
                   await SDKProperty.SQLiteConn.InsertAsync(firstTask);
               }
               else
               {
                   var item = t.Result;
                   if (item != null)
                   {
                       item.deadTime = room.fromTime;
                       await SDKProperty.SQLiteConn.UpdateAsync(item);
                   }
                   else
                   {
                       OffLineMsgTask firstTask = new OffLineMsgTask()
                       {
                           deadTime = room.fromTime,
                           isGroup = room.entryType == 2 ? true : false,
                           roomId = room.entryId,
                           earlyTime = room.earlyTime
                       };
                       await SDKProperty.SQLiteConn.InsertAsync(firstTask);
                   }

               }
           }, TaskContinuationOptions.NotOnCanceled);
            });
            
            
            var lst = await IMRequest.GetMessage(room);//获取到条目消息

            if (lst.success)
            {
                head.Wait();
                List<DB.messageDB> dblst = new List<messageDB>();
                foreach (var item in lst.msgList.Where(l=>l.content.reply==null||l.content.reply!=1))
                {
                    var msg = await DAL.DALMessageHelper.ReceiveMsgtoDB(item.content);
                    if (msg != null)
                        dblst.Add(msg);
                }
                if (dblst.Any())
                {
                    OfflineMessageContext offlineMessage = new OfflineMessageContext();
                    foreach (var item in dblst)
                    {
                        var isgroup = item.roomType == 1 ? true : false;
                        IList<MessageEntity> msglst = new List<MessageEntity>();
                        //  if (offlineMessage.context.TryAdd((item.roomId, isgroup), msglst))

                        if (offlineMessage.context.TryAdd(new Tuple<int, bool>(item.roomId, isgroup), msglst))
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
                    if(needShow)
                        SDKClient.Instance.OnOffLineMessageEventHandle(offlineMessage);//推送到UI
                   
                    room.fromTime = lst.msgList.Min(o => o.time);
                    CreateTask_Offline(room);
                }
                else //没消息说明已经拉完,删除任务
                {
                    await SDKProperty.SQLiteReader.FindAsync<OffLineMsgTask>(o => o.roomId == room.entryId && o.isGroup == isGroup && o.earlyTime == room.earlyTime).ContinueWith(async t =>
                    {
                        if (t.IsFaulted)
                        {
                            return;
                        }
                        else
                        {
                            var item = t.Result;
                            if(item!=null)
                                await SDKProperty.SQLiteConn.DeleteAsync(item);
                        }
                    });
                }

            }
        }
        internal static async void RunOffLineTasks()
        {
            double lastTime = 0;
            if (SDKClient.Instance.property.CurrentAccount.GetOfflineMsgTime.HasValue)
            {

                lastTime = (SDKClient.Instance.property.CurrentAccount.GetOfflineMsgTime.Value.ToUniversalTime() - new DateTime(1970, 1, 1)).TotalMilliseconds;
            }
            var lst = await SDKProperty.SQLiteReader.Table<OffLineMsgTask>().Where(o => o.earlyTime > lastTime).ToListAsync();
            foreach (var item in lst)
            {
                RoomInfo room = new RoomInfo()
                {
                    earlyTime = item.earlyTime,
                    entryId = item.roomId,
                    entryType = item.isGroup ? 2 : 1,
                    fromTime = item.deadTime
                };
                CreateTask_Offline(room);
            }
        }
    }
}
