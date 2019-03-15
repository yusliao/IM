
using SuperSocket.SocketBase.Command;
using SuperSocket.SocketBase.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Util;

namespace SDKClient.P2P.Command
{
    class Quit : CommandBase<CustomProtocolSession, BinaryRequestInfo>
    {
        public override void ExecuteCommand(CustomProtocolSession session, BinaryRequestInfo requestInfo)
        {
            //TODO:
            /*
             * 数据库更新消息
             * 
             */
            string id = Util.Helpers.Json.ToObject<string>(Encoding.UTF8.GetString(requestInfo.Body));
            if (P2PClient.FileCache.Keys.Any(s => s == id))
            {

                P2PClient p2PHelper = P2PClient.FileCache[id];
                p2PHelper.CancelSend();
               
                //P2PPackage package = new P2PPackage()
                //{
                //    RoomId = p2PHelper.To,
                //    FileName = p2PHelper.FileName,
                //    MD5 = p2PHelper.MD5,
                //    MsgId = p2PHelper.MsgId,
                //    PackageCode = P2PPakcageState.cancel
                //};
                //SDKClient.Instance.OnP2PPackagePush(package);

            }
            
            
        }

    }
}
