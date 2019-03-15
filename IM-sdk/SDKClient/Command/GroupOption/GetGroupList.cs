using SuperSocket.ClientEngine;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SDKClient.Model;
using ToolGood.Words;

namespace SDKClient.Command
{
    /// <summary>
    /// 获取群组信息
    /// </summary>
    [Export(typeof(CommandBase))]
    
    class GetGroupList_cmd : CommandBase
    {
        public override string Name
        {
            get => Protocol.ProtocolBase.GetgroupList;
            
        }

        public override void ExecuteCommand(EasyClientBase session, PackageInfo packageInfo)
        {
            GetGroupListPackage package = (GetGroupListPackage)packageInfo;

         
            if (packageInfo.code == 0)
            {
                SDKClient.Instance.OnNewDataRecv(packageInfo);
                Util.Helpers.Async.Run(async () =>
                {
                    var pack = Util.Helpers.Json.ToJson(package.data);
                    var md5 = Util.Helpers.Encrypt.Md5By32(pack);
                    var dbobj = await DAL.DALGroupOptionHelper.GetGroupListPackage();
                    if (dbobj == null)
                    {
                        DB.groupListDB db = new DB.groupListDB();
                        db.getGroupListPackage = Util.Helpers.Json.ToJson(package);
                        db.MD5 = md5;
                        await SDKProperty.SQLiteConn.InsertAsync(db);
                        await DAL.DALGroupOptionHelper.InsertGroupInfo(package);

                    }
                    else if (dbobj.MD5 == md5)
                    {
                        return;
                    }
                    else
                    {
                        dbobj.getGroupListPackage = Util.Helpers.Json.ToJson(package);
                        dbobj.MD5 = md5;

                        await SDKProperty.SQLiteConn.UpdateAsync(dbobj);
                        await DAL.DALGroupOptionHelper.UpdateGroupInfo(package);

                    }
                });


                SDKClient.Instance.property.CurrentAccount.GroupCount = package.data.items.adminGroup.Count + package.data.items.joinGroup.Count + package.data.items.ownerGroup.Count;
              
            }
           
            #region 继续请求
            //if (package.code == 0 && package.data.total > package.data.max)//继续发送请求
            //{
            //    var to = package.from;
            //    package.from = package.to;
            //    package.to = to;
            //    package.data.min = package.data.max;
            //    package.data.max = package.data.total;
            //    base.SendCommand(session, package);
            //}
            //else
            //{

            //}
            #endregion

           
            base.ExecuteCommand(session, packageInfo);
        }
        public override void SendCommand(EasyClientBase session, PackageInfo packageinfo)
        {
            base.SendCommand(session, packageinfo);
            var card = SDKClient.Instance.property.CurrentAccount.imei ?? System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(N => N.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback)?.GetPhysicalAddress().ToString();

            if (SDKClient.Instance.property.CurrentAccount.preimei == card)
            {
                var dbobj = Util.Helpers.Async.Run(async () => await DAL.DALGroupOptionHelper.GetGroupListPackage());
                if (dbobj != null)
                {
                    var package = Util.Helpers.Json.ToObject<GetGroupListPackage>(dbobj.getGroupListPackage);

                    SDKClient.Instance.OnNewDataRecv(package);

                }

            }
        }


    }
}
