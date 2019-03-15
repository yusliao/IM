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
    class ForceExit_cmd : CommandBase
    {
        public override string Name
        {
            get => Protocol.ProtocolBase.ForceExit;
        }
        public override void ExecuteCommand(EasyClientBase session, PackageInfo packageInfo)
        {
            base.ExecuteCommand(session, packageInfo);
            SDKClient.Instance.OnNewDataRecv(packageInfo);
            
        }
    }
}
