using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SuperSocket.ClientEngine;
using System.ComponentModel.Composition;
using SuperSocket.ClientEngine.Protocol;
using SDKClient.Model;
using SuperSocket.ProtoBase;
using NLog;
using SDKClient.Protocol;
using SDKClient.WebAPI;

namespace SDKClient.Command
{
    [Export(typeof(CommandBase))]
    class Auth_cmd : CommandBase 
    {
        public override string Name
        {
            get => Protocol.ProtocolBase.auth;
            
        }
        private static readonly new  Logger logger = NLog.LogManager.GetCurrentClassLogger();
        public override void ExecuteCommand(EasyClientBase session, PackageInfo packageInfo)
        {
            
            if (packageInfo.code == 0&&packageInfo.syncMsg!=1)
            {
                SDKClient.Instance.property.m_StateCode = ServerStateConst.Running;

                AuthPackage model = packageInfo as AuthPackage;
                
                SDKClient.Instance.property.CurrentAccount.userID = model.data.userId;
                
                
                SDKClient.Instance.property.CurrentAccount.imei = model.data.imei;
             
                DAL.DALSqliteHelper.CreatePersonDB(SDKClient.Instance.property.CurrentAccount.userID);
               
               
                
                Util.Helpers.Async.Run(async () => await DAL.DALAccount.UpdateAccount(SDKClient.Instance.property.CurrentAccount));
                

                if (!SDKClient.Instance.property.FriendApplyList.Any())
                {
                    SDKClient.Instance.property.FriendApplyList = Util.Helpers.Async.Run(async () => await DAL.DALFriendApplyListHelper.GetFriendApplyList());
                }

                //设置连接状态
                SDKClient.Instance.OnSendConnState(true);
               
                SDKClient.Instance.property.RaiseConnEvent = true;
                
            }
           
            SDKClient.Instance.OnNewDataRecv(packageInfo);


        }

       
    }
}
