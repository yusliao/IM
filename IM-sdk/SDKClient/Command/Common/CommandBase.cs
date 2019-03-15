using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel.Composition;
using SuperSocket.ClientEngine.Protocol;
using SuperSocket.ClientEngine;
using SDKClient.Model;
using SuperSocket.ProtoBase;
using NLog;
using Util;
using static SDKClient.SDKProperty;
using Util.Logs.Aspects;
using System.Threading;

namespace SDKClient
{
    /// <summary>
    /// 命令
    /// </summary>
    [Export(typeof(CommandBase))]
    public   class CommandBase : ICommand<EasyClientBase, PackageInfo> 
    {

        public static Logger logger = LogManager.GetCurrentClassLogger();
        /// <summary>
        /// 命令名称，默认是协议的api值
        /// </summary>
        public virtual string Name { get; } = "common";
        
        public virtual  void ExecuteCommand(EasyClientBase session, PackageInfo packageInfo)
        {
            
            MessageConfirmPackage package = new MessageConfirmPackage();
            package.id = packageInfo.id;
            package.from = SDKClient.Instance.property.CurrentAccount.userID.ToString();

            SDKClient.Instance.OnSendCommand(package);
            
        }
      
       
        public virtual void SendCommand(EasyClientBase session, PackageInfo packageinfo)
        {
            var str = Util.Helpers.Json.ToJson(packageinfo);
            
          
            var sendBytes = Encoding.UTF8.GetBytes("\0\0\0\0\0\0\0\0\0\0" + str + "\0\0\0\0\0\0\0\0\0\0");
            for (var i = 10; i < sendBytes.Length - 10; i++)
            {
                if (i % 2 == 0)
                {
                    sendBytes[i] = (byte)(sendBytes[i] + 7);
                }
                else
                    sendBytes[i] = (byte)(sendBytes[i] + 5);
            }
            sendBytes[0] = 0xFF;
            sendBytes[1] = 0xFF; //
            Array.Copy(BitConverter.GetBytes(sendBytes.Length - 10 - 10), 0, sendBytes, 2, 4);
            sendBytes[sendBytes.Length - 2] = 0xFF;
            sendBytes[sendBytes.Length - 1] = 0xFE;
            try
            {
                SendCompletedCallBack(true, packageinfo, session);
                session.Send(sendBytes);
               
            }
            catch (Exception ex)
            {
                
                logger.Error(ex, ex.Message);
                logger.Error($"发送失败，消息内容:{str}");
                SDKClient.Instance.OnSendConnState(false);
                SendCompletedCallBack(false, packageinfo, session);
                SDKClient.Instance.StartReConn();
                
            }
            
           
        }

        /// <summary>
        /// 发起超时事件
        /// </summary>
        /// <param name="isRanCompleted">是否发送成功</param>
        /// <param name="package">协议包</param>
        /// <param name="easyClientBase">通讯对象</param>
        /// <param name="expires">租期值，秒为单位</param>
        protected virtual void SendCompletedCallBack(bool isRanCompleted, PackageInfo package, EasyClientBase easyClientBase,int expires=45)
        {
            if(isRanCompleted)
            {
            }
            else
            {
                logger.Error($"发送失败:\tsession:{SDKClient.Instance.property.CurrentAccount.Session}:\r\n {Util.Helpers.Json.ToJson(package)}");
            }

        }

        protected  void SendHeart(EasyClientBase session)

        {
            var heart = Encoding.UTF8.GetString(new byte[] { 0x20 });
            var sendBytes = Encoding.UTF8.GetBytes("\0\0\0\0\0\0\0\0\0\0" + heart + "\0\0\0\0\0\0\0\0\0\0");
            for (var i = 10; i < sendBytes.Length - 10; i++)
            {
                if (i % 2 == 0)
                {
                    sendBytes[i] = (byte)(sendBytes[i] + 7);
                }
                else
                    sendBytes[i] = (byte)(sendBytes[i] + 5);
            }
            sendBytes[0] = 0xFF;
            sendBytes[1] = 0xFF; //
            Array.Copy(BitConverter.GetBytes(sendBytes.Length - 10 - 10), 0, sendBytes, 2, 4);
            sendBytes[sendBytes.Length - 2] = 0xFF;
            sendBytes[sendBytes.Length - 1] = 0xFE;
            try
            {
                session.Send(sendBytes);
                logger.Info($"心跳发送成功-{SDKClient.Instance.property.CurrentAccount.Session}");
            }
            catch (Exception ex)
            {
                logger.Error(ex, ex.Message);
                logger.Error($"心跳发送失败-{SDKClient.Instance.property.CurrentAccount.Session}");
                SDKClient.Instance.OnSendConnState(false);
              
                SDKClient.Instance.StartReConn();
            }


        }

    }
   
}
