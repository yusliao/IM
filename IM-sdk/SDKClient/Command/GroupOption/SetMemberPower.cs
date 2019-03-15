using SDKClient.Model;
using SuperSocket.ClientEngine;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDKClient.Command
{
    [Export(typeof(CommandBase))]
    class SetMemberPower_cmd:CommandBase
    {
        public override string Name => Protocol.ProtocolBase.SetMemberPower;

        public override void ExecuteCommand(EasyClientBase session, PackageInfo packageInfo)
        {
            SetMemberPowerPackage package = packageInfo as SetMemberPowerPackage;
            if (packageInfo.code == 0)
            {
                Util.Helpers.Async.Run(async () => await DAL.DALGroupOptionHelper.DeleteGroupListPackage());
                Util.Helpers.Async.Run(async () => await DAL.DALGroupOptionHelper.SendMsgtoDB(package));
            }
            SDKClient.Instance.OnNewDataRecv(packageInfo);
            base.ExecuteCommand(session, packageInfo);
        }
    }
}
