using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using SDKClient.Model;
using System.Net.Sockets;
using Util;
using System.IO;



using SuperSocket.ProtoBase;
using SuperSocket.SocketBase.Protocol;
using SDKClient.Protocol;

namespace SDKClient.P2P
{
    public class P2PClient
    {
        
        /// 文件缓存，key:msgId
        /// </summary>
        internal static Dictionary<string, P2PClient> FileCache { get; set; } = new Dictionary<string, P2PClient>();
        private static NLog.Logger Logger  = NLog.LogManager.GetCurrentClassLogger();
        TcpClient tcpClient = null;
        /// <summary>
        /// 文件发送方
        /// </summary>
        public int From { get; set; }
        /// <summary>
        /// 文件接收方
        /// </summary>
        public int To { get; set; }
        public IPAddress RemoteIP { get; set; }
        public int RemotePort { get; set; }
        public string MD5 { get; set; }
        public string MsgId { get; set; }
        public long FileSize { get; set; }
        private bool IsCancelled = false;
        public System.Threading.CancellationToken? CancellationToken { get; set; }
        public event Action<long> SetProgressSize;
        public event Action<(int isSuccess, string imgMD5, string imgId,NotificatonPackage notifyPackage)> SendComplete;
        public event Action<long> ProgressChanged;
        private System.Threading.Thread recvThread = null;
        /// <summary>
        /// 要接收的文件名
        /// </summary>
        public string FileName { get; set; }

