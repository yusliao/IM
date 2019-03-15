using SDKClient.Model;
using SuperSocket.ClientEngine;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDKClient.Command.FriendOption
{
    [Export(typeof(CommandBase))]
    
    class RequestIp_cmd:CommandBase
    {
        public override string Name => Protocol.ProtocolBase.RequestIp;

        public override void ExecuteCommand(EasyClientBase session, PackageInfo packageInfo)
        {
            RequestIpPackage package = packageInfo as RequestIpPackage;
            //收到对方的P2P请求
            if (package.code == 0 && package.from != SDKClient.Instance.property.CurrentAccount.userID.ToString())
            {
                /*建立处理对象
                 * 初始化处理
                 * 回包处理
                 */

               
            }

            base.ExecuteCommand(session, packageInfo);
        }
    }
}
