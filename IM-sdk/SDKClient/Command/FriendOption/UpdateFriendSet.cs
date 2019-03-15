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
    class UpdateFriendSet_cmd : CommandBase
    {
        public override string Name => Protocol.ProtocolBase.UpdateFriendSet;

        public override void ExecuteCommand(EasyClientBase session, PackageInfo packageInfo)
        {
          
            if (packageInfo.code == 0)
            {
                Util.Helpers.Async.Run(async () => await DAL.DALContactListHelper.DeleteCurrentContactListPackage());
                UpdateFriendSetPackage package = packageInfo as UpdateFriendSetPackage;
                DB.ContactDB contact = Util.Helpers.Async.Run(async () => await DAL.DALUserInfoHelper.Get(package.data.friendId));

                if (contact != null)
                {
                    if (package.data.setType == (int)UpdateFriendSetPackage.FriendSetOption.设置好友备注)
                    {
                        contact.Remark = package.data.content;
                        Util.Helpers.Async.Run(async () => await DAL.DALUserInfoHelper.UpdateRecord(contact));
                    }
                }
            }
            SDKClient.Instance.OnNewDataRecv(packageInfo);
            base.ExecuteCommand(session, packageInfo);
        }
    }
}
