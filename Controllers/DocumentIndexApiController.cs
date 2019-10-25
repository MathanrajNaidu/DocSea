using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using DocSea.Models;

namespace DocSea.Controllers
{
    [Authorize]
    public class DocumentIndexApiController : ApiController
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: api/DocumentIndexApi
        public IQueryable<DocumentIndex> GetDocumentIndexes()
        {
            return db.DocumentIndexes;
        }

        // GET: api/DocumentIndexApi/5
        [ResponseType(typeof(DocumentIndex))]
        public IHttpActionResult GetDocumentIndex(int id)
        {
            DocumentIndex documentIndex = db.DocumentIndexes.Find(id);
            if (documentIndex == null)
            {
                return NotFound();
            }

            return Ok(documentIndex);
        }

        // PUT: api/DocumentIndexApi/5
        [ResponseType(typeof(void))]
        public IHttpActionResult PutDocumentIndex(int id, UpdateDocumentIndex updateDocumentIndex)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var documentIndex = db.DocumentIndexes.FirstOrDefault(x => x.Id == updateDocumentIndex.Id);

            documentIndex.Id = updateDocumentIndex.Id;
            documentIndex.Username = updateDocumentIndex.Username;
            documentIndex.Password = updateDocumentIndex.Password;
            documentIndex.DirectoryPath = updateDocumentIndex.DirectoryPath;

            if (id != documentIndex.Id)
            {
                return BadRequest();
            }

            db.Entry(documentIndex).State = EntityState.Modified;

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DocumentIndexExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return StatusCode(HttpStatusCode.NoContent);
        }

        // POST: api/DocumentIndexApi
        [ResponseType(typeof(DocumentIndex))]
        public IHttpActionResult PostDocumentIndex(CreateDocumentIndex createDocumentIndex)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            DocumentIndex documentIndex = new DocumentIndex() {
                Username = createDocumentIndex.Username,
                Password = createDocumentIndex.Password,
                DirectoryPath = createDocumentIndex.DirectoryPath
            };


            db.DocumentIndexes.Add(documentIndex);
            db.SaveChanges();

            return CreatedAtRoute("DefaultApi", new { id = documentIndex.Id }, documentIndex);
        }

        // DELETE: api/DocumentIndexApi/5
        [ResponseType(typeof(DocumentIndex))]
        public IHttpActionResult DeleteDocumentIndex(int id)
        {
            DocumentIndex documentIndex = db.DocumentIndexes.Find(id);
            if (documentIndex == null)
            {
                return NotFound();
            }

            db.DocumentIndexes.Remove(documentIndex);
            db.SaveChanges();

            return Ok(documentIndex);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool DocumentIndexExists(int id)
        {
            return db.DocumentIndexes.Count(e => e.Id == id) > 0;
        }
    }
}