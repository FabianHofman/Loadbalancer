using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loadbalancer.Http
{
    class HttpResponse : HttpMessage
    {
        public HttpResponse(string firstLine, List<HttpHeader> headers, byte[] body, byte[] responseInBytes) : base(firstLine, headers, body, responseInBytes) { }

        public static HttpResponse TryParse(byte[] responseBytes)
        {
            string responseString = Encoding.UTF8.GetString(responseBytes);
            List<string> responseLines = ToLines(responseString);

            string firstLine = responseLines[0];
            List<HttpHeader> headers = ReadHeaders(responseLines);
            byte[] body = ReadBody(responseString);

            if (headers.Count() > 0)
            {
                return new HttpResponse(firstLine, headers, body, responseBytes);
            }

            return null;
        }

        public static HttpResponse GetInternalServerErrorResponse()
        {
            string firstLine = "HTTP/1.1 500 Internal Server Error";
            List<HttpHeader> headers = new List<HttpHeader>
            {
                new HttpHeader("Connection", "close"),
                new HttpHeader("Content-Type", "image/svg+xml"),
                new HttpHeader("Date", DateTime.Now.ToUniversalTime().ToString("r"))
            };
            byte[] body = Encoding.UTF8.GetBytes("Internal server error! Try again later!");

            return new HttpResponse(firstLine, headers, body, new byte[0]);
        }
    }
}
