using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DocSea.Models
{
    public class SearchModel
    {
        public string SearchKeyword { get; set; }

        public string SelectedPath { get; set; }

        public List<string> SearchResults { get; set; }

        public IEnumerable<SelectListItem> DocumentPaths { get; set; }
    }
}