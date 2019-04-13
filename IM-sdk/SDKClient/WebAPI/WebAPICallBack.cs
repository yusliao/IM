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

      
        /// <summary>
        /// 获取群二维码
        /// </summary>
        /// <returns></returns>
        public static AuthResponse GetQrCodeImg()
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
        public static WebResponse SendErrorToCloud(ErrorPackage request)
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
                SDKClient.logger.Info($"SendErrorToCloud: {Util.Helpers.Json.ToJson(response)}");
                return response;
            }
            catch (Exception ex)
            {
                SDKClient.logger.Error($"SendErrorToCloud: {ex.Message}");
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
