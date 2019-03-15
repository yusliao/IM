using SDKClient.Model;
using SDKClient.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Util;
using static SDKClient.SDKProperty;

namespace SDKClient.WebAPI
{
    class WebAPICallBack
    {
        /// <summary>
        /// 检查用户是否在线
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async static Task<bool> CheckUserIsOnline(int userId)
        {
            var obj = await new Util.Webs.Clients.WebClient().Post(Protocol.ProtocolBase.GetUserPcOnlineInfo)
               .Data("userId", userId)
                .Header("token", SDKClient.Instance.property.CurrentAccount.token)
                .Header("signature", Util.Helpers.Encrypt.Md5By32(SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks + ProtocolBase.ImLinkSignUri))
               .Header("version", SDKClient.Instance.property.CurrentAccount.httpVersion ?? "1.0")
                .Header("timeStamp", SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks)
               .ResultFromJsonAsync<dynamic>();

            return obj.isPcOnline;


        }
        /// <summary>
        /// 断点上传文件
        /// </summary>
        /// <param name="datas"></param>
        /// <param name="blocknum"></param>
        /// <param name="blockSize"></param>
        /// <param name="resourceId"></param>
        /// <param name="totalSize"></param>
        /// <param name="blockCount"></param>
        /// <returns></returns>
        public async static Task<(bool Success, bool isFinished, string code, string error)> ResumeUpload(byte[] datas, int blocknum, long blockSize, string resourceId, long totalSize, long blockCount)
        {
            var resp = await new Util.Webs.Clients.WebClient().Post(Protocol.ProtocolBase.ResumeUpload)
                .Header("token", SDKClient.Instance.property.CurrentAccount.token)
                .Header("signature", Util.Helpers.Encrypt.Md5By32(SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks + ProtocolBase.ImLinkSignUri))
                .Header("version", SDKClient.Instance.property.CurrentAccount.httpVersion ?? "1.0")
                .Header("timeStamp", SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks)
                .OnFail((s, c) => SDKClient.logger.Error($"ResumeUpload 调用失败: {s},错误码：{c.Value()}"))
                .AddMulitpartFile(datas, blocknum, blockSize, resourceId, null, totalSize, blockCount)
                .ContentType(Util.Webs.Clients.HttpContentType.Formdata)
                .ResultFromJsonAsync<dynamic>();
            if (resp == null)
                return (false, false, "", "");
            SDKClient.logger.Info(Util.Helpers.Json.ToJson(resp));
            if (resp == null)
                return (false, false, "-1", null);
            return (resp.Success, resp.isFinished, resp.code, resp.error);

        }

        /// <summary>
        /// 账号、密码验证登录
        /// </summary>
        /// <returns></returns>
        public static AuthResponse GetAuthByUserPassword()
        {
            var config = System.Configuration.ConfigurationManager.OpenExeConfiguration("IMUI.exe");
            string version = config.AppSettings.Settings["version"].Value ?? "1";
            var resp = new Util.Webs.Clients.WebClient().Post(ProtocolBase.AuthUri)
                        .Data("userMobile", SDKClient.Instance.property.CurrentAccount.loginId)
                        .Data("deviceModel", "PC")
                        .Data("imVersion", version)
                        .Data("imei", System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(N => N.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback)?.GetPhysicalAddress().ToString())
                        .Data("userPwd", Util.Helpers.Encrypt.Md5By32(SDKClient.Instance.property.CurrentAccount.userPass, Encoding.UTF8).ToLower())
                        .OnFail((s, c) => SDKClient.logger.Error($"GetAuthByUserPassword 调用失败: {s},错误码：{c.Value()};请求参数：userPwd:{Util.Helpers.Encrypt.Md5By32(SDKClient.Instance.property.CurrentAccount.userPass, Encoding.UTF8).ToLower()},imei:{System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(N => N.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback)?.GetPhysicalAddress().ToString()}"))
                        .ContentType(Util.Webs.Clients.HttpContentType.Json)

                        .ResultFromJson<AuthResponse>();
            SDKClient.logger.Info($"GetAuthByUserPassword: {Util.Helpers.Json.ToJson(resp)}");
            return resp;
        }
        /// <summary>
        /// 扫码登录
        /// </summary>
        /// <returns></returns>
        public static AuthResponse GetAuthByToken()
        {
            SDKClient.logger.Info($"调用 GetAuthByToken前: 请求参数：token:{SDKClient.Instance.property.CurrentAccount.token},imei:{System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(N => N.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback)?.GetPhysicalAddress().ToString()}");
            return new Util.Webs.Clients.WebClient().Post(ProtocolBase.AuthUri)
                       .Header("token", SDKClient.Instance.property.CurrentAccount.token)
                       .Data("deviceModel", "PC")
                       .Data("imei", System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(N => N.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback)?.GetPhysicalAddress().ToString())
                       .OnFail((s, c) => SDKClient.logger.Error($"GetAuthByToken 调用失败: {s},错误码：{c.Value()};请求参数：token:{SDKClient.Instance.property.CurrentAccount.token},imei:{System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(N => N.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback)?.GetPhysicalAddress().ToString()}"))
                       .ContentType(Util.Webs.Clients.HttpContentType.Json)
                       .ResultFromJson<AuthResponse>();
        }

