using NLog;
using SDKClient.Model;
using SuperSocket.ClientEngine;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Util;
using static SDKClient.SDKProperty;

namespace SDKClient.Protocol
{
    static class ProtocolHelper
    {
        public static PackageInfo ComposeHead(this PackageInfo info,string to,string from)
        {
            info.to = to;
            info.from = from;
            info.id = SDKProperty.RNGId;
            info.time = DateTime.Now;
            return info;
        }
        internal static PackageInfo Send(this PackageInfo info, EasyClientBase easyClientBase)
        {
            Util.Logs.Log.GetLog().Info($"SEND:\tsession:\t{SDKClient.Instance.property.CurrentAccount.Session}:\r\n {Util.Helpers.Json.ToJson(info)}");
            SDKClient.Instance.OnSendCommand(info);
            return info;
        }
        internal static void ErrorLog(this PackageInfo info)
        {
            Util.Logs.Log.GetLog().Error($"session:\t{SDKClient.Instance.property.CurrentAccount.Session}\r\n{Util.Helpers.Json.ToJson(info)}");
        }
        internal static void RECVLog(this PackageInfo info)
        {
            Util.Logs.Log.GetLog().Info($"RECV:\tsession:\t{SDKClient.Instance.property.CurrentAccount.Session}:\r\n {Util.Helpers.Json.ToJson(info)}");
        }
    }
}
