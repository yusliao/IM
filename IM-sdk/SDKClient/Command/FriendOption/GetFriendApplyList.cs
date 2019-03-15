using SDKClient.Model;
using SDKClient.Protocol;
using SuperSocket.ClientEngine;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDKClient.Command.FriendOption
{
    [Export(typeof(CommandBase))]
    class GetFriendApplyList_cmd:CommandBase
    {
        public override string Name => Protocol.ProtocolBase.GetFriendApplyList;
        public override void ExecuteCommand(EasyClientBase session, PackageInfo packageInfo)
        {
            GetFriendApplyListPackage package = packageInfo as GetFriendApplyListPackage;
            if (packageInfo.code == 0)
            {
                //Util.Helpers.Async.Run(async () =>
                //{
                //    var pack = Util.Helpers.Json.ToJson(package.data);
                //    var md5 = Util.Helpers.Encrypt.Md5By32(pack);
                //    var dbobj = await DAL.DALFriendApplyListHelper.GetFriendApplyList();
                //    if (dbobj == null)
                //    {
                //        DB.friendApplyList db = new DB.friendApplyList();
                //        db.getFriendApplyListPackage = Util.Helpers.Json.ToJson(package);
                //        db.MD5 = md5;
                //        SDKClient.Instance.OnNewDataRecv(packageInfo);
                //        await SDKProperty.SQLiteConn.InsertAsync(db);

                //    }
                //    else if (dbobj.MD5 == md5)
                //    {
                //        return;
                //    }
                //    else
                //    {
                //        dbobj.getFriendApplyListPackage = Util.Helpers.Json.ToJson(package);
                //        dbobj.MD5 = md5;
                //        SDKClient.Instance.OnNewDataRecv(packageInfo);
                //        await SDKProperty.SQLiteConn.UpdateAsync(dbobj);
                //    }
                //});
            }
            SDKClient.Instance.OnNewDataRecv(packageInfo);
            base.ExecuteCommand(session, packageInfo);
        }
    }
}
