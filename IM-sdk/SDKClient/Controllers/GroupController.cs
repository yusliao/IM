using SDKClient.Model;
using SDKClient.Protocol;
using SDKClient.WebAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDKClient.Controllers
{
    class GroupController
    {
        private static Util.Logs.ILog logger = Util.Logs.Log.GetLog(typeof(GroupController));
        public static void GetJoinGroupList(int groupId)
        {
            var lst = Util.Helpers.Async.Run(async () => await DAL.DALJoinGroupHelper.GetJoinGroupList(groupId).ConfigureAwait(false));
            foreach (var item in lst)
            {
                var obj = Util.Helpers.Json.ToObject<JoinGroupPackage>(item.JoinGroupPackage);
                SDKClient.Instance.OnNewDataRecv(obj);
            }
        }
        public string GetGroupList()
        {
            var card = SDKClient.Instance.property.CurrentAccount.imei ?? System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(N => N.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback)?.GetPhysicalAddress().ToString();

            if (SDKClient.Instance.property.CurrentAccount.preimei == card)
            {
                var dbobj = Util.Helpers.Async.Run(async () => await DAL.DALGroupOptionHelper.GetGroupListPackage());
                if (dbobj != null && dbobj.getGroupListPackage != null)
                {
                    var groupListPackage = Util.Helpers.Json.ToObject<GetGroupListPackage>(dbobj.getGroupListPackage);
                    SDKClient.Instance.OnNewDataRecv(groupListPackage);
                }

            }
            GetGroupListPackage package = new GetGroupListPackage();
            package.ComposeHead(SDKClient.Instance.property.ServerJID, SDKClient.Instance.property.CurrentAccount.userID.ToString());
            package.data = new grouplist()
            {
                max = 100,
                min = 1,
                groupType = 0, //普通群
                userId = SDKClient.Instance.property.CurrentAccount.userID

            };
            var obj = IMRequest.GetGroupListPackage(package);
            if (obj != null && obj.code == 0)
            {
                var groupListPackage = obj;
                if (groupListPackage != null)
                {
                    try
                    {
                        var cmd = SDKClient.Instance.CommmandSet.FirstOrDefault(c => c.Name == groupListPackage.api);
                        cmd?.ExecuteCommand(SDKClient.Instance.ec, groupListPackage);//日志及入库操作

                    }
                    catch (Exception ex)
                    {
                        logger.Error($"获取群列表数据处理异常：error:{ex.Message},stack:{ex.StackTrace};\r\ncontent:{Util.Helpers.Json.ToJson(groupListPackage)}");

                        System.Threading.Interlocked.CompareExchange(ref SDKClient.Instance.property.CanHandleMsg, 2, 1);
                        logger.Info("CanHandleMsg 值修改为:2");
                    }
                }
            }
            else
            {

            }
            //package.Send(ec);
            return package.id;
        }
        /// <summary>
        /// 获取单个群成员信息
        /// </summary>
        /// <param name="userId">查看着ID</param>
        /// <param name="groupId">群ID</param>
        /// <param name="partnerId">被查看着ID</param>
        /// <returns></returns>
        public string GetGroupMember(int userId, int groupId, int partnerId)
        {

            GetGroupMemberPackage package = new GetGroupMemberPackage();
            package.ComposeHead(SDKClient.Instance.property.ServerJID, SDKClient.Instance.property.CurrentAccount.userID.ToString());

            package.data = new GetGroupMemberPackage.Data
            {
                groupId = groupId,
                userId = userId,
                partnerId = partnerId
            };
            package.Send(SDKClient.Instance.ec);
            return package.id;
        }
        public string GetGroupMemberList(int groupId, bool isLoaclData = false)
        {

            //var dbobj = Util.Helpers.Async.Run(async () => await DAL.DALGroupOptionHelper.GetGroupListPackage());

            var card = SDKClient.Instance.property.CurrentAccount.imei ?? System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(N => N.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback)?.GetPhysicalAddress().ToString();


            if (isLoaclData && SDKClient.Instance.property.CurrentAccount.preimei == card)
            {
                var dbobj = Util.Helpers.Async.Run(async () => await DAL.DALGroupOptionHelper.GetGroupMemberListPackage(groupId));
                if (dbobj != null && dbobj.getGroupMemberListPackage != null)
                {
                    var groupListPackage = Util.Helpers.Json.ToObject<GetGroupMemberListPackage>(dbobj.getGroupMemberListPackage);
                    SDKClient.Instance.OnNewDataRecv(groupListPackage);
                }
            }
            GetGroupMemberListPackage package = new GetGroupMemberListPackage();
            package.ComposeHead(SDKClient.Instance.property.ServerJID, SDKClient.Instance.property.CurrentAccount.userID.ToString());
            package.data = new groupmemberlist()
            {
                max = 100,
                min = 1,
                groupId = groupId //普通群
            };
            var obj = IMRequest.GetMemberList(package);
            if (obj != null)
            {
                var groupMemberListPackage = obj;
                if (groupMemberListPackage != null)
                {
                    try
                    {
                        var cmd = SDKClient.Instance.CommmandSet.FirstOrDefault(c => c.Name == groupMemberListPackage.api);
                        cmd?.ExecuteCommand(SDKClient.Instance.ec, groupMemberListPackage);//日志及入库操作

                    }
                    catch (Exception ex)
                    {
                        logger.Error($"获取群成员数据处理异常：error:{ex.Message},stack:{ex.StackTrace};\r\ncontent:{Util.Helpers.Json.ToJson(groupMemberListPackage)}");

                        System.Threading.Interlocked.CompareExchange(ref SDKClient.Instance.property.CanHandleMsg, 2, 1);
                        logger.Info("CanHandleMsg 值修改为:2");
                    }
                }
            }
            else
            {

            }
            //package.Send(ec).id
            return package.id;

        }
    }
}
