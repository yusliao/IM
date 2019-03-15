
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SDKClient.Model;
using SuperSocket.ClientEngine;
using SDKClient.Protocol;
using System.ComponentModel.Composition;

namespace SDKClient.Command
{
    [Export(typeof(CommandBase))]
    class GetAttentionList_cmd :CommandBase
    {
        public override string Name => Protocol.ProtocolBase.GetAttentionList;
        public override void ExecuteCommand(EasyClientBase session, PackageInfo packageInfo)
        {
            SDKClient.Instance.OnNewDataRecv(packageInfo);
            base.ExecuteCommand(session, packageInfo);
        }
    }
}