using SDKClient.Model;
using SDKClient.WebAPI;
using SuperSocket.ClientEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static SDKClient.SDKProperty;

namespace SDKClient.Controllers
{
    public static class ResourceController
    {
       
        private static object face_lock = new object();
       
        internal static Util.Logs.ILog logger = Util.Logs.Log.GetLog(typeof(ResourceController));
        public static void DownLoadFacePhoto(string resourceName, Action ErrorCallBack = null, Action SuccessCallBack = null)
        {
#if !CUSTOMSERVER
            //针对资源存放在固定的FTP服务器上的资源
            System.Threading.CancellationTokenSource source = new System.Threading.CancellationTokenSource();

            WebClient webClient = new WebClient();
            webClient.DownloadDataCompleted += (s, e) =>
            {

                source.Cancel();
                //区分下载出错的异常信息和下载完毕通过content-type
                if (!((WebClient)s).ResponseHeaders.GetValue("Content-Type").ToLower().Contains("application/json"))
                {
                    if (e.Error == null && e.Cancelled == false && e.Result.Length > 0)
                    {
                        lock (face_lock)
                        {
                            var data = e.Result;
                            //var str = Encoding.UTF8.GetString(e.Result);
                            var filename = Path.Combine(SDKClient.Instance.property.CurrentAccount.facePath, resourceName);
                            if (File.Exists(filename))
                            {
                                SuccessCallBack?.Invoke();
                            }
                            else if (File.Exists(filename))
                            {
                                SuccessCallBack?.Invoke();
                            }
                            else
                            {
                                File.WriteAllBytes(Path.Combine(SDKClient.Instance.property.CurrentAccount.facePath, resourceName), data);
                                SuccessCallBack?.Invoke();
                            }
                        }

                    }
                }
                else
                {
                    ErrorCallBack?.Invoke();
                    if (e.Error == null)
                        logger.Error($"下载头像失败;message:{Encoding.UTF8.GetString(e.Result)},name:{resourceName}");
                    else
                        logger.Error($"下载头像失败;message:{e.Error.Message},name:{resourceName}");

                }

            };
            Task.Run(() =>
            {
                string uri = $"{Protocol.ProtocolBase.downLoadResource}{resourceName}";
                webClient.DownloadDataAsync(new Uri(uri));
            }).ContinueWith(task =>
            {
                Task.Delay(32 * 1000, source.Token).ContinueWith(t =>
                {
                    if (t.IsCanceled)
                    {
                        return;
                    }
                    else
                    {
                        ErrorCallBack?.Invoke();
                        logger.Error($"下载头像超时;{resourceName}");
                    }
                });
            });
#else
            //针对资源名是URL形式
            Task.Run(() =>
            {
                try
                {
                    string fileName = Path.GetFileName(resourceName);
                    fileName = Path.Combine(property.CurrentAccount.imgPath, fileName);
                    var stream = WebRequest.Create(resourceName).GetResponse().GetResponseStream();

                    using (FileStream fs = new FileStream(fileName, FileMode.OpenOrCreate))
                    {
                        byte[] buff = new byte[4096];
                        while (true)
                        {
                            int i = stream.Read(buff, 0, 4096);
                            fs.Write(buff, 0, i);

                            if (i == 0)
                                break;
                        }
                        fs.Flush();

                    }
                }
                catch (Exception e)
                {

                    ErrorCallBack?.Invoke();

                    logger.Error($"下载资源失败;message:{e.Message},name:{resourceName}");
                }

                SuccessCallBack?.Invoke();
            });
#endif

        }
        /// <summary>
        /// 查找资源是否存在
        /// </summary>
        /// <param name="resourceName">资源全路径</param>
        /// <returns></returns>
        public static async Task<(bool existed, string resourceId, long fileSize)> FindResource(string resourceFullName)
        {
            try
            {

                //验证资源是否存在

                //  var filedata = File.ReadAllBytes(resourceFullName);
                // var name = Util.Helpers.Encrypt.Md5By32(filedata);
                using (FileStream fs = File.OpenRead(resourceFullName))
                {
                    string md5 = Util.Helpers.Encrypt.Md5By32(fs);

                    var t = await WebAPICallBack.FindResource($"{md5}{Path.GetExtension(resourceFullName)}");

                    if (t.isExist)
                        return (t.isExist, $"{md5}{Path.GetExtension(resourceFullName)}", fs.Length);
                    else
                        return (false, $"{md5}{Path.GetExtension(resourceFullName)}", fs.Length);
                }
            }
            catch (Exception)
            {

                return (false, null, 0);
            }
        }
        /// <summary>
        /// 查找资源是否存在
        /// </summary>
        /// <param name="resourceName">资源全路径</param>
        /// <returns></returns>
        public static async Task<fileInfo> IsFileExist(string resourceFullName)
        {
            try
            {

                //验证资源是否存在

                //  var filedata = File.ReadAllBytes(resourceFullName);
                // var name = Util.Helpers.Encrypt.Md5By32(filedata);
                using (FileStream fs = File.OpenRead(resourceFullName))
                {
                    long len = fs.Length;
                    string md5 = Util.Helpers.Encrypt.Md5By32(fs);

                    var file = await WebAPICallBack.FindResource($"{md5}{Path.GetExtension(resourceFullName)}");
                    if (file == null || !file.isExist)
                    {
                        file.fileSize = len;
                        file.fileCode = $"{md5}{Path.GetExtension(resourceFullName)}";

                    }
                    return file;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return null;
            }
        }
        /// <summary>
        /// 上传资源
        /// </summary>
        /// <param name="resourceName">资源全路径名称</param>
        /// <param name="UploadProgressChanged">上传进度</param>
        /// <param name="UploadDataCompleted"></param>
        /// <returns></returns>
        public static void UpLoadResource(string resourceFullName, Action<long> UploadProgressChanged, Action<bool, string, SDKProperty.ErrorState> UploadDataCompleted,
            System.Threading.CancellationToken? cancellationToken = null)
        {

            //上传资源
            if (File.Exists(resourceFullName))
            {
                FileInfo info = new FileInfo(resourceFullName);
                if (info.Length > 500 * 1000 * 1000)
                {
                    UploadDataCompleted?.Invoke(false, null, SDKProperty.ErrorState.OutOftheControl);
                    return;
                }
                byte[] filedata = null;
                try
                {
                    filedata = File.ReadAllBytes(resourceFullName);
                }
                catch (Exception ex)
                {
                    UploadDataCompleted?.Invoke(false, resourceFullName, SDKProperty.ErrorState.AppError);
                    logger.Error($"UploadError: filename:{resourceFullName},ex:{ex.Message}");
                    return;
                }

#if !CUSTOMSERVER

                string flag = DateTime.Now.Ticks.ToString("x");//不能删除
                var boundary = "---------------------------" + flag;

                string httpRow = $"--{boundary}\r\nContent-Disposition: form-data; name=\"uploadfile\"; filename=\"{Path.GetFileName(resourceFullName)}\"\r\nContent-Type: application/octet-stream\r\n\r\n";
                var datas = Encoding.UTF8.GetBytes(httpRow);
                datas = datas.Concat(filedata)
                    .Concat(Encoding.UTF8.GetBytes("\r\n"))
                    .Concat(Encoding.UTF8.GetBytes($"--{ boundary}--\r\n")).ToArray();


                WebClient webClient = new WebClient();
                webClient.Headers.Add("Content-Type", "multipart/form-data; boundary=" + boundary);
                webClient.UploadProgressChanged += (s, e) =>
                {
                    if (cancellationToken == null || !cancellationToken.Value.IsCancellationRequested)
                        UploadProgressChanged?.Invoke(e.BytesSent);
                    else
                        webClient.CancelAsync();

                };
                webClient.UploadDataCompleted += (s, e) =>
                {

                    if (e.Error == null)
                    {
                        UploadResult webapiResult = Util.Helpers.Json.ToObject<WebAPI.UploadResult>(Encoding.UTF8.GetString(e.Result));
                        if (webapiResult.code == 0)
                        {
                            UploadDataCompleted?.Invoke(true, webapiResult.uploadResult.First().fileCode, SDKProperty.ErrorState.None);
                            var fileFullName = Path.Combine(SDKClient.Instance.property.CurrentAccount.filePath, Path.GetFileName(resourceFullName));
                            //if(fileType== FileType.file)
                            //{
                            //    lock (obj_lock)
                            //    {

                            //        File.WriteAllBytes(fileFullName, filedata);
                            //    }
                            //}
                            //else
                            //{
                            //    if (!File.Exists(fileFullName))
                            //    {
                            //        lock (obj_lock)
                            //        {
                            //            if (!File.Exists(fileFullName))
                            //                File.WriteAllBytes(fileFullName, filedata);
                            //        }
                            //    }
                            //}
                        }
                        else
                        {
                            UploadDataCompleted?.Invoke(false, webapiResult.uploadResult.First().fileCode, SDKProperty.ErrorState.ServerException);
                            logger.Error($"UploadError: code:{webapiResult.code},error:{webapiResult.error},id:{webapiResult.uploadResult.First().fileCode}");
                        }
                    }
                    else
                    {
                        if (e.Cancelled)
                        {
                            UploadDataCompleted?.Invoke(false, resourceFullName, SDKProperty.ErrorState.Cancel);
                            logger.Info($"UploadCancel: filename:{resourceFullName}");
                        }
                        else
                        {
                            UploadDataCompleted?.Invoke(false, resourceFullName, SDKProperty.ErrorState.NetworkException);
                            logger.Error($"UploadError: filename:{resourceFullName},ex:{e.Error.Message}");
                        }
                    }
                };

                webClient.UploadDataAsync(new Uri(Protocol.ProtocolBase.uploadresource), datas);
#else
                WebClient webClient = new WebClient();
                webClient.UploadFileCompleted += (s, e) =>
                {
                    if (e.Error == null)
                    {
                        dynamic d = Util.Helpers.Json.ToObject<dynamic>(Encoding.UTF8.GetString(e.Result));
                        if (d.code != 1)
                        {
                            UploadDataCompleted?.Invoke(false, d.message, SDKProperty.ErrorState.ServerException);
                            logger.Error($"UploadError: code:{d.code},error:{d.message},id:{resourceFullName}");
                        }
                        else
                        {
                            UploadDataCompleted?.Invoke(true, $"{d.data.originalphoto},{d.data.thumbnailphoto}", SDKProperty.ErrorState.None);
                        }
                    }
                    else
                    {
                        if (e.Cancelled)
                        {
                            UploadDataCompleted?.Invoke(false, resourceFullName, SDKProperty.ErrorState.Cancel);
                            logger.Info($"UploadCancel: filename:{resourceFullName}");
                        }
                        else
                        {
                            UploadDataCompleted?.Invoke(false, resourceFullName, SDKProperty.ErrorState.NetworkException);
                            logger.Error($"UploadError: filename:{resourceFullName},ex:{e.Error.Message}");
                        }
                    }
                };
                webClient.UploadProgressChanged += (s, e) =>
                {
                    if (cancellationToken == null || !cancellationToken.Value.IsCancellationRequested)
                        UploadProgressChanged?.Invoke(e.BytesSent);
                    else
                        webClient.CancelAsync();
                };
                string strtime = DateTime.Now.ToString("yyyyMMddHHmmss");
                string param = $"upload{strtime}{CustomServerURL.CSKEY}";
                string signatureresult = MJD.Utility.UtilityCrypto.Encrypt(param, MJD.Utility.CryptoProvider.MD5).ToLower();

                webClient.Headers.Add("signature", signatureresult);
                webClient.Headers.Add("action", "upload");
                webClient.Headers.Add("time", strtime);
                var source = new System.Drawing.Bitmap(resourceFullName);
                webClient.UploadFileAsync(new Uri($"{WebAPI.CustomServerURL.UploadIMG}?width={source.Width}&height={source.Height}"), resourceFullName);
#endif
                return;
            }
            else
                return;
        }
      
        public static async void ResumeUpload(string resourceFullName, string md5, Action<long> UploadProgressChanged, Action<bool, string, SDKProperty.ErrorState> UploadDataCompleted,
           List<int> blockNum, System.Threading.CancellationToken? cancellationToken = null)
        {

            int blocklen = 1024 * 1024 * 2;
            using (FileStream fs = new FileStream(resourceFullName, FileMode.Open))
            {

                long totalCount = fs.Length;
                long totalnum = totalCount / blocklen;
                if (totalCount % blocklen != 0)
                    totalnum += 1;
                fs.Seek(0, SeekOrigin.Begin);
                SDKProperty.ErrorState errorState = ErrorState.None;
                for (int i = 1; i < (totalnum + 1); i++)
                {
                    if (blockNum != null && blockNum.Contains(i))
                        continue;
                    if (cancellationToken.HasValue && cancellationToken.Value.IsCancellationRequested)
                    {
                        errorState = ErrorState.Cancel;
                        break;
                    }
                    fs.Seek((i - 1) * blocklen, SeekOrigin.Begin);
                    byte[] buff = new byte[blocklen];
                    int len = fs.Read(buff, 0, buff.Length);
                    if (len == blocklen)
                    {
                        await WebAPICallBack.ResumeUpload(buff, i, blocklen, md5, totalCount, totalnum).ContinueWith(async t =>
                        {
                            if (t.IsFaulted)//服务不可用
                            {
                                //TODO:
                                // UploadDataCompleted?.Invoke(false, md5, ErrorState.NetworkException);
                                errorState = errorState == ErrorState.Cancel ? errorState : ErrorState.NetworkException;

                            }
                            else
                            {
                                var obj = t.Result;
                                if (!obj.Success)
                                {
                                    if (obj.code == "-999")
                                    {
                                        errorState = errorState == ErrorState.Cancel ? errorState : ErrorState.NetworkException;
                                        return;
                                    }
                                    bool isOk = false;
                                    for (int j = 0; j < 5; j++)
                                    {
                                        var r = await WebAPICallBack.ResumeUpload(buff, i, blocklen, md5, totalCount, totalnum);
                                        isOk = r.Success;
                                        if (isOk)
                                            break;

                                    }
                                    if (!isOk)
                                        errorState = errorState == ErrorState.Cancel ? errorState : ErrorState.NetworkException;

                                }
                                else
                                {
                                    UploadProgressChanged?.Invoke(len);
                                }
                            }
                        });

                    }
                    else
                    {
                        byte[] temp = new byte[len];
                        Buffer.BlockCopy(buff, 0, temp, 0, len);
                        await WebAPICallBack.ResumeUpload(temp, i, len, md5, totalCount, totalnum).ContinueWith(async t =>
                        {
                            if (t.IsFaulted)
                            {
                                //TODO:
                                errorState = errorState == ErrorState.Cancel ? errorState : ErrorState.NetworkException;
                            }
                            else
                            {
                                var obj = t.Result;
                                if (!obj.Success)
                                {

                                    if (obj.code == "-999")
                                    {
                                        errorState = errorState == ErrorState.Cancel ? errorState : ErrorState.NetworkException;
                                        return;
                                    }
                                    bool isOk = false;
                                    for (int j = 0; j < 5; j++)
                                    {
                                        var r = await WebAPICallBack.ResumeUpload(buff, i, blocklen, md5, totalCount, totalnum);
                                        isOk = r.Success;
                                        if (isOk)
                                            break;

                                    }
                                    if (!isOk)
                                        errorState = errorState == ErrorState.Cancel ? errorState : ErrorState.NetworkException;

                                }
                                else
                                {
                                    UploadProgressChanged?.Invoke(len);
                                }
                            }

                        });
                    }


                }
                if (errorState != ErrorState.None)
                {
                    UploadDataCompleted?.Invoke(false, md5, errorState);
                }
                else
                {
                    UploadDataCompleted?.Invoke(true, md5, ErrorState.None);
                }

            }
        }



        private static object obj_lock = new object();
        //DownloadFileWithResume
        public static void DownLoadResource(string resourceName, string fileName, FileType fileType, Action<long> downloadProgressChanged,
            Action<bool> downloadDataCompleted, string msgId, System.Threading.CancellationToken? cancellationToken = null)
        {
            void UpdateFileState(DB.messageDB message, int fileState)
            {
                if (message == null)
                    return;
                int i = Util.Helpers.Async.Run(async () => await SDKProperty.SQLiteConn.ExecuteAsync($"update messageDB set fileState={fileState} where Id='{message.Id}'"));
            }
            var m = Util.Helpers.Async.Run(async () => await DAL.DALMessageHelper.Get(msgId));
#if !CUSTOMSERVER

            WebClient webClient = new WebClient();

            UpdateFileState(m, (int)ResourceState.Working);
            webClient.DownloadProgressChanged += (s, e) =>
            {
                if (cancellationToken == null || !cancellationToken.Value.IsCancellationRequested)
                    downloadProgressChanged?.Invoke(e.BytesReceived);
                else
                {
                    webClient.CancelAsync();
                    UpdateFileState(m, (int)ResourceState.IsCancelled);

                }
            };

            webClient.DownloadDataCompleted += (s, e) =>
            {
                try
                {
                    // if (((WebClient)s).ResponseHeaders.GetValue("Content-Type") != "application/json")
                    if (!((WebClient)s).ResponseHeaders.GetValue("Content-Type").ToLower().Contains("application/json"))
                    {
                        if (e.Error == null && e.Cancelled == false && e.Result.Length > 0)
                        {
                            try
                            {
                                if (e.Result.Length < 100)
                                {
                                    logger.Error($"下载资源失败;message:{Encoding.UTF8.GetString(e.Result)},name:{resourceName}");
                                    downloadDataCompleted?.Invoke(false);
                                    return;
                                }
                                var data = e.Result;
                                string basePath = null;

                                switch (fileType)
                                {
                                    case FileType.img:
                                        basePath = Path.Combine(SDKProperty.imgPath, SDKClient.Instance.property.CurrentAccount.loginId);
                                        if (!Directory.Exists(basePath))
                                            Directory.CreateDirectory(basePath);
                                        break;
                                    case FileType.file:
                                        basePath = Path.Combine(SDKProperty.filePath, SDKClient.Instance.property.CurrentAccount.loginId);
                                        if (!Directory.Exists(basePath))
                                            Directory.CreateDirectory(basePath);
                                        break;
                                    default:
                                        break;
                                }
                                if (Path.IsPathRooted(fileName))
                                {
                                    var dir = Path.GetDirectoryName(fileName);
                                    if (!Directory.Exists(dir))
                                        Directory.CreateDirectory(dir);
                                    if (!File.Exists(fileName))
                                    {
                                        lock (obj_lock)
                                        {
                                            if (!File.Exists(fileName))
                                            {
                                                File.WriteAllBytes(fileName, data);
                                                // Util.Helpers.Async.Run(async () => await DAL.DALMessageHelper.UpdateMsgNewFileName(msgId, fileName));
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (!File.Exists(Path.Combine(basePath, fileName)))
                                    {
                                        lock (obj_lock)
                                        {
                                            if (!File.Exists(Path.Combine(basePath, fileName)))
                                            {
                                                File.WriteAllBytes(Path.Combine(basePath, fileName), data);
                                                //  Util.Helpers.Async.Run(async () => await DAL.DALMessageHelper.UpdateMsgNewFileName(msgId, Path.Combine(basePath, fileName)));
                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {

                                logger.Error($"下载资源失败;message:{ex.Message},name:{resourceName}");
                                UpdateFileState(m, (int)ResourceState.NoStart);
                                downloadDataCompleted?.Invoke(false);
                                return;
                            }
                            UpdateFileState(m, (int)ResourceState.IsCompleted);
                            downloadDataCompleted?.Invoke(true);
                        }
                        else
                        {
                            logger.Error($"下载资源失败;message:{Encoding.UTF8.GetString(e.Result)},name:{resourceName}");
                            UpdateFileState(m, (int)ResourceState.NoStart);
                            downloadDataCompleted?.Invoke(false);
                        }
                    }
                    else
                    {
                        UpdateFileState(m, (int)ResourceState.NoStart);
                        downloadDataCompleted?.Invoke(false);
                        if (e.Error == null)
                            logger.Error($"下载资源失败;message:{Encoding.UTF8.GetString(e.Result)},name:{resourceName}");
                        else
                            logger.Error($"下载资源失败;message:{e.Error.Message},name:{resourceName}");
                    }
                }
                catch (Exception ex)
                {
                    logger.Error($"下载资源失败;message:{ex.Message},name:{resourceName}");
                    UpdateFileState(m, (int)ResourceState.NoStart);
                    downloadDataCompleted?.Invoke(false);
                }
            };
            logger.Info($"开始下载资源：{resourceName}");
            webClient.DownloadDataAsync(new Uri(string.Format("{0}{1}", Protocol.ProtocolBase.downLoadResource, resourceName)));

#else

            if (Path.IsPathRooted(fileName))
            {
                var dir = Path.GetDirectoryName(fileName);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                Util.Helpers.Async.Run(async () => await DAL.DALMessageHelper.UpdateMsgNewFileName(msgId, fileName));

            }
            else
            {
                fileName = Path.Combine(property.CurrentAccount.imgPath, fileName);

                Util.Helpers.Async.Run(async () => await DAL.DALMessageHelper.UpdateMsgNewFileName(msgId, fileName));

            }
            Task.Run(() =>
            {
                try
                {
                    var stream = WebRequest.Create(resourceName).GetResponse().GetResponseStream();

                    using (FileStream fs = new FileStream(fileName, FileMode.OpenOrCreate))
                    {
                        byte[] buff = new byte[4096];
                        while (true)
                        {
                            int i = stream.Read(buff, 0, 4096);
                            fs.Write(buff, 0, i);

                            if (i == 0)
                                break;
                        }
                        fs.Flush();

                    }
                }
                catch (Exception e)
                {

                    downloadDataCompleted?.Invoke(false);

                    logger.Error($"下载资源失败;message:{e.Message},name:{resourceName}");
                }
                UpdateFileState(m, (int)ResourceState.IsCompleted);
                downloadDataCompleted?.Invoke(true);
            });
#endif


        }
        /// <summary>
        /// 断点续传
        /// </summary>
        /// <param name="resourceName"></param>
        /// <param name="fileName"></param>
        /// <param name="fileType"></param>
        /// <param name="msgId"></param>
        /// <param name="InitProgress"></param>
        /// <param name="downloadProgressChanged"></param>
        /// <param name="downloadDataCompleted"></param>
        /// <param name="cancellationToken"></param>
        public static void DownloadFileWithResume(string resourceName, string fileName, FileType fileType, Action<long> downloadProgressChanged,
            Action<bool> downloadDataCompleted, string msgId, Action<long, long> InitProgress = null, System.Threading.CancellationToken? cancellationToken = null)
        {
            void UpdateFileState(DB.messageDB message, int fileState)
            {
                if (message == null)
                    return;
                int i = Util.Helpers.Async.Run(async () => await SDKProperty.SQLiteConn.ExecuteAsync($"update messageDB set fileState={fileState} where Id='{message.Id}'"));
            }
            var m = Util.Helpers.Async.Run(async () => await DAL.DALMessageHelper.Get(msgId));


            UpdateFileState(m, (int)ResourceState.Working);
            string sourceUrl = string.Format("{0}{1}", Protocol.ProtocolBase.DownloadFileWithResume, resourceName);
            string basePath = null;

            if (!Path.IsPathRooted(fileName))
            {
                switch (fileType)
                {
                    case FileType.img:
                        basePath = Path.Combine(SDKProperty.imgPath,SDKClient.Instance.property.CurrentAccount.loginId);
                        if (!Directory.Exists(basePath))
                            Directory.CreateDirectory(basePath);
                        break;
                    case FileType.file:
                        basePath = Path.Combine(SDKProperty.filePath, SDKClient.Instance.property.CurrentAccount.loginId);
                        if (!Directory.Exists(basePath))
                            Directory.CreateDirectory(basePath);
                        break;
                    default:
                        break;
                }
                fileName = Path.Combine(basePath, fileName);
            }
            IMRequest.DownloadFileWithResume(sourceUrl, fileName, InitProgress, downloadProgressChanged, (b) => {
                if (b)
                {
                    UpdateFileState(m, (int)ResourceState.IsCompleted);
                }
                else
                    UpdateFileState(m, (int)ResourceState.Working);
                downloadDataCompleted?.Invoke(b);

            }, cancellationToken);

        }
        public static async Task UploadImg(string imgFullName,
          Action<(bool isSuccess, string imgMD5, ErrorState errorState)> SendComplete,
           System.Threading.CancellationToken? cancellationToken)
        {

            var imgresult = await FindResource(imgFullName);
            if (imgresult.existed)
            {

                SendComplete?.Invoke((true, imgresult.resourceId, SDKProperty.ErrorState.None));
            }
            else
            {

                Action<bool, string, SDKProperty.ErrorState> uploadDataCompleted = (b, s, e) =>
                {
                    if (!b)
                    {
                        SendComplete?.Invoke((false, s, e));

                    }
                    else
                    {

                        SendComplete?.Invoke((true, s, SDKProperty.ErrorState.None));

                    }
                };

                UpLoadResource(imgFullName, null, uploadDataCompleted, cancellationToken);
            }

        }
        public static async Task UploadFile(string fileFullName,
          Action<(bool isSuccess, string fileMD5, long fileSize, ErrorState errorState)> SendComplete,
           Action<long, long> SetProgressSize, Action<long> UploadProgressChanged,
           System.Threading.CancellationToken? cancellationToken)
        {

            //var result = await SDKClient.Instance.FindResource(fileFullName);
            var result = await IsFileExist(fileFullName);
            if (result.isExist)
            {
                SendComplete?.Invoke((true, result.fileCode, result.fileSize, SDKProperty.ErrorState.None));
            }
            else
            {
               
                Action<bool, string, SDKProperty.ErrorState> uploadDataCompleted = async (b, s, e) =>
                {
                    if (!b)
                    {
#if !CUSTOMSERVER

                        await DAL.DALResourceManifestHelper.UpdateResourceState(s, ResourceState.Working);
#endif

                        SendComplete?.Invoke((false, s, 0, e));

                    }
                    else
                    {
#if !CUSTOMSERVER
                        await DAL.DALResourceManifestHelper.UpdateResourceState(s, ResourceState.IsCompleted);
#endif
                        FileInfo info = new FileInfo(fileFullName);
                        SendComplete?.Invoke((true, s, info.Length, SDKProperty.ErrorState.None));

                    }
                };
#if CUSTOMSERVER
                SDKClient.Instance.UpLoadResource(fileFullName, UploadProgressChanged, uploadDataCompleted, cancellationToken);
#else



                if (result.blocks != null && result.blocks.Any())
                {
                    SetProgressSize?.Invoke(result.blocks.Count * result.blocks[0].blockSize, result.fileSize);
                    ResumeUpload(fileFullName, result.fileCode, UploadProgressChanged, uploadDataCompleted, result.blocks.Select(b => b.blockNum).ToList(), cancellationToken);
                }
                else
                {
                    SetProgressSize?.Invoke(0, result.fileSize);
                    ResumeUpload(fileFullName, result.fileCode, UploadProgressChanged, uploadDataCompleted, null, cancellationToken);
                }

#endif

            }

        }

    }
}
