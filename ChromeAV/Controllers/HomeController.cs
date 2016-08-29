using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ChromeAV.Mvc;

namespace ChromeAV.Controllers
{
    [SessionState(System.Web.SessionState.SessionStateBehavior.Disabled)]
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult GetStream(bool sendAsChunked, string burstCache) 
        {
            if (sendAsChunked)
                this.Response.AddHeader("Accept-Ranges", "none");

            return new RangeFilePathResult(this.Server.MapPath("~/App_Data/Big_Buck_Bunny_Trailer_400p.ogg"), "video/ogg", sendAsChunked);
        }
    }
}
