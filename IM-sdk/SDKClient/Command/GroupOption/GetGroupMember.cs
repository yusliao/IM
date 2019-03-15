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
    class GetGroupMember_cmd : CommandBase
    {
        public override string Name => Protocol.ProtocolBase.GetgroupMember;

        public override void ExecuteCommand(EasyClientBase session, PackageInfo packageInfo)
        {
           
            SDKClient.Instance.OnNewDataRecv(packageInfo);
            if (packageInfo.code == 0)
            {
                GetGroupMemberPackage package = packageInfo as GetGroupMemberPackage;

                Util.Helpers.Async.Run(async () => await DAL.DALGroupOptionHelper.UpdateGroupMemberInfo(package));
            }
            base.ExecuteCommand(session, packageInfo);
        }
    }
}
