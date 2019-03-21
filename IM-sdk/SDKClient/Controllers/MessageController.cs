using SDKClient.Model;
using SDKClient.P2P;
using SDKClient.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Util;
using Util.ImageOptimizer;
using static SDKClient.SDKProperty;
 
namespace SDKClient.Controllers
{
    public static  class MessageController 
    {
        private static object myimg_lock = new object();
        static readonly Util.ImageOptimizer.Compressor compressor = new Util.ImageOptimizer.Compressor();//图片压缩处理对象
        public static string SendRetractMessage(string msgId, string to, chatType type = chatType.chat, int groupId = 0, SDKProperty.RetractType retractType = RetractType.Normal, SDKProperty.SessionType sessionType = SessionType.CommonChat, message.ReceiverInfo recverInfo = null)
        {
            MessagePackage package = new MessagePackage()
            {
                from = SDKClient.Instance.property.CurrentAccount.userID.ToString(),
                to = to,
                id = SDKProperty.RNGId
            };
            package.data = new message()
            {
                body = new retractBody()
                {
                    retractId = msgId,
                    retractType = (int)retractType

                },
                senderInfo = new message.SenderInfo()
                {
                    photo = SDKClient.Instance.property.CurrentAccount.photo,
                    userName = SDKClient.Instance.property.CurrentAccount.userName
                },
                receiverInfo = recverInfo,
                subType = "retract",
                chatType = to == SDKClient.Instance.property.CurrentAccount.userID.ToString() ? (int)SessionType.FileAssistant : (int)sessionType,
                type = type == chatType.chat ? nameof(chatType.chat) : nameof(chatType.groupChat)
            };
            if (type == chatType.groupChat)
            {
                package.data.groupInfo = new message.msgGroup()
                {
                    groupId = groupId
                };
            }

            package.Send(SDKClient.Instance.ec);
            return package.id;
        }
        /// <summary>
        /// 发送本文消息
        /// </summary>
        /// <param name="content">消息内容</param>
        /// <param name="to">接收者</param>
        /// <param name="userIds">被@对象集合,@ALL定义为值-1</param>
        /// <param name="type">消息类型chat,groupchat</param>
        /// <returns>id</returns>
        public static string Sendtxt(string content, string to, IList<int> userIds = null, chatType type = chatType.chat, string groupName = null, SDKProperty.SessionType sessionType = SessionType.CommonChat, message.ReceiverInfo recverInfo = null)
        {


            MessagePackage package = new MessagePackage();
            package.ComposeHead(to, SDKClient.Instance.property.CurrentAccount.userID.ToString());

            package.data = new message()
            {
                body = new TxtBody()
                {
                    text = content
                },
                senderInfo = new message.SenderInfo()
                {
                    photo = SDKClient.Instance.property.CurrentAccount.photo,
                    userName = SDKClient.Instance.property.CurrentAccount.userName ?? SDKClient.Instance.property.CurrentAccount.loginId
                },
                receiverInfo = recverInfo,
                chatType = to == SDKClient.Instance.property.CurrentAccount.userID.ToString() ? (int)SessionType.FileAssistant : (int)sessionType,
                subType = "txt",
                tokenIds = userIds,
                type = type == chatType.chat ? nameof(chatType.chat) : nameof(chatType.groupChat)
            };
            if (type == chatType.groupChat)
            {
                package.data.groupInfo = new message.msgGroup()
                {
                    groupId = to.ToInt(),
                    groupName = groupName
                };
            }

            package.Send(SDKClient.Instance.ec);
            return package.id;


        }
        public static async Task SendImgMessage(string imgFullName, Action<(int isSuccess, string imgMD5, string msgId, string smallId,
            SDKProperty.ErrorState)> SendComplete,
            string to, chatType type = chatType.chat, int groupId = 0,
            System.Threading.CancellationToken? cancellationToken = null, string groupName = null)
        {
            System.Threading.CancellationToken token;
            if (cancellationToken.HasValue)
                token = cancellationToken.Value;

            string sourcefile = imgFullName;
            CompressionResult imgResult = null;
            if (Compressor.IsFileSupported(imgFullName))
            {

                imgResult = compressor.CompressFileAsync(imgFullName, false);
                if (!string.IsNullOrEmpty(imgResult.ResultFileName))
                {
                    imgFullName = imgResult.ResultFileName;
                }

            }
#if CUSTOMSERVER
            Action<long> uploadProgressChanged = (x) =>
            {

            };
            Action<bool, string, SDKProperty.ErrorState> uploadDataCompleted = (b, s, e) =>
            {
                if (b)
                {
                    string imgId = Instance.SendImgMessage(imgFullName, to, s.Split(new char[] { ',' })[0], s.Split(new char[] { ',' })[1], type, cancellationToken);

                    SendComplete?.Invoke((1, s, imgId, null, e));

                }
                else
                {
                    SendComplete((0, imgFullName, null, null, e));
                }

            };
            await Task.Run(() => Instance.UpLoadResource(imgFullName, uploadProgressChanged, uploadDataCompleted, cancellationToken)).ConfigureAwait(false);
#else
            await ResourceController.UploadImg(imgFullName, async result =>
            {
                string imgId;
                if (result.isSuccess)
                {
                    if (Path.GetExtension(imgFullName).ToLower() == ".gif")//GIF原图发送
                    {
                        imgId = SDKProperty.RNGId;
                        SendComplete?.Invoke((1, result.imgMD5, imgId, null, SDKProperty.ErrorState.None));
                        imgId = SendImgMessage(imgFullName, to, result.imgMD5, result.imgMD5, type, cancellationToken, groupName, SessionType.CommonChat, imgId);
                    }
                    else
                    {

                        var bitImgFile = Path.Combine(SDKClient.Instance.property.CurrentAccount.imgPath, $"my{result.imgMD5}");

                        if (!File.Exists(bitImgFile))//小图本地不存在
                        {
                            var source = new System.Drawing.Bitmap(imgFullName);

                            var With = (int)Math.Min(source.Width, 300);
                            var h = With * source.Height / source.Width;
                            var bmp = Util.ImageProcess.GetThumbnail(imgFullName, With, h);
                            using (MemoryStream ms = new MemoryStream())
                            {
                                bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);

                                ms.Seek(0, SeekOrigin.Begin);
                                var bmpArray = ms.ToArray();
                                lock (myimg_lock)
                                {
                                    if (!File.Exists(bitImgFile))
                                        File.WriteAllBytes(bitImgFile, bmpArray);
                                }

                            }

                        }


                        // var smallresult = Util.Helpers.Async.Run(async () => await Instance.FindResource(bitImgFile));
                        await ResourceController.UploadImg(bitImgFile, smallresult =>
                        {
                            if (smallresult.isSuccess)
                            {
                                imgId = SDKProperty.RNGId;
                                SendComplete((1, result.imgMD5, imgId, smallresult.imgMD5, SDKProperty.ErrorState.None));
                                imgId = SendImgMessage(imgFullName, to, result.imgMD5, smallresult.imgMD5, type, cancellationToken, groupName, SessionType.CommonChat, imgId);
                            }
                            else
                            {
                                SendComplete((0, smallresult.imgMD5, null, null, smallresult.errorState));
                            }
                        }, token);

                    }
                }
                else
                {

                    SendComplete((0, result.imgMD5, null, null, result.errorState));

                }
                var fullname = Path.Combine(SDKClient.Instance.property.CurrentAccount.imgPath, result.imgMD5);
                if (!File.Exists(fullname))
                {
                    var filedata = File.ReadAllBytes(imgFullName);
                    File.WriteAllBytes(fullname, filedata);

                }
            }, token);
#endif

        }
        public static string SendImgMessage(string path, string to, string resourceId, string smallresourceId,
           chatType type = chatType.chat, System.Threading.CancellationToken? cancellationToken = null, string groupName = null, SDKProperty.SessionType sessionType = SessionType.CommonChat, string imgId = "")
        {
            MessagePackage package = new MessagePackage();
            package.ComposeHead(to,SDKClient.Instance.property.CurrentAccount.userID.ToString());
            if (!string.IsNullOrEmpty(imgId))
                package.id = imgId;
            string width = string.Empty, height = string.Empty;
            try
            {
                using (var bmp = new System.Drawing.Bitmap(path))
                {
                    width = bmp.Width.ToString();
                    height = bmp.Height.ToString();
                }
            }
            catch (Exception ex)
            {
                SDKClient.logger.Error($"发送图片消息提取图片宽高：error:{ex.Message},stack:{ex.StackTrace};\r\n");
            }
            if (to == SDKClient.Instance.property.CurrentAccount.userID.ToString())
            {

            }
            package.data = new message()
            {
                body = new ImgBody()
                {
                    fileName = path,
                    id = resourceId,
                    smallId = smallresourceId,
                    width = width,
                    height = height
                },
                senderInfo = new message.SenderInfo()
                {
                    photo = SDKClient.Instance.property.CurrentAccount.photo,
                    userName = SDKClient.Instance.property.CurrentAccount.userName ?? SDKClient.Instance.property.CurrentAccount.loginId
                },
                subType = "img",
                chatType = to == SDKClient.Instance.property.CurrentAccount.userID.ToString() ? (int)SessionType.FileAssistant : (int)sessionType,
                type = type == chatType.chat ? nameof(chatType.chat) : nameof(chatType.groupChat)
            };
            if (type == chatType.groupChat)
            {
                package.data.groupInfo = new message.msgGroup()
                {
                    groupId = to.ToInt(),
                    groupName = groupName
                };
            }

            if (cancellationToken != null && cancellationToken.HasValue)
            {
                if (!cancellationToken.Value.IsCancellationRequested)
                    package.Send(SDKClient.Instance.ec );
            }
            else
                package.Send(SDKClient.Instance.ec);
            return package.id;
        }

