using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SDKClient.Model;
using SuperSocket.ClientEngine;
using System.ComponentModel.Composition;

namespace SDKClient.Command
{
    [Export(typeof(CommandBase))]
    class SearchNewFriend_cmd:CommandBase
    {
        public override string Name => Protocol.ProtocolBase.SearchNewFriend;
        public override void ExecuteCommand(EasyClientBase session, PackageInfo packageInfo)
        {

            SDKClient.Instance.OnNewDataRecv(packageInfo);
            base.ExecuteCommand(session, packageInfo);
        }
    }
}