        #region 客服
        public static CSEntity GetGrant()
        {

            var resp = new Util.Webs.Clients.WebClient().Post(ProtocolBase.Grant)
                        .Data("id", Guid.NewGuid().ToString())
                        .Data("appName", "MJD.IM")
                        .Data("mobile", SDKClient.Instance.property.CurrentAccount.loginId)
                             .Data("password", SDKClient.Instance.property.CurrentAccount.userPass)
                        .Data("appKey", "5%MClxd6#3axiGROhx70PRWxNF4$wGnw")
                        .Data("userType", (int)SDKProperty.userType.customserver)
                        .OnFail((s, c) => SDKClient.logger.Error($"GetGrant 调用失败: {s},错误码：{c.Value()}"))
                        .ContentType(Util.Webs.Clients.HttpContentType.Json)
                        .ResultFromJson<CSEntity>();
            SDKClient.logger.Info($"GetGrant: {Util.Helpers.Json.ToJson(resp)}");

            return resp;

        }
        public static (bool data, string message, int code) CSlogin()
        {
            string parm = $"{SDKClient.Instance.property.CurrentAccount.loginId}|{SDKClient.Instance.property.CurrentAccount.userPass}|{SDKClient.Instance.property.CurrentAccount.userID.ToString()}";
            string strtime = DateTime.Now.ToString("yyyyMMddHHmmss");
            string param = $"login{strtime}{CustomServerURL.CSKEY}";
            string signatureresult = MJD.Utility.UtilityCrypto.Encrypt(param, MJD.Utility.CryptoProvider.MD5).ToLower();
            var resp = new Util.Webs.Clients.WebClient().Post(CustomServerURL.CSlogin)
                        .Header("signature", signatureresult)
                        .Header("action", "login")
                        .Header("time", strtime)
                       .Data("paramencrypt",
                       $"{MJD.Utility.UtilityCrypto.AESEncrypt(parm, "2017manjdAES1234")}")
                       .ContentType(Util.Webs.Clients.HttpContentType.Json)
                       .ResultFromJson<dynamic>();
            return (resp.data, resp.message, resp.code);
        }
        public static async Task<CSRoomList> GetCSRoomlist()
        {
            string parm = $"{SDKClient.Instance.property.CurrentAccount.loginId}|{SDKClient.Instance.property.CurrentAccount.userPass}|{SDKClient.Instance.property.CurrentAccount.userID.ToString()}";
            string strtime = DateTime.Now.ToString("yyyyMMddHHmmss");
            string param = $"recentlylist{strtime}{CustomServerURL.CSKEY}";
            string signatureresult = MJD.Utility.UtilityCrypto.Encrypt(param, MJD.Utility.CryptoProvider.MD5).ToLower();
            return await new Util.Webs.Clients.WebClient().Get($"{CustomServerURL.GetCSRoomlist}{SDKClient.Instance.property.CurrentAccount.userID.ToString()}")
                        .Header("signature", signatureresult)
                        .Header("action", "recentlylist")
                        .Header("time", strtime)
                       .ContentType(Util.Webs.Clients.HttpContentType.Json)
                       .ResultFromJsonAsync<CSRoomList>();
        }
        /// <summary>
        /// </summary>
        /// <param name="exchangeId"></param>
        /// <param name="closeOtherSession">是否关闭其他的会话（0: 如果存在与其他客服的会话，则分配失败。1: 如果存在与其他客服的会话，则强制结束会话后，继续分配。）</param>
        /// <returns></returns>
        public static async Task<CustomExchange> CustomExchange(int to,int exchangeId,int closeOtherSession=0)
        {
          
            string parm = $"{SDKClient.Instance.property.CurrentAccount.loginId}|{SDKClient.Instance.property.CurrentAccount.userPass}|{SDKClient.Instance.property.CurrentAccount.userID.ToString()}";
            string strtime = DateTime.Now.ToString("yyyyMMddHHmmss");
            string param = $"assignServicer{strtime}{CustomServerURL.CSKEY}";
            string signatureresult = MJD.Utility.UtilityCrypto.Encrypt(param, MJD.Utility.CryptoProvider.MD5).ToLower();
            var resp = await new Util.Webs.Clients.WebClient().Post(CustomServerURL.CustomExchange)
                        .Header("signature", signatureresult)
                        .Header("action", "assignServicer")
                        .Header("time", strtime)
                        .Data("imUserId", to)
                         .Data("closeOtherSession", closeOtherSession)
                        .Data("servicerId", exchangeId)
                       .ContentType(Util.Webs.Clients.HttpContentType.Json)
                       .ResultFromJsonAsync<CustomExchange>();
            return resp;
        }
        /// <summary>
        /// 获取快速回复类别集合
        /// </summary>
        /// <param name="serviceId"></param>
        /// <param name="cateType"></param>
        /// <returns></returns>
        public static QuickReplycategory GetQuickReplycategory(int cateType)
        {
            string parm = $"{SDKClient.Instance.property.CurrentAccount.loginId}|{SDKClient.Instance.property.CurrentAccount.userPass}|{SDKClient.Instance.property.CurrentAccount.userID.ToString()}";
            string strtime = DateTime.Now.ToString("yyyyMMddHHmmss");
            string param = $"category{strtime}{CustomServerURL.CSKEY}";
            string signatureresult = MJD.Utility.UtilityCrypto.Encrypt(param, MJD.Utility.CryptoProvider.MD5).ToLower();
            return new Util.Webs.Clients.WebClient().Get($"{CustomServerURL.QuickReplycategory}?servicerId={SDKClient.Instance.property.CurrentAccount.CustomProperty.ServicerId}&cateType={cateType}")
                        .Header("signature", signatureresult)
                        .Header("action", "category")
                        .Header("time", strtime)
                       .ContentType(Util.Webs.Clients.HttpContentType.Json)
                       .ResultFromJson<QuickReplycategory>();
        }

