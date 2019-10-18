using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DocSea.Models
{
    public class SearchModel
    {
        public string SearchKeyword { get; set; }

        public List<string> SearchResults { get; set; }
    }
}