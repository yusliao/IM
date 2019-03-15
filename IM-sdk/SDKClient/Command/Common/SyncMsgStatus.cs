using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SDKClient.Model;
using SuperSocket.ClientEngine;

namespace SDKClient.Command
{
    [Export(typeof(CommandBase))]
    class SyncMsgStatus_cmd: CommandBase
    {
        public override string Name
        {
            get => Protocol.ProtocolBase.SyncMsgStatus;
        }
        public override void ExecuteCommand(EasyClientBase session, PackageInfo packageInfo)
        {
            var package = packageInfo as SyncMsgStatusPackage;
            if (package.code == 0)
            {
                if (package.reply == null)
                {
                    if (package.data.partnerId == 0)
                        Util.Helpers.Async.Run(async () => await DAL.DALMessageHelper.UpdateMsgIsRead(package.data.groupId, (int)SDKProperty.chatType.groupChat));
                    else
                        Util.Helpers.Async.Run(async () => await DAL.DALMessageHelper.UpdateMsgIsRead(package.data.partnerId, (int)SDKProperty.chatType.chat));
                }
                else
                    return;
                SDKClient.Instance.OnNewDataRecv(packageInfo);
            }
            base.ExecuteCommand(session, packageInfo);
        }
        protected override void SendCompletedCallBack(bool isRanCompleted, PackageInfo package, EasyClientBase easyClientBase, int expires = 45)
        {
            if (!isRanCompleted)
            {
                SDKClient.Instance.property.SendPackageCache.Enqueue(package);
            }
            base.SendCompletedCallBack(isRanCompleted, package, easyClientBase, expires);
        }
    }
}
