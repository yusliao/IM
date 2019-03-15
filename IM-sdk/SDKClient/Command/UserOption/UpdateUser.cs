using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SDKClient.Model;
using SuperSocket.ClientEngine;
using System.ComponentModel.Composition;
using SDKClient.Protocol;

namespace SDKClient.Command
{
    [Export(typeof(CommandBase))]
    class UpdateUser_cmd : CommandBase
    {
        public override string Name => Protocol.ProtocolBase.UpdateUser;

        public override void ExecuteCommand(EasyClientBase session, PackageInfo packageInfo)
        {
            UpdateuserPackage package = packageInfo as UpdateuserPackage;
            if (package.code == 0)
            {
                switch (package.data.updateType)
                {
                    case (int)UpdateUserOption.修改昵称:
                          SDKClient.Instance.property.CurrentAccount.userName = package.data.content;

                        break;
                    case (int)UpdateUserOption.修改头像:
                        
                         SDKClient.Instance.property.CurrentAccount.photo = package.data.content;
                        
                        
                        break;
                    case (int)UpdateUserOption.修改性别:
                       // SDKClient.Instance.property.CurrentAccount.Sex = package.data.content;

                        break;
                    case (int)UpdateUserOption.修改生日:
                      
                        break;
                    case (int)UpdateUserOption.修改所在地:
                       
                        break;
                    default:
                        break;
                }
                Util.Helpers.Async.Run(async()=>await DAL.DALAccount.UpdateAccount(SDKClient.Instance.property.CurrentAccount));
            }
            SDKClient.Instance.OnNewDataRecv(packageInfo);
            base.ExecuteCommand(session, packageInfo);
        }
    }
}
