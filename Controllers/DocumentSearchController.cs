using DocSea.Process;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;

namespace DocSea.Controllers
{
    public class DocumentSearchController : ApiController
    {

        private readonly IndexDocumentManager indexDocumentsManager = new IndexDocumentManager();

        // GET: api/DocumentSearchApi/"path"/keyword
        [ResponseType(typeof(List<string>))]
        [Route("api/DocumentSearch")]
        public IHttpActionResult GetDocumentSearch(string DirectoryPathOrName, string SearchKeyword)
        {
            if (string.IsNullOrEmpty(SearchKeyword) || string.IsNullOrEmpty(DirectoryPathOrName))
            {
                return NotFound();
            }
            var results = indexDocumentsManager.Search(DirectoryPathOrName, SearchKeyword);

            return Ok(results);
        }
    }
}