       // public event EventHandler<BinaryRequestInfo> NewDataRecv; //转发服务端数据
        public  void SendBody(CustomProtocolSession session)
        {
            Logger.Info($"在线文件开始发送:file:{FileName}，msgid:{MsgId}");
            
            if (!File.Exists(FileName))
            {
                    
                string content = $"源文件被修改或删除，您取消了\"{Path.GetFileName(FileName)}\"({FileSize.GetFileSizeString()})的发送，文件传输失败。";
                NotificatonPackage notificaton = new NotificatonPackage()
                {

                    Content = content,
                    ErrorCode = SDKProperty.ErrorState.AppError,
                    IsSuccess = false,
                    From = From,
                    To = To,
                    FileSize = FileSize,
                    FileName = FileName,
                    Error = "文件不存在"
                };
                Util.Helpers.Async.Run(async () => await DAL.DALMessageHelper.UpdateMsgContent(MsgId, content, SDKProperty.MessageType.notification, SDKProperty.MessageState.cancel));
                this.SendComplete?.Invoke((0, MD5, MsgId, notificaton));
                return;
            }
            try
            {
                using (FileStream fs = new FileStream(FileName, FileMode.Open, FileAccess.Read))
                {
                    fs.Seek(0, SeekOrigin.Begin);

                    int index = 0;
                    if (this.SetProgressSize != null)
                        this.SetProgressSize(fs.Length);

                    while (!IsCancelled)
                    {

                        if (this.CancellationToken == null || (this.CancellationToken != null && !this.CancellationToken.Value.IsCancellationRequested))
                        {
                            if (session.Connected)
                            {
                                byte[] requestNameData = Encoding.ASCII.GetBytes("Body");
                                if (!IsCancelled)
                                {
                                    session.Send(requestNameData, 0, requestNameData.Length);
                                }

                                byte[] buff = new byte[4096];
                                int len = fs.Read(buff, 0, 4096);
                                
                                byte[] bodylen = BitConverter.GetBytes(len);
                                if (!IsCancelled)
                                {
                                    session.Send(bodylen, 0, 4);

                                    session.Send(buff, 0, len);
                                }
                                index += len;
                                this.ProgressChanged?.Invoke(index);
                                if (index == FileSize)
                                {

                                    Logger.Info($"在线文件发送完毕:file:{FileName}，msgid:{MsgId}");
                                    NotificatonPackage notificaton = new NotificatonPackage()
                                    {
                                        MsgId = SDKProperty.RNGId,
                                        Content = $"对方已成功接收您发送的文件\"{Path.GetFileName(FileName)}\"({FileSize.GetFileSizeString()})",
                                        ErrorCode = SDKProperty.ErrorState.None,
                                        IsSuccess = true,
                                        From = From,
                                        To = To,
                                        FileSize = FileSize,
                                        FileName = FileName
                                    };
                                    int i = Util.Helpers.Async.Run(async () => await SDKProperty.SQLiteConn.ExecuteAsync($"update messageDB set fileState={(int)SDKProperty.ResourceState.IsCompleted} where msgId='{MsgId}'"));
                                    Util.Helpers.Async.Run(async () => await DAL.DALMessageHelper.SendMsgtoDB(notificaton));

                                    this.SendComplete?.Invoke((1, MD5, MsgId, notificaton));

                                    break;
                                }
                            }
                            else
                            {
                                Logger.Info($"在线文件发送失败:file:{FileName}，msgid:{MsgId}");
                                if (IsCancelled)
                                    return;
                                string content = $"网络异常 \"{Path.GetFileName(FileName)}\"({FileSize.GetFileSizeString()}) 文件传输失败。";
                                NotificatonPackage notificaton = new NotificatonPackage()
                                {
                                    MsgId = SDKProperty.RNGId,
                                    Content = content,
                                    ErrorCode = SDKProperty.ErrorState.NetworkException,
                                    IsSuccess = false,
                                    From = From,
                                    To = To,
                                    FileSize = FileSize,
                                    FileName = FileName
                                };
                                Util.Helpers.Async.Run(async () => await DAL.DALMessageHelper.UpdateMsgContent(MsgId, content, SDKProperty.MessageType.notification, SDKProperty.MessageState.cancel));
                                this.SendComplete?.Invoke((0, MD5, MsgId, notificaton));
                                break;
                            }
                        }
                        else
                        {
                            Logger.Info($"在线文件取消发送:file:{FileName}，msgid:{MsgId}");
                            string content = $"您取消了\"{Path.GetFileName(FileName)}\"({FileSize.GetFileSizeString()})的发送，文件传输失败。";
                            Util.Helpers.Async.Run(async () => await DAL.DALMessageHelper.UpdateMsgContent(MsgId, content, SDKProperty.MessageType.notification, SDKProperty.MessageState.cancel));
                            SendQuit(session);
                            break;
                        }
                    }
                   


                }
            }
            
            catch(SocketException ex)
            {
                Logger.Error($"在线文件发送失败:file:{FileName}，msgid:{MsgId}");
                string content = $"对方取消了\"{Path.GetFileName(FileName)}\"({FileSize.GetFileSizeString()})的发送，文件传输失败。";
                NotificatonPackage notificaton = new NotificatonPackage()
                {
                    
                    Content = content,
                    ErrorCode = SDKProperty.ErrorState.NetworkException,
                    IsSuccess = false,
                    From = From,
                    To = To,
                    FileSize = FileSize,
                    FileName = FileName
                };
                Util.Helpers.Async.Run(async () => await DAL.DALMessageHelper.UpdateMsgContent(MsgId, content, SDKProperty.MessageType.notification, SDKProperty.MessageState.cancel));
                this.SendComplete?.Invoke((0, MD5, MsgId, notificaton));
            }
            catch (Exception ex)
            {
                Logger.Error($"在线文件发送失败:file:{FileName}，msgid:{MsgId}");
                string content = $"网络异常，您取消了\"{Path.GetFileName(FileName)}\"({FileSize.GetFileSizeString()})的发送，文件传输失败。";
                NotificatonPackage notificaton = new NotificatonPackage()
                {

                    Content = content,
                    ErrorCode = SDKProperty.ErrorState.AppError,
                    IsSuccess = false,
                    From = From,
                    To = To,
                    FileSize = FileSize,
                    FileName = FileName,
                    Error = ex.Message
                };
                Util.Helpers.Async.Run(async () => await DAL.DALMessageHelper.UpdateMsgContent(MsgId, content, SDKProperty.MessageType.notification, SDKProperty.MessageState.cancel));
                this.SendComplete?.Invoke((0, MD5, MsgId, notificaton));
            }


        }
        private void SendQuit(CustomProtocolSession session)
        {
            try
            {
                byte[] requestNameData = Encoding.ASCII.GetBytes("Quit");
                session.Send(requestNameData, 0, requestNameData.Length);
                byte[] bodylen = Encoding.UTF8.GetBytes(MsgId);

                session.Send(BitConverter.GetBytes(bodylen.Length), 0, 4);
               
                session.Send(bodylen, 0, bodylen.Length);
            }
            catch (Exception)
            {
                Logger.Error($"在线文件取消消息发送失败:file:{FileName}，msgid:{MsgId}");
                string content = $"对方取消了\"{Path.GetFileName(FileName)}\"({FileSize.GetFileSizeString()})的发送，文件传输失败。";
                NotificatonPackage notificaton = new NotificatonPackage()
                {
                   
                    Content = content,
                    ErrorCode = SDKProperty.ErrorState.NetworkException,
                    IsSuccess = false,
                    From = From,
                    To = To,
                    FileSize = FileSize,
                    FileName = FileName
                };
                Util.Helpers.Async.Run(async () => await DAL.DALMessageHelper.UpdateMsgContent(MsgId, content, SDKProperty.MessageType.notification, SDKProperty.MessageState.cancel));
                this.SendComplete?.Invoke((0, MD5, MsgId, notificaton));
            }

        }
        /// <summary>
        /// 接收端发送退出消息
        /// </summary>
        private void SendQuit()
        {
            try
            {
                var networkStream = this.tcpClient.GetStream();
                byte[] requestNameData = Encoding.ASCII.GetBytes("Quit");
                networkStream.Write(requestNameData,0,requestNameData.Length);


                byte[] bodylen = Encoding.UTF8.GetBytes(MsgId);
                var blen = BitConverter.GetBytes(bodylen.Length);
                networkStream.Write(blen, 0,4);
                networkStream.Write(bodylen,0, bodylen.Length);
                networkStream.Flush();
                System.Threading.Thread.Sleep(4000);
            }
            catch (Exception)
            {
                Logger.Error($"在线文件取消消息发送失败:file:{FileName}，msgid:{MsgId}");
                HandleNetworkError();
            }

        }
        /// <summary>
        /// 收到接收端取消接收命令，中止发送
        /// </summary>
        public void CancelSend()
        {
            IsCancelled = true;
            string content = $"对方取消了\"{Path.GetFileName(FileName)}\"({FileSize.GetFileSizeString()})的接收，文件传输失败。";
           
            NotificatonPackage notificaton = new NotificatonPackage()
            {
                
                Content = content,
                ErrorCode = SDKProperty.ErrorState.Cancel,
                IsSuccess = false,
                From = From,
                To = To,
                FileSize = FileSize,
                FileName = FileName
            };
            this.SendComplete?.Invoke((0, MD5, MsgId, notificaton));
            Util.Helpers.Async.Run(async () => await DAL.DALMessageHelper.UpdateMsgContent(MsgId, content, SDKProperty.MessageType.notification, SDKProperty.MessageState.cancel));
        }
        /// <summary>
        /// 发送文件头请求
        /// </summary>
        public void SendHeader()
        {
          
            FileHead fileHead = new FileHead()
            {
                From = From,
                FileName = FileName,
                MD5 = MD5,
                MsgId = MsgId,
                To = To
                
            };
            try
            {
                var networkStream = this.tcpClient.GetStream();
                byte[] requestNameData = Encoding.ASCII.GetBytes("File");
                networkStream.Write(requestNameData, 0, requestNameData.Length);

                var temp = Util.Helpers.Json.ToJson(fileHead);
                byte[] filedata = Encoding.UTF8.GetBytes(temp);

                byte[] bodylen = BitConverter.GetBytes(filedata.Length);
                networkStream.Write(bodylen, 0, 4);
                networkStream.Write(filedata, 0, filedata.Length);
                networkStream.Flush();
               
            }
            catch (Exception)
            {
                Logger.Error($"在线文件消息头部发送失败:file:{FileName}，msgid:{MsgId}");
                HandleNetworkError();
            }

        }
        
