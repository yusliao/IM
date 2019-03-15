using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SDKClient.Model;
using SuperSocket.ClientEngine;
using System.ComponentModel.Composition;
using SDKClient.Protocol;

namespace SDKClient.Command
{
    [Export(typeof(CommandBase))]
    class DeleteFriend_cmd : CommandBase
    {
        public override string Name => Protocol.ProtocolBase.DeleteFriend;

        public override void ExecuteCommand(EasyClientBase session, PackageInfo packageInfo)
        {
            DeleteFriendPackage package = packageInfo as DeleteFriendPackage;
            if(package.code==0)
            {
                if (package.from == SDKClient.Instance.property.CurrentAccount.userID.ToString())
                {
                    if(package.data.type==0)
                        Util.Helpers.Async.Run(async () => await DAL.DALMessageHelper.DeleteHistoryMsg(package.data.friendId, SDKProperty.chatType.chat));

                    Util.Helpers.Async.Run(async () => await DAL.DALContactListHelper.DeleteCurrentContactListPackage());
                    Util.Helpers.Async.Run(async () => await DAL.DALUserInfoHelper.DeleteItem(package.data.friendId));
                }
            }

            SDKClient.Instance.OnNewDataRecv(packageInfo);
            base.ExecuteCommand(session, packageInfo);
        }
    }
}
