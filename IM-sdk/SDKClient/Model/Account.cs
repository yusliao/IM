using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDKClient.Model
{
    public class Account
    {
        /// <summary>
        /// 客服属性
        /// </summary>

        private readonly CustomModule customModule = new CustomModule();

        public CustomModule CustomProperty
        {
            get { return customModule; }
        }
        /// <summary>
        /// 是否被禁言
        /// </summary>
        public bool Isforbidden { get; set; } = false;
        /// <summary>
        /// 解禁禁言时间
        /// </summary>
        public long removeGagTime { get; set; }
        /// <summary>
        /// 状态
        /// </summary>
        public bool State = false;
        /// <summary>
        /// 好友总数
        /// </summary>
        public int friendTotal;
        /// <summary>
        /// 群总数量
        /// </summary>
        public int GroupCount { get; set; }
        /// <summary>
        /// 已加载的群数量
        /// </summary>
        public int curGroupCount = 0;

        public int CurGroupCount
        {
            get { return curGroupCount; }
        }

        public string photo { get; set; }
        /// <summary>
        /// imei
        /// </summary>
        public string imei;
        public string preimei;
        /// <summary>
        /// 用户ID
        /// </summary>
        public int userID;
        /// <summary>
        /// 会话
        /// </summary>
        public string Session;
        public string token { get; set; }
        /// <summary>
        /// 上一次ID
        /// </summary>
        public string lastId;
        /// <summary>
        /// 用户名
        /// </summary>
        public string userName;
        /// <summary>
        /// 密码
        /// </summary>
        public string userPass;
        /// <summary>
        /// 登录ID
        /// </summary>
        public string loginId;
        public string Province { get; set; }
        public string City { get; set; }
        public byte Sex { get; set; }
        /// <summary>
        /// 登录状态
        /// </summary>
        public LoginStatus LoginStatus;
        private string _imVersion;
        public string imVersion
        {
            get
            {
#if CUSTOMSERVER
                var config = System.Configuration.ConfigurationManager.OpenExeConfiguration("CSClient.exe");
#else
                var config = System.Configuration.ConfigurationManager.OpenExeConfiguration("IMUI.exe");
#endif
                string version = "v1.3.0";
                if (config.AppSettings.Settings["externalversion"] == null)
                {
                    config.AppSettings.Settings.Add("externalversion", version);
                    config.Save();
                }
                else
                {

                    version = config.AppSettings.Settings["externalversion"].Value;
                }
                var strversion = version;
                if (!version.ToLower().Contains("v"))
                {
                    strversion = "v" + version;
                }
                if (strversion.Length > 3)
                    httpVersion = strversion.Substring(0, 4);
                else
                    httpVersion = strversion;
                return strversion.Replace("v","");
            }
            set
            {
                _imVersion = value;

            }
        }

        private string _httpVersion;
        public string httpVersion
        {
            get { return _httpVersion; }
            set { _httpVersion = value; }
        }

        /// <summary>
        /// 用户信息
        /// </summary>
        public user userInfo;
        /// <summary>
        /// 用户类型
        /// </summary>
        public int CurUserType { get; set; } = (int)SDKProperty.userType.imcustomer;
        public DateTime? lastlastLoginTime { get; set; }
        public DateTime? GetOfflineMsgTime { get; set; }
      
        public LoginMode LoginMode { get; set; }
        public bool CloseSound { get; set; }
        public bool IsRemind { get; set; }
        public string imgPath
        {
            get
            {
                var path = Path.Combine(SDKProperty.imgPath, loginId);
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                return path;
            }


        }
        public string filePath
        {
            get
            {
                var path = Path.Combine(SDKProperty.filePath, loginId);
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                return path;
            }
        }
        public string facePath
        {
            get
            {
                if (string.IsNullOrEmpty(loginId))
                {
                    if (!Directory.Exists(SDKProperty.facePath))
                        Directory.CreateDirectory(SDKProperty.facePath);
                    return SDKProperty.facePath;
                }

                var path = Path.Combine(SDKProperty.facePath, loginId);
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                return path;
            }
        }
        public string qrCodePath
        {
            get
            {
                if (string.IsNullOrEmpty(loginId))
                    return SDKProperty.QrCodePath;
                var path = Path.Combine(SDKProperty.QrCodePath, loginId);
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                return path;
            }
        }


    }
    /// <summary>
    /// 登录状态
    /// </summary>
    public enum LoginStatus
    {
        Online,
        Offline
    }
    //登录模式
    [Flags]
    public enum LoginMode
    {
        None = 0,
        Save = 0x01,
        Scan = 0x02,
        All = 0x03
    }
    /// <summary>
    /// 客服模块
    /// </summary>
    public class CustomModule
    {
        /// <summary>
        /// 1 管理员 2 普通人员
        /// </summary>
        public int? Role { get; set; }
        public int? ServicerId { get; set; }
        public string Nickname { get; set; }
        /// <summary>
        /// 职位;1.综合客服 2.商品客服 3.订单客服 4.售后客服 
        /// </summary>
        public int Station { get; set; }

    }

}
