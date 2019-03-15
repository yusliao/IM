using SDKClient.Model;
using SDKClient.Protocol;
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
    class UpdateUserDetail_cmd:CommandBase
    {
        public override string Name => Protocol.ProtocolBase.UpdateUserDetail;

        public override void ExecuteCommand(EasyClientBase session, PackageInfo packageInfo)
        {
            
            SDKClient.Instance.OnNewDataRecv(packageInfo);
            base.ExecuteCommand(session, packageInfo);
        }
    }
}
