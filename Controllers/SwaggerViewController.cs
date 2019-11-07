using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DocSea.Controllers
{
    public class SwaggerViewController : Controller
    {
        // GET: SwaggerView
        public ActionResult Index()
        {
            return View();
        }
    }
}