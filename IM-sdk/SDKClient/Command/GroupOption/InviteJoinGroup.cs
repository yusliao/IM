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
    class InviteJoinGroup_cmd : CommandBase
    {
        public override string Name => Protocol.ProtocolBase.InviteJoinGroup;

        public override void ExecuteCommand(EasyClientBase session, PackageInfo packageInfo)
        {
            
            if (packageInfo.code==0)
            {
                //收到入群申请
                if(packageInfo.from!=SDKClient.Instance.property.CurrentAccount.userID.ToString())
                {
                    InviteJoinGroupPackage package = packageInfo as InviteJoinGroupPackage;
                    Util.Helpers.Async.Run(async () => await DAL.DALGroupOptionHelper.SendMsgtoDB(package));
                }
               
            }
            SDKClient.Instance.OnNewDataRecv(packageInfo);
            base.ExecuteCommand(session, packageInfo);
        }
    }
}
