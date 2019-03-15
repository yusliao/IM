using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SuperSocket.ProtoBase;
using SDKClient.Protocol;

using System.ComponentModel.Composition;
using SDKClient.Model;
using Autofac;
using Util.Logs.Aspects;
using Util.Dependency;

namespace SDKClient
{
    class RecvFilter2: FixedHeaderReceiveFilter<Model.PackageInfo>
    {
        static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly static byte[] BeginMark = new byte[] { 0xFF, 0xFF };
        private readonly static byte[] EndMark = new byte[] { 0xFF, 0xFE };
        private const int HEADLEN = 10;


        public RecvFilter2()
            : base(HEADLEN)
        {

        }
        
        public override Model.PackageInfo ResolvePackage(IBufferStream bufferStream)
        {
            byte[] buff = new byte[bufferStream.Length];
            int index = 0;
            foreach (var item in bufferStream.Buffers)
            {
                Buffer.BlockCopy(item.Array, item.Offset, buff, index, item.Count);
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
              //  var info = Util.Helpers.Ioc.Create<IRequestInfoParser<PackageInfo>>().ParseRequestInfo(buff,10,sourcelen);
                if (info.code != 0)
                {
                    info.ErrorLog();
                }
                else
                    info.RECVLog();
                return info;
               
              
            }

            //  return null;

        }

        protected override int GetBodyLengthFromHeader(IBufferStream bufferStream, int length)
        {
            byte[] buff = new byte[bufferStream.Length];
            int index = 0;
            foreach (var item in bufferStream.Buffers)
            {
                Buffer.BlockCopy(item.Array, item.Offset, buff, index, item.Count);
                index += item.Count;
            }
            int len = BitConverter.ToInt32(buff, 2);
            return len+10;

        }
    }
    [Export(typeof(Util.Dependency.ConfigBase))]
    class RecvFilterConfig : Util.Dependency.ConfigBase
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.AddSingleton<FixedHeaderReceiveFilter<Model.PackageInfo>, RecvFilter2>();
            builder.AddSingleton<IRequestInfoParser<PackageInfo>, RequestInfoParse>();
        }
    }
    


}
