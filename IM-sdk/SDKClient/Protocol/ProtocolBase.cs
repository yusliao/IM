using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDKClient.Protocol
{
    public class ProtocolBase
    {

#if CHECK
        //public const string QrLoginIP = "192.168.4.22";
        public const string QrLoginIP = "ltim.xxx.com";
        public const int QrLoginPort = 18020;

        //public const string IMIP = "192.168.10.223 ";
        public const string IMIP = "ltim.xxx.com";
#if CUSTOMSERVER
        public const int IMPort = 18010;
#else
        public const int IMPort = 18000;
#endif

        public const string BaseURL = "http://ltapi.xxx.com/webapi/";
        public const string MJDBaseURL = "http://ltapi.xxx.com/mjd";
        public const string BaseFileURL = "http://ltfile.xxx.com";
        public const string BaseDownLoadFile = "http://ltcdn.xxx.com";
        public const string ImLinkSignUri = "ffff";
        public const string LatestVersionNum = BaseFileURL + "/api/Version/GetPCVersion?versionNum=";
        public const string Grant = MJDBaseURL + "/Api/Grant";
        public static string uploadresource = BaseFileURL + "/api/"+ SDKClient.Instance.property.CurrentAccount.httpVersion+ "/FileOperator/Upload";
        public static string findresource = BaseFileURL + "/api/"+ SDKClient.Instance.property.CurrentAccount.httpVersion+"/FileOperator/IsFileExist/";
        public static string downLoadResource = BaseDownLoadFile + "/api/"+ SDKClient.Instance.property.CurrentAccount.httpVersion+ "/File/Download/";  
        public static string DownloadFileWithResume = BaseDownLoadFile+ "/api/" + SDKClient.Instance.property.CurrentAccount.httpVersion+"/FileOperator/Download?fileCode=";
        public static string downLoadUpdateFile = BaseFileURL + "/api/" + SDKClient.Instance.property.CurrentAccount.httpVersion+"/Version/GetPcSubUpgradeFile";
#elif DEBUG
        public const string QrLoginIP = "192.168.4.21";
        public const int QrLoginPort = 8399;
        
#if CUSTOMSERVER
        public const string IMIP = "192.168.4.26";
        public const int IMPort = 2018;

#else
        public const string IMIP = "192.168.4.26";
        public const int IMPort = 2018;
#endif

        public const string BaseURL = "http://192.168.4.26/";
        public const string ImLinkSignUri = "ffff";
        public const string LatestVersionNum = "http://192.168.4.24:8090/api/Version/GetPCVersion?versionNum=";
         public const string Grant = "http://192.168.4.25" + "/Api/Grant";
        public const string uploadresource = "http://ltfile.xxx.com/api/FileOperator/Upload";
        public const string findresource = "http://192.168.4.24:8090/api/FileOperator/IsFileExist/";
        public const string downLoadResource = "http://192.168.4.24:8090/api/File/Download/";
        public const string DownloadFileWithResume = "http://192.168.4.24:8090/api/FileOperator/Download?fileCode=";
        public const string downLoadUpdateFile = "http://192.168.4.24:8090/api/Version/GetPcSubUpgradeFile";
       
#elif HUIDU
        // public const string QrLoginIP = "14.17.113.145";
        public const int IMPort = 18000;
        public const string QrLoginIP = "otim.xxx.com";
        public const int QrLoginPort = 18020;
        // public const string IMIP = "14.17.113.145";
        public const string IMIP = "otim.xxx.com";
        public const string BaseURL = "https://otapi.xxx.com/webapi/";
        public const string BaseFileURL = "https://otfile.xxx.com";
        public const string BaseDownLoadFile = "https://otcdn.xxx.com";
        public const string ImLinkSignUri = "xxx";
        public const string LatestVersionNum = BaseFileURL + "/api/Version/GetPCVersion?versionNum=";
        public const string Grant = BaseURL + "Api/Grant";
        public const string uploadresource = BaseFileURL + "/api/FileOperator/Upload";
        public const string findresource = BaseFileURL + "/api/FileOperator/IsFileExist/";
        public const string downLoadResource = BaseDownLoadFile + "/api/File/Download/";
        public const string DownloadFileWithResume = BaseDownLoadFile+ "/api/FileOperator/Download?fileCode=";
        public const string downLoadUpdateFile = BaseFileURL + "/api/Version/GetPcSubUpgradeFile";
#elif RELEASE
        public const string BaseURL = "https://api.xxx.com/webapi/";
        public const string BaseFileURL = "https://file.xxx.com";
        public const string MJDURL = "https://api.xxx.com/mjd";
        public const string BaseDownLoadFile = "https://cdn.xxx.com";
        // public const string QrLoginIP = "14.17.113.145";
        public const string QrLoginIP = "im.xxx.com";
        public const int QrLoginPort = 18020;


#if CUSTOMSERVER
        //public const string IMIP = "xxx.xx.com";
        public const string IMIP = "im.xxx.com"; 
        public const int IMPort = 18010;
#else
        public const string IMIP = "im.xxx.com";
        public const int IMPort = 18000;
#endif
        public const string ImLinkSignUri = "xxx";
        public const string LatestVersionNum = BaseFileURL + "/api/Version/GetPCVersion?versionNum=";
        public const string Grant = MJDURL + "/Api/Grant";
        public const string uploadresource = BaseFileURL + "/api/FileOperator/Upload";
        public const string findresource = BaseFileURL + "/api/FileOperator/IsFileExist/";
        public const string downLoadResource = BaseDownLoadFile + "/api/File/Download/";
        public const string DownloadFileWithResume = BaseDownLoadFile+ "/api/FileOperator/Download?fileCode=";
        public const string downLoadUpdateFile = BaseFileURL + "/api/Version/GetPcSubUpgradeFile";

#endif

        public static string registerUri = BaseURL+SDKClient.Instance.property.CurrentAccount.httpVersion + "/User/Register";
        public static string AddFeedBack = BaseURL + SDKClient.Instance.property.CurrentAccount.httpVersion + "/Public/AddFeedBack";

        public static string BadWordUpdateTimeUri = BaseURL + SDKClient.Instance.property.CurrentAccount.httpVersion + "/Public/GetSensitiveWords";


        public static string SmsUri = BaseURL + SDKClient.Instance.property.CurrentAccount.httpVersion + "/message/help";
        public static string QrCodeInfoUri = BaseURL + SDKClient.Instance.property.CurrentAccount.httpVersion + "/User/GetQrCodeInfo";

        public static string AuthUri = BaseURL + SDKClient.Instance.property.CurrentAccount.httpVersion + "/User/Auth";
        public static string SearchQuery = BaseURL + SDKClient.Instance.property.CurrentAccount.httpVersion + "/api/Search/Query";
        public static string AddMsgFaceBack = BaseURL + SDKClient.Instance.property.CurrentAccount.httpVersion + "/MsgFaceBack/AddMsgFaceBack";
        public static string GetUserPcOnlineInfo = BaseURL + SDKClient.Instance.property.CurrentAccount.httpVersion + "/User/GetUserPcOnlineInfo";


        public static string ResumeUpload = "http://ltfile.xxx.com" + "/api/" + SDKClient.Instance.property.CurrentAccount.httpVersion + "/FileOperator/ResumeUploadFile";
        // public static string ResumeUpload = "http://192.168.10.116:8089/api/v1.4/fileoperator/ResumeUploadFile";


        /// <summary>
        /// 图片默认名字，用于下载失败
        /// </summary>
        public const string imgDefault = "imgDefault.png";
        /// <summary>
        /// 图片加载中的名字，用于加载中
        /// </summary>
        public const string imgLoading = "imgLoading1.gif";
        public const string ErrorPackage = "ErrorPackage";
        public const int ErrorPackageCode = 9999;
        public const string NoHandlePackage = "NoHandle";
        public const int NoHandlePackageCode = 9998;
        public const string HeartMsg = "HeartMsg";
        public const int HeartMsgCode = 1000;
        public const string login = "Connect";
        public const int loginCode = 1001;
        public const string auth = "Auth";
        public const int authCode = 1002;
        public const string message = "Message";
        public const int messageCode = 1003;
        public const string GetContactsList = "GetContactsList";
        public const int GetContactsListCode = 1004;
        public const string GetBlackList = "GetBlackList";
        public const int GetBlackListCode = 1005;
        public const string GetgroupMember = "GetMember";

        public const int GetgroupMemberCode = 1006;
        public const string GetgroupMemberList = "GetMemberList";
        public const int GetgroupMemberListCode = 1007;

        public const string adminList = "GetAdminList";
        public const int adminListCode = 1008;
        public const string GetUser = "GetUser";
        public const int GetUserCode = 1009;
        public const string UpdateUser = "UpdateUser";
        public const int UpdateUserCode = 1010;
        public const string UpdateFriendSet = "UpdateFriendSet";
        public const int UpdateFriendSetCode = 1011;
        public const string CreateGroup = "CreateGroup";
        public const int CreateGroupCode = 1012;
        public const string UpdateGroup = "UpdateGroup";
        public const int UpdateGroupCode = 1013;

        public const string GetgroupList = "GetMyGroupList";
        public const int GetgroupListCode = 1014;
        public const string AddFriend = "AddFriend";
        public const int AddFriendCode = 1015;
        public const string AddFriendAccepted = "AddFriendAccepted";
        public const int AddFriendAcceptedCode = 1016;

        public const string DeleteFriend = "DeleteFriend";
        public const int DeleteFriendCode = 1019;
        public const string UpdateFriendRelation = "UpdateFriendRelation";
        public const int UpdateFriendRelationCode = 1020;
        public const string GetFriendApplyList = "GetFriendApplyList";
        public const int GetFriendApplyListCode = 1021;
        public const string JoinGroup = "JoinGroup";
        public const int JoinGroupCode = 1023;

        public const string JoinGroupAccepted = "JoinGroupAccepted";
        public const int JoinGroupAcceptedCode = 1024;
        public const string InviteJoinGroup = "InviteJoinGroup";
        public const int InviteJoinGroupCode = 1025;

        public const string ExitGroup = "ExitGroup";
        public const int ExitGroupCode = 1026;
        public const string SetMemberPower = "SetMemberPower";
        public const int SetMemberPowerCode = 1027;

        public const string GetGroup = "GetGroup";
        public const int GetGroupCode = 1028;

        public const string UpdateUserSetsInGroup = "UpdateUserSetsInGroup";
        public const int UpdateUserSetsInGroupCode = 1029;


        public const string GetOfflineMessageList = "GetOfflineMessageList";
        public const int GetOfflineMessageListCode = 1030;
        public const string SyncMsgStatus = "SyncMsgStatus";
        public const int SyncMsgStatusCode = 1031;

        public const string DismissGroup = "DismissGroup";
        public const int DismissGroupCode = 1040;
        public const string RequestIp = "RequestIp";
        public const int RequestIpCode = 1043;
        public const string MessageConfirm = "MessageConfirm";
        public const int MessageConfirmCode = 1045;

        public const string GetAttentionList = "GetAttentionList";
        public const int GetAttentionListCode = 1046;

        public const string TopAttentionUser = "TopAttentionUser";
        public const int TopAttentionUserCode = 1047;
        public const string DeleteFriendApply = "DeleteFriendApply";
        public const int DeleteFriendApplyCode = 1048;

        public const string DeleteAttentionUser = "DeleteAttentionUser";
        public const int DeleteAttentionUserCode = 1049;

        public const string GetUserPrivacySetting = "GetUserPrivacySetting";
        public const int GetUserPrivacySettingCode = 1055;

        public const string SearchNewFriend = "SearchNewFriend";
        public const int SearchNewFriendCode = 1062;
        public const string UpdateUserDetail = "UpdateUserDetail";
        public const int UpdateUserDetailCode = 1063;
        public const string AddAttention = "AddAttention";
        public const int AddAttentionCode = 1065;
        public const string SetStrangerDoNotDisturb = "SetStrangerDoNotDisturb";
        public const int SetStrangerDoNotDisturbCode = 1067;

        public const string GetFriend = "GetFriend";
        public const int GetFriendCode = 1068;

        public const string Logout = "Logout";
        public const int LogoutCode = 1074;


        public const string CustomService = "CustomService";
        public const int CustomServiceCode = 1075;
        public const string CustomExchange = "CustomExchange";
        public const int CustomExchangeCode = 1076;
        public const string GetClientID = "GetClientID";
        public const int GetClientIDCode = 1077;
        public const string QRScan = "QRScan";
        public const int QRScanCode = 1078;
        public const string QRConfirm = "QRConfirm";
        public const int QRConfirmCode = 1079;
        public const string QRCancel = "QRCancel";
        public const int QRCancelCode = 1080;
        public const string QRExpired = "QRExpired";
        public const int QRExpiredCode = 1081;

        public const string ForceExit = "ForceExit";
        public const int ForceExitCode = 1082;
        public const string ExitNotify = "ExitNotify";
        public const int ExitNotifyCode = 1083;

        public const string GetLoginQRCode = "GetLoginQRCode";
        public const int GetLoginQRCodeCode = 1084;

        public const string PCAutoLoginApply = "PCAutoLoginApply";
        public const int PCAutoLoginApplyCode = 1093;


        public const string DeviceRepeatloginNotify = "DeviceRepeatloginNotify";
        public const int DeviceRepeatloginNotifyCode = 1085;

        public const string AddNotice = "AddNotice";
        public const int AddNoticeCode = 1086;
        public const string RetreatNotice = "RetreatNotice";
        public const int RetreatNoticeCode = 1087;
        //专用于客服的离线未读消息的同步到其它客服端
        public const string CSSyncMsgStatus = "CSSyncMsgStatus";
        public const int CSSyncMsgStatusCode = 1088;

        public const string CSMessage = "CSMessage";
        public const int CSMessageCode = 1090;



        public const string SysNotify = "SysNotify";


        public const int SysNotifyCode = 1100;


    }

}
