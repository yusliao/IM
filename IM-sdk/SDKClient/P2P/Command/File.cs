using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SuperSocket.SocketBase.Command;
using SuperSocket.SocketBase.Protocol;

namespace SDKClient.P2P.Command
{
    public class File : CommandBase<CustomProtocolSession, BinaryRequestInfo>
    {
        public override void ExecuteCommand(CustomProtocolSession session, BinaryRequestInfo requestInfo)
        {
            //TODO:
            /*
             * 生成文件
             * 生成消息记录
             */
            var obj = Util.Helpers.Json.ToObject<FileHead>(Encoding.UTF8.GetString(requestInfo.Body));
            if(P2PClient.FileCache.Keys.Any(s=>s==obj.MsgId))
            {
               
                P2PClient p2PHelper = P2PClient.FileCache[obj.MsgId];
                p2PHelper.SendBody(session);
                
              
            }
          
            // msg.roomType = 0;
           
                   
            //msg.msgType = nameof(SDKProperty.MessageType.onlinefile);


            //msg.fileName = obj.FileName;
            //msg.fileSize = package.data.body.fileSize;
            //msg.content = "[文件]";
                       
            //try
            //{
            //    await SDKProperty.SQLiteConn.InsertAsync(msg);
            //}
            //catch (Exception)
            //{
            //}

            //}
        }

    }
   
}
