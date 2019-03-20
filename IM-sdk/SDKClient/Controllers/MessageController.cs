using SDKClient.Model;
using SDKClient.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
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
        /// 变更消息
        /// </summary>
        /// <param name="msgId"></param>
        /// <param name="content">内容</param>
        /// <param name="messageType"></param>
        public static void UpdateHistoryMsgContent(string msgId, string content, SDKProperty.MessageType messageType = SDKProperty.MessageType.notification)
        {
            Util.Helpers.Async.Run(async () => await DAL.DALMessageHelper.UpdateMsgContent(msgId, content, messageType));
        }

    }

}
