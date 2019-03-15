using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SDKClient.Model;
using SuperSocket.ClientEngine;

namespace SDKClient.Command.FriendOption
{
    [Export(typeof(CommandBase))]
    class GetFriend_cmd:CommandBase
    {
        public override string Name => Protocol.ProtocolBase.GetFriend;
        public override void ExecuteCommand(EasyClientBase session, PackageInfo packageInfo)
        {
            base.ExecuteCommand(session, packageInfo);
        }
        public override void SendCommand(EasyClientBase session, PackageInfo packageinfo)
        {
            base.SendCommand(session, packageinfo);
        }
    }
}
