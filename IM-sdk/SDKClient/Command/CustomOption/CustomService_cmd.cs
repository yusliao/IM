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
    class CustomService_cmd: CommandBase
    {
        private  System.Collections.Concurrent.ConcurrentDictionary<string, string> sessionDic = new System.Collections.Concurrent.ConcurrentDictionary<string, string>();
       
        public override string Name
        {
            get => Protocol.ProtocolBase.CustomService;

        }
      
        public override void ExecuteCommand(EasyClientBase session, PackageInfo packageInfo)
        {

            if (packageInfo.code == 0&&packageInfo.reply!=1)
            {

                CustomServicePackage model = packageInfo as CustomServicePackage;
                
                Util.Helpers.Async.Run(async()=>await DAL.DALMessageHelper.RecvMsgtoDB(model));
                if (model.data.type == (int)SDKProperty.customOption.conn)
                {
                    //发送客服确认消息
                    SendConnConfirmed(session, model);

                    sessionDic.AddOrUpdate(model.data.mobile, model.data.sessionId, (k, oldv) => model.data.sessionId);
                    SDKClient.logger.Info($"新的客户接入，mobile:{model.data.mobile}, sessionId:{model.data.sessionId}");
                    Task.Run(async() =>
                    {
                        //服务用户数+1
                        WebAPI.WebAPICallBack.AddSessionItem(model.data.sessionId);
                        //更新用户表

                        await DAL.DALUserInfoHelper.InsertOrUpdateItem(model);
                    });

                }
                else if(model.data.type == (int)SDKProperty.customOption.over)
                {
                    string sId = string.Empty;
                    if(sessionDic.Keys.Any(s=>s.Equals(model.data.mobile)))
                    {
                        sId = sessionDic[model.data.mobile];
                        if (sId.Equals(model.data.sessionId))
                        {
                            sessionDic.TryRemove(model.data.mobile, out sId);
                        }
                        else
                        {
                            base.ExecuteCommand(session, packageInfo);
                            return;
                        }
                    }
                    else
                    {
                        base.ExecuteCommand(session, packageInfo);
                        return;
                    }
                    Task.Run(() =>
                    {
                        logger.Info($"会话结束\t：mobile:{model.data.mobile}, sessionId：{model.data.sessionId}");
                        WebAPICallBack.DiminishingSessionItem(model.data.sessionId);
                    });
                }

                SDKClient.Instance.OnNewDataRecv(packageInfo);

            }

            base.ExecuteCommand(session, packageInfo);

        }

        private static void SendConnConfirmed(EasyClientBase session, CustomServicePackage model)
        {
            CustomServicePackage package = new CustomServicePackage();
            package.id = SDKProperty.RNGId;
            package.from = model.to;
            package.to = model.from;
            package.data = new CustomServicePackage.Data();
            package.data.type = 3;//客服确认
            package.data.sessionId = model.data.sessionId ?? "sessionid:00";
            SDKClient.logger.Info($"SEND:\tsession{SDKClient.Instance.property.CurrentAccount.Session}:\r\n {Util.Helpers.Json.ToJson(package)}");
            package.Send(session);
        }

        public override void SendCommand(EasyClientBase session, PackageInfo packageinfo)
        {
            CustomServicePackage p = packageinfo as CustomServicePackage;
           
            Util.Helpers.Async.Run(async () => await DAL.DALMessageHelper.SendMsgtoDB(p));
            base.SendCommand(session, p);
        }

    }
}
