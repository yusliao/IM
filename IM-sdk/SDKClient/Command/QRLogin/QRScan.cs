using SDKClient.Model;
using SuperSocket.ClientEngine;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDKClient.Command.QRLogin
{
    [Export(typeof(CommandBase))]
    class QRScan_cmd : CommandBase
    {
        public override string Name
        {
            get => Protocol.ProtocolBase.QRScan;
        }
        public override void ExecuteCommand(EasyClientBase session, PackageInfo packageInfo)
        {
            SDKClient.Instance.OnNewDataRecv(packageInfo);
            base.ExecuteCommand(session, packageInfo);
        }
    }
}
