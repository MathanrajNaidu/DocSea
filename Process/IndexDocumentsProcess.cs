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
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using TikaOnDotNet.TextExtraction;
using Directory = System.IO.Directory;

namespace DocSea.Process
{
    public class IndexDocumentsProcess
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        private static readonly ILog Log = LogManager.GetLogger(typeof(IndexDocumentsProcess));
        private static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

        public async Task IndexingTimerAsync()
        {
            while (true)
            {
                var documentIndexes = db.DocumentIndexes.Where(x => !x.ForceStop).ToList();
                foreach (var index in documentIndexes)
                {
                   await CheckAndStartNewIndexingProcessAsync(index.Id);
                };
                await Task.Delay(TimeSpan.FromSeconds(30));
            }
        }

        public async Task CheckAndStartNewIndexingProcessAsync(int id, bool forceStop = false)
        {
            try
            {
               await semaphoreSlim.WaitAsync();

                var index = db.DocumentIndexes.FirstOrDefault(x => x.Id == id);
                if (index == null) return;
                if (forceStop) ForceStopIndexingJob(index.JobId);
                if (index.ForceStop) return;
                if (!string.IsNullOrEmpty(index.JobId))
                {
                    var jobState = GetJobStatus(index.JobId);
                    var jobStartIfStates = new List<string> { "succeeded", "failed", "deleted" };
                   
                    if (jobState != null && !jobStartIfStates.Contains(jobState.ToLower()))
                        return;
                        
                }
                var jobId = BackgroundJob.Schedule(() => ProcessDirectory(index.Id), TimeSpan.FromMinutes(1));
                index.JobId = jobId;
                db.Entry(index).State = EntityState.Modified;
                db.SaveChanges();
            }
            catch(Exception ex)
            {
                Log.Error(ex);
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        public void ProcessDirectory(int id)
        {
            var index = db.DocumentIndexes.Find(id);
            try
            {
                if (string.IsNullOrEmpty(index.Username) || string.IsNullOrEmpty(index.Password))
                {
                    IndexDirectory(index.DirectoryPath);
                }
                else
                {
                    using (var conn = new NetworkConnection(index.DirectoryPath, new NetworkCredential(index.Username, index.Password, "Petronas")))
                    {
                        IndexDirectory(index.DirectoryPath);
                        conn.Dispose();
                    }
                }
                Task.Factory.StartNew(() => CheckAndStartNewIndexingProcessAsync(id, false)); 
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        public void IndexDirectory(string directoryPath)
        {
            if (!Directory.Exists(directoryPath)) return;
            var files = Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories);

            var AppLuceneVersion = LuceneVersion.LUCENE_48;

            string workingDirectory = Environment.CurrentDirectory;

            string projectDirectory = Directory.GetParent(workingDirectory).Parent.FullName;

            var indexLocation = $"{projectDirectory}Indexes\\{Path.GetFileName(directoryPath)}";
            if (!Directory.Exists(indexLocation)) Directory.CreateDirectory(indexLocation);


            //create an analyzer to process the text
            var analyzer = new StandardAnalyzer(AppLuceneVersion);

            int i = 1;
            foreach (var file in files)
            {
                try
                {
                    var textExtrator = new TextExtractor();

                    var text = textExtrator.Extract(file).Text;

                    var doc = new Document();

                    doc.Add(new StringField("path", file, Field.Store.YES));

                    doc.Add(new TextField("text", text, Field.Store.YES));

                    var dir = FSDirectory.Open(indexLocation);  

                    //create an index writer
                    var indexConfig = new IndexWriterConfig(AppLuceneVersion, analyzer);

                    using (var writer = new IndexWriter(dir, indexConfig))
                    {
                        writer.UpdateDocument(new Term("path", file), doc);
                        writer.Flush(triggerMerge: false, applyAllDeletes: false);
                        writer.Commit();
                        writer.Dispose();
                    };

                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }
                finally
                {
                    File.AppendAllText($"{projectDirectory}Indexes\\Count.txt", $"{Path.GetFileName(directoryPath)}, {i}");
                    i++;
                }

            }
        }

        public List<string> Search(string directoryNameOrPath, string keyword)
        {
            var AppLuceneVersion = LuceneVersion.LUCENE_48;

            string workingDirectory = Environment.CurrentDirectory;

            string projectDirectory = Directory.GetParent(workingDirectory).Parent.FullName;

            var indexLocation = $"{projectDirectory}Indexes\\{Path.GetFileName(directoryNameOrPath)}";
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
        
        public void ForceStopIndexingJob(string jobId)
        {
            if (string.IsNullOrEmpty(jobId)) return;
            BackgroundJob.Delete(jobId);
        }

        public string GetJobStatus(string jobId)
        {
            if (jobId == null) return "Idle";
            IStorageConnection connection = JobStorage.Current.GetConnection();
            JobData jobData = connection.GetJobData(jobId);

            return jobData?.State;
        }

    }
}