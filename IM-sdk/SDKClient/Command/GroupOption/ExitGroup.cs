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
    class ExitGroup_cmd : CommandBase
    {
        public override string Name => Protocol.ProtocolBase.ExitGroup;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        /// <param name="packageInfo"></param>
        public override void ExecuteCommand(EasyClientBase session, PackageInfo packageInfo)
        {
            ExitGroupPackage package = packageInfo as ExitGroupPackage;
          
            if (packageInfo.code==0)
            {
                if (package.data.userIds.Contains(SDKClient.Instance.property.CurrentAccount.userID))//自己退群
                {
                    if (package.data.adminId == 0)
                    {
                        //删除群的聊天记录
                        Util.Helpers.Async.Run(async () => await DAL.DALMessageHelper.DeleteHistoryMsg(package.data.groupId, SDKProperty.chatType.groupChat));
                        Util.Helpers.Async.Run(async () => await DAL.DALGroupOptionHelper.DeleteGroupListPackage());
                        //  Util.Helpers.Async.Run(async () => await DAL.DALGroupOptionHelper.DeleteGroupMemberListPackage(package.data.groupId));
                        Util.Helpers.Async.Run(async () => await DAL.DALJoinGroupHelper.DeleteJoinGroupItem(package.data.groupId));//删除该群的入群申请列表
                    }
                    else//被T出
                    {
                        Util.Helpers.Async.Run(async () => await DAL.DALGroupOptionHelper.SendMsgtoDB(package));
                        Util.Helpers.Async.Run(async () => await DAL.DALMessageHelper.UpdateMsgIsRead(package.data.groupId,1));
                        Util.Helpers.Async.Run(async () => await DAL.DALJoinGroupHelper.DeleteJoinGroupItem(package.data.groupId));//删除该群的入群申请列表
                    }
                }
                else
                {
                    if (package.data.adminIds!=null&&package.data.adminIds.Contains(SDKClient.Instance.property.CurrentAccount.userID))
                        Util.Helpers.Async.Run(async () => await DAL.DALGroupOptionHelper.SendMsgtoDB(package));
                }
                
            }
            SDKClient.Instance.OnNewDataRecv(packageInfo);


            base.ExecuteCommand(session, packageInfo);
        }
    }
}
