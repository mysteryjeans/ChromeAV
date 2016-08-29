using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using System.Threading;

namespace ChromeAV.Mvc
{
    public class RangeFilePathResult : FilePathResult
    {
        private long startIndex;
        private long? endIndex;
        private bool sendAsChunked;

        public RangeFilePathResult(string fileName, string contentType, bool sendAsChunked)
            : base(fileName, contentType)
        {
            this.sendAsChunked = sendAsChunked;
        }

        public override void ExecuteResult(ControllerContext context)
        {
            var request = context.RequestContext.HttpContext.Request;

            try
            {
                var range = request.Headers["Range"].Trim().Substring("bytes=".Length).Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
                this.startIndex = long.Parse(range[0]);
                this.endIndex = range.Length > 1 ? (long?)long.Parse(range[1]) : null;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Unable to parse Range request header: {0}", request.Headers["Range"]), ex);
            }

            base.ExecuteResult(context);
        }

        protected override void WriteFile(HttpResponseBase response)
        {
            var info = new FileInfo(this.FileName);
            if (!info.Exists)
                throw new FileNotFoundException(this.FileName);

            this.endIndex = this.endIndex ?? info.Length - 1;
            if (this.startIndex >= 0 && this.endIndex >= 0 && this.startIndex < info.Length && this.endIndex < info.Length)
            {
                var contentLength = this.endIndex - this.startIndex + 1;

                if (contentLength == info.Length)
                    response.StatusCode = 200;
                else
                {
                    response.StatusCode = 206;
                    response.AddHeader("Content-Range", string.Format("bytes {0}-{1}/{2}", this.startIndex, this.endIndex, info.Length));
                }

                if (!this.sendAsChunked)
                    response.AddHeader("Content-Length", contentLength.ToString());

                response.Buffer = false;
                byte[] buffer = new byte[1024 * 10]; // 100K
                using (var stream = info.OpenRead())
                {
                    stream.Position = this.startIndex;
                    while (stream.Position <= this.endIndex)
                    {
                        Thread.Sleep(100);
                        var readSize = (int)(stream.Position + buffer.Length - 1 <= this.endIndex ? buffer.Length : this.endIndex - stream.Position + 1);

                        readSize = stream.Read(buffer, 0, readSize);
                        response.OutputStream.Write(buffer, 0, readSize);
                    }
                }
            }
            else
            {
                response.StatusCode = 416;
                response.StatusDescription = "Requested Range Not Satisfiable";
            }
        }
    }
}