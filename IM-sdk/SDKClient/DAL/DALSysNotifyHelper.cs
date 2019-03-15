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
    class DALSysNotifyHelper
    {
        public static async Task SendMsgtoDB(Model.SysNotifyPackage package)
        {
            DB.messageDB msg = new DB.messageDB()
            {
                from = package.from,
                to = package.to,
                msgTime = package.time.Value,
                msgId = package.id,
                body = Util.Helpers.Json.ToJson(package.data),
                optionRecord = 1,
                roomId = package.from.ToInt(),
                Source = Util.Helpers.Json.ToJson(package),
                roomType = 1
            };
            StringBuilder sb = new StringBuilder();
            if (package.data.body.type == nameof(chatType.groupChat))
            {
                msg.roomId = package.data.body.groupInfo.groupId;
                msg.roomType = 1;

            }
            else
            {
                msg.roomType = 0;
            }
            msg.content = "[红包退款通知]";

            msg.msgType = nameof(SDKProperty.MessageType.notification);
            try
            {
                await SDKProperty.SQLiteConn.InsertAsync(msg);
            }
            catch (Exception ex)
            {
                SDKClient.logger.Error($"消息处理异常：error:{ex.Message},stack:{ex.StackTrace};\r\n");

            }


        }
    }
}
