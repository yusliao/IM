using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel.Composition;
using SuperSocket.ClientEngine;
using SDKClient.Model;
using SuperSocket.ClientEngine.Protocol;
using NLog;
using SDKClient.Protocol;
using SDKClient.WebAPI;
using System.Management;
using System.Threading;

namespace SDKClient.Command
{
    [Export(typeof(CommandBase))]
    class Login_cmd : CommandBase
    {
      
      
        public override string Name
        {
            get => Protocol.ProtocolBase.login;
        }

        public override void ExecuteCommand(EasyClientBase session, PackageInfo packageInfo)
        {
            if (packageInfo.code == 0)
            {

                LoginPackage loginPackage = packageInfo as LoginPackage;
                SDKClient.Instance.property.CurrentAccount.Session = loginPackage.data.session;
                //客服登陆请求
                if (SDKClient.Instance.property.CurUserType == SDKProperty.userType.customserver)
                {
                    var response = WebAPICallBack.GetGrant();
                    if (response.code == 0)//成功
                    {
                        var ca  = SDKClient.Instance.property.CurrentAccount;
                        ca.token = response.token;
                        ca.CustomProperty.Role = response.role ?? 0;
                        ca.CustomProperty.ServicerId = response.servicerId ?? 0;
                        ca.CustomProperty.Nickname = response.nickname;
                        ca.CustomProperty.Station = response.station;
                        AuthPackage package = new AuthPackage();
                        package.ComposeHead(loginPackage.from, null);
                        package.data = new Model.auth()
                        {
                            deviceId = loginPackage.data.deviceId,
                            session = loginPackage.data.session,
                            imVersion = SDKClient.Instance.property.CurrentAccount.imVersion,
                            userType = (int)SDKClient.Instance.property.CurUserType,
                            deviceOs = GetOSName(),
                            token = response.token
                        };
                        package.Send(session);

                        logger.Info($"session:{loginPackage.data.session}");
                    }
                    else
                    {
                        packageInfo.code = response.code;
                      
                        packageInfo.error = Util.Helpers.Enum.GetDescription<StatusCode>(packageInfo.code);
                        
                    }
                }
                else//IM登陆请求
                {



                    var res =(SDKClient.Instance.property.CurrentAccount.LoginMode &LoginMode.Scan)==LoginMode.Scan
                        ? WebAPICallBack.GetAuthByToken()
                        : WebAPICallBack.GetAuthByUserPassword();
                    
                   
                    if (res!=null&&res.code == 0)
                    {
                        SDKClient.Instance.property.CurrentAccount.userID = res.userId;
                        if ((SDKClient.Instance.property.CurrentAccount.LoginMode & LoginMode.Scan) == LoginMode.Scan)
                        {
                            SDKClient.Instance.property.CurrentAccount.loginId = res.mobile;
                        }
                        SDKClient.Instance.property.CurrentAccount.token = res.token;
                        SDKClient.Instance.property.CurrentAccount.preimei = res.preImei;
                        SDKClient.Instance.property.CurrentAccount.lastlastLoginTime = res.preLoginTime;
                        SDKClient.Instance.property.CurrentAccount.removeGagTime = res.removeGagTime;
                        
                        if (res.removeGagTime!=0)
                        {
                            
                            var removeGagTime = TimeSpan.FromSeconds(res.removeGagTime);
                            if (removeGagTime.TotalSeconds > 60)
                            {
                                logger.Info($"你被禁言{removeGagTime.TotalMinutes}分钟");
                                SDKClient.Instance.property.CurrentAccount.Isforbidden = true;

                                Task.Delay(removeGagTime).ContinueWith(t =>
                                {
                                    logger.Info($"禁言结束");
                                    SDKClient.Instance.property.CurrentAccount.Isforbidden = false;
                                });
                            }
                        }
                        
                        var removeNologinTime = TimeSpan.FromSeconds(res.removeNologinTime);
                        if (removeNologinTime.TotalSeconds > 60)
                        {
                            long days = res.removeNologinTime / (24 * 60 * 60);
                            long hours = (res.removeNologinTime % (24 * 60 * 60)) / (60 * 60);
                            long minutes = ((res.removeNologinTime % (24 * 60 * 60)) % (60 * 60))/60;
                            if(days>0)
                                packageInfo.error= string.Format("该账号已封停，剩余解封时间：{0}天{1}小时，若有疑问请联系客服：{2}", days, hours, GlobalConfig.CompanyPhone);
                            else
                                packageInfo.error= string.Format("该账号已封停，剩余解封时间：{0}小时{1}分钟，若有疑问请联系客服：{2}", hours, minutes, GlobalConfig.CompanyPhone);
                            packageInfo.code = -1;
                            SDKClient.Instance.OnSendConnState(false);
                        }
                        else
                        {
                            
                            AuthPackage package = new AuthPackage(); //用token登录
                            package.ComposeHead(loginPackage.from, null);
                            package.data = new Model.auth()
                            {
                                deviceId = loginPackage.data.deviceId,
                                token = res.token,
                                imVersion = SDKClient.Instance.property.CurrentAccount.imVersion,
                                login = (SDKClient.Instance.property.CurrentAccount.LoginMode & LoginMode.Scan) == LoginMode.Scan ? 2 : 1,
                                pcStatus = (SDKClient.Instance.property.CurrentAccount.LoginMode & LoginMode.Scan) == LoginMode.Scan ? 2 : 1,
                                deviceOs = GetOSName(),
                                session = loginPackage.data.session
                            };
                            package.Send(session);
                        }
                       
                    }
                    else
                    {
                        if (res != null)
                        {
                            packageInfo.code = (int)res.code;
                            packageInfo.error = res.error;
                        }
                        else
                        {
                            packageInfo.code = (int)Protocol.StatusCode.NoFound;
                            packageInfo.error = Util.Helpers.Enum.GetDescription<Protocol.StatusCode>(Protocol.StatusCode.NoFound);
                        }
                        //设置连接状态
                        SDKClient.Instance.OnSendConnState(false);

                    }
                    logger.Info($"session:{loginPackage.data.session}");
                }
            }
            SDKClient.Instance.OnNewDataRecv(packageInfo);
        }
        string GetOSName()
        {
            ManagementScope managementScope = new ManagementScope("root\\CIMV2");
            SelectQuery selectQuery = new SelectQuery("SELECT * FROM Win32_OperatingSystem");
            ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher(managementScope, selectQuery);
            string result = string.Empty;
            var lst = managementObjectSearcher.Get();
            foreach (var item in lst)
            {

                result = item["Caption"].ToString();
                break;
            }
            return result;
            
        }
    }
}
