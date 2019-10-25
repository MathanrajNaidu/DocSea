using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using DocSea.Models;
using DocSea.Process;

namespace DocSea.Controllers
{
    [Authorize]
    public class DocumentIndexController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        private IndexDocumentManager indexDocumentsManager = new IndexDocumentManager();

        // GET: DocumentIndexes
        public ActionResult Index()
        {
            var docIndexes = db.DocumentIndexes.ToList();
            docIndexes =  docIndexes.Select(x => { x.Status = indexDocumentsManager.GetJobStatus(x.JobId); return x; }).ToList();
            return View(docIndexes);
        }

        // GET: DocumentIndexes/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DocumentIndex documentIndex = db.DocumentIndexes.Find(id);
            if (documentIndex == null)
            {
                return HttpNotFound();
            }

            documentIndex.Status = indexDocumentsManager.GetJobStatus(documentIndex.JobId);
            return View(documentIndex);
        }

        // GET: DocumentIndexes/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: DocumentIndexes/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,Username,Password,DirectoryPath,Status,JobId")] DocumentIndex documentIndex)
        {
            if (ModelState.IsValid)
            {
                db.DocumentIndexes.Add(documentIndex);
                db.SaveChanges();
                Task.Factory.StartNew(() => indexDocumentsManager.CheckAndStartNewIndexingProcessAsync(documentIndex.Id));
                return RedirectToAction("Index");
            }

            return View(documentIndex);
        }

        // GET: DocumentIndexes/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DocumentIndex documentIndex = db.DocumentIndexes.Find(id);
            if (documentIndex == null)
            {
                return HttpNotFound();
            }
            return View(documentIndex);
        }

        // POST: DocumentIndexes/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,Username,Password,DirectoryPath,Status,ForceStop")] DocumentIndex documentIndex)
        {
            if (ModelState.IsValid)
            {
                var di = db.DocumentIndexes.FirstOrDefault(x => x.Id == documentIndex.Id);
                di.Username = documentIndex.Username;
                di.Password = documentIndex.Password;
                di.DirectoryPath = documentIndex.DirectoryPath;
                di.ForceStop = documentIndex.ForceStop;
                db.Entry(di).State = EntityState.Modified;
                db.SaveChanges();
                Task.Factory.StartNew(() => indexDocumentsManager.CheckAndStartNewIndexingProcessAsync(di.Id, true));
                return RedirectToAction("Index");
            }
            return View(documentIndex);
        }

        // GET: DocumentIndexes/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DocumentIndex documentIndex = db.DocumentIndexes.Find(id);
            if (documentIndex == null)
            {
                return HttpNotFound();
            }
            return View(documentIndex);
        }

        // POST: DocumentIndexes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            DocumentIndex documentIndex = db.DocumentIndexes.Find(id);
            indexDocumentsManager.ForceStopIndexingJob(documentIndex.JobId);
            db.DocumentIndexes.Remove(documentIndex);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
