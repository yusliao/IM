using SDKClient.Model;
using SuperSocket.ClientEngine;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDKClient.Command.CustomOption
{
    [Export(typeof(CommandBase))]
    class CSSyncMsgStatus_cmd : CommandBase
    {
        private System.Collections.Concurrent.ConcurrentDictionary<string, string> sessionDic = new System.Collections.Concurrent.ConcurrentDictionary<string, string>();

        public override string Name
        {
            get => Protocol.ProtocolBase.CSSyncMsgStatus;

        }

        public override void ExecuteCommand(EasyClientBase session, PackageInfo packageInfo)
        {

            SDKClient.Instance.OnNewDataRecv(packageInfo);

            base.ExecuteCommand(session, packageInfo);

        }

        public override void SendCommand(EasyClientBase session, PackageInfo packageinfo)
        {
            CSSyncMsgStatusPackage p = packageinfo as CSSyncMsgStatusPackage;

            base.SendCommand(session, p);
        }
    }
}