        /// <summary>
        /// 获取客服系统配置
        /// </summary>
        /// <returns></returns>
        public static async Task<CSSysConfig> GetOnLineServicerSysConfig()
        {
            string parm = $"{SDKClient.Instance.property.CurrentAccount.loginId}|{SDKClient.Instance.property.CurrentAccount.userPass}|{SDKClient.Instance.property.CurrentAccount.userID.ToString()}";
            string strtime = DateTime.Now.ToString("yyyyMMddHHmmss");
            string param = $"OnlineService_GetOnLineServicerSysConfig{strtime}{CustomServerURL.CSKEY}";
            string signatureresult = MJD.Utility.UtilityCrypto.Encrypt(param, MJD.Utility.CryptoProvider.MD5).ToLower();
            return await new Util.Webs.Clients.WebClient().Get(CustomServerURL.GetOnLineServicerSysConfig)
                        .Header("signature", signatureresult)
                        .Header("action", "OnlineService_GetOnLineServicerSysConfig")
                        .Header("time", strtime)
                       .ContentType(Util.Webs.Clients.HttpContentType.Json)
                       .ResultFromJsonAsync<CSSysConfig>();
        }
        /// <summary>
        /// 根据日期查询会话记录
        /// </summary>
        /// <param name="date"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public static async Task<CSSysConfig> GetSessionDate(DateTime date,int userId)
        {
            string parm = $"{SDKClient.Instance.property.CurrentAccount.loginId}|{SDKClient.Instance.property.CurrentAccount.userPass}|{SDKClient.Instance.property.CurrentAccount.userID.ToString()}";
            string strtime = DateTime.Now.ToString("yyyyMMddHHmmss");
            string param = $"OnlineService_GetSessionDate{strtime}{CustomServerURL.CSKEY}";
            string signatureresult = MJD.Utility.UtilityCrypto.Encrypt(param, MJD.Utility.CryptoProvider.MD5).ToLower();
            return await new Util.Webs.Clients.WebClient().Get($"{CustomServerURL.GetSessionDate}?imUserId={userId}&date={date.ToString()}")
                        .Header("signature", signatureresult)
                        .Header("action", "OnlineService_GetSessionDate")
                        .Header("time", strtime)
                       .ContentType(Util.Webs.Clients.HttpContentType.Json)
                       .ResultFromJsonAsync<CSSysConfig>();
        }
        public static async Task<IPEntity> GetAddressByIP(string ip)
        {
            string parm = $"{SDKClient.Instance.property.CurrentAccount.loginId}|{SDKClient.Instance.property.CurrentAccount.userPass}|{SDKClient.Instance.property.CurrentAccount.userID.ToString()}";
            string strtime = DateTime.Now.ToString("yyyyMMddHHmmss");
            string param = $"address{strtime}{CustomServerURL.CSKEY}";
            string signatureresult = MJD.Utility.UtilityCrypto.Encrypt(param, MJD.Utility.CryptoProvider.MD5).ToLower();
            return await new Util.Webs.Clients.WebClient().Get($"{CustomServerURL.GetAddressByIP}?request.ip={ip}")
                        .Header("signature", signatureresult)
                        .Header("action", "address")
                        .Header("time", strtime)
                       .ContentType(Util.Webs.Clients.HttpContentType.Json)
                       .ResultFromJsonAsync<IPEntity>();
        }
        public static async Task<OnlineStatusEntity> GetfreeServicers()
        {
            string parm = $"{SDKClient.Instance.property.CurrentAccount.loginId}|{SDKClient.Instance.property.CurrentAccount.userPass}|{SDKClient.Instance.property.CurrentAccount.userID.ToString()}";
            string strtime = DateTime.Now.ToString("yyyyMMddHHmmss");
            string param = $"freeservicers{strtime}{CustomServerURL.CSKEY}";
            string signatureresult = MJD.Utility.UtilityCrypto.Encrypt(param, MJD.Utility.CryptoProvider.MD5).ToLower();
            return await new Util.Webs.Clients.WebClient().Get($"{CustomServerURL.Getfreeservicers}?serviceType=0")
                        .Header("signature", signatureresult)
                        .Header("action", "freeservicers")
                        .Header("time", strtime)
                       .ContentType(Util.Webs.Clients.HttpContentType.Json)
                       .ResultFromJsonAsync<OnlineStatusEntity>();
        }
        /// <summary>
        /// 获取快速回复上下文
        /// </summary>
        /// <param name="serviceId"></param>
        /// <param name="cateType"></param>
        /// <param name="actionType"></param>
        /// <returns></returns>
        public static async Task<QuickReplycontent> GetQuickReplycontext(int cateType)
        {
            string parm = $"{SDKClient.Instance.property.CurrentAccount.loginId}|{SDKClient.Instance.property.CurrentAccount.userPass}|{SDKClient.Instance.property.CurrentAccount.userID.ToString()}";
            string strtime = DateTime.Now.ToString("yyyyMMddHHmmss");
            string param = $"content{strtime}{CustomServerURL.CSKEY}";
            string signatureresult = MJD.Utility.UtilityCrypto.Encrypt(param, MJD.Utility.CryptoProvider.MD5).ToLower();
            return await new Util.Webs.Clients.WebClient().Get($"{CustomServerURL.QuickReplycontent}?servicerId={SDKClient.Instance.property.CurrentAccount.CustomProperty.ServicerId}&cateType={cateType}")
                        .Header("signature", signatureresult)
                        .Header("action", "content")
                        .Header("time", strtime)
                       .ContentType(Util.Webs.Clients.HttpContentType.Json)
                       .ResultFromJsonAsync<QuickReplycontent>();
        }
        public static QuickReplyCategorycontents GetQuickReplyCategorycontents(int cateId)
        {
            string parm = $"{SDKClient.Instance.property.CurrentAccount.loginId}|{SDKClient.Instance.property.CurrentAccount.userPass}|{SDKClient.Instance.property.CurrentAccount.userID.ToString()}";
            string strtime = DateTime.Now.ToString("yyyyMMddHHmmss");
            string param = $"content{strtime}{CustomServerURL.CSKEY}";
            string signatureresult = MJD.Utility.UtilityCrypto.Encrypt(param, MJD.Utility.CryptoProvider.MD5).ToLower();
            return new Util.Webs.Clients.WebClient().Get($"{CustomServerURL.QuickReplycontent}?cateId={cateId}")
                        .Header("signature", signatureresult)
                        .Header("action", "content")
                        .Header("time", strtime)
                       .ContentType(Util.Webs.Clients.HttpContentType.Json)
                       .ResultFromJson<QuickReplyCategorycontents>();
        }





