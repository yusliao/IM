using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SDKClient.Model;
using SuperSocket.ClientEngine;

namespace SDKClient.Command
{
    [Export(typeof(CommandBase))]
    class MessageConfirm:CommandBase
    {
        public override string Name => Protocol.ProtocolBase.MessageConfirm;
        public override void ExecuteCommand(EasyClientBase session, PackageInfo packageInfo)
        {
            base.ExecuteCommand(session, packageInfo);
        }
        public override void SendCommand(EasyClientBase session, PackageInfo packageinfo)
        {
            base.SendCommand(session, packageinfo);
            logger.Info($"MessageConfirm-session:{SDKClient.Instance.property.CurrentAccount.Session},id:{packageinfo.id}\r\n{packageinfo.ToString()}");
        }
    }
}
