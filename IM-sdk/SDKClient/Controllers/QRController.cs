using SDKClient.Model;
using SDKClient.Protocol;
using SDKClient.WebAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SDKClient.Controllers
{
    class QRController
    {
        private static Util.Logs.ILog logger = Util.Logs.Log.GetLog(typeof(QRController));
        /// <summary>
        /// 获取二维码图片
        /// </summary>
        /// <param name="Id">个人或者群组编号</param>
        /// <param name="userOrgroup">1:个人；2：群</param>
        /// <returns></returns>
        public static string GetQrCodeImg(string Id, string userOrgroup)
        {
            if (string.IsNullOrEmpty(SDKClient.Instance.property.CurrentAccount.token))
            {
                var res = WebAPICallBack.Getfuck();
                SDKClient.Instance.property.CurrentAccount.token = res.token;

                logger.Error($"获取token：token:{res.token}");
            }
            //获取二维码
            var server = Util.Tools.QrCode.QrCodeFactory.Create(SDKClient.Instance.property.CurrentAccount.qrCodePath);
            var response = WebAPICallBack.GetQrCode(Id, userOrgroup);
            if (response.success)
            {
                return server.Size(Util.Tools.QrCode.QrSize.Middle).Save(response.qrCode);
            }
            else
            {
                logger.Error($"获取二维码错误：imei:{SDKClient.Instance.property.CurrentAccount.imei}," +
                    $"token:{SDKClient.Instance.property.CurrentAccount.token}," +
                    $"signature:{Util.Helpers.Encrypt.Md5By32(SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks + ProtocolBase.ImLinkSignUri)}," +
                    $"timeStamp:{SDKClient.Instance.property.CurrentAccount.lastlastLoginTime.Value.Ticks}，code:{response.code}");
                logger.Error($"获取二维码错误：{response.error}，code:{response.code}");
                return null;
            }
        }
        public static string GetLoginQrCodeImg(string session)
        {

            //获取二维码
            var server = Util.Tools.QrCode.QrCodeFactory.Create(SDKProperty.QrCodePath);

            return server.Size(Util.Tools.QrCode.QrSize.Middle).Save(session);

        }
        public static string GetLoginQRCode()
        {
            GetLoginQRCodePackage package = new GetLoginQRCodePackage();
            package.data = new GetLoginQRCodePackage.Data();

            package.id = SDKProperty.RNGId;
            package.Send(SDKClient.Instance.ec);
            return package.id;
        }
        public static string QuickLogonMsg()
        {
            PCAutoLoginApplyPackage package = new PCAutoLoginApplyPackage();
            package.data = new PCAutoLoginApplyPackage.Data();
            package.data.token = SDKClient.Instance.property.CurrentAccount.token;
            package.id = SDKProperty.RNGId;
            package.Send(SDKClient.Instance.ec);
            return package.id;
        }
       
    }
}
