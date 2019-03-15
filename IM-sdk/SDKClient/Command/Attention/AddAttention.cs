using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SDKClient.Model;
using SuperSocket.ClientEngine;
using System.ComponentModel.Composition;

namespace SDKClient.Command.Attention
{
    [Export(typeof(CommandBase))]
    class AddAttention_cmd:CommandBase
    {
        public override string Name => Protocol.ProtocolBase.AddAttention;
        public override void ExecuteCommand(EasyClientBase session, PackageInfo packageInfo)
        {
            SDKClient.Instance.OnNewDataRecv(packageInfo);
            base.ExecuteCommand(session, packageInfo);

        }
    }
}