        public static string SendFileMessage(string path, string to, string resourceId, long fileSize, chatType type = chatType.chat, string groupName = null, int width = 0, int height = 0, string imgMD5 = null, SDKProperty.SessionType sessionType = SessionType.CommonChat, string msgId = "")
        {
            MessagePackage package = new MessagePackage()
            {
                from = SDKClient.Instance.property.CurrentAccount.userID.ToString(),
                to = to,
                id = string.IsNullOrEmpty(msgId) ? SDKProperty.RNGId : msgId
            };
            package.data = new message()
            {
                body = new fileBody()
                {
                    fileSize = fileSize,
                    fileName = path,
                    id = resourceId,
                    width = width,
                    height = height,
                    img = imgMD5
                },
                chatType = to == SDKClient.Instance.property.CurrentAccount.userID.ToString() ? (int)SessionType.FileAssistant : (int)sessionType,
                subType = "file",
                senderInfo = new message.SenderInfo()
                {
                    photo = SDKClient.Instance.property.CurrentAccount.photo,
                    userName = SDKClient.Instance.property.CurrentAccount.userName
                },
                type = type == chatType.chat ? nameof(chatType.chat) : nameof(chatType.groupChat)
            };

            if (type == chatType.groupChat)
            {
                package.data.groupInfo = new message.msgGroup()
                {
                    groupId = to.ToInt(),
                    groupName = groupName
                };
            }

            package.Send(SDKClient.Instance.ec);
            return package.id;
        }
        public static string SendFiletoDB(string path, string to, string resourceId, long fileSize, chatType type = chatType.chat, string groupName = null, int width = 0, int height = 0, string imgMD5 = null, SDKProperty.SessionType sessionType = SessionType.CommonChat, string msgId = "")
        {
            MessagePackage package = new MessagePackage()
            {
                from = SDKClient.Instance.property.CurrentAccount.userID.ToString(),
                to = to,
                id = string.IsNullOrEmpty(msgId) ? SDKProperty.RNGId : msgId
            };
            package.data = new message()
            {
                body = new fileBody()
                {
                    fileSize = fileSize,
                    fileName = Path.GetFileName(path),
                    id = resourceId,
                    width = width,
                    height = height,
                    img = imgMD5
                },

                chatType = to == SDKClient.Instance.property.CurrentAccount.userID.ToString() ? (int)SessionType.FileAssistant : (int)sessionType,
                subType = "file",
                senderInfo = new message.SenderInfo()
                {
                    photo = SDKClient.Instance.property.CurrentAccount.photo,
                    userName = SDKClient.Instance.property.CurrentAccount.userName
                },
                type = type == chatType.chat ? nameof(chatType.chat) : nameof(chatType.groupChat)
            };

            if (type == chatType.groupChat)
            {
                package.data.groupInfo = new message.msgGroup()
                {
                    groupId = to.ToInt(),
                    groupName = groupName
                };
            }
            Util.Helpers.Async.Run(async () => await DAL.DALMessageHelper.SendFiletoDB(package));

            return package.id;
        }
        public static string SendSmallVideoMessage(string path, string to, string recordTime, string resourceId, string previewId, int width, int height, long fileSize, chatType type = chatType.chat, string groupName = null, SDKProperty.SessionType sessionType = SessionType.CommonChat, string msgId = "")
        {
            MessagePackage package = new MessagePackage()
            {
                from = SDKClient.Instance.property.CurrentAccount.userID.ToString(),
                to = to,
                id = string.IsNullOrEmpty(msgId) ? SDKProperty.RNGId : msgId,
            };
            package.data = new message()
            {
                body = new smallVideoBody()
                {
                    fileSize = fileSize,
                    fileName = path,
                    id = resourceId,
                    previewId = previewId,
                    width = width,
                    height = height,

                    recordTime = recordTime

                },
                subType = Util.Helpers.Enum.GetDescription<SDKProperty.MessageType>(SDKProperty.MessageType.smallvideo),
                chatType = to == SDKClient.Instance.property.CurrentAccount.userID.ToString() ? (int)SessionType.FileAssistant : (int)sessionType,
                senderInfo = new message.SenderInfo()
                {
                    photo = SDKClient.Instance.property.CurrentAccount.photo,
                    userName = SDKClient.Instance.property.CurrentAccount.userName
                },
                type = type == chatType.chat ? nameof(chatType.chat) : nameof(chatType.groupChat)
            };

            if (type == chatType.groupChat)
            {
                package.data.groupInfo = new message.msgGroup()
                {
                    groupId = to.ToInt(),
                    groupName = groupName
                };
            }

            package.Send(SDKClient.Instance.ec);
            return package.id;
        }
        public static async void RetrySendMessageByMsgId(string msgId)
        {
            var db = await DAL.DALMessageHelper.Get(msgId);
            if (db != null)
            {

                MessagePackage package = Util.Helpers.Json.ToObject<MessagePackage>(db.Source);
                package.Send(SDKClient.Instance.ec);
            }
        }
        public static void DeleteMsg(string msgId)
        {
            Util.Helpers.Async.Run(async () => await DAL.DALMessageHelper.UpdateMsgHidden(msgId));
        }
        /// <summary>
        /// 变更消息为通知类型
        /// </summary>
        /// <param name="msgId"></param>
        /// <param name="content">内容</param>
        public static void UpdateHistoryMsgToNotification(string msgId, string content)
        {
            Util.Helpers.Async.Run(async () => await DAL.DALMessageHelper.UpdateMsgContent(msgId, content, SDKProperty.MessageType.notification));
        }
        private static string SendOnlineFileMessage(string path, string to, string resourceId, long fileSize, string ip, int port, SDKProperty.SessionType sessionType = SessionType.CommonChat)
        {
            string id;
            MessagePackage package = new MessagePackage()
            {
                from = SDKClient.Instance.property.CurrentAccount.userID.ToString(),
                to = to,
                id = SDKProperty.RNGId
            };
            id = package.id;
            Task.Run(() =>
            {
                package.data = new message()
                {
                    body = new OnlineFileBody()
                    {
                        fileSize = fileSize,
                        fileName = path,
                        id = resourceId,
                        IP = ip,
                        Port = port
                    },
                    subType = nameof(SDKProperty.MessageType.onlinefile),
                    chatType = (int)sessionType,
                    senderInfo = new message.SenderInfo()
                    {
                        photo = SDKClient.Instance.property.CurrentAccount.photo,
                        userName = SDKClient.Instance.property.CurrentAccount.userName
                    },
                    type = nameof(chatType.chat)
                };
                package.Send(SDKClient.Instance.ec);
            });
            return id;
        }

