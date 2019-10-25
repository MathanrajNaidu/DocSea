using DocSea.Models;
using DocSea.Process;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace DocSea.Controllers
{
    public class SearchController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        private IndexDocumentManager indexDocumentsManager = new IndexDocumentManager();

        public ActionResult Index([Bind(Include = "SearchKeyword,SelectedPath")] SearchModel search)
        {
            search.SearchResults = new List<string>();
            if (!string.IsNullOrEmpty(search.SearchKeyword) && !string.IsNullOrEmpty(search.SelectedPath))
            {
                search.SearchResults = indexDocumentsManager.Search(search.SelectedPath, search.SearchKeyword);
            }

            search.DocumentPaths = GetDocumentPaths();

            return View(search);
        }

        private IEnumerable<SelectListItem> GetDocumentPaths()
        {
            var documents = db.DocumentIndexes.Select(x =>
            new SelectListItem {
                Text = x.DirectoryPath,
                Value = x.DirectoryPath
            });

            return new SelectList(documents, "Value", "Text");
        }
    }
}