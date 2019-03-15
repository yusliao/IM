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
    class GetUser_cmd:CommandBase
    {
        public override string Name => Protocol.ProtocolBase.GetUser;
        public override void ExecuteCommand(EasyClientBase session, PackageInfo packageInfo)
        {
            if (packageInfo.code == 0)
            {
               
                GetUserPackage package = packageInfo as GetUserPackage;
                if (package.data.user != null && package.data.user.userId == SDKClient.Instance.property.CurrentAccount.userID)
                {
                    SDKClient.Instance.property.CurrentAccount.photo = package.data.user.photo;
                    SDKClient.Instance.property.CurrentAccount.userName = package.data.user.userName;
                    SDKClient.Instance.property.CurrentAccount.Sex = package.data.user.sex;
                    SDKClient.Instance.property.CurrentAccount.Province = package.data.user.areaAName;
                    SDKClient.Instance.property.CurrentAccount.City = package.data.user.areaBName;

                    Util.Helpers.Async.Run(async()=>await DAL.DALAccount.UpdateAccount(SDKClient.Instance.property.CurrentAccount));

                }
                else
                {
                    if (package.data.user == null) return;
                    DB.ContactDB contact = Util.Helpers.Async.Run(async () => await DAL.DALUserInfoHelper.Get(package.data.user.userId));
                    if (contact == null)
                        contact = new DB.ContactDB();
                    contact.HeadImgMD5 = package.data.user.photo;
                    contact.NickName = package.data.user.userName;
                    contact.UserId = package.data.user.userId;
                    contact.Area = package.data.user.areaAName + package.data.user.areaBName;
                    contact.Sex = package.data.user.sex;
                    contact.Mobile = package.data.user.mobile;
                    contact.KfId = package.data.user.kfId;
                    contact.haveModifiedKfid = package.data.user.haveModifiedKfid;
                    contact.Remark = package.data.user.partnerRemark;
                    Util.Helpers.Async.Run(async () => await DAL.DALUserInfoHelper.UpdateRecord(contact));

                }
                SDKClient.Instance.OnNewDataRecv(packageInfo);


            }
            base.ExecuteCommand(session, packageInfo);

        }
      
       


    }
}
