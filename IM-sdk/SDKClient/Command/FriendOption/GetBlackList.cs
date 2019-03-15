using SDKClient.Model;
using SDKClient.Protocol;
using SuperSocket.ClientEngine;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDKClient.Command
{
    [Export(typeof(CommandBase))]
    class GetBlackList_cmd:CommandBase
    {
        public override string Name => Protocol.ProtocolBase.GetBlackList;

        public override void ExecuteCommand(EasyClientBase session, PackageInfo packageInfo)
        {
            GetBlackListPackage package = packageInfo as GetBlackListPackage;
           if (package.code == 0 && package.data.total > package.data.max)//继续发送请求
            {
                GetBlackListPackage p = new GetBlackListPackage();
                p.data = new GetBlackListPackage.Data();
                p.from = SDKClient.Instance.property.CurrentAccount.userID.ToString();
                p.id = package.id;
                p.data.userId = SDKClient.Instance.property.CurrentAccount.userID;
                p.data.min = package.data.max + 1;
                p.data.max = p.data.min + 100;
                base.SendCommand(session, package);
            }
            SDKClient.Instance.OnNewDataRecv(packageInfo);
            base.ExecuteCommand(session, packageInfo);
        }
    }
}
