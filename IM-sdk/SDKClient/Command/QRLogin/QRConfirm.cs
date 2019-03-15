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
    class QRConfirm_cmd : CommandBase
    {
        public override string Name
        {
            get => Protocol.ProtocolBase.QRConfirm;
        }
        public override void ExecuteCommand(EasyClientBase session, PackageInfo packageInfo)
        {
            if (SDKClient.Instance.property.m_StateCode > ServerStateConst.Starting)
                return;
            SDKClient.Instance.property.m_StateCode = ServerStateConst.Starting;

            QRConfirmPackage package = packageInfo as QRConfirmPackage;
            SDKClient.Instance.property.CurrentAccount.token = package.data.token;
            SDKClient.Instance.property.CurrentAccount.userID = package.data.userId;
            SDKClient.Instance.OnNewDataRecv(packageInfo);
            base.ExecuteCommand(session, packageInfo);
        }
    }
}
