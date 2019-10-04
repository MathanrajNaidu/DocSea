using Dart.NetworkFolders;
using DocSea.Helpers;
using DocSea.Models;
using Hangfire;
using Hangfire.Storage;
using log4net;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Directory = System.IO.Directory;

namespace DocSea.Process
{
    public class IndexDocumentsProcess
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        private static readonly ILog Log = LogManager.GetLogger(typeof(IndexDocumentsProcess));

        public void IndexingTimer()
        {            
            while (true)
            {
                CheckAllDirectoryIndexing();
                Task.Delay(TimeSpan.FromHours(1));
            }
        }

        public void CheckAllDirectoryIndexing()
        {
            var documentIndexes = db.DocumentIndexes.Where(x => !x.ForceStop).ToList();
            foreach(var index in documentIndexes)
            {
                CheckAndStartNewIndexingProcess(index.Id);
            };
        }

        public void CheckAndStartNewIndexingProcess(int id)
        {
            var index = db.DocumentIndexes.FirstOrDefault(x => x.Id == id);

            IStorageConnection connection = JobStorage.Current.GetConnection();
            JobData jobData = connection.GetJobData(index.JobId.ToString());
            if (jobData.State.ToLower() != "succeeded" || jobData.State.ToLower() != "failed")
                return;
            BackgroundJob.Enqueue(() => ProcessDirectory(index.Id));
        }
                
        public void ProcessDirectory(int id)
        {
            var index = db.DocumentIndexes.Find(id);
            try
            {
                if (string.IsNullOrEmpty(index.Username) || string.IsNullOrEmpty(index.Password))
                {
                    IndexDirectory(index.DirectoryPath);
                    return;
                }
                else
                {
                    using (var conn = new NetworkConnection(index.DirectoryPath, new NetworkCredential(index.Username, index.Password, "Petronas")))
                    {
                        IndexDirectory(index.DirectoryPath);
                        conn.Dispose();
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            finally
            {
                CheckAndStartNewIndexingProcess(id);
            }
        }

        public void IndexDirectory(string directoryPath)
        {
            if(!Directory.Exists(directoryPath)) return;
            var files = Directory.GetFiles(directoryPath).ToList();


            var AppLuceneVersion = LuceneVersion.LUCENE_48;

            var indexLocation = $"Indexes/{Path.GetDirectoryName(directoryPath)}";
            var dir = FSDirectory.Open(indexLocation);

            //create an analyzer to process the text
            var analyzer = new StandardAnalyzer(AppLuceneVersion);

            //create an index writer
            var indexConfig = new IndexWriterConfig(AppLuceneVersion, analyzer);

            foreach (var file in files)
            {
                var text = file.Convert();

                var doc = new Document();
                // StringField indexes but doesn't tokenise
                doc.Add(new StringField("path", file, Field.Store.YES));

                doc.Add(new TextField("text", text, Field.Store.YES));

                var writer = new IndexWriter(dir, indexConfig);

                writer.AddDocument(doc);
                writer.Flush(triggerMerge: false, applyAllDeletes: false);
            };
        }

        public void ForceStopIndexingJob(int jobId)
        {
            BackgroundJob.Delete(jobId.ToString());
            return;
        }

        public List<string> Search(string directoryNameOrPath, string keyword)
        {
            var AppLuceneVersion = LuceneVersion.LUCENE_48;

            var indexLocation = $"Indexes/{Path.GetDirectoryName(directoryNameOrPath)}";
            var dir = FSDirectory.Open(indexLocation);

            //create an analyzer to process the text
            var analyzer = new StandardAnalyzer(AppLuceneVersion);

            //create an index writer
            var indexConfig = new IndexWriterConfig(AppLuceneVersion, analyzer);
            var writer = new IndexWriter(dir, indexConfig);

            // search with a phrase
            var phrase = new MultiPhraseQuery();
            phrase.Add(new Term("path", keyword));
            phrase.Add(new Term("text", keyword));

            // re - use the writer to get real - time updates
            var searcher = new IndexSearcher(writer.GetReader(applyAllDeletes: true));
            var hits = searcher.Search(phrase, 20 /* top 20 */).ScoreDocs;
            var paths = new List<string>();
            foreach (var hit in hits)
            {
                var foundDoc = searcher.Doc(hit.Doc);
                paths.Add(foundDoc.GetField("path").Name);
            }
            return paths;
        }
    }
}