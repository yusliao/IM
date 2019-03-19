using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDKClient.WebAPI
{
    class IMServiceURL
    {
#if DEBUG
        private const string BaseUrl = "http://192.168.4.25/";
         private const string NoticeUrl_http = "http://192.168.4.21:8299/";
          private const string NoticeUrl_https = "https://192.168.4.21:8299/";
       
#elif CHECK
        private const string BaseUrl = "http://ltapi.xxx.com/webapi/";
         private const string NoticeUrl_http = "http://ltim.xxx.com:18040/square/";
        private const string NoticeUrl_https = "https://ltapi.xxx.com/square/";
        private const string NoticePort = "";
#elif RELEASE

        private const string BaseUrl = "https://api.xxx.com/webapi/";
        private const string NoticeUrl_http = "http://im.xxx.com:18040/square/";
        private const string NoticeUrl_https = "https://im.xxx.com:18040/square/";
        private const string NoticePort = "18040";
#elif HUIDU
        private const string BaseUrl = "https://otapi.xxx.com/webapi/";
        private const string NoticeUrl_http = "http://otim.xxx.com:18040/square/";
        private const string NoticeUrl_https = "https://otim.xxx.com:18040/square/";
        private const string NoticePort = "18040";
#endif
        public static string GetAttentionList = BaseUrl + SDKClient.Instance.property.CurrentAccount.httpVersion + "/Contacts/GetAttentionListIncr";
        public static string GetBlackList = BaseUrl + SDKClient.Instance.property.CurrentAccount.httpVersion + "/Contacts/GetMyBlackListIncr";

        public static string GetCard = BaseUrl+ SDKClient.Instance.property.CurrentAccount.httpVersion + "/User/GetCard";
        public static string GetContactsList = BaseUrl + SDKClient.Instance.property.CurrentAccount.httpVersion + "/Contacts/GetFriendListIncr";
        public static string GetFriendApplyList = BaseUrl + SDKClient.Instance.property.CurrentAccount.httpVersion + "/Contacts/GetFriendApplyList";
        public static string GetFriend = BaseUrl + SDKClient.Instance.property.CurrentAccount.httpVersion + "/Contacts/GetFriend";
        public static string GetGroup = BaseUrl + SDKClient.Instance.property.CurrentAccount.httpVersion + "/Group/GetGroup";
        public static string GetMember = BaseUrl + SDKClient.Instance.property.CurrentAccount.httpVersion + "/Group/GetMember";

        public static string GetMemberList = BaseUrl + SDKClient.Instance.property.CurrentAccount.httpVersion + "/Group/GetMemberList";

        public static string GetImDataListIncr = BaseUrl + SDKClient.Instance.property.CurrentAccount.httpVersion + "/Contacts/GetImDataListIncr"; 
        public static string GetOfflineMessageList = BaseUrl + SDKClient.Instance.property.CurrentAccount.httpVersion + "/Messages/GetOfflineMessageList";
      
        public static string GetStrangerList = BaseUrl + SDKClient.Instance.property.CurrentAccount.httpVersion + "/Contacts/GetStrangerList";
        public static string GetFriendApplyStatus = BaseUrl+SDKClient.Instance.property.CurrentAccount.httpVersion+ "/Contacts/GetFriendApplyStatus";
        public static string GetUser = BaseUrl + SDKClient.Instance.property.CurrentAccount.httpVersion + "/User/GetUser";
        public static string GetUserPrivacySetting =BaseUrl + SDKClient.Instance.property.CurrentAccount.httpVersion + "/UserSetting/GetUserPrivacySetting";
        public static string GetGroupList = BaseUrl + SDKClient.Instance.property.CurrentAccount.httpVersion + "/Group/GetMyGroupList";

#if RELEASE || HUIDU
        public static string CSGetSummaryInfo = NoticeUrl_https + SDKClient.Instance.property.CurrentAccount.httpVersion + "/OfflineMsg/CSGetSummaryInfo";
        public static string GetSummaryInfo = NoticeUrl_https + SDKClient.Instance.property.CurrentAccount.httpVersion + "/OfflineMsg/GetSummaryInfo";
        public static string GetMessage = NoticeUrl_https + SDKClient.Instance.property.CurrentAccount.httpVersion + "/OfflineMsg/GetMessage";
#else
        public static string CSGetSummaryInfo = NoticeUrl_http + SDKClient.Instance.property.CurrentAccount.httpVersion + "/OfflineMsg/CSGetSummaryInfo";
         public static string GetSummaryInfo = NoticeUrl_http + SDKClient.Instance.property.CurrentAccount.httpVersion + "/OfflineMsg/GetSummaryInfo";
         public static string GetMessage = NoticeUrl_http + SDKClient.Instance.property.CurrentAccount.httpVersion + "/OfflineMsg/GetMessage";
#endif

        
        public static string CreateNotice = NoticeUrl_http  + SDKClient.Instance.property.CurrentAccount.httpVersion + "/Group/CreateNotice";
        public static string DeleteNotice = NoticeUrl_http  + SDKClient.Instance.property.CurrentAccount.httpVersion +  "/Group/DeleteNotice";
        public static string GetJoinGroupNotice = NoticeUrl_http  + SDKClient.Instance.property.CurrentAccount.httpVersion + "/Group/GetTopNoticeList";
        public static string GetNoticeList = NoticeUrl_http  + SDKClient.Instance.property.CurrentAccount.httpVersion +  "/Group/GetNoticeList";
        public static string GetNotice = NoticeUrl_http  + SDKClient.Instance.property.CurrentAccount.httpVersion +  "/Group/GetNotice";
        public static string GetWebLinkInfo = NoticeUrl_http  + SDKClient.Instance.property.CurrentAccount.httpVersion + "/Dynamic/GetWebLinkInfo";

        public static string CreateNotice_https = NoticeUrl_https + SDKClient.Instance.property.CurrentAccount.httpVersion + "/Group/CreateNotice";
        public static string DeleteNotice_https = NoticeUrl_https + SDKClient.Instance.property.CurrentAccount.httpVersion +  "/Group/DeleteNotice";
        public static string GetJoinGroupNotice_https = NoticeUrl_https + SDKClient.Instance.property.CurrentAccount.httpVersion +  "/Group/GetTopNoticeList";
        public static string GetNoticeList_https = NoticeUrl_https + SDKClient.Instance.property.CurrentAccount.httpVersion + "/Group/GetNoticeList";
        public static string GetNotice_https = NoticeUrl_https + SDKClient.Instance.property.CurrentAccount.httpVersion + "/Group/GetNotice";
        public static string GetWebLinkInfo_https = NoticeUrl_https + SDKClient.Instance.property.CurrentAccount.httpVersion + "/Dynamic/GetWebLinkInfo";


        public static string GetUserGroupSet = BaseUrl + SDKClient.Instance.property.CurrentAccount.httpVersion + "/Group/GetUserGroupSet";
        public static string GetServerDatetime = BaseUrl + SDKClient.Instance.property.CurrentAccount.httpVersion + "GetServerDatetime";

        public static string GetBadWordEditTime = BaseUrl + SDKClient.Instance.property.CurrentAccount.httpVersion + "/Public/GetSensitiveWords";
        


    }
}
