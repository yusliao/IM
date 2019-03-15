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
    class UpdateGroup_cmd : CommandBase
    {
        public override string Name => Protocol.ProtocolBase.UpdateGroup;

        public override void ExecuteCommand(EasyClientBase session, PackageInfo packageInfo)
        {
           
       
            if (packageInfo.code == 0)
            {
                Task.Run(async () => await DAL.DALGroupOptionHelper.DeleteGroupListPackage());
                UpdateGroupPackage package = packageInfo as UpdateGroupPackage;

                Util.Helpers.Async.Run(async () => await DAL.DALGroupOptionHelper.SendMsgtoDB(package));

            }
            SDKClient.Instance.OnNewDataRecv(packageInfo);
            base.ExecuteCommand(session, packageInfo);
        }
    }
}
