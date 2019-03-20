using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SuperSocket.ProtoBase;
using SDKClient.Model;
using SDKClient.Protocol;
using Util.Logs.Aspects;

using System.ComponentModel.Composition;
namespace SDKClient
{
    /// <summary>
    /// 协议解析器
    /// </summary>
     class RequestInfoParse :IRequestInfoParser<PackageInfo>
    {
        public static readonly RequestInfoParse instance = new RequestInfoParse();

       
        [DebugLog]
        public PackageInfo ParseRequestInfo(byte[] source, int offset, int lengh)
        {
            byte[] buff = new byte[lengh];
            Buffer.BlockCopy(source, offset, buff, 0, lengh);
            for (int i = 0; i < lengh; i++)
            {
                if ((i) % 2 == 0)
                {
                    buff[i] = (byte)(buff[i] - 7);
                }
                else
                {
                    buff[i] = (byte)(buff[i] - 5);
                }
            }
            PackageInfo info;
            try
            {
                info = Util.Helpers.Json.ToObject<PackageInfo>(Encoding.UTF8.GetString(buff));
              
            }
            catch
            {
                var error = new ErrorPackage();
                error.code = 1;
                error.Content = Encoding.UTF8.GetString(buff);
                return error;
            }
          
            try
            {
                switch (info.apiId)
                {
                    case Protocol.ProtocolBase.authCode:
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<AuthPackage>(Encoding.UTF8.GetString(buff));

                    case Protocol.ProtocolBase.AddFriendCode:
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<AddFriendPackage>(Encoding.UTF8.GetString(buff));
                    case Protocol.ProtocolBase.AddFriendAcceptedCode:
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<AddFriendAcceptedPackage>(Encoding.UTF8.GetString(buff));
                    case Protocol.ProtocolBase.AddAttentionCode:
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<AddAttentionPackage>(Encoding.UTF8.GetString(buff));
                    case Protocol.ProtocolBase.AddNoticeCode:
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<AddNoticePackage>(Encoding.UTF8.GetString(buff));

                    case Protocol.ProtocolBase.CreateGroupCode:
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<CreateGroupComponsePackage>(Encoding.UTF8.GetString(buff));
                    case Protocol.ProtocolBase.CustomServiceCode:
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<CustomServicePackage>(Encoding.UTF8.GetString(buff));
                    case Protocol.ProtocolBase.CSSyncMsgStatusCode:
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<CSSyncMsgStatusPackage>(Encoding.UTF8.GetString(buff));
                    case Protocol.ProtocolBase.CSMessageCode:
                        var obj = Newtonsoft.Json.JsonConvert.DeserializeObject<CSMessagePackage>(Encoding.UTF8.GetString(buff));
                        MessagePackage mp = new MessagePackage();
                        mp.from = obj.from;
                        mp.to = obj.to;
                        mp.id = obj.id;
                        mp.version = obj.version;
                        mp.data = ((MessagePackage)obj).data;
                        mp = obj;
                        return mp;
                    case Protocol.ProtocolBase.CustomExchangeCode:
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<CustomExchangePackage>(Encoding.UTF8.GetString(buff));
                    case Protocol.ProtocolBase.DeleteFriendApplyCode:
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<DeleteFriendApplyPackage>(Encoding.UTF8.GetString(buff));
                    case Protocol.ProtocolBase.DeleteAttentionUserCode:
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<DeleteAttentionUserPackage>(Encoding.UTF8.GetString(buff));



                    case Protocol.ProtocolBase.DeleteFriendCode:
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<DeleteFriendPackage>(Encoding.UTF8.GetString(buff));
                    case Protocol.ProtocolBase.DismissGroupCode:
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<DismissGroupPackage>(Encoding.UTF8.GetString(buff));
                    case Protocol.ProtocolBase.DeviceRepeatloginNotifyCode:
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<DeviceRepeatloginNotifyPackage>(Encoding.UTF8.GetString(buff));
                    case Protocol.ProtocolBase.ExitGroupCode:
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<ExitGroupPackage>(Encoding.UTF8.GetString(buff));
                    case Protocol.ProtocolBase.ForceExitCode:
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<ForceExitPackage>(Encoding.UTF8.GetString(buff));

                    case Protocol.ProtocolBase.GetAttentionListCode:
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<GetAttentionListPackage>(Encoding.UTF8.GetString(buff));

                    case Protocol.ProtocolBase.GetContactsListCode:
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<GetContactsListPackage>(Encoding.UTF8.GetString(buff));
                    case Protocol.ProtocolBase.GetClientIDCode:
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<GetClientIDPackage>(Encoding.UTF8.GetString(buff));
                    case Protocol.ProtocolBase.GetFriendCode:
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<GetFriendPackage>(Encoding.UTF8.GetString(buff));
                    case Protocol.ProtocolBase.GetFriendApplyListCode:
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<GetFriendApplyListPackage>(Encoding.UTF8.GetString(buff));

                    case Protocol.ProtocolBase.GetBlackListCode:
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<GetBlackListPackage>(Encoding.UTF8.GetString(buff));
                    case Protocol.ProtocolBase.GetGroupCode:
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<GetGroupPackage>(Encoding.UTF8.GetString(buff));
                    case Protocol.ProtocolBase.GetgroupMemberCode:
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<GetGroupMemberPackage>(Encoding.UTF8.GetString(buff));
                    case Protocol.ProtocolBase.GetgroupListCode:
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<GetGroupListPackage>(Encoding.UTF8.GetString(buff));
                    case Protocol.ProtocolBase.GetLoginQRCodeCode:
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<GetLoginQRCodePackage>(Encoding.UTF8.GetString(buff));
                    case Protocol.ProtocolBase.GetgroupMemberListCode:
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<GetGroupMemberListPackage>(Encoding.UTF8.GetString(buff));
                    case Protocol.ProtocolBase.GetOfflineMessageListCode:
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<GetOfflineMessageListPackage>(Encoding.UTF8.GetString(buff));
                    case Protocol.ProtocolBase.GetUserCode:
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<GetUserPackage>(Encoding.UTF8.GetString(buff));
                    case Protocol.ProtocolBase.GetUserPrivacySettingCode:
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<GetUserPrivacySettingPackage>(Encoding.UTF8.GetString(buff));
                    case Protocol.ProtocolBase.InviteJoinGroupCode:
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<InviteJoinGroupPackage>(Encoding.UTF8.GetString(buff));
                    case Protocol.ProtocolBase.JoinGroupCode:
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<JoinGroupPackage>(Encoding.UTF8.GetString(buff));
                    case Protocol.ProtocolBase.JoinGroupAcceptedCode:
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<JoinGroupAcceptedPackage>(Encoding.UTF8.GetString(buff));

                    case Protocol.ProtocolBase.loginCode:
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<LoginPackage>(Encoding.UTF8.GetString(buff));
                    case Protocol.ProtocolBase.LogoutCode:
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<LogoutPackage>(Encoding.UTF8.GetString(buff));
                    case Protocol.ProtocolBase.messageCode:
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<MessagePackage>(Encoding.UTF8.GetString(buff));
                    case Protocol.ProtocolBase.MessageConfirmCode:
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<MessageConfirmPackage>(Encoding.UTF8.GetString(buff));
                    case Protocol.ProtocolBase.QRScanCode:
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<QRScanPackage>(Encoding.UTF8.GetString(buff));
                    case Protocol.ProtocolBase.QRCancelCode:
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<QRCancelPackage>(Encoding.UTF8.GetString(buff));
                    case Protocol.ProtocolBase.QRConfirmCode:
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<QRConfirmPackage>(Encoding.UTF8.GetString(buff));
                    case Protocol.ProtocolBase.QRExpiredCode:
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<QRExpiredPackage>(Encoding.UTF8.GetString(buff));
                    case Protocol.ProtocolBase.PCAutoLoginApplyCode:
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<PCAutoLoginApplyPackage>(Encoding.UTF8.GetString(buff));
                    case Protocol.ProtocolBase.SearchNewFriendCode:
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<SearchNewFriendPackage>(Encoding.UTF8.GetString(buff));
                    case Protocol.ProtocolBase.SetStrangerDoNotDisturbCode:
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<SetStrangerDoNotDisturbPackage>(Encoding.UTF8.GetString(buff));
                    case Protocol.ProtocolBase.SetMemberPowerCode:
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<SetMemberPowerPackage>(Encoding.UTF8.GetString(buff));
                    case Protocol.ProtocolBase.SysNotifyCode:
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<SysNotifyPackage>(Encoding.UTF8.GetString(buff));
                    case Protocol.ProtocolBase.SyncMsgStatusCode:
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<SyncMsgStatusPackage>(Encoding.UTF8.GetString(buff));
                    case Protocol.ProtocolBase.TopAttentionUserCode:
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<TopAttentionUserPackage>(Encoding.UTF8.GetString(buff));

                    case Protocol.ProtocolBase.UpdateFriendRelationCode:
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<UpdateFriendRelationPackage>(Encoding.UTF8.GetString(buff));
                    case Protocol.ProtocolBase.UpdateGroupCode:
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<UpdateGroupPackage>(Encoding.UTF8.GetString(buff));
                    case Protocol.ProtocolBase.UpdateUserCode:
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<UpdateuserPackage>(Encoding.UTF8.GetString(buff));
                    case Protocol.ProtocolBase.UpdateFriendSetCode:
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<UpdateFriendSetPackage>(Encoding.UTF8.GetString(buff));

                    case Protocol.ProtocolBase.UpdateUserSetsInGroupCode:
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<UpdateUserSetsInGroupPackage>(Encoding.UTF8.GetString(buff));
                    case Protocol.ProtocolBase.UpdateUserDetailCode:
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<UpdateUserDetailPackage>(Encoding.UTF8.GetString(buff));


                    default:
                        var s = Encoding.UTF8.GetString(buff);
                        return new NoHandlePackage() { source= s };
                }
            }
            catch (Exception ex)
            {
                SDKClient.logger.Error($"error:{ex.Message},stack:{ex.StackTrace},package:{info.ToString()}");
                return new NoHandlePackage() { source = info.ToString() }; 
            }
        }

       
    }
  
}
