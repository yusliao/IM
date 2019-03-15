using SDKClient.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SDKClient.WebAPI
{
    /// <summary>
    /// IM服务相关的WEB请求
    /// </summary>
    internal class IMRequest
    {
        public static Model.GetUserPrivacySettingPackage GetUserPrivacySettingPackage(Model.GetUserPrivacySettingPackage package)
        {
            var resp = new Util.Webs.Clients.WebClient().Post(IMServiceURL.GetUserPrivacySetting)
                  .Header("token", SDKClient.Instance.property.CurrentAccount.token)
                .Header("signature", Util.Helpers.Encrypt.Md5By32(SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks + ProtocolBase.ImLinkSignUri))
                .Header("version", SDKClient.Instance.property.CurrentAccount.httpVersion ?? "1.0")
                .Header("timeStamp", SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks)
                .JsonData<Model.GetUserPrivacySettingPackage>(package)
                .OnFail((s, c) => SDKClient.logger.Error($"GetUserPrivacySetting 调用失败: {s},错误码：{(int)c}"))
                  .ContentType(Util.Webs.Clients.HttpContentType.Json)
                  .ResultFromJson<Model.GetUserPrivacySettingPackage>();
            return resp;
        }

        public static Model.GetUserPackage GetUser(Model.GetUserPackage package)
        {
            var resp = new Util.Webs.Clients.WebClient().Post(IMServiceURL.GetUser)
                  .Header("token", SDKClient.Instance.property.CurrentAccount.token)
                .Header("signature", Util.Helpers.Encrypt.Md5By32(SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks + ProtocolBase.ImLinkSignUri))
                .Header("version", SDKClient.Instance.property.CurrentAccount.httpVersion ?? "1.0")
                .Header("timeStamp", SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks)
                .JsonData<Model.GetUserPackage>(package)
                .OnFail((s, c) => SDKClient.logger.Error($"GetUser 调用失败: {s},错误码：{(int)c}"))
                  .ContentType(Util.Webs.Clients.HttpContentType.Json)
                  .ResultFromJson<Model.GetUserPackage>();
            return resp;
        }


        /// <summary>
        /// 获取好友信息
        /// </summary>
        /// <param name="package"></param>
        /// <returns></returns>
        public static Model.GetFriendPackage GetFreind(Model.GetFriendPackage package)
        {
            var resp = new Util.Webs.Clients.WebClient().Post(IMServiceURL.GetFriend)
                  .Header("token", SDKClient.Instance.property.CurrentAccount.token)
                .Header("signature", Util.Helpers.Encrypt.Md5By32(SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks + ProtocolBase.ImLinkSignUri))
                .Header("version", SDKClient.Instance.property.CurrentAccount.httpVersion ?? "1.0")
                .Header("timeStamp", SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks)
                .JsonData(package)
                .OnFail((s, c) => SDKClient.logger.Error($"GetUser 调用失败: {s},错误码：{(int)c}"))
                  .ContentType(Util.Webs.Clients.HttpContentType.Json)
                  .ResultFromJson<Model.GetFriendPackage>();
            return resp;
        }

        /// <summary>
        /// 获取离线消息
        /// </summary>
        /// <param name="package"></param>
        /// <returns></returns>
        public static Model.GetOfflineMessageListPackage GetOfflineMessageList(Model.GetOfflineMessageListPackage package)
        {
            var resp = new Util.Webs.Clients.WebClient().Post(IMServiceURL.GetOfflineMessageList)
                  .Header("token", SDKClient.Instance.property.CurrentAccount.token)
                .Header("signature", Util.Helpers.Encrypt.Md5By32(SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks + ProtocolBase.ImLinkSignUri))
                .Header("version", SDKClient.Instance.property.CurrentAccount.httpVersion ?? "1.0")
                .Header("timeStamp", SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks)
                .JsonData<Model.GetOfflineMessageListPackage>(package)
                .OnFail((s, c) => SDKClient.logger.Error($"GetOfflineMessageList 调用失败: {s},错误码：{(int)c}"))
                  .ContentType(Util.Webs.Clients.HttpContentType.Json)
                  .ResultFromJson<Model.GetOfflineMessageListPackage>();
            return resp;
        }

        public static async Task<TempCustomSetResponse> GetTempCSRoomlist()
        {
            var resp =await new Util.Webs.Clients.WebClient().Post(IMServiceURL.CSGetSummaryInfo)
                  .Header("token", SDKClient.Instance.property.CurrentAccount.token)
                //.Header("signature", Util.Helpers.Encrypt.Md5By32(SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks + ProtocolBase.ImLinkSignUri))
                //.Header("version", SDKClient.Instance.property.CurrentAccount.httpVersion ?? "1.0")
                //.Header("timeStamp", SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks)
                .Data<int>("userId",SDKClient.Instance.property.CurrentAccount.userID)
                .Data<string>("appName","MJD.IM")
               
                .Data<int>("resourceType",1)
                .OnFail((s, c) => SDKClient.logger.Error($"GetTempCSRoomlist 调用失败: {s},错误码：{(int)c}"))
                  .ContentType(Util.Webs.Clients.HttpContentType.Json)
                  .ResultFromJsonAsync<TempCustomSetResponse>();

            return resp;
        }
        public static async void DownloadFileWithResume(string sourceUrl, string destinationPath,Action<long,long>InitProgress,  Action<long> downloadProgressChanged, 
            Action<bool> downloadDataCompleted, System.Threading.CancellationToken? cancellationToken = null)
        {
            long existLen = 0;
            FileStream saveFileStream;
            if (File.Exists(destinationPath))
            {
                var fInfo = new FileInfo(destinationPath);
                existLen = fInfo.Length;
            }
            else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
            }
            if (existLen > 0)
                saveFileStream = new FileStream(destinationPath,
                                                            FileMode.Append, FileAccess.Write,
                                                            FileShare.ReadWrite);
            else
            {
                saveFileStream = new FileStream(destinationPath,
                                                            FileMode.Create, FileAccess.Write,
                                                            FileShare.ReadWrite);
            }
            HttpClient httpClient = new HttpClient();
            try
            {
                HttpRequestMessage msg = new HttpRequestMessage(HttpMethod.Get, sourceUrl);
                msg.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(existLen, null);
                var response = await httpClient.SendAsync(msg, HttpCompletionOption.ResponseHeadersRead);
                long len = response.Content.Headers.ContentRange.Length.Value;
                
                InitProgress?.BeginInvoke(existLen, len,null,null);
                var resp = await response.Content.ReadAsStreamAsync();
                byte[] buff = new byte[1024 * 80];
                long index = 0;
                while (true)
                {
                    if(cancellationToken!=null&&cancellationToken.HasValue&&cancellationToken.Value.IsCancellationRequested)
                    {
                        saveFileStream.Flush();
                        downloadDataCompleted?.BeginInvoke(false, null, null);
                        break;
                    }
                    int r = resp.Read(buff, 0, buff.Length);

                    downloadProgressChanged?.BeginInvoke(r, null, null);
                    index += r;
                    saveFileStream.Write(buff, 0, r);
                    if (index == len)
                    {
                        saveFileStream.Flush();
                        saveFileStream.Close();
                        downloadDataCompleted?.BeginInvoke(true,null,null);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                downloadDataCompleted?.BeginInvoke(true, null, null);
                SDKClient.logger.Error(ex.Message);
                SDKClient.logger.Error(ex.StackTrace);
            }
          
        }
        /// <summary>
        /// 获取关注人列表
        /// </summary>
        /// <param name="package"></param>
        /// <returns></returns>
        public static Model.GetAttentionListPackage GetAttentionList(Model.GetAttentionListPackage package)
        {
            var resp = new Util.Webs.Clients.WebClient().Post(IMServiceURL.GetAttentionList)
                  .Header("token", SDKClient.Instance.property.CurrentAccount.token)
                .Header("signature", Util.Helpers.Encrypt.Md5By32(SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks + ProtocolBase.ImLinkSignUri))
                .Header("version", SDKClient.Instance.property.CurrentAccount.httpVersion ?? "1.0")
                .Header("timeStamp", SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks)
                .JsonData<Model.GetAttentionListPackage>(package)
                .OnFail((s, c) => SDKClient.logger.Error($"GetAttentionList 调用失败: {s},错误码：{(int)c}"))
                  .ContentType(Util.Webs.Clients.HttpContentType.Json)
                  .ResultFromJson<Model.GetAttentionListPackage>();
            return resp;
        }


        /// <summary>
        /// 获取黑名单列表
        /// </summary>
        /// <param name="package"></param>
        /// <returns></returns>
        public static Model.GetBlackListPackage GetBlackList(Model.GetBlackListPackage package)
        {
            var resp = new Util.Webs.Clients.WebClient().Post(IMServiceURL.GetBlackList)
                  .Header("token", SDKClient.Instance.property.CurrentAccount.token)
                .Header("signature", Util.Helpers.Encrypt.Md5By32(SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks + ProtocolBase.ImLinkSignUri))
                .Header("version", SDKClient.Instance.property.CurrentAccount.httpVersion ?? "1.0")
                .Header("timeStamp", SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks)
                .JsonData<Model.GetBlackListPackage>(package)
                .OnFail((s, c) => SDKClient.logger.Error($"GetBlackList 调用失败: {s},错误码：{(int)c}"))
                  .ContentType(Util.Webs.Clients.HttpContentType.Json)
                  .ResultFromJson<Model.GetBlackListPackage>();
            return resp;
        }

        /// <summary>
        /// 获取好友列表
        /// </summary>
        /// <param name="package"></param>
        /// <returns></returns>
        public static Model.GetContactsListPackage GetContactsList(Model.GetContactsListPackage package)
        {
            var resp = new Util.Webs.Clients.WebClient().Post(IMServiceURL.GetContactsList)
                  .Header("token", SDKClient.Instance.property.CurrentAccount.token)
                .Header("signature", Util.Helpers.Encrypt.Md5By32(SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks + ProtocolBase.ImLinkSignUri))
                .Header("version", SDKClient.Instance.property.CurrentAccount.httpVersion ?? "1.0")
                .Header("timeStamp", SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks)
                .JsonData<Model.GetContactsListPackage>(package)
                .OnFail((s, c) => SDKClient.logger.Error($"GetContactsList 调用失败: {s},错误码：{(int)c}"))
                  .ContentType(Util.Webs.Clients.HttpContentType.Json)
                  .ResultFromJson<Model.GetContactsListPackage>();
            return resp;
        }

       
        /// <summary>
        /// 获取好友申请列表
        /// </summary>
        /// <param name="package"></param>
        /// <returns></returns>
        public static async Task<Model.GetFriendApplyListPackage> GetFriendApplyList(Model.GetFriendApplyListPackage package)
        {
            var resp = await new Util.Webs.Clients.WebClient().Post(IMServiceURL.GetFriendApplyList)
                  .Header("token", SDKClient.Instance.property.CurrentAccount.token)
                .Header("signature", Util.Helpers.Encrypt.Md5By32(SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks + ProtocolBase.ImLinkSignUri))
                .Header("version", SDKClient.Instance.property.CurrentAccount.httpVersion ?? "1.0")
                .Header("timeStamp", SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks)
                .JsonData<Model.GetFriendApplyListPackage>(package)
                .OnFail((s, c) => SDKClient.logger.Error($"GetFriendApplyList 调用失败: {s},错误码：{(int)c}"))
                  .ContentType(Util.Webs.Clients.HttpContentType.Json)
                  .ResultFromJsonAsync<Model.GetFriendApplyListPackage>();
            return resp;
        }
        /// <summary>
        /// 获取群列表
        /// </summary>
        /// <param name="package"></param>
        /// <returns></returns>
        public static Model.GetGroupListPackage GetGroupListPackage(Model.GetGroupListPackage package)
        {
            var resp = new Util.Webs.Clients.WebClient().Post(IMServiceURL.GetGroupList)
                  .Header("token", SDKClient.Instance.property.CurrentAccount.token)
                .Header("signature", Util.Helpers.Encrypt.Md5By32(SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks + ProtocolBase.ImLinkSignUri))
                .Header("version", SDKClient.Instance.property.CurrentAccount.httpVersion ?? "1.0")
                .Header("timeStamp", SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks)
                .JsonData<Model.GetGroupListPackage>(package)
                .OnFail((s, c) => SDKClient.logger.Error($"GetMyGroupList 调用失败: {s},错误码：{(int)c}"))
                  .ContentType(Util.Webs.Clients.HttpContentType.Json)
                  .ResultFromJson<Model.GetGroupListPackage>();
            return resp;
        }
        /// <summary>
        /// 获取群成员列表
        /// </summary>
        /// <param name="package"></param>
        /// <returns></returns>
        public static Model.GetGroupMemberListPackage GetMemberList(Model.GetGroupMemberListPackage package)
        {
            var resp = new Util.Webs.Clients.WebClient().Post(IMServiceURL.GetMemberList)
                  .Header("token", SDKClient.Instance.property.CurrentAccount.token)
                .Header("signature", Util.Helpers.Encrypt.Md5By32(SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks + ProtocolBase.ImLinkSignUri))
                .Header("version", SDKClient.Instance.property.CurrentAccount.httpVersion ?? "1.0")
                .Header("timeStamp", SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks)
                .JsonData<Model.GetGroupMemberListPackage>(package)
                .OnFail((s, c) => SDKClient.logger.Error($"GetMemberList 调用失败: {s},错误码：{(int)c}"))
                  .ContentType(Util.Webs.Clients.HttpContentType.Json)
                  .ResultFromJson<Model.GetGroupMemberListPackage>();
            return resp;
        }
        public static async Task<Model.GetImDataListIncrPackage> GetImDataListIncr()
        {
            Model.GetImDataListIncrPackage package = new Model.GetImDataListIncrPackage();
            package.data = new Model.GetImDataListIncrPackage.Data() { userId = SDKClient.Instance.property.CurrentAccount.userID };
            var resp = await new Util.Webs.Clients.WebClient().Post(IMServiceURL.GetImDataListIncr)
                  .Header("token", SDKClient.Instance.property.CurrentAccount.token)
                .Header("signature", Util.Helpers.Encrypt.Md5By32(SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks + ProtocolBase.ImLinkSignUri))
                .Header("version", SDKClient.Instance.property.CurrentAccount.httpVersion ?? "1.0")
                .Header("timeStamp", SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks)
                .JsonData<Model.GetImDataListIncrPackage>(package)
                .OnFail((s, c) => SDKClient.logger.Error($"GetImDataListIncr 调用失败: {s},错误码：{(int)c}"))
                  .ContentType(Util.Webs.Clients.HttpContentType.Json)
                  .ResultFromJsonAsync<Model.GetImDataListIncrPackage>();
            return resp;
        }


        public static async Task<CreateNoticeResponse> AddNotice(int groupId, string title, string content, SDKProperty.NoticeType noticeType)
        {
            var resp = await new Util.Webs.Clients.WebClient().Post(IMServiceURL.CreateNotice)
                .Header("token", SDKClient.Instance.property.CurrentAccount.token)
                .Header("signature", Util.Helpers.Encrypt.Md5By32(SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks + ProtocolBase.ImLinkSignUri))
                .Header("version", SDKClient.Instance.property.CurrentAccount.httpVersion ?? "1.0")
                .Header("timeStamp", SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks)
                .Data("userId", SDKClient.Instance.property.CurrentAccount.userID)
                .Data("groupId", groupId)
                .Data("title", title)
                .Data("content", content)
                .Data("type", (int)noticeType)
                .OnFail((s, c) => SDKClient.logger.Error($"AddNotice 调用失败: {s},错误码：{(int)c}"))
                .ContentType(Util.Webs.Clients.HttpContentType.Json)
                .Timeout(10)
                .ResultFromJsonAsync<CreateNoticeResponse>();
            if (resp == null)
            {
                resp = await new Util.Webs.Clients.WebClient().Post(IMServiceURL.CreateNotice_https)
               .Header("token", SDKClient.Instance.property.CurrentAccount.token)
               .Header("signature", Util.Helpers.Encrypt.Md5By32(SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks + ProtocolBase.ImLinkSignUri))
               .Header("version", SDKClient.Instance.property.CurrentAccount.httpVersion ?? "1.0")
               .Header("timeStamp", SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks)
               .Data("userId", SDKClient.Instance.property.CurrentAccount.userID)
               .Data("groupId", groupId)
               .Data("title", title)
               .Data("content", content)
               .Data("type", (int)noticeType)
               .OnFail((s, c) => SDKClient.logger.Error($"AddNotice 调用失败: {s},错误码：{(int)c}"))
               .ContentType(Util.Webs.Clients.HttpContentType.Json)
               .Timeout(10)
               .ResultFromJsonAsync<CreateNoticeResponse>();
            }
            return resp;
        }
        public static async Task<WebResponse> DeleteNotice(int noticeId)
        {
            var resp = await new Util.Webs.Clients.WebClient().Post(IMServiceURL.DeleteNotice)
                .Header("token", SDKClient.Instance.property.CurrentAccount.token)
                .Header("signature", Util.Helpers.Encrypt.Md5By32(SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks + ProtocolBase.ImLinkSignUri))
                .Header("version", SDKClient.Instance.property.CurrentAccount.httpVersion ?? "1.0")
                .Header("timeStamp", SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks)
                .Data("userId", SDKClient.Instance.property.CurrentAccount.userID)
                .Data("noticeId", noticeId)
                .OnFail((s, c) => SDKClient.logger.Error($"DeleteNotice 调用失败: {s},错误码：{(int)c}"))
                .ContentType(Util.Webs.Clients.HttpContentType.Json)
                .Timeout(10)
                .ResultFromJsonAsync<WebResponse>();
            if (resp == null)
            {
                resp = await new Util.Webs.Clients.WebClient().Post(IMServiceURL.DeleteNotice_https)
               .Header("token", SDKClient.Instance.property.CurrentAccount.token)
               .Header("signature", Util.Helpers.Encrypt.Md5By32(SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks + ProtocolBase.ImLinkSignUri))
               .Header("version", SDKClient.Instance.property.CurrentAccount.httpVersion ?? "1.0")
               .Header("timeStamp", SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks)
               .Data("userId", SDKClient.Instance.property.CurrentAccount.userID)
               .Data("noticeId", noticeId)
               .OnFail((s, c) => SDKClient.logger.Error($"DeleteNotice 调用失败: {s},错误码：{(int)c}"))
               .ContentType(Util.Webs.Clients.HttpContentType.Json)
               .Timeout(10)
               .ResultFromJsonAsync<WebResponse>();
            }
            return resp;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="friendApplyId">申请ID</param>
        /// <returns> 4、已过期 0、可操作</returns>
        public static int GetFriendApplyStatus(int friendApplyId)
        {
            var resp = new Util.Webs.Clients.WebClient().Post(IMServiceURL.GetFriendApplyStatus)
                .Header("token", SDKClient.Instance.property.CurrentAccount.token)
                .Header("signature", Util.Helpers.Encrypt.Md5By32(SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks + ProtocolBase.ImLinkSignUri))
                .Header("version", SDKClient.Instance.property.CurrentAccount.httpVersion ?? "1.0")
                .Header("timeStamp", SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks)
                //.Data("appName","kefang")
                // .Data("appId", SDKClient.Instance.property.CurrentAccount.userID)
                //.Data("appKey", SDKClient.Instance.property.CurrentAccount.userID)
                .Data("friendApplyId", friendApplyId)
                .OnFail((s, c) => SDKClient.logger.Error($"GetFriendApplyStatus 调用失败: {s},错误码：{(int)c}"))
               .ContentType(Util.Webs.Clients.HttpContentType.Json)
               .ResultFromJson<dynamic>();
            var auditStatus = 0;
            if (resp != null && resp.code == 0)
                auditStatus = resp.auditStatus;

            return auditStatus;
        }
        public static async Task<DTO.NoticeEntity> GetNotice(int noticeId)
        {
            var resp = await new Util.Webs.Clients.WebClient().Post(IMServiceURL.GetNotice)
                .Header("token", SDKClient.Instance.property.CurrentAccount.token)
                .Header("signature", Util.Helpers.Encrypt.Md5By32(SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks + ProtocolBase.ImLinkSignUri))
                .Header("version", SDKClient.Instance.property.CurrentAccount.httpVersion ?? "1.0")
                .Header("timeStamp", SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks)

                .Data("noticeId", noticeId)
                .OnFail((s, c) => SDKClient.logger.Error($"DeleteNotice 调用失败: {s},错误码：{(int)c}"))
                .ContentType(Util.Webs.Clients.HttpContentType.Json)

                .ResultFromJsonAsync<NoticeResponse>();
            if (resp == null)
            {
                resp = await new Util.Webs.Clients.WebClient().Post(IMServiceURL.GetNotice_https)
               .Header("token", SDKClient.Instance.property.CurrentAccount.token)
               .Header("signature", Util.Helpers.Encrypt.Md5By32(SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks + ProtocolBase.ImLinkSignUri))
               .Header("version", SDKClient.Instance.property.CurrentAccount.httpVersion ?? "1.0")
               .Header("timeStamp", SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks)

               .Data("noticeId", noticeId)
               .OnFail((s, c) => SDKClient.logger.Error($"DeleteNotice 调用失败: {s},错误码：{(int)c}"))
               .ContentType(Util.Webs.Clients.HttpContentType.Json)

               .ResultFromJsonAsync<NoticeResponse>();
            }
            if (resp != null && resp.success)
            {
                WebAPI.GetNoticeListResponse.NoticeInfo info = new GetNoticeListResponse.NoticeInfo()
                {
                    content = resp.content,
                    groupId = resp.groupId,
                    noticeId = resp.noticeId,
                    publishTime = resp.publishTime,
                    title = resp.title,
                    type = resp.type
                };
                DTO.NoticeEntity entity = new DTO.NoticeEntity() { db = info };
                return entity;
            }
            else
                return null;

        }
        public static async Task<GetNoticeListResponse> GetJoinGroupNotice(int groupId)
        {
            var resp = await new Util.Webs.Clients.WebClient().Post(IMServiceURL.GetJoinGroupNotice)
                .Header("token", SDKClient.Instance.property.CurrentAccount.token)
                .Header("signature", Util.Helpers.Encrypt.Md5By32(SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks + ProtocolBase.ImLinkSignUri))
                .Header("version", SDKClient.Instance.property.CurrentAccount.httpVersion ?? "1.0")
                .Header("timeStamp", SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks)
                .Data("type", (int)SDKProperty.NoticeType.JoinGroupNotice)
                .Data("groupId", groupId)
                .OnFail((s, c) => SDKClient.logger.Error($"GetJoinGroupNotice 调用失败: {s},错误码：{(int)c}"))
                .ContentType(Util.Webs.Clients.HttpContentType.Json)
                .Timeout(10)
                .ResultFromJsonAsync<GetNoticeListResponse>();
            if (resp == null)
            {
                resp = await new Util.Webs.Clients.WebClient().Post(IMServiceURL.GetJoinGroupNotice_https)
               .Header("token", SDKClient.Instance.property.CurrentAccount.token)
               .Header("signature", Util.Helpers.Encrypt.Md5By32(SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks + ProtocolBase.ImLinkSignUri))
               .Header("version", SDKClient.Instance.property.CurrentAccount.httpVersion ?? "1.0")
               .Header("timeStamp", SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks)
               .Data("type", (int)SDKProperty.NoticeType.JoinGroupNotice)
               .Data("groupId", groupId)
               .OnFail((s, c) => SDKClient.logger.Error($"GetJoinGroupNotice 调用失败: {s},错误码：{(int)c}"))
               .ContentType(Util.Webs.Clients.HttpContentType.Json)
               .Timeout(10)
               .ResultFromJsonAsync<GetNoticeListResponse>();
            }
            return resp;
        }
        /// <summary>
        /// 获取群公告列表
        /// </summary>
        /// <param name="groupId">群ID</param>
        /// <param name="type">操作意图：0-初始化获取，1-取最新的公告，2-取历史公告</param>
        /// <param name="maxRowNum">公告数量</param>
        /// <param name="refdate">公告发布时间</param>
        /// <returns></returns>
        public static async Task<GetNoticeListResponse> GetNoticeList(int groupId, int type, int maxRowNum, DateTime? refdate)
        {
            var resp = await new Util.Webs.Clients.WebClient().Post(IMServiceURL.GetNoticeList)
                .Header("token", SDKClient.Instance.property.CurrentAccount.token)
                .Header("signature", Util.Helpers.Encrypt.Md5By32(SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks + ProtocolBase.ImLinkSignUri))
                .Header("version", SDKClient.Instance.property.CurrentAccount.httpVersion ?? "v1.0")
                .Header("timeStamp", SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks)
                .Data("groupId", groupId)
                .Data("type", type)
                .Data("refdate", refdate == null ? "" : refdate.Value.ToString("YYYY-MM-DD HH24:mm:ss.ff"))
                .Data("maxRowNum", maxRowNum)
                .OnFail((s, c) => SDKClient.logger.Error($"GetNoticeList 调用失败: {s},错误码：{(int)c}"))
                .ContentType(Util.Webs.Clients.HttpContentType.Json)
                .Timeout(10)
                .ResultFromJsonAsync<GetNoticeListResponse>();
            if (resp == null)
            {
                resp = await new Util.Webs.Clients.WebClient().Post(IMServiceURL.GetNoticeList_https)
               .Header("token", SDKClient.Instance.property.CurrentAccount.token)
               .Header("signature", Util.Helpers.Encrypt.Md5By32(SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks + ProtocolBase.ImLinkSignUri))
               .Header("version", SDKClient.Instance.property.CurrentAccount.httpVersion ?? "1.0")
               .Header("timeStamp", SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks)
               .Data("groupId", groupId)
               .Data("type", type)
               .Data("refdate", refdate == null ? null : refdate.Value.ToString("YYYY-MM-DD HH24:mm:ss.ff"))
               .Data("maxRowNum", maxRowNum)
               .OnFail((s, c) => SDKClient.logger.Error($"GetNoticeList 调用失败: {s},错误码：{(int)c}"))
               .ContentType(Util.Webs.Clients.HttpContentType.Json)
               .Timeout(10)
               .ResultFromJsonAsync<GetNoticeListResponse>();
            }
            return resp;
        }
        /// <summary>
        /// 获取服务器时间
        /// </summary>
        /// <returns></returns>
        internal static async Task<DateTime?> GetServerDatetime()
        {
            var resp = await new Util.Webs.Clients.WebClient().Get(IMServiceURL.GetServerDatetime)
                .Header("token", SDKClient.Instance.property.CurrentAccount.token)
                .Header("signature", Util.Helpers.Encrypt.Md5By32(SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks + ProtocolBase.ImLinkSignUri))
                .Header("version", SDKClient.Instance.property.CurrentAccount.httpVersion ?? "1.0")
                .Header("timeStamp", SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks)
                .OnFail((s, c) => SDKClient.logger.Error($"GetJoinGroupNotice 调用失败: {s},错误码：{(int)c}"))
                .ContentType(Util.Webs.Clients.HttpContentType.Json)
                .Timeout(3)
                .ResultFromJsonAsync<WebResponse>();

            return DateTime.Now;
        }
        internal static async Task<bool> AddFeedBack(string content)
        {
            var resp = await new Util.Webs.Clients.WebClient().Post(ProtocolBase.AddFeedBack)
                .Header("token", SDKClient.Instance.property.CurrentAccount.token)
                .Header("signature", Util.Helpers.Encrypt.Md5By32(SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks + ProtocolBase.ImLinkSignUri))
                .Header("version", SDKClient.Instance.property.CurrentAccount.httpVersion ?? "1.0")
                .Header("timeStamp", SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks)
                .Data("content", content)
                .Data("userId", SDKClient.Instance.property.CurrentAccount.userID)
                .Data("sourceType", 2)
                .OnFail((s, c) => SDKClient.logger.Error($"AddFeedBack 调用失败: {s},错误码：{(int)c}"))
                .ContentType(Util.Webs.Clients.HttpContentType.Json)
                .Timeout(10)
                .ResultFromJsonAsync<(string appName, int code, string error)>();
            return resp.code == 0 ? true : false;
        }
        internal static async Task<SummaryInfo> GetSummaryInfo()
        {

            double lastTime = 0;
            if(SDKClient.Instance.property.CurrentAccount.GetOfflineMsgTime.HasValue)
            {
                
                lastTime = (SDKClient.Instance.property.CurrentAccount.GetOfflineMsgTime.Value.ToUniversalTime() - new DateTime(1970, 1, 1)).TotalMilliseconds;
            }
            
            var resp = await new Util.Webs.Clients.WebClient().Post(IMServiceURL.GetSummaryInfo)
                .Header("token", SDKClient.Instance.property.CurrentAccount.token)
                .Header("signature", Util.Helpers.Encrypt.Md5By32(SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks + ProtocolBase.ImLinkSignUri))
                .Header("version", SDKClient.Instance.property.CurrentAccount.httpVersion ?? "1.0")
                .Header("timeStamp", SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks)
                .Data("appName", "kefang")
                .Data("userId", SDKClient.Instance.property.CurrentAccount.userID)
                .Data("resourceType", 1)
                .Data("lastTime", lastTime)
                .OnFail((s, c) => SDKClient.logger.Error($"GetSummaryInfo 调用失败: {s},错误码：{(int)c}"))
                .ContentType(Util.Webs.Clients.HttpContentType.Json)
                .Timeout(10)
                .ResultFromJsonAsync<SummaryInfo>();
            SDKClient.logger.Info($"GetSummaryInfo:{Util.Helpers.Json.ToJson(resp)}");
            return resp;
        }
        internal static async Task<offlineMsg> GetMessage(RoomInfo info)
        {
            var resp = await new Util.Webs.Clients.WebClient().Post(IMServiceURL.GetMessage)
                .Header("token", SDKClient.Instance.property.CurrentAccount.token)
                .Header("signature", Util.Helpers.Encrypt.Md5By32(SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks + ProtocolBase.ImLinkSignUri))
                .Header("version", SDKClient.Instance.property.CurrentAccount.httpVersion ?? "1.0")
                .Header("timeStamp", SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks)
                .Data("appName", "kefang")
                .Data("userId", SDKClient.Instance.property.CurrentAccount.userID)
                .Data("resourceType", 1)
                .Data("reqList",new List<RoomInfo>() { info })
                .OnFail((s, c) => SDKClient.logger.Error($"GetMessage 调用失败: {s},错误码：{(int)c}"))
                .ContentType(Util.Webs.Clients.HttpContentType.Json)
                .Timeout(10)
                .ResultFromJsonAsync<offlineMsg>();
            SDKClient.logger.Info($"GetMessage:{Util.Helpers.Json.ToJson(resp)}");
            return resp;
        }



    }
}
