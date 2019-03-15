using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SDKClient.Model;
using SDKClient.WebAPI;
using SuperSocket.ClientEngine;

namespace SDKClient.Command
{
    [Export(typeof(CommandBase))]
    class SysNotify_cmd: CommandBase
    {
        public override string Name
        {
            get => Protocol.ProtocolBase.SysNotify;

        }
        public override void ExecuteCommand(EasyClientBase session, PackageInfo packageInfo)
        {
            var package = packageInfo as SysNotifyPackage;
            if (package.reply == 1)
                return;
            //   Util.Helpers.Async.Run(async () => await DAL.DALMessageHelper.RecvMsgtoDB(model));
           
            if (package.data.type==3&&package.data.subType==-100)
            {
                Task.Run(() =>
                {
                    logger.Info($"会话结束\t：sessionId：{package.data.body}");
                   
                });
            }
            ///  101 –帐户封停
            ///   102 –帐户解封
            ///   103-禁止发消息 
            ///   104-解禁发消息
            ///   105-禁止发动态
            /// 106解禁发动态
            /// 107 用户登录官网通知
            /// 108用户退出官网通知
            if (package.data.type==1&& package.data.subType==103)
            {
                SDKClient.Instance.property.CurrentAccount.Isforbidden = true;
                fengjinBody body = new fengjinBody() { disableTime = package.data.body.disableTime, userId = package.data.body.userId };
                var removeGagTime = TimeSpan.FromSeconds(body.disableTime);
                SDKClient.Instance.property.CurrentAccount.removeGagTime = body.disableTime;
                logger.Info($"你被禁言{removeGagTime.TotalMinutes}分钟");
                Task.Delay(removeGagTime).ContinueWith(t =>
                {
                    logger.Info($"禁言结束");
                    SDKClient.Instance.property.CurrentAccount.Isforbidden = false;
                });
            }
            if (package.data.type == 1 && package.data.subType == 104)
                SDKClient.Instance.property.CurrentAccount.Isforbidden = false;


            SDKClient.Instance.OnNewDataRecv(packageInfo);
           

            base.ExecuteCommand(session, packageInfo);
        }
    }
}
