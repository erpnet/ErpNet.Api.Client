using System;
using System.Collections.Generic;
using System.Text;

namespace ErpNet.Api.Client.OData
{
    /// <summary>
    /// Represents an error returned by ERP.net odata service.
    /// </summary>
    public class ODataException: Exception
    {
        /// <summary>
        /// Creates an instance of <see cref="ODataException"/>
        /// </summary>
        /// <param name="type"></param>
        /// <param name="code"></param>
        /// <param name="message"></param>
        /// <param name="info"></param>
        public ODataException(string type, int code, string message, string info): base(message)
        {
            Type = type;
            Code = code;
            Info = info;
        }

        /// <summary>
        /// The code of the server exception. 
        /// </summary>
        public int Code { get; }

        /// <summary>
        /// The server type of the error.
        /// </summary>
        public string Type { get; }

        /// <summary>
        /// Typically the error stack trace.
        /// </summary>
        public string Info { get; }
    }
}
