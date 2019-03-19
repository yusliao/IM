using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDKClient.WebAPI
{
    public class CustomServerURL
    {
        
#if CHECK
         private const string BaseUrl = "https://checkapi.manjd.com";
         public const string GetHistoryMessageList = "http://ltapi.xxx.com/mjd/Messages/GetHistoryMessageList";
           public const string CSKEY = "Test@2011WebService";
#elif DEBUG
        private const string BaseUrl = "https://devapi.manjd.com";
        public const string GetHistoryMessageList = "http://192.168.4.25/Messages/GetHistoryMessageList";
        public const string CSKEY = "CBLE@2011WebService";
#elif RELEASE

         private const string BaseUrl = "https://api.manjd.com";
        public const string GetHistoryMessageList = "https://api.xxx.com/mjd/Messages/GetHistoryMessageList";
         public const string CSKEY = "MJD@2017#Passport";
#elif HUIDU
        private const string BaseUrl = "https://api.manjd.com";
        public const string GetHistoryMessageList = "https://otapi.xxx.com/mjd/Messages/GetHistoryMessageList";
        public const string CSKEY = "MJD@2017#Passport";

#endif
        public const string CSlogin = BaseUrl+ "/api/onlineservice/login";
       
        public const string UpdateServicerinfo = BaseUrl + "/api/onlineservice/updateservicerinfo";
        public const string GetCSRoomlist = BaseUrl + "/api/onlineservice/recentlylist?imOpenId=";
        public const string CustomExchange = BaseUrl + "/api/onlineservice/assignServicer";
        public const string UploadIMG = BaseUrl + "/api/onlineservice/pc/upload";
         public const string QuickReplycontent = BaseUrl + "/api/onlineservice/content";
        public const string QuickReplycategory_contents = BaseUrl + "/api/onlineservice/category/content";
        public const string QuickReplycategory = BaseUrl + "/api/onlineservice/category";
        public const string GetOnLineServicerSysConfig = BaseUrl + "/api/onlineservice/sys/config";
        public const string GetSessionDate = BaseUrl + "/api/onlineservice/session/date";
        public const string GetAddressByIP = BaseUrl + "/api/location/address";
        public const string Getfreeservicers = BaseUrl + "/api/onlineservice/freeservicers";
        public const string QuickReplycategoryedit = BaseUrl + "/api/onlineservice/category/edit";
        public const string QuickReplycontentedit = BaseUrl + "/api/onlineservice/content/edit";
        public const string GetUserInfoByMobile = BaseUrl + "/api/onlineservice/session/find";
        public const string QueryuserByMobile = BaseUrl + "/api/onlineservice/user/query";
        public const string Getuserhistorylist = BaseUrl + "/api/onlineservice/user/history-list";
    }
}
