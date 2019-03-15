using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SDKClient.Model;
using SuperSocket.ClientEngine;
using System.ComponentModel.Composition;

namespace SDKClient.Command
{
    [Export(typeof(CommandBase))]
    class GetGroupMemberList_cmd : CommandBase
    {
        
        public override string Name => Protocol.ProtocolBase.GetgroupMemberList;
        
        public override void ExecuteCommand(EasyClientBase session, PackageInfo packageInfo)
        {
            GetGroupMemberListPackage package = (GetGroupMemberListPackage)packageInfo;
            if (packageInfo.code == 0)
            {
                SDKClient.Instance.OnNewDataRecv(package);
                Util.Helpers.Async.Run(async () =>
                {
                    var items = package.data.items.ToList();
                    var pack = Util.Helpers.Json.ToJson(items);
                    var md5 = Util.Helpers.Encrypt.Md5By32(pack);
                    var dbobj = await DAL.DALGroupOptionHelper.GetGroupMemberListPackage(package.data.groupId);
                    if (dbobj == null)
                    {
                        
                        DB.groupMemberListDB db = new DB.groupMemberListDB();
                        db.getGroupMemberListPackage = Util.Helpers.Json.ToJson(package);
                        db.MD5 = md5;
                        db.groupId = package.data.groupId;
                        await SDKProperty.SQLiteConn.InsertAsync(db);
                        await DAL.DALGroupOptionHelper.InsertGroupMemberInfo(package);

                    }
                    else if (dbobj.MD5 == md5)
                    {
                        return;
                    }
                    else
                    {
                        dbobj.getGroupMemberListPackage = Util.Helpers.Json.ToJson(package);
                        dbobj.MD5 = md5;
                        await SDKProperty.SQLiteConn.UpdateAsync(dbobj);
                        //await DAL.DALGroupOptionHelper.DeleteGroupMemberInfo();
                        await DAL.DALGroupOptionHelper.InsertGroupMemberInfo(package);

                    }
                });
            }
          
            System.Threading.Interlocked.Increment(ref SDKClient.Instance.property.CurrentAccount.curGroupCount);
           
            base.ExecuteCommand(session, packageInfo);
        }
    }
}
