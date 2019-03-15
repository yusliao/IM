using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SuperSocket.ClientEngine.Protocol;

using System.ComponentModel.Composition;
using SuperSocket.ClientEngine;
using SDKClient.Model;
using ToolGood.Words;
using System.IO;

namespace SDKClient.Command
{
    [Export(typeof(CommandBase))]
    class GetContactsList_cmd : CommandBase
    {
        public override string Name
        {
            get => Protocol.ProtocolBase.GetContactsList;
            
        }

        public override void ExecuteCommand(EasyClientBase session, PackageInfo packageInfo)
        {
            
            GetContactsListPackage package = (GetContactsListPackage)packageInfo;
            SDKClient.Instance.OnNewDataRecv(packageInfo);
            if (packageInfo.code == 0)
            {
                Util.Helpers.Async.Run(async () =>
                {

                    var pack = Util.Helpers.Json.ToJson(package.data);
                    var md5 = Util.Helpers.Encrypt.Md5By32(pack);
                    var dbobj = await DAL.DALContactListHelper.GetCurrentContactListPackage();
                    if (dbobj == null)
                    {
                        DB.contactListDB db = new DB.contactListDB();
                        db.getContactsListPackage = Util.Helpers.Json.ToJson(package);
                        db.MD5 = md5;
                        try
                        {
                            await SDKProperty.SQLiteConn.InsertAsync(db);
                            //await DAL.DALUserInfoHelper.DeleteRecords();
                            await DAL.DALUserInfoHelper.InsertContactDB(package);
                        }
                        catch (Exception ex)
                        {
                            SDKClient.logger.Error($"消息处理异常：error:{ex.Message},stack:{ex.StackTrace};\r\n contactListDB:{Util.Helpers.Json.ToJson(db)}");
                        }
                    }
                    else if (dbobj.MD5 == md5)
                    {
                        return;
                    }
                    else
                    {
                        dbobj.getContactsListPackage = Util.Helpers.Json.ToJson(package);
                        dbobj.MD5 = md5;
                        try
                        {
                            await SDKProperty.SQLiteConn.UpdateAsync(dbobj);
                            //await DAL.DALUserInfoHelper.DeleteRecords();
                            await DAL.DALUserInfoHelper.InsertContactDB(package);
                        }
                        catch (Exception ex)
                        {
                            SDKClient.logger.Error($"消息处理异常：error:{ex.Message},stack:{ex.StackTrace};\r\n contactListDB:{Util.Helpers.Json.ToJson(dbobj)}");
                        }
                    }




                });
            }
            base.ExecuteCommand(session, packageInfo);

        }
        public override void SendCommand(EasyClientBase session, PackageInfo packageinfo)
        {
            var card = SDKClient.Instance.property.CurrentAccount.imei ?? System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(N => N.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback)?.GetPhysicalAddress().ToString();

            if (SDKClient.Instance.property.CurrentAccount.preimei == card)
            {
                var dbobj = Util.Helpers.Async.Run(async () => await DAL.DALContactListHelper.GetCurrentContactListPackage());
                if (dbobj != null)
                {
                    var package = Util.Helpers.Json.ToObject<GetContactsListPackage>(dbobj.getContactsListPackage);

                    SDKClient.Instance.OnNewDataRecv(package);

                }
            }
            base.SendCommand(session, packageinfo);
        }
    }
}
