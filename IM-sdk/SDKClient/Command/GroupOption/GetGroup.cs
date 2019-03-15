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
    class GetGroup_cmd : CommandBase
    {
        public override string Name => Protocol.ProtocolBase.GetGroup;

        public override void ExecuteCommand(EasyClientBase session, PackageInfo packageInfo)
        {
           
            SDKClient.Instance.OnNewDataRecv(packageInfo);
            base.ExecuteCommand(session, packageInfo);
        }
        public override void SendCommand(EasyClientBase session, PackageInfo packageinfo)
        {
            Util.Helpers.Async.Run(async () =>
            {
                GetGroupPackage p = packageinfo as GetGroupPackage;
                if(p == null)
                {
                    return;
                }
                var obj = await DAL.DALGroupOptionHelper.GetGroupInfo(p.data.groupId);
                if (obj == null || string.IsNullOrEmpty(obj.getGroupPackage))
                {
                    return;
                }
                else
                {
                    GetGroupPackage t = Util.Helpers.Json.ToObject<GetGroupPackage>(obj.getGroupPackage);
                    SDKClient.Instance.OnNewDataRecv(t);
                }
            });
            base.SendCommand(session, packageinfo);
        }
    }
}
