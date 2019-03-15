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
    class DismissGroup_cmd : CommandBase
    {
        public override string Name => Protocol.ProtocolBase.DismissGroup;

        public override void ExecuteCommand(EasyClientBase session, PackageInfo packageInfo)
        {
            DismissGroupPackage package = packageInfo as DismissGroupPackage;
            if (packageInfo.code == 0)
            {
                //群主本人则删除群的聊天记录
                if (package.data.ownerId == SDKClient.Instance.property.CurrentAccount.userID)
                {
                    Util.Helpers.Async.Run(async () => await DAL.DALMessageHelper.DeleteHistoryMsg(package.data.groupId, SDKProperty.chatType.groupChat));
                    //Util.Helpers.Async.Run(async()=>await DAL.DALGroupOptionHelper.DeleteGroupMemberInfo)
                }
                else
                {
                    Util.Helpers.Async.Run(async () => await DAL.DALGroupOptionHelper.SendMsgtoDB(package));
                    Util.Helpers.Async.Run(async () => await DAL.DALJoinGroupHelper.DeleteJoinGroupItem(package.data.groupId));//删除该群的入群申请列表
                }
                Util.Helpers.Async.Run(async () => await DAL.DALGroupOptionHelper.DeleteGroupListPackage());
                //Util.Helpers.Async.Run(async () => await DAL.DALGroupOptionHelper.DeleteGroupMemberListPackage(package.data.groupId));
               
               // Util.Helpers.Async.Run(async () => await DAL.DALMessageHelper.UpdateMsgIsRead(package.data.groupId, 1));
            }
            SDKClient.Instance.OnNewDataRecv(packageInfo);
            base.ExecuteCommand(session, packageInfo);
        }
    }
}
