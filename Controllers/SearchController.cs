using DocSea.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DocSea.Controllers
{
    public class SearchController : Controller
    {
        public ActionResult Index([Bind(Include = "SearchKeyword")] SearchModel search)
        {
            search.SearchResults = new List<string>();
            if (!string.IsNullOrEmpty(search.SearchKeyword))
            {
                search.SearchResults.Add(search.SearchKeyword);
            }
            return View(search);
        }
    }
}