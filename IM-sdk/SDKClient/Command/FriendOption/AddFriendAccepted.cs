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
    class AddFriendAccepted_cmd : CommandBase
    {
        public override string Name => Protocol.ProtocolBase.AddFriendAccepted;

        public async override void ExecuteCommand(EasyClientBase session, PackageInfo packageInfo)
        {
            AddFriendAcceptedPackage package = packageInfo as AddFriendAcceptedPackage;
            //接收加好友
            if (packageInfo.code == 0||package.code==(int)Protocol.StatusCode.AlreadyBecomeFriend)
            {
                await DAL.DALContactListHelper.DeleteCurrentContactListPackage();
                if (package.data.userId == SDKClient.Instance.property.CurrentAccount.userID)
                {
                    await DAL.DALFriendApplyListHelper.UpdateItemIsChecked(package.data.friendId);
                    await DAL.DALUserInfoHelper.UpdateItemIsChecked(package.data.friendId);
                    await DAL.DALMessageHelper.SendMsgtoDB(package.id,package.from, package.to, "已经是好朋友，开始聊天吧", package.data.friendId,package.data.userId, SDKProperty.MessageType.addfriendaccepted, SDKProperty.MessageState.isRead);
                    await DAL.DALMessageHelper.UpdateMsgSessionTypeToCommon(package.data.friendId);
                    await DAL.DALStrangerOptionHelper.DeleteStranger(package.data.friendId);
                }
                else
                {
                    await DAL.DALFriendApplyListHelper.UpdateItemIsChecked(package.data.userId);
                    await DAL.DALUserInfoHelper.UpdateItemIsChecked(package.data.userId);
                    if(package.data.type!=1)//服务器代发的同意消息，不需要添加提示
                        await DAL.DALMessageHelper.SendMsgtoDB(package.id, package.from, package.to, "已经是好朋友，开始聊天吧", package.data.userId, package.data.userId, SDKProperty.MessageType.addfriendaccepted, SDKProperty.MessageState.isRead);
                    await DAL.DALMessageHelper.UpdateMsgSessionTypeToCommon(package.data.userId);
                    await DAL.DALStrangerOptionHelper.DeleteStranger(package.data.userId);
                }
                
                
            }
            else if(packageInfo.code==(int)Protocol.StatusCode.AuditFriendApplyError)
            {
                if (package.data.userId == SDKClient.Instance.property.CurrentAccount.userID)
                    await DAL.DALFriendApplyListHelper.DeleteItem(package.data.friendId);
                else
                    await DAL.DALFriendApplyListHelper.DeleteItem(package.data.friendId);
            }

            
            SDKClient.Instance.OnNewDataRecv(packageInfo);
            base.ExecuteCommand(session, packageInfo);
            
        }
      

    }
}
