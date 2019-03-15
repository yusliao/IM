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
    class JoinGroup_cmd : CommandBase
    {
        public override string Name => Protocol.ProtocolBase.JoinGroup;

        public override void ExecuteCommand(EasyClientBase session, PackageInfo packageInfo)
        {
            
            JoinGroupPackage package = packageInfo as JoinGroupPackage;
          
            if (packageInfo.code==0)
            {
                //管理员收到入群申请
                if (packageInfo.from != SDKClient.Instance.property.CurrentAccount.userID.ToString())
                {
                    Util.Helpers.Async.Run(async () => await DAL.DALJoinGroupHelper.RecvJoinGroup(packageInfo as JoinGroupPackage));
                    Util.Helpers.Async.Run(async () => await DAL.DALJoinGroupHelper.SendMsgtoDB(package));
                }
                else
                {
                    if (package.data.isAccepted)
                    {
                        Util.Helpers.Async.Run(async () => await DAL.DALJoinGroupHelper.SendMsgtoDB(package));
                        Util.Helpers.Async.Run(async () => await DAL.DALJoinGroupHelper.DeleteJoinGroupItem(package));
                    }
                }

            }
            else if(packageInfo.code == (int)Protocol.StatusCode.UserIsGroupMember|| packageInfo.code == (int)Protocol.StatusCode.AlreadyCompleted)
            {
                
                Util.Helpers.Async.Run(async () => await DAL.DALJoinGroupHelper.DeleteJoinGroupItem(package));
            }
           
           // Util.Helpers.Async.Run(async () => await DAL.DALJoinGroupHelper.SendMsgtoDB(package));

            SDKClient.Instance.OnNewDataRecv(packageInfo);
            base.ExecuteCommand(session, packageInfo);
        }
    }
}
