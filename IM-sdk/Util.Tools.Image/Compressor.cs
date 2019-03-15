using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Util.ImageOptimizer
{
    public class Compressor
    {
        // 最大图片大小 1000KB
        const int maxSize = 1536;
        // 图片最大分辨率
        const int imageHeight = 1920;
        const int imageWidth = 1080;
        static readonly string[] _supported = { ".png", ".jpg", ".jpeg", ".gif" };
        string _cwd;
        protected virtual int ToolTimeout => 60000; // in msec
        public Compressor()
        {
            _cwd = Path.Combine(Path.GetDirectoryName(GetType().Assembly.Location), @"Resources\Tools\");
        }

        public Compressor(string cwd)
        {
            _cwd = cwd;
        }

        public CompressionResult CompressFileAsync(string fileName, bool lossy)
        {
            string targetFile = Path.ChangeExtension(Path.GetTempFileName(), Path.GetExtension(fileName));
            if (Path.GetExtension(fileName).ToLower() == ".gif")
            {
               
                ProcessStartInfo start = new ProcessStartInfo("cmd")
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    WorkingDirectory = _cwd,
                    Arguments = GetArguments(fileName, targetFile, lossy),
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                var stopwatch = Stopwatch.StartNew();
                using (var process = Process.Start(start))
                {
                    process.WaitForExit();
                    if (process.ExitCode != 0)
                    {
                        //异常退出。
                    }
                }
                stopwatch.Stop();

                return new CompressionResult(fileName, targetFile, stopwatch.Elapsed);
            }
            else
            {
              
                var stopwatch = Stopwatch.StartNew();
                using (FileStream file = new FileStream(fileName, FileMode.Open,FileAccess.Read))
                {
                    long quality = 80L;
                    
                    int index = 0;
                    byte[] img = File.ReadAllBytes(fileName);
                    while (img.Length / 1024 > maxSize)
                    {
                        index++;
                        quality -= 10;
                        img = CompressionImage(file, quality);
                        if (index >= 5)
                            break;
                    }
                   
                    targetFile = CreateImageFromBytes(targetFile, img);
                   
                }

                stopwatch.Stop();

                return new CompressionResult(fileName, targetFile, stopwatch.Elapsed);
            }
           
        }
        public static int getRatioSize(int bitWidth, int bitHeight)
        {
            
            // 缩放比
            int ratio = 1;
            // 缩放比,由于是固定比例缩放，只用高或者宽其中一个数据进行计算即可
            if (bitWidth > bitHeight && bitWidth > imageWidth)
            {
                // 如果图片宽度比高度大,以宽度为基准
                ratio = bitWidth / imageWidth;
            }
            else if (bitWidth < bitHeight && bitHeight > imageHeight)
            {
                // 如果图片高度比宽度大，以高度为基准
                ratio = bitHeight / imageHeight;
            }
            // 最小比率为1
            if (ratio <= 0)
                ratio = 1;
            return ratio;
        }
        /// <summary>
		/// 壓縮圖片 /// </summary>
		/// <param name="fileStream">圖片流</param>
		/// <param name="quality">壓縮質量0-100之間 數值越大質量越高</param>
		/// <returns></returns>
		private byte[] CompressionImage(Stream fileStream, long quality)
        {
          
            using (System.Drawing.Image img = System.Drawing.Image.FromStream(fileStream))
            {
                // 获取尺寸压缩倍数
                int ratio = getRatioSize(img.Width, img.Height);
                using (Bitmap bitmap = new Bitmap(img,img.Width/ratio,img.Height/ratio))
                {
                    
                    ImageCodecInfo CodecInfo = GetEncoder(img.RawFormat);
                    System.Drawing.Imaging.Encoder myEncoder = System.Drawing.Imaging.Encoder.Quality;
                    EncoderParameters myEncoderParameters = new EncoderParameters(1);
                    EncoderParameter myEncoderParameter = new EncoderParameter(myEncoder, quality);
                    myEncoderParameters.Param[0] = myEncoderParameter;
                    using (MemoryStream ms = new MemoryStream())
                    {
                        bitmap.Save(ms, CodecInfo, myEncoderParameters);
                        myEncoderParameters.Dispose();
                        myEncoderParameter.Dispose();
                        return ms.ToArray();
                    }
                }
            }
        }
        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                { return codec; }
            }
            return null;
        }
        /// <summary>
        /// Convert Byte[] to a picture and Store it in file
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static string CreateImageFromBytes(string fileName, byte[] buffer)
        {
            string file = fileName;
            Image image = BytesToImage(buffer);
            ImageFormat format = image.RawFormat;
            if (format.Equals(ImageFormat.Jpeg))
            {
                file= Path.ChangeExtension(file, ".jpeg");
              
            }
            else if (format.Equals(ImageFormat.Png))
            {
                file = Path.ChangeExtension(file, ".png");
               
            }
            else if (format.Equals(ImageFormat.Bmp))
            {
                file = Path.ChangeExtension(file, ".bmp");
               
            }
            else if (format.Equals(ImageFormat.Gif))
            {
                file = Path.ChangeExtension(file, ".gif");
               // file += ".gif";
            }
            else if (format.Equals(ImageFormat.Icon))
            {
                file = Path.ChangeExtension(file, ".icon");
               // file += ".icon";
            }
            System.IO.FileInfo info = new System.IO.FileInfo(file);
            System.IO.Directory.CreateDirectory(info.Directory.FullName);
            File.WriteAllBytes(file, buffer);
            return file;
        }
        /// <summary>
		/// Convert Byte[] to Image
		/// </summary>
		/// <param name="buffer"></param>
		/// <returns></returns>
		public static Image BytesToImage(byte[] buffer)
        {
            MemoryStream ms = new MemoryStream(buffer);
            Image image = System.Drawing.Image.FromStream(ms);
            return image;
        }
        private static string GetArguments(string sourceFile, string targetFile, bool lossy)
        {
            if (!Uri.IsWellFormedUriString(sourceFile, UriKind.RelativeOrAbsolute) && !File.Exists(sourceFile))
                return null;

            string ext;

            try
            {
                ext = Path.GetExtension(sourceFile).ToLowerInvariant();
            }
            catch (ArgumentException ex)
            {
                
                return null;
            }

            switch (ext)
            {
                case ".png":
                    File.Copy(sourceFile, targetFile);

                    if (lossy)
                        return string.Format(CultureInfo.CurrentCulture, "/c pingo -s8 -q -palette=79 \"{0}\"", targetFile);
                    else
                        return string.Format(CultureInfo.CurrentCulture, "/c pingo -s8 -q \"{0}\"", targetFile);
                //if (lossy)
                //    return string.Format(CultureInfo.CurrentCulture, "/c PNGOptimizerCL -file \"{0}\"", targetFile);
                //else
                //    return string.Format(CultureInfo.CurrentCulture, "/c PNGOptimizerCL -s8 -q \"{0}\"", targetFile);

                case ".jpg":
                case ".jpeg":
                    if (lossy)
                        return string.Format(CultureInfo.CurrentCulture, "/c cjpeg -quality 80,60 -dct float -smooth 5 -outfile \"{1}\" \"{0}\"", sourceFile, targetFile);
                    else
                        return string.Format(CultureInfo.CurrentCulture, "/c jpegtran -copy none -optimize -progressive -outfile \"{1}\" \"{0}\"", sourceFile, targetFile);
                        //return string.Format(CultureInfo.CurrentCulture, "/c guetzli_windows_x86-64 \"{0}\" \"{1}\"", sourceFile, targetFile);

                case ".gif":
                    return string.Format(CultureInfo.CurrentCulture, "/c gifsicle -O3 \"{0}\" --output=\"{1}\"", sourceFile, targetFile);
            }

            return null;
        }

        public static bool IsFileSupported(string fileName)
        {
            string ext = Path.GetExtension(fileName);

            return _supported.Any(s => s.Equals(ext, StringComparison.OrdinalIgnoreCase));
        }


        #region new compress

        /// <summary>
        /// 无损压缩图片
        /// </summary>
        /// <param name="sFile">原图片地址</param>
        /// <param name="dFile">压缩后保存图片地址</param>
        /// <param name="flag">压缩质量（数字越小压缩率越高）1-100</param>
        /// <param name="size">压缩后图片的最大大小</param>
        /// <param name="sfsc">是否是第一次调用</param>
        /// <returns></returns>
        public static bool CompressImage(string sFile, string dFile, int flag = 90, int size = 300, bool sfsc = true)
        {
            //如果是第一次调用，原始图像的大小小于要压缩的大小，则直接复制文件，并且返回true
            FileInfo firstFileInfo = new FileInfo(sFile);
            if (sfsc == true && firstFileInfo.Length < size * 1024)
            {
                firstFileInfo.CopyTo(dFile);
                return true;
            }
            Image iSource = Image.FromFile(sFile);
            ImageFormat tFormat = iSource.RawFormat;
            int dHeight = iSource.Height / 2;
            int dWidth = iSource.Width / 2;
            int sW = 0, sH = 0;
            //按比例缩放
            Size tem_size = new Size(iSource.Width, iSource.Height);
            if (tem_size.Width > dHeight || tem_size.Width > dWidth)
            {
                if ((tem_size.Width * dHeight) > (tem_size.Width * dWidth))
                {
                    sW = dWidth;
                    sH = (dWidth * tem_size.Height) / tem_size.Width;
                }
                else
                {
                    sH = dHeight;
                    sW = (tem_size.Width * dHeight) / tem_size.Height;
                }
            }
            else
            {
                sW = tem_size.Width;
                sH = tem_size.Height;
            }

            Bitmap ob = new Bitmap(dWidth, dHeight);
            Graphics g = Graphics.FromImage(ob);

            g.Clear(Color.WhiteSmoke);
            g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

            g.DrawImage(iSource, new Rectangle((dWidth - sW) / 2, (dHeight - sH) / 2, sW, sH), 0, 0, iSource.Width, iSource.Height, GraphicsUnit.Pixel);

            g.Dispose();

            //以下代码为保存图片时，设置压缩质量
            EncoderParameters ep = new EncoderParameters();
            long[] qy = new long[1];
            qy[0] = flag;//设置压缩的比例1-100
            EncoderParameter eParam = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, qy);
            ep.Param[0] = eParam;

            try
            {
                ImageCodecInfo[] arrayICI = ImageCodecInfo.GetImageEncoders();
                ImageCodecInfo jpegICIinfo = null;
                for (int x = 0; x < arrayICI.Length; x++)
                {
                    if (arrayICI[x].FormatDescription.Equals("JPEG"))
                    {
                        jpegICIinfo = arrayICI[x];
                        break;
                    }
                }
                if (jpegICIinfo != null)
                {
                    ob.Save(dFile, jpegICIinfo, ep);//dFile是压缩后的新路径
                    FileInfo fi = new FileInfo(dFile);
                    if (fi.Length > 1024 * size)
                    {
                        flag = flag - 10;
                        CompressImage(sFile, dFile, flag, size, false);
                    }
                }
                else
                {
                    ob.Save(dFile, tFormat);
                }
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                iSource.Dispose();
                ob.Dispose();
            }
        }

        #endregion
    }
}
