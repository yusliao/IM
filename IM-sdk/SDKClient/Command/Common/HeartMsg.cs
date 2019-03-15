using SDKClient.Model;
using SuperSocket.ClientEngine;

using System.ComponentModel.Composition;


namespace SDKClient.Command
{
    [Export(typeof(CommandBase))]
    class HeartMsg_cmd:CommandBase
    {
        public override string Name
        {
            get => Protocol.ProtocolBase.HeartMsg;

        }
        public override void ExecuteCommand(EasyClientBase session, PackageInfo packageInfo)
        {
            return;
        }
        public override void SendCommand(EasyClientBase session, PackageInfo packageinfo)
        {
            base.SendHeart(session);
        }
    }
}
