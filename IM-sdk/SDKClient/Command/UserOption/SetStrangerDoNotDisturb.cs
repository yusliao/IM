using SDKClient.Model;
using SuperSocket.ClientEngine;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDKClient.Command.Attention
{
    [Export(typeof(CommandBase))]
    class SetStrangerDoNotDisturb_cmd:CommandBase
    {
        public override string Name => Protocol.ProtocolBase.SetStrangerDoNotDisturb;
        public async override void ExecuteCommand(EasyClientBase session, PackageInfo packageInfo)
        {
            SetStrangerDoNotDisturbPackage package = packageInfo as SetStrangerDoNotDisturbPackage;
            if(package.code==0)
            {
                if (package.data.userId == SDKClient.Instance.property.CurrentAccount.userID)
                    await DAL.DALStrangerOptionHelper.SetStrangerdoNotDisturb(package.data.strangerId, package.data.isNotdisturb);
                else
                    await DAL.DALStrangerOptionHelper.SetStrangerdoNotDisturb(package.data.userId, package.data.isNotdisturb);
            }
            SDKClient.Instance.OnNewDataRecv(packageInfo);
            base.ExecuteCommand(session, packageInfo);

        }
    }
}
