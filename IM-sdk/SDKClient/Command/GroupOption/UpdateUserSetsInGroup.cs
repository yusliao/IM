using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SDKClient.Model;
using SuperSocket.ClientEngine;

namespace SDKClient.Command
{
    [Export(typeof(CommandBase))]
    class UpdateUserSetsInGroup_cmd : CommandBase
    {
        public override string Name => Protocol.ProtocolBase.UpdateUserSetsInGroup;
        public override void ExecuteCommand(EasyClientBase session, PackageInfo packageInfo)
        {
            UpdateUserSetsInGroupPackage package = packageInfo as UpdateUserSetsInGroupPackage;
            if(packageInfo.code==0)
            {
             //   Task.Run(async () => await DAL.DALGroupOptionHelper.DeleteGroupMemberListPackage(package.data.groupId));
                Util.Helpers.Async.Run(async () => await DAL.DALGroupOptionHelper.DeleteGroupListPackage());
                if(package.data.setType == 1)
                {
                    var member = Util.Helpers.Async.Run(async () => await DAL.DALGroupOptionHelper.GetGroupMemberInfo(package.data.userId, package.data.groupId));
                    if(member!=null&&member.userId!=0)
                    {
                        member.memoInGroup = package.data.content;
                        Util.Helpers.Async.Run(async () => await DAL.DALGroupOptionHelper.UpdateGroupMemberInfo(member));
                    }
                }

            }
            SDKClient.Instance.OnNewDataRecv(packageInfo);
            base.ExecuteCommand(session, packageInfo);
        }
    }
}