        /// <summary>
        /// CURD快速回复类型
        /// </summary>
        /// <param name="editType">1新增，2修改，3删除</param>
        /// <param name="categoryId"></param>
        /// <param name="categoryName"></param>
        /// <param name="categoryType">1公共，2个人</param>
        /// <returns></returns>
        public static (bool success, int id) PostQuickReplyCategoryedit(int editType, int categoryId, string categoryName, int categoryType)
        {
            string parm = $"{SDKClient.Instance.property.CurrentAccount.loginId}|{SDKClient.Instance.property.CurrentAccount.userPass}|{SDKClient.Instance.property.CurrentAccount.userID.ToString()}";
            string strtime = DateTime.Now.ToString("yyyyMMddHHmmss");
            string param = $"edit{strtime}{CustomServerURL.CSKEY}";
            string signatureresult = MJD.Utility.UtilityCrypto.Encrypt(param, MJD.Utility.CryptoProvider.MD5).ToLower();
            var resp = new Util.Webs.Clients.WebClient().Post($"{CustomServerURL.QuickReplycategoryedit}")
                        .Header("signature", signatureresult)
                        .Header("action", "edit")
                        .Header("time", strtime)
                        .Data("editType", editType)
                        .Data("categoryId", categoryId)
                        .Data("categoryName", categoryName)
                        .Data("categoryType", categoryType)
                        .Data("servicerId", SDKClient.Instance.property.CurrentAccount.CustomProperty.ServicerId)
                       .ContentType(Util.Webs.Clients.HttpContentType.Json)
                       .ResultFromJson<dynamic>();
            SDKClient.logger.Info($"PostQuickReplyCategoryedit: {Util.Helpers.Json.ToJson(resp)}");
            if (resp != null)
            {
                if (resp.code == 1)
                {
                    return (true, resp.data);
                }
            }
            return (false, 0);
        }
        /// <summary>
        /// CURD快速回复内容项
        /// </summary>
        /// <param name="editType">1新增，2修改，3删除</param>
        /// <param name="contentId"></param>
        /// <param name="content"></param>
        /// <param name="categoryId"></param>
        /// <returns></returns>
        public static (bool success, int id) PostQuickReplyContentedit(int editType, int contentId, string content, int categoryId)
        {
            string parm = $"{SDKClient.Instance.property.CurrentAccount.loginId}|{SDKClient.Instance.property.CurrentAccount.userPass}|{SDKClient.Instance.property.CurrentAccount.userID.ToString()}";
            string strtime = DateTime.Now.ToString("yyyyMMddHHmmss");
            string param = $"edit{strtime}{CustomServerURL.CSKEY}";
            string signatureresult = MJD.Utility.UtilityCrypto.Encrypt(param, MJD.Utility.CryptoProvider.MD5).ToLower();
            var resp = new Util.Webs.Clients.WebClient().Post($"{CustomServerURL.QuickReplycontentedit}")
                        .Header("signature", signatureresult)
                        .Header("action", "edit")
                        .Header("time", strtime)
                        .Data("editType", editType)
                        .Data("contentId", contentId)
                        .Data("categoryId", categoryId)
                         .Data("content", content)
                       .ContentType(Util.Webs.Clients.HttpContentType.Json)
                       .ResultFromJson<dynamic>();
            SDKClient.logger.Info($"PostQuickReplyContentedit: {Util.Helpers.Json.ToJson(resp)}");
            if (resp != null)
            {
                if (resp.code == 1)
                {
                    return (true, resp.data);
                }
            }
            return (false, 0);
        }
        public static (bool success, DTO.CSRoomListEntity.entity entity) GetUserInfoByMobile(string mobile)
        {
            string parm = $"{SDKClient.Instance.property.CurrentAccount.loginId}|{SDKClient.Instance.property.CurrentAccount.userPass}|{SDKClient.Instance.property.CurrentAccount.userID.ToString()}";
            string strtime = DateTime.Now.ToString("yyyyMMddHHmmss");
            string param = $"find{strtime}{CustomServerURL.CSKEY}";
            string signatureresult = MJD.Utility.UtilityCrypto.Encrypt(param, MJD.Utility.CryptoProvider.MD5).ToLower();
            var resp = new Util.Webs.Clients.WebClient().Get($"{CustomServerURL.GetUserInfoByMobile}?mobile={mobile}&servicerId={SDKClient.Instance.property.CurrentAccount.CustomProperty.ServicerId}")
                        .Header("signature", signatureresult)
                        .Header("action", "find")
                        .Header("time", strtime)
                       .ContentType(Util.Webs.Clients.HttpContentType.Json)
                       .ResultFromJson<dynamic>();

            SDKClient.logger.Info($"GetUserInfoByMobile: {Util.Helpers.Json.ToJson(resp)}");
            if (resp != null)
            {
                if (resp.code == 1)
                {
                    if (resp.data.Count != 0)
                    {
                        var obj = new DTO.CSRoomListEntity.entity()
                        {
                            mobile = resp.data[0].mobile,
                            photo = resp.data[0].photo,
                            userId = resp.data[0].imopenid
                        };
                        return (true, obj);
                    }
                    else
                        return (true, null);
                }
            }
            return (false, null);
        }
        public static (bool success, DTO.CSRoomListEntity.entity entity) QueryuserByMobile(string mobile)
        {
            string parm = $"{SDKClient.Instance.property.CurrentAccount.loginId}|{SDKClient.Instance.property.CurrentAccount.userPass}|{SDKClient.Instance.property.CurrentAccount.userID.ToString()}";
            string strtime = DateTime.Now.ToString("yyyyMMddHHmmss");
            string param = $"query{strtime}{CustomServerURL.CSKEY}";
            string signatureresult = MJD.Utility.UtilityCrypto.Encrypt(param, MJD.Utility.CryptoProvider.MD5).ToLower();
            var resp = new Util.Webs.Clients.WebClient().Get($"{CustomServerURL.QueryuserByMobile}?mobile={mobile}&servicerImOpenId={SDKClient.Instance.property.CurrentAccount.userID}")
                        .Header("signature", signatureresult)
                        .Header("action", "query")
                        .Header("time", strtime)
                       .ContentType(Util.Webs.Clients.HttpContentType.Json)
                       .ResultFromJson<dynamic>();

            SDKClient.logger.Info($"QueryuserByMobile: {Util.Helpers.Json.ToJson(resp)}");
            if (resp != null)
            {
                if (resp.code == 1)
                {
                    if (resp.data!= null)
                    {
                        var obj = new DTO.CSRoomListEntity.entity()
                        {
                            mobile = resp.data.mobile,
                            photo = resp.data.userPhoto,
                            shopName = resp.data.userName,
                            userId = int.Parse(resp.data.imOpenId),
                            sessionType = resp.data.sessionType,
                            sessionId = resp.data.sessionId
                        };
                        return (true, obj);
                    }
                    else
                        return (true, null);
                }
            }
            return (false, null);
        }
        /// <summary>
        /// 获取客服历史消息列表
        /// </summary>
        /// <param name="dateTime"></param>
        /// <param name="roomId"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static List<DTO.MessageEntity> GetHistoryMessageList(DateTime dateTime, int roomId = 0, int count = 20)
        {

            // var result = new Util.Webs.Clients.WebClient().Post("http://192.168.10.72/Messages/GetHistoryMessageList")
            var result = new Util.Webs.Clients.WebClient().Post($"{CustomServerURL.GetHistoryMessageList}")
                        .Data("id", SDKProperty.RNGId)
                        .Data("appName", "MJD.IM")
                         .Data("appKey", "5%MClxd6#3axiGROhx70PRWxNF4$wGnw")
                        .Data("time", dateTime.ToString("yyyy-MM-ddTHH:mm:ss.fff"))
                        .Data("count", count)
                        .Data("newver", 0)
                         .Data("userId", roomId)
                         .Data("userType", 2)

                       .ContentType(Util.Webs.Clients.HttpContentType.Json)
                       .ResultFromJson<dynamic>();
            SDKClient.logger.Info($"GetHistoryMessageList: {Util.Helpers.Json.ToJson(result)}");
            if (result.code != 0)
                return new List<DTO.MessageEntity>();
            else
            {
                var str = Util.Helpers.Json.ToJson(result.data.items);
                SDKClient.logger.Info($"GetHistoryMessageList ：{str}");
                List<MessagePackage> lst = Util.Helpers.Json.ToObject<List<MessagePackage>>(str);
                //   Util.Helpers.Async.Run(async () => await DAL.DALMessageHelper.ReceiveMsgtoDB(roomId, lst));
                return lst.Select(package =>
                {
                    DB.messageDB msg = new DB.messageDB()
                    {
                        from = package.from,
                        to = package.to,
                        msgTime = package.time.Value,
                        msgId = package.id,
                        body = Util.Helpers.Json.ToJson(package.data.body),
                        roomType = 0,
                        optionRecord = package.read == 0 ? 0 : 1,
                        roomId = roomId,
                        SenderName = package.data.senderInfo.userName,
                        SenderPhoto = package.data.senderInfo.photo,
                        Source = Util.Helpers.Json.ToJson(package)
                    };
                    
                    switch (package.data.subType.ToLower())
                    {
                        case nameof(SDKProperty.MessageType.txt):
                            // string cc = package.data.body.text;
                            // var content = SDKProperty.stringSearchEx.Replace(cc);
                            msg.content = package.data.body.text;
                            msg.msgType = nameof(SDKProperty.MessageType.txt);
                            break;
                        case nameof(SDKProperty.MessageType.img):
                            msg.resourceId = package.data.body.id;
                            msg.resourcesmallId = package.data.body.smallId;
                            msg.msgType = nameof(SDKProperty.MessageType.img);
                            msg.fileName = package.data.body.fileName;
                            msg.content = "[图片]";
                            break;
                        case nameof(SDKProperty.MessageType.onlinefile):
                            msg.msgType = nameof(SDKProperty.MessageType.onlinefile);
                            msg.resourceId = package.data.body.id;
                            msg.fileName = Path.GetFileName($"{package.data.body.fileName}");

                            msg.fileSize = package.data.body.fileSize;
                            msg.content = "[文件]";
                            break;
                        case nameof(SDKProperty.MessageType.file):
                            msg.msgType = nameof(SDKProperty.MessageType.file);
                            msg.resourceId = package.data.body.id;
                            msg.fileName = Path.GetFileName($"{package.data.body.fileName}" ?? $"{package.data.body.id}");

                            msg.fileSize = package.data.body.fileSize;
                            msg.content = "[文件]";
                            msg.fileState = (int)ResourceState.NoStart;
                            break;
                        case nameof(SDKProperty.MessageType.audio):
                            msg.msgType = nameof(SDKProperty.MessageType.audio);
                            msg.content = "对方发送语音消息，请在手机端查看";
                            break;
                        case nameof(SDKProperty.MessageType.video):
                            msg.msgType = nameof(SDKProperty.MessageType.video);
                            msg.resourceId = package.data.body.id;
                            msg.content = "[视频]";
                            msg.fileSize = package.data.body.fileSize;
                            msg.fileName = Path.GetFileName($"{package.data.body.fileName}" ?? $"{package.data.body.id}");


                            break;
                        case nameof(SDKProperty.MessageType.smallvideo):
                            msg.msgType = nameof(SDKProperty.MessageType.smallvideo);
                            msg.resourceId = package.data.body.id;
                            msg.content = "[小视频]";
                            msg.fileSize = package.data.body.fileSize;
                            msg.fileName = Path.GetFileName($"{package.data.body.fileName}" ?? $"{package.data.body.id}");


                            break;
                        case nameof(SDKProperty.MessageType.retract)://过滤掉撤回消息
                            msg.msgType = nameof(SDKProperty.MessageType.retract);
                            msg.content = "[撤回消息]";
                            
                            break;
                        //case "bigtxt":
                        //    msg.msgType = "bigtxt";
                        //    msg.content = package.data.body.partName;
                        //    Util.Helpers.Async.Run(async () => await InsertBigTxtMsg(package));
                        //    break;
                        case nameof(SDKProperty.MessageType.redenvelopessendout):
                            msg.msgType = nameof(SDKProperty.MessageType.redenvelopessendout);
                            msg.content = "[您有新红包，请在手机上查看]";
                            break;
                        case nameof(SDKProperty.MessageType.redenvelopesreceive):
                            msg.msgType = nameof(SDKProperty.MessageType.redenvelopesreceive);
                            msg.content = "[有人领取了你的红包，请在手机端查看]";
                            break;
                        case nameof(SDKProperty.MessageType.eval):
                            msg.msgType = nameof(SDKProperty.MessageType.eval);
                            msg.content = "[对方已评价]";
                            msg.body = Util.Helpers.Json.ToJson(package.data.body);
                            break;
                        case nameof(SDKProperty.MessageType.goods):
                            msg.msgType = nameof(SDKProperty.MessageType.goods);
                            msg.content = "[商品链接]";
                            msg.body = Util.Helpers.Json.ToJson(package.data.body);
                            break;
                        case nameof(SDKProperty.MessageType.order):
                            msg.msgType = nameof(SDKProperty.MessageType.order);
                            msg.content = "[商品链接]";
                            msg.body = Util.Helpers.Json.ToJson(package.data.body);
                            break;
                        case nameof(SDKProperty.MessageType.custom):
                            msg.msgType = nameof(SDKProperty.MessageType.custom);
                            msg.content = "[商品链接]";
                            msg.body = Util.Helpers.Json.ToJson(package.data.body);
                            break;
                        default:
                            break;
                    }
                    
                    return new DTO.MessageEntity() { db = msg };
                }).Reverse().ToList();
            }

        }
        public static async Task<HistoryRecordListResp> Getuserhistorylist(int PageIndex=0)
        {
            string strtime = DateTime.Now.ToString("yyyyMMddHHmmss");
            string param = $"history-list{strtime}{CustomServerURL.CSKEY}";
            string signatureresult = MJD.Utility.UtilityCrypto.Encrypt(param, MJD.Utility.CryptoProvider.MD5).ToLower();
            var result = await new Util.Webs.Clients.WebClient().Get($"{CustomServerURL.Getuserhistorylist}?servicerImOpenId={SDKClient.Instance.property.CurrentAccount.userID}&pageIndex={PageIndex}")
                        .Header("signature", signatureresult)
                        .Header("action", "history-list")
                        .Header("time", strtime)
                        .OnFail((s, c) => SDKClient.logger.Error($"Getuserhistorylist 调用失败: {s},错误码：{c.Value()}"))
                      .ContentType(Util.Webs.Clients.HttpContentType.Json)
                      .ResultFromJsonAsync<HistoryRecordListResp>();
            SDKClient.logger.Info($"Getuserhistorylist: {Util.Helpers.Json.ToJson(result)}");
            return result;

        }
        public static (bool data, string message, int code) PostCustomServerState(SDKProperty.customState customState)
        {
            string strtime = DateTime.Now.ToString("yyyyMMddHHmmss");
            string param = $"updateservicerinfo{strtime}{CustomServerURL.CSKEY}";
            string signatureresult = MJD.Utility.UtilityCrypto.Encrypt(param, MJD.Utility.CryptoProvider.MD5).ToLower();
            var resp = new Util.Webs.Clients.WebClient().Post(CustomServerURL.UpdateServicerinfo)
                        .Header("signature", signatureresult)
                        .Header("action", "updateservicerinfo")
                        .Header("time", strtime)
                       .Data("imopenid", SDKClient.Instance.property.CurrentAccount.userID.ToString())
                       .Data("updatetype", 2)
                       .Data("value", (int)customState)
                       .ContentType(Util.Webs.Clients.HttpContentType.Json)
                       .ResultFromJson<dynamic>();
            SDKClient.logger.Info($"PostCustomServerState： {Util.Helpers.Json.ToJson(resp)}");
            if (resp == null)
                return (false, "调用失败", 0);
            return (resp.data, resp.message, resp.code);
        }
        public static (bool data, string message, int code) AddSessionItem(string session)
        {
            string strtime = DateTime.Now.ToString("yyyyMMddHHmmss");
            string param = $"updateservicerinfo{strtime}{CustomServerURL.CSKEY}";
            string signatureresult = MJD.Utility.UtilityCrypto.Encrypt(param, MJD.Utility.CryptoProvider.MD5).ToLower();
            var resp = new Util.Webs.Clients.WebClient().Post(CustomServerURL.UpdateServicerinfo)
                        .Header("signature", signatureresult)
                        .Header("action", "updateservicerinfo")
                        .Header("time", strtime)
                       .Data("sessionid", session)
                       .Data("updatetype", 3)
                       .Data("value", 1)
                       .OnFail((s, c) => SDKClient.logger.Error($"AddSessionItem 调用失败: {s},错误码：{c.Value()}"))
                       .ContentType(Util.Webs.Clients.HttpContentType.Json)
                       .ResultFromJson<dynamic>();
            SDKClient.logger.Info($"AddSessionItem: {Util.Helpers.Json.ToJson(resp)}");
            return (resp.data, resp.message, resp.code);
        }
        public static (bool data, string message, int code) DiminishingSessionItem(string sessionId)
        {
            string strtime = DateTime.Now.ToString("yyyyMMddHHmmss");
            string param = $"updateservicerinfo{strtime}{CustomServerURL.CSKEY}";
            string signatureresult = MJD.Utility.UtilityCrypto.Encrypt(param, MJD.Utility.CryptoProvider.MD5).ToLower();
            var resp = new Util.Webs.Clients.WebClient().Post(CustomServerURL.UpdateServicerinfo)
                        .Header("signature", signatureresult)
                        .Header("action", "updateservicerinfo")
                        .Header("time", strtime)
                       .Data("sessionid", sessionId)
                       .Data("updatetype", 4)
                       .Data("value", 1)
                       .OnFail((s, c) => SDKClient.logger.Error($"DiminishingSessionItem 调用失败: {s},错误码：{c.Value()}"))
                       .ContentType(Util.Webs.Clients.HttpContentType.Json)
                       .ResultFromJson<dynamic>();
            SDKClient.logger.Info($"结束会话 session:{sessionId}, resp:{Util.Helpers.Json.ToJson(resp)}");
            return (resp.data, resp.message, resp.code);
        }
        public static (bool data, string message, int code) PostAppraisal(SDKProperty.Satisfaction satisfaction)
        {
            string strtime = DateTime.Now.ToString("yyyyMMddHHmmss");
            string param = $"updateservicerinfo{strtime}{CustomServerURL.CSKEY}";
            string signatureresult = MJD.Utility.UtilityCrypto.Encrypt(param, MJD.Utility.CryptoProvider.MD5).ToLower();
            var resp = new Util.Webs.Clients.WebClient().Post(CustomServerURL.UpdateServicerinfo)
                        .Header("signature", signatureresult)
                        .Header("action", "updateservicerinfo")
                        .Header("time", strtime)
                       .Data("imopenid", SDKClient.Instance.property.CurrentAccount.userID.ToString())
                       .Data("updatetype", 1)
                       .Data("value", (int)satisfaction)
                       .ContentType(Util.Webs.Clients.HttpContentType.Json)
                       .ResultFromJson<dynamic>();

            return (resp.data, resp.message, resp.code);
        }
        #endregion
        /// <summary>
        /// 获取群二维码
        /// </summary>
        /// <returns></returns>
        public static AuthResponse Getfuck()
        {
            return new Util.Webs.Clients.WebClient().Post(ProtocolBase.AuthUri)
                       .Data("userMobile", SDKClient.Instance.property.CurrentAccount.loginId)
                       .Data("deviceModel", "PC")
                       .Data("imei", SDKClient.Instance.property.CurrentAccount.imei)
                       .Data("userPwd", Util.Helpers.Encrypt.Md5By32(SDKClient.Instance.property.CurrentAccount.userPass, Encoding.UTF8).ToLower())
                       .ContentType(Util.Webs.Clients.HttpContentType.Json)
                       .ResultFromJson<AuthResponse>();
        }
        public async static Task<fileInfo> FindResource(string filename)
        {
            var resp = await new Util.Webs.Clients.WebClient().Get($"{Protocol.ProtocolBase.findresource}{filename}/3/true")
                       .ContentType(Util.Webs.Clients.HttpContentType.Json)
                       .ResultFromJsonAsync<fileInfo>();
            if (resp != null)
                return resp;
            else
                return null;
        }
        /// <summary>
        /// 搜索联系人
        /// </summary>
        /// <param name="keyWord"></param>
        /// <param name="searchType"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public static SearchResult GetSearchResult(string keyWord, string searchType = "1,2", int pageIndex = 1, int pageSize = 30)
        {
            SearchResult result = new Util.Webs.Clients.WebClient().Post(ProtocolBase.SearchQuery)
               .Header("token", SDKClient.Instance.property.CurrentAccount.token)
               .Header("signature", Util.Helpers.Encrypt.Md5By32(SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks + ProtocolBase.ImLinkSignUri))
               .Header("version", SDKClient.Instance.property.CurrentAccount.httpVersion ?? "1.2.0")
               .Header("timeStamp", SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks)
               .Data("keyWord", keyWord)
               .Data("searchType", searchType)
               .Data("pageIndex", pageIndex)
               .Data("pageSize", pageSize)
               .ContentType(Util.Webs.Clients.HttpContentType.Json)
               .ResultFromJson<SearchResult>();
            return result;
        }
        /// <summary>
        /// 获取二维码
        /// </summary>
        /// <param name="Id"></param>
        /// <param name="userOrgroup"></param>
        /// <returns></returns>
        public static QrCodeResponse GetQrCode(string Id, string userOrgroup)
        {
            QrCodeRequest request = new QrCodeRequest() { keyId = Id, qrType = userOrgroup };
            var response = new Util.Webs.Clients.WebClient().Post(ProtocolBase.QrCodeInfoUri)
                .Header("token", SDKClient.Instance.property.CurrentAccount.token)
                .Header("signature", Util.Helpers.Encrypt.Md5By32(SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks + ProtocolBase.ImLinkSignUri))
                .Header("version", SDKClient.Instance.property.CurrentAccount.httpVersion ?? "1.0")
                .Header("timeStamp", SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks)
                .Data(nameof(request.keyId), request.keyId)
                .Data(nameof(request.qrType), request.qrType)
                .ContentType(Util.Webs.Clients.HttpContentType.Json)
                .ResultFromJson<QrCodeResponse>();
            return response;
        }
        /// <summary>
        /// 错误日志上报服务器
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static WebResponse AddMsgFaceBack(ErrorPackage request)
        {

            try
            {
                var response = new Util.Webs.Clients.WebClient().Post(ProtocolBase.AddMsgFaceBack)
              .Header("token", SDKClient.Instance.property.CurrentAccount.token)
              .Header("signature", Util.Helpers.Encrypt.Md5By32(SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks + ProtocolBase.ImLinkSignUri))
              .Header("version", SDKClient.Instance.property.CurrentAccount.httpVersion ?? "1.0")
              .Header("timeStamp", SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks)
              .Data(nameof(request.msgType), request.msgType)
              .Data(nameof(request.receiverId), request.receiverId)
              .Data(nameof(request.targetId), request.targetId)
              .Data(nameof(request.senderId), request.senderId)
              .Data(nameof(request.msgId), request.msgId)
              .Data(nameof(request.imei), request.imei)
              .Data(nameof(request.sourceOS), request.sourceOS)
              .Data(nameof(request.content), request.content)
              .ContentType(Util.Webs.Clients.HttpContentType.Json)
              .OnFail((s, c) => SDKClient.logger.Error($"AddMsgFaceBack 调用失败: {s},错误码：{c.Value()}"))
              .ResultFromJson<WebResponse>();
                SDKClient.logger.Info($"AddMsgFaceBack: {Util.Helpers.Json.ToJson(response)}");
                return response;
            }
            catch (Exception ex)
            {
                SDKClient.logger.Error($"AddMsgFaceBack: {ex.Message}");
                return null;
            }

        }
        /// <summary>
        /// 获取最新版本号
        /// </summary>
        /// <returns></returns>
        public static PCVersion GetLatestVersionNum()
        {
            var config = System.Configuration.ConfigurationManager.OpenExeConfiguration("IMUI.exe");
            string version = config.AppSettings.Settings["version"].Value ?? "1";
         
            var subgradeversion = "76";
            //string version = "0";
            if (config.AppSettings.Settings["updateversion"] != null)
            {
                subgradeversion = config.AppSettings.Settings["updateversion"].Value;
            }
            else
            {
                config.AppSettings.Settings.Add("updateversion", subgradeversion);
                config.Save();
            }
            SDKClient.logger.Info($"GetLatestVersionNum_升级包版本号：" + subgradeversion);
            PCVersion resp = new Util.Webs.Clients.WebClient().Get($"{ProtocolBase.LatestVersionNum}{version}&subgradeVersionNum={subgradeversion}")
              .Header("token", SDKClient.Instance.property.CurrentAccount.token)
              .Header("signature", Util.Helpers.Encrypt.Md5By32(SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks + ProtocolBase.ImLinkSignUri))
              .Header("version", SDKClient.Instance.property.CurrentAccount.httpVersion ?? "1.0")
              .Header("timeStamp", SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks)
             .OnFail((s, c) => SDKClient.logger.Error($"GetLatestVersionNum 调用失败: {s},错误码：{c.Value()}"))
              .ContentType(Util.Webs.Clients.HttpContentType.Json)
             .ResultFromJson<PCVersion>();
            SDKClient.logger.Info($"GetPCVersion: {Util.Helpers.Json.ToJson(resp)}");
            return resp;
        }
        /// <summary>
        /// 获取敏感词列表
        /// </summary>
        /// <param name="lastUpdateTime"></param>
        /// <returns></returns>
        public static GetSensitiveWordsResponse GetBadWordUpdate(string lastUpdateTime)
        {
            string uri = ProtocolBase.BadWordUpdateTimeUri + string.Format("?time={0}", lastUpdateTime);
            var resp = new Util.Webs.Clients.WebClient().Get(uri)
                .OnFail((s, c) => SDKClient.logger.Error($"获取最新更新时间调用失败: {s},错误码：{(int)c}"))
                .ContentType(Util.Webs.Clients.HttpContentType.Json)
                .ResultFromJson<GetSensitiveWordsResponse>();
            if (resp != null && resp.code == 0)
            {
                return resp;
            }
            else
                return null;
        }






    }
}
