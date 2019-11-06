using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loadbalancer.Http
{
    class HttpRequest : HttpMessage
    {
        public HttpRequest(string firstLine, List<HttpHeader> headers, byte[] body, byte[] requestInBytes) : base(firstLine, headers, body, requestInBytes) { }

        public static HttpRequest TryParse(byte[] requestBytes)
        {
            string requestString = Encoding.UTF8.GetString(requestBytes);
            List<string> requestLines = ToLines(requestString);

            string firstLine = requestLines[0];
            List<HttpHeader> headers = ReadHeaders(requestLines);
            byte[] body = ReadBody(requestString);

            if (headers.Count() > 0) return new HttpRequest(firstLine, headers, body, requestBytes);

            return null;
        }

        public string GetHost()
        {
            if (HasHeader("Host"))
            {
                return GetHeader("Host").Value;
            }

            return FirstLine.Split(' ')[1];
        }
    }
}
