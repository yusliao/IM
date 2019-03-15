using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static SDKClient.SDKProperty;

namespace SDKClient.Controllers
{
    //class MessageController : IMessageController
    //{
    //    public async Task SendFileMessage(string fileFullName, Action<long> SetProgressSize, Action<(int isSuccess, string fileMD5, string msgId, string imgId, SDKProperty.ErrorState errorState)> SendComplete, Action<long> ProgressChanged, string to,
    //      chatType type = chatType.chat, System.Threading.CancellationToken? cancellationToken = null, MessageType messageType = MessageType.file, string groupName = null, string imgFullName = null)
    //    {
    //        await UploadFile(fileFullName, async result =>
    //        {
    //            if (result.isSuccess)
    //            {
    //                if (!string.IsNullOrEmpty(imgFullName))
    //                {
    //                    if (fileFullName.Equals(imgFullName))//图片文件
    //                    {
    //                        var bitImgFile = Path.Combine(property.CurrentAccount.imgPath, $"my{result.fileMD5}");
    //                        if (!File.Exists(bitImgFile))//小图本地不存在
    //                        {
    //                            var source = new System.Drawing.Bitmap(imgFullName);
    //                            var With = (int)Math.Min(source.Width, 300);
    //                            var h = With * source.Height / source.Width;
    //                            var bmp = Util.ImageProcess.GetThumbnail(imgFullName, With, h);
    //                            using (MemoryStream ms = new MemoryStream())
    //                            {
    //                                bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);

    //                                ms.Seek(0, SeekOrigin.Begin);
    //                                var bmpArray = ms.ToArray();
    //                                File.WriteAllBytes(bitImgFile, bmpArray);
    //                            }
    //                        }

    //                        await UploadImg(bitImgFile, imgresult =>
    //                        {
    //                            if (imgresult.isSuccess)
    //                            {
    //                                using (var bmp = new System.Drawing.Bitmap(bitImgFile))
    //                                {
    //                                    string msgId = SDKClient.Instance.SendFileMessage(fileFullName, to, result.fileMD5, result.fileSize, type, groupName, bmp.Width, bmp.Height, imgresult.imgMD5);

    //                                    SendComplete?.Invoke((1, result.fileMD5, msgId, imgresult.imgMD5, SDKProperty.ErrorState.None));
    //                                }

    //                            }
    //                            else
    //                            {
    //                                SendComplete?.Invoke((0, result.fileMD5, null, null, result.errorState));
    //                            }

    //                        }, cancellationToken);

    //                    }
    //                    else
    //                    {

    //                        //视频缩略图
    //                        if (Compressor.IsFileSupported(imgFullName))
    //                        {
    //                            CompressionResult imgResult = null;
    //                            imgResult = compressor.CompressFileAsync(imgFullName, false);
    //                            if (!string.IsNullOrEmpty(imgResult.ResultFileName))
    //                            {
    //                                imgFullName = imgResult.ResultFileName;
    //                            }

    //                        }
    //                        await UploadImg(imgFullName, imgresult =>
    //                        {
    //                            if (imgresult.isSuccess)
    //                            {
    //                                using (var bmp = new System.Drawing.Bitmap(imgFullName))
    //                                {
    //                                    string msgId = SDKClient.Instance.SendFileMessage(fileFullName, to, result.fileMD5, result.fileSize, type, groupName, bmp.Width, bmp.Height, imgresult.imgMD5);

    //                                    SendComplete?.Invoke((1, result.fileMD5, msgId, imgresult.imgMD5, SDKProperty.ErrorState.None));
    //                                }

    //                            }
    //                            else
    //                            {
    //                                SendComplete?.Invoke((0, result.fileMD5, null, null, result.errorState));
    //                            }

    //                        }, cancellationToken);
    //                    }
    //                }
    //                else
    //                {
    //                    string msgId = SDKClient.Instance.SendFileMessage(fileFullName, to, result.fileMD5, result.fileSize, type, groupName);