        public static string SendOnlineFile(int to, string fileFullName, Action<long> SetProgressSize, Action<(int isSuccess, string imgMD5, string imgId, NotificatonPackage notifyPackage)> SendComplete, Action<long> ProgressChanged,
            System.Threading.CancellationToken? cancellationToken = null)
        {

            string MD5 = string.Empty;
            long fileSize = 0;
            FileInfo info = new FileInfo(fileFullName);
            fileSize = info.Length;
            //发送在线文件消息给对方
            if (cancellationToken != null && !cancellationToken.Value.IsCancellationRequested)
            {
                string id = SendOnlineFileMessage(fileFullName, to.ToString(), MD5, fileSize, SDKProperty.P2PServer.GetLocalIP(), SDKProperty.P2PServer.GetLocalPort());
                P2P.P2PClient p2PHelper = new P2P.P2PClient()
                {
                    CancellationToken = cancellationToken,
                    FileName = fileFullName,
                    MD5 = MD5,
                    MsgId = id,
                    From = SDKClient.Instance.property.CurrentAccount.userID,
                    To = to,
                    FileSize = fileSize
                };
                p2PHelper.SendComplete += SendComplete;
                p2PHelper.SetProgressSize += SetProgressSize;
                p2PHelper.ProgressChanged += ProgressChanged;
                P2P.P2PClient.FileCache.Add(id, p2PHelper);
                return id;
            }
            else
                return null;

        }
        /// <summary>
        /// 接收在线文件
        /// </summary>
        /// <param name="msgId"></param>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="to"></param>
        /// <param name="fileName"></param>
        /// <param name="resourceId"></param>
        /// <param name="SetProgressSize"></param>
        /// <param name="SendComplete"></param>
        /// <param name="ProgressChanged"></param>
        /// <param name="cancellationToken"></param>
        public static bool RecvOnlineFile(string msgId, string ip, int port, int to, long fileSize, string fileName, string resourceId, Action<long> SetProgressSize, Action<(int isSuccess, string imgMD5, string imgId, NotificatonPackage notifyPackage)> SendComplete, Action<long> ProgressChanged,
         System.Threading.CancellationToken? cancellationToken = null)
        {
            P2P.P2PClient p2PHelper = new P2P.P2PClient()
            {
                CancellationToken = cancellationToken,
                FileName = fileName,
                RemotePort = port,
                RemoteIP = IPAddress.Parse(ip),
                From = to,
                To = SDKClient.Instance.property.CurrentAccount.userID,
                MD5 = resourceId,
                MsgId = msgId,
                FileSize = fileSize
            };
            p2PHelper.SendComplete += SendComplete;
            p2PHelper.SetProgressSize += SetProgressSize;
            p2PHelper.ProgressChanged += ProgressChanged;
            SDKClient.Instance.property.SendP2PList.Add(p2PHelper);
            if (p2PHelper.TryConnect())
            {
                p2PHelper.SendHeader();

                return true;
            }
            else
            {

                return false;
            }

        }
    }

}
