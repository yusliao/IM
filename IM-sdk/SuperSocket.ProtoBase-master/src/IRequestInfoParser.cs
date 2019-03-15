using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperSocket.ProtoBase
{
    /// <summary>
    /// The interface for request info parser 
    /// </summary>
    public interface IRequestInfoParser<TRequestInfo>
        where TRequestInfo : IPackageInfo
    {
        /// <summary>
        /// Parses the request info from the source string.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="offset"></param>
        /// <param name="lengh"></param>
        /// <returns></returns>
        TRequestInfo ParseRequestInfo(byte[] source,int offset,int lengh);
    }
}
