using SDKClient.Model;
using SDKClient.WebAPI;
using SuperSocket.ClientEngine;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Util;
using SDKClient.Protocol;
namespace SDKClient.Command.CustomOption
{
    [Export(typeof(CommandBase))]
    class CustomExchange_cmd: CommandBase
    {
        private  System.Collections.Concurrent.ConcurrentDictionary<string, string> sessionDic = new System.Collections.Concurrent.ConcurrentDictionary<string, string>();
       
        public override string Name
        {
            get => Protocol.ProtocolBase.CustomExchange;

        }
      
        public override void ExecuteCommand(EasyClientBase session, PackageInfo packageInfo)
        {

            SDKClient.Instance.OnNewDataRecv(packageInfo);

            base.ExecuteCommand(session, packageInfo);

        }

        public override void SendCommand(EasyClientBase session, PackageInfo packageinfo)
        {
            CustomExchangePackage p = packageinfo as CustomExchangePackage;
           
            base.SendCommand(session, p);
        }

    }
}