        public bool TryConnect()
        {
            if(tcpClient==null)
                tcpClient = new TcpClient();
            try
            {

                tcpClient.Connect(RemoteIP, RemotePort);
                tcpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                this.recvThread = new System.Threading.Thread((o) => RecvCallBack());
                tcpClient.ReceiveTimeout = 6 * 1000;
                this.recvThread.IsBackground = true;
                this.recvThread.Start();
            }
            catch (Exception)
            {
                Logger.Error($"在线文件连接建立失败:file:{FileName}，msgid:{MsgId}");
                string content = $"您取消了\"{Path.GetFileName(FileName)}\"({FileSize.GetFileSizeString()})的接收，文件传输失败。";
                Util.Helpers.Async.Run(async () => await DAL.DALMessageHelper.UpdateMsgContent(MsgId, content, SDKProperty.MessageType.notification, SDKProperty.MessageState.cancel));
                return false;
            }
            return tcpClient.Connected;

        }
        private void RecvCallBack()
        {
            try
            {
                bool foundHeader = false;
                int total = 0;
                using (FileStream fs = new FileStream(FileName, FileMode.Create, FileAccess.Write))
                {
                    while (!IsCancelled)
                    {
                        if (total == FileSize)
                        {
                            if (this.CancellationToken == null || (this.CancellationToken != null && !this.CancellationToken.Value.IsCancellationRequested))
                            {
                                NotificatonPackage notificaton = new NotificatonPackage()
                                {
                                    MsgId = SDKProperty.RNGId,
                                    Content = $"您已成功接收文件\"{Path.GetFileName(FileName)}\"({FileSize.GetFileSizeString()})",
                                    ErrorCode = SDKProperty.ErrorState.None,
                                    IsSuccess = true,
                                    From = From,
                                    To = To,
                                    FileSize = FileSize,
                                    FileName = FileName

                                };
                               
                                string content = FileName;
                                int i = Util.Helpers.Async.Run(async () => await SDKProperty.SQLiteConn.ExecuteAsync($"update messageDB set fileState={(int)SDKProperty.ResourceState.IsCompleted},content='{FileName}',fileName='{FileName}' where msgId='{MsgId}'"));
                               
                                this.SendComplete?.Invoke((1, MD5, MsgId, notificaton));
                                fs.Flush();
                            }
                            else
                            {
                                string content = $"您取消了\"{Path.GetFileName(FileName)}\"({FileSize.GetFileSizeString()})的接收，文件传输失败。";
                                Util.Helpers.Async.Run(async () => await DAL.DALMessageHelper.UpdateMsgContent(MsgId, content, SDKProperty.MessageType.notification, SDKProperty.MessageState.cancel));
                                NotificatonPackage notificaton = new NotificatonPackage()
                                {
                                    MsgId = SDKProperty.RNGId,
                                    Content = content,
                                    ErrorCode = SDKProperty.ErrorState.Cancel,
                                    IsSuccess = false,
                                    From = From,
                                    To = To,
                                    FileSize = FileSize,
                                    FileName = FileName
                                };

                                Logger.Info($"您取消了在线文件的接收:file:{FileName}，msgid:{MsgId}");
                                IsCancelled = true;
                                Util.Helpers.Async.Run(async () => await DAL.DALMessageHelper.UpdateMsgContent(MsgId, content, SDKProperty.MessageType.notification, SDKProperty.MessageState.cancel));
                                this.SendComplete?.Invoke((0, MD5, MsgId, notificaton));

                            }
                            break;
                        }
                        int curCount = 0;
                        if (!foundHeader)
                        {
                            byte[] head = new byte[8];
                            int index = 0;
                            while (!IsCancelled)
                            {
                                if (this.CancellationToken == null || (this.CancellationToken != null && !this.CancellationToken.Value.IsCancellationRequested))
                                {
                                    if (tcpClient.Connected)// && System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
                                    {
                                        int headlen = tcpClient.GetStream().Read(head, index, 8 - index);
                                        index += headlen;
                                        if (index == 8)
                                        {
                                            string h = Encoding.ASCII.GetString(head, 0, 4);
                                            if (h.ToLower() == "body")
                                            {
                                                curCount = BitConverter.ToInt32(head, 4);
                                                break;
                                            }
                                            else if (h.ToLower() == "quit")
                                            {
                                                IsCancelled = true;
                                                //P2PPackage package = new P2PPackage()
                                                //{
                                                //    RoomId = From,
                                                //    FileName = FileName,
                                                //    MD5 = MD5,
                                                //    MsgId = MsgId,
                                                //    PackageCode = P2PPakcageState.cancel
                                                //};
                                                //SDKClient.Instance.OnP2PPackagePush(package);
                                                string content = $"对方取消了\"{Path.GetFileName(FileName)}\"({FileSize.GetFileSizeString()})的发送，文件传输失败。";
                                                
                                              
                                                Util.Helpers.Async.Run(async () => await DAL.DALMessageHelper.UpdateMsgContent(MsgId, content, SDKProperty.MessageType.notification, SDKProperty.MessageState.cancel));
                                                NotificatonPackage notificaton = new NotificatonPackage()
                                                {
                                                    MsgId = SDKProperty.RNGId,
                                                    Content = content,
                                                    ErrorCode = SDKProperty.ErrorState.Cancel,
                                                    IsSuccess = false,
                                                    From = From,
                                                    To = To,
                                                    FileSize = FileSize,
                                                    FileName = FileName
                                                };
                                                this.SendComplete?.Invoke((0, MD5, MsgId, notificaton));

                                                return;
                                            }
                                            else
                                                index = 0;
                                        }
                                    }
                                    else
                                    {
                                        HandleNetworkError();
                                        return;
                                    }
                                }
                                else
                                {
                                    Logger.Info($"您取消了在线文件的接收:file:{FileName}，msgid:{MsgId}");
                                    IsCancelled = true;
                                    string content = $"您取消了\"{Path.GetFileName(FileName)}\"({FileSize.GetFileSizeString()})的接收，文件传输失败。";
                                    Util.Helpers.Async.Run(async () => await DAL.DALMessageHelper.UpdateMsgContent(MsgId, content, SDKProperty.MessageType.notification, SDKProperty.MessageState.cancel));
                                    SendQuit();
                                }

                            }

                        }
                        int totalParse = 0;
                        while (!IsCancelled)
                        {
                            if (this.CancellationToken == null || (this.CancellationToken != null && !this.CancellationToken.Value.IsCancellationRequested))
                            {
                                byte[] buff = new byte[curCount];
                                if (tcpClient.Connected)//&& System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
                                {
                                 
                                    int len = tcpClient.GetStream().Read(buff, 0, curCount - totalParse);
                                    fs.Write(buff, 0, len);

                                    total += len;
                                    this.ProgressChanged?.Invoke(total);
                                    totalParse += len;

                                    if (totalParse == curCount)
                                        break;
                                }
                                else
                                {
                                    HandleNetworkError();
                                    return;
                                }
                            }
                            else
                            {
                                Logger.Info($"您取消了在线文件的接收:file:{FileName}，msgid:{MsgId}");
                                IsCancelled = true;
                                string content = $"您取消了\"{Path.GetFileName(FileName)}\"({FileSize.GetFileSizeString()})的接收，文件传输失败。";
                                Util.Helpers.Async.Run(async () => await DAL.DALMessageHelper.UpdateMsgContent(MsgId, content, SDKProperty.MessageType.notification, SDKProperty.MessageState.cancel));
                                SendQuit();
                            }
                        }

                    }
                }
            }
            catch (Exception)
            {
                Logger.Error($"在线文件的接收出现异常:file:{FileName}，msgid:{MsgId}");
                IsCancelled = true;
                P2PPackage package = new P2PPackage()
                {
                    RoomId = From,
                    FileName = FileName,
                    MD5 = MD5,
                    MsgId = MsgId,
                    PackageCode = P2PPakcageState.stop
                };
                SDKClient.Instance.OnP2PPackagePush(package);
                HandleNetworkError();
            }
         
        }

        private void HandleNetworkError()
        {
            string content = $"对方取消了\"{Path.GetFileName(FileName)}\"({FileSize.GetFileSizeString()})的发送，文件传输失败。";
            Util.Helpers.Async.Run(async () => await DAL.DALMessageHelper.UpdateMsgContent(MsgId, content, SDKProperty.MessageType.notification, SDKProperty.MessageState.cancel));
            NotificatonPackage notificaton = new NotificatonPackage()
            {
                MsgId = SDKProperty.RNGId,
                Content = content,
                ErrorCode = SDKProperty.ErrorState.NetworkException,
                IsSuccess = false,
                From = From,
                To = To,
                FileSize = FileSize,
                FileName = FileName
            };
            this.SendComplete?.Invoke((0, MD5, MsgId, notificaton));
        }



    }
    
}
