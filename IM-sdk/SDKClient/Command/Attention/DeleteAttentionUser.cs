using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SDKClient.Model;
using SuperSocket.ClientEngine;
using SDKClient.Protocol;
using System.ComponentModel.Composition;

namespace SDKClient.Command
{
    [Export(typeof(CommandBase))]
    class DeleteAttentionUser:CommandBase
    {
        public override string Name => ProtocolBase.DeleteAttentionUser;
        public override void ExecuteCommand(EasyClientBase session, PackageInfo packageInfo)
        {
            if (packageInfo.code != 0)
            {
                packageInfo.ErrorLog();
            }
            SDKClient.Instance.OnNewDataRecv(packageInfo);
            base.ExecuteCommand(session, packageInfo);
        }
    }
}
