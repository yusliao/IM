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
    class ErrorPackage_cmd: CommandBase
    {
        public override string Name
        {
            get => Protocol.ProtocolBase.ErrorPackage;
        }
        public override void ExecuteCommand(EasyClientBase session, PackageInfo packageInfo)
        {
            //业务包解析出错；暂无业务要求
        }
    }
}
