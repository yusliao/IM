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
    class AddFriend_cmd : CommandBase
    {
        public override string Name => Protocol.ProtocolBase.AddFriend;

        public override void ExecuteCommand(EasyClientBase session, PackageInfo packageInfo)
        {
            AddFriendPackage package = packageInfo as AddFriendPackage;
            //收到对方的好友申请
            if(package.code==0&&package.data.userId!= SDKClient.Instance.property.CurrentAccount.userID)
            {
                Util.Helpers.Async.Run(async()=>await DAL.DALFriendApplyListHelper.InsertOrUpdateItem(package));
                Util.Helpers.Async.Run(async () => await DAL.DALUserInfoHelper.InsertOrUpdateItem(package));
                SDKClient.Instance.property.FriendApplyList = Util.Helpers.Async.Run(async () => await DAL.DALFriendApplyListHelper.GetFriendApplyList());
            }
            SDKClient.Instance.OnNewDataRecv(packageInfo);
            base.ExecuteCommand(session, packageInfo);
        }
        public override void SendCommand(EasyClientBase session, PackageInfo packageinfo)
        {
            AddFriendPackage p = packageinfo as AddFriendPackage;
            Util.Helpers.Async.Run(async()=> await DAL.DALUserInfoHelper.InsertOrUpdateItem(p));
            base.SendCommand(session, packageinfo);
        }
    }
}
