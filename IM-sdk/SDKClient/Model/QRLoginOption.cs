using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDKClient.Model
{
    public class GetClientIDPackage:PackageInfo
    {
        public override string api => Protocol.ProtocolBase.GetClientID;
        public override int apiId => Protocol.ProtocolBase.GetClientIDCode;
        public Data data { get; set; }
        public class Data
        {
            public string clientId { get; set; }
        }

    }
    public class GetLoginQRCodePackage : PackageInfo
    {
        public override string api => Protocol.ProtocolBase.GetLoginQRCode;
        public override int apiId => Protocol.ProtocolBase.GetLoginQRCodeCode;
        public Data data { get; set; }
        public class Data
        {
            public string imei => System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(N => N.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback)?.GetPhysicalAddress().ToString();
            public string qrcode { get; set; }
        }

    }

    public class PCAutoLoginApplyPackage : PackageInfo
    {
        public override string api => Protocol.ProtocolBase.PCAutoLoginApply;
        public override int apiId => Protocol.ProtocolBase.PCAutoLoginApplyCode;
        public Data data { get; set; }
        public class Data
        {
            public string imei => System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(N => N.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback)?.GetPhysicalAddress().ToString();
           public string token { get; set; }
        }
    }
    public class QRConfirmPackage : PackageInfo
    {
        public override string api => Protocol.ProtocolBase.QRConfirm;
        public override int apiId => Protocol.ProtocolBase.QRConfirmCode;
        public Data data { get; set; }
        public class Data
        {
            public string token { get; set; }
          
            public int userId { get; set; }
        }
    }
    public class QRCancelPackage : PackageInfo
    {
        public override string api => Protocol.ProtocolBase.QRCancel;
        public override int apiId => Protocol.ProtocolBase.QRCancelCode;
        
    }
    public class QRExpiredPackage : PackageInfo
    {
        public override string api => Protocol.ProtocolBase.QRExpired;
        public override int apiId => Protocol.ProtocolBase.QRExpiredCode;
        public Data data { get; set; }
        public class Data
        {
            public string qrcode { get; set; }
            /// <summary>
            /// 0表示一直未扫描过期，1表示已扫码但未点确定过期，2 表示其它
            /// </summary>
            public int reason { get; set; }
            public string userId { get; set; }
        }
    }
    public class QRScanPackage : PackageInfo
    {
        public override string api => Protocol.ProtocolBase.QRScan;
        public override int apiId => Protocol.ProtocolBase.QRScanCode;
        public Data data { get; set; }
        public class Data
        {
            public string mobile { get; set; }
            public string username { get; set; }
            public string photo { get; set; }
            public string userId { get; set; }
        }
    }
    public class DeviceRepeatloginNotifyPackage: PackageInfo
    {
        public override string api => Protocol.ProtocolBase.DeviceRepeatloginNotify;
        public override int apiId => Protocol.ProtocolBase.DeviceRepeatloginNotifyCode;
    }
}
