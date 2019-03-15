using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SuperSocket.ClientEngine;
using SuperSocket.ProtoBase;

using SDKClient.Model;

namespace SDKClient
{
   /// <summary>
   /// 开始,结尾，固定长度过滤器
   /// </summary>
    public class RecvFilter : BeginEndMarkFixedBodyReceiveFilter<Model.PackageInfo>
    {
        static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly static byte[] BeginMark = new byte[] { 0xFF,0xFF };
        private readonly static byte[] EndMark = new byte[] { 0xFF,0xFE };
        private const int FIXEDLEN = 4;


        public RecvFilter()
            : base(BeginMark, EndMark, FIXEDLEN)
        {

        }
        public override PackageInfo Filter(BufferList data, out int rest)
        {
            return base.Filter(data, out rest);
        }
        public override Model.PackageInfo ResolvePackage(IBufferStream bufferStream)
        {
            byte[] buff = new byte[bufferStream.Length];
            int index = 0;
            foreach (var item in bufferStream.Buffers)
            {
                Buffer.BlockCopy(item.Array, item.Offset, buff,index, item.Count);
                index += item.Count;
            }
            int sourcelen = buff.Length - 20;
            if (sourcelen == 1)//心跳包
            {
                logger.Info($"收到心跳包:\t{SDKClient.Instance.property.CurrentAccount.Session}");
                return new Model.HeartMsgPackage();
            }
            else
            {
                var info = RequestInfoParse.instance.ParseRequestInfo(buff, 10, sourcelen);
                
                return info;
            }
            
          //  return null;

        }

       
    }
}
