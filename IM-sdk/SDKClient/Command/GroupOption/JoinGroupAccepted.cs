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
    class JoinGroupAccepted_cmd : CommandBase
    {
        public override string Name => Protocol.ProtocolBase.JoinGroupAccepted;

        public override void ExecuteCommand(EasyClientBase session, PackageInfo packageInfo)
        {
            JoinGroupAcceptedPackage package = packageInfo as JoinGroupAcceptedPackage;
          
            if (packageInfo.code == 0)
            {
               // Task.Run(async () => await DAL.DALGroupOptionHelper.DeleteGroupMemberListPackage(package.data.groupId));
                //TODO:删除群申请列表中申请记录
                Util.Helpers.Async.Run(async()=>await DAL.DALJoinGroupHelper.DeleteJoinGroupItem(packageInfo as JoinGroupAcceptedPackage));
                //入群通知消息入库
                if(package.data.auditStatus==1)
                    Util.Helpers.Async.Run(async () => await DAL.DALGroupOptionHelper.SendMsgtoDB(package));
            }
            else if (packageInfo.code == (int)Protocol.StatusCode.UserIsGroupMember|| packageInfo.code == (int)Protocol.StatusCode.AlreadyCompleted)
            {
                if (!Util.Helpers.Async.Run(async () => await DAL.DALJoinGroupHelper.DeleteJoinGroupItem(packageInfo as JoinGroupAcceptedPackage)))
                    logger.Error($"删除入群申请记录失败：{package.ToString()}");
               
            }
            SDKClient.Instance.OnNewDataRecv(packageInfo);
            base.ExecuteCommand(session, packageInfo);
        }
    }
}