    //                    SendComplete?.Invoke((1, result.fileMD5, msgId, null, SDKProperty.ErrorState.None));
    //                }
    //            }
    //            else
    //            {
    //                SendComplete?.Invoke((0, result.fileMD5, null, null, result.errorState));
    //            }
    //        }, s =>
    //        {
    //            SetProgressSize?.Invoke(s);
    //        }, c =>
    //        {
    //            ProgressChanged?.Invoke(c);
    //        }
    //        , cancellationToken);
    //    }
    //    /// <summary>
    //    /// 发送小视频消息
    //    /// </summary>
    //    /// <param name="fileFullName">视频文件全路径</param>
    //    /// <param name="recordTime">时长</param>
    //    /// <param name="imgFullName">缩略图全路径</param>
    //    /// <param name="SetProgressSize">设置文件大小</param>
    //    /// <param name="SendComplete">发送完毕CB</param>
    //    /// <param name="ProgressChanged">进度条CB</param>
    //    /// <param name="to">目标ID</param>
    //    /// <param name="type">聊天类型<see cref="SDKProperty.chatType"/></param>
    //    /// <param name="cancellationToken">取消结构体对象</param>
    //    /// <param name="messageType">消息类型<see cref="SDKProperty.MessageType"/></param>
    //    /// <param name="groupName">如果type=[<see cref="SDKProperty.chatType.groupChat"/>]，需要提供群名称</param>
    //    /// <returns></returns>
    //    public async Task SendSmallVideoMessage(string fileFullName, string recordTime, string imgFullName, Action<long> SetProgressSize, Action<(int isSuccess, string videoMD5, string msgId, string imgId, SDKProperty.ErrorState errorState)> SendComplete, Action<long> ProgressChanged, string to,
    //       chatType type = chatType.chat, System.Threading.CancellationToken? cancellationToken = null, MessageType messageType = MessageType.smallvideo, string groupName = null)
    //    {

    //        await UploadFile(fileFullName, async result =>
    //        {

    //            if (result.isSuccess)
    //            {
    //                await UploadImg(imgFullName, imgresult =>
    //                {
    //                    if (imgresult.isSuccess)
    //                    {
    //                        using (var source = new System.Drawing.Bitmap(imgFullName))
    //                        {
    //                            string msgId = SDKClient.Instance.SendSmallVideoMessage(fileFullName, to, recordTime,
    //                                result.fileMD5, imgresult.imgMD5, source.Width, source.Height, result.fileSize, type, groupName);
    //                            SendComplete?.Invoke((1, result.fileMD5, msgId, imgresult.imgMD5, SDKProperty.ErrorState.None));
    //                        }
    //                    }
    //                    else
    //                    {
    //                        SendComplete?.Invoke((0, result.fileMD5, null, null, result.errorState));
    //                    }
    //                }, cancellationToken);

    //            }
    //            else
    //            {
    //                SendComplete?.Invoke((0, result.fileMD5, null, null, result.errorState));
    //            }
    //        }, s => SetProgressSize?.Invoke(s), c => ProgressChanged?.Invoke(c), cancellationToken);

    //    }


    //}
    interface IMessageController
    {
        Task<string> SendFileMessage(string path, string to, string resourceId, long fileSize, chatType type = chatType.chat, string groupName = null, int width = 0, int height = 0, string imgMD5 = null, SDKProperty.SessionType sessionType = SessionType.CommonChat);
        Task<string> SendSmallVideoMessage(string path, string to, string recordTime, string resourceId, string previewId, int width, int height, long fileSize, chatType type = chatType.chat, string groupName = null, SDKProperty.SessionType sessionType = SessionType.CommonChat);
        Task<string> SendOnlineFileMessage(string path, string to, string resourceId, long fileSize, string ip, int port, SDKProperty.SessionType sessionType = SessionType.CommonChat);
        Task<string> SendSyncMsgStatus(int roomId, int readNum, string lastMsgID, SDKProperty.chatType chatType);
        Task<string> Sendtxt(string content, string to, IList<int> userIds = null, chatType type = chatType.chat, string groupName = null, SDKProperty.SessionType sessionType = SessionType.CommonChat);
        Task<string> SendImgMessage(string path, string to, string resourceId, string smallresourceId,
            chatType type = chatType.chat, System.Threading.CancellationToken? cancellationToken = null, string groupName = null, SDKProperty.SessionType sessionType = SessionType.CommonChat);
        Task<string> SendRetractMessage(string msgId, string to, chatType type = chatType.chat, int groupId = 0, SDKProperty.RetractType retractType = RetractType.Normal, SDKProperty.SessionType sessionType = SessionType.CommonChat);

    }
}
