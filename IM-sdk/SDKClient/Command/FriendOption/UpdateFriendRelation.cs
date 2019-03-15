using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SDKClient.Model;
using SuperSocket.ClientEngine;
using SDKClient.Protocol;
using System.ComponentModel.Composition;

namespace SDKClient.Command.FriendOption
{
    [Export(typeof(CommandBase))]
    class UpdateFriendRelation_cmd:CommandBase
    {
        public override string Name => Protocol.ProtocolBase.UpdateFriendRelation;
        public override void ExecuteCommand(EasyClientBase session, PackageInfo packageInfo)
        {
            SDKClient.Instance.OnNewDataRecv(packageInfo);
            if(packageInfo.code==0)
            {
                Util.Helpers.Async.Run(async () => await DAL.DALContactListHelper.DeleteCurrentContactListPackage());
            }
            base.ExecuteCommand(session, packageInfo);
        }
    }
}
