using Dart.NetworkFolders;
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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using TikaOnDotNet.TextExtraction;
using Directory = System.IO.Directory;

namespace DocSea.Process
{
    public class IndexDocumentManager
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        private static readonly ILog Log = LogManager.GetLogger(typeof(IndexDocumentManager));
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
                await Task.Delay(TimeSpan.FromHours(1));
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
            catch (Exception ex)
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
                Task.Factory.StartNew(() => UpdateReader(index.DirectoryPath));
                Task.Factory.StartNew(() => CheckAndStartNewIndexingProcessAsync(id, false));
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        public void UpdateReader(string directoryNameOrPath)
        {
            if (string.IsNullOrEmpty(directoryNameOrPath)) return;

            var indexLocation = $"{Directory.GetParent(Environment.CurrentDirectory).Parent.FullName}Indexes\\{Path.GetFileName(directoryNameOrPath)}";
            var reader = DirectoryReader.Open(FSDirectory.Open(indexLocation));
            MemoryCache.Default.Add(new CacheItem(indexLocation, reader), new CacheItemPolicy {});
        }

        public void IndexDirectory(string directoryPath)
        {
            if (!Directory.Exists(directoryPath)) return;

            var files = Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories);

            var indexLocation = $"{Directory.GetParent(Environment.CurrentDirectory).Parent.FullName}Indexes\\{Path.GetFileName(directoryPath)}";

            if (!Directory.Exists(indexLocation)) Directory.CreateDirectory(indexLocation);

            var dir = FSDirectory.Open(indexLocation);

            var AppLuceneVersion = LuceneVersion.LUCENE_48;

            //create an analyzer to process the text
            var analyzer = new StandardAnalyzer(AppLuceneVersion);

            //create an index writer
            var indexConfig = new IndexWriterConfig(AppLuceneVersion, analyzer);
            var writer = new IndexWriter(dir, indexConfig);

            #region Temp to monitor text extract and indexing timing
            StreamWriter sw = File.AppendText($@"C:\\Indexes\{Path.GetFileName(directoryPath)}Count.txt");
            int i = 1;
            #endregion

            SemaphoreSlim indexingSemaphoreSlim = new SemaphoreSlim(1, 1);

            var parallelOptions = new ParallelOptions() { MaxDegreeOfParallelism = 5 };

            Parallel.ForEach(files, parallelOptions, async (file) =>
            {
                try
                {
                    #region Temp to monitor text extract and indexing timing - 1
                    Stopwatch textExtractStopWatch = new Stopwatch();
                    Stopwatch indexingStopWatch = new Stopwatch();
                    textExtractStopWatch.Start();
                    #endregion

                    //Extract text from files
                    var textExtrator = new TextExtractor();
                    var text = textExtrator.Extract(file).Text;

                    #region Temp to monitor text extract and indexing timing - 2
                    textExtractStopWatch.Stop();
                    TimeSpan tE = textExtractStopWatch.Elapsed;
                    string tEElapsedTime = string.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                        tE.Hours, tE.Minutes, tE.Seconds,
                        tE.Milliseconds / 10);
                    sw.WriteLine($"{Path.GetFileName(directoryPath)} - {i}; Text Extract time elapsed: {tEElapsedTime};  filePath: {file}");
                    #endregion

                    try
                    {
                        //One indexing at a time to avoid NativeFSLock
                        await indexingSemaphoreSlim.WaitAsync();

                        #region Temp to monitor text extract and indexing timing - 3
                        indexingStopWatch.Start();
                        #endregion

                        //Add or update text and path to Index
                        var doc = new Document();
                        doc.Add(new StringField("path", file, Field.Store.YES));
                        doc.Add(new TextField("text", text, Field.Store.YES));

                        writer.UpdateDocument(new Term("path", file), doc);

                        #region Temp to monitor text extract and indexing timing - 4
                        indexingStopWatch.Stop();
                        #endregion
                    }
                    catch (OutOfMemoryException)
                    {
                        writer.Dispose();
                        writer = new IndexWriter(dir, indexConfig);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex);
                    }
                    finally
                    {
                        #region Temp to monitor text extract and indexing timing - 5
                        TimeSpan iE = indexingStopWatch.Elapsed;
                        string iEElapsedTime = string.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                            iE.Hours, iE.Minutes, iE.Seconds,
                            iE.Milliseconds / 10);
                        sw.WriteLine($"{Path.GetFileName(directoryPath)} - {i}; Indexing time elapsed: {iEElapsedTime}; filePath: {file}");
                        i++;
                        #endregion

                        indexingSemaphoreSlim.Release();
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }
            });

            #region Temp to monitor text extract and indexing timing - 6
            sw.Close();
            #endregion

            writer.Flush(triggerMerge: false, applyAllDeletes: false);
            writer.Commit();
            writer.Dispose();
            dir.Dispose();
        }

        public List<string> Search(string directoryNameOrPath, string keyword)
        {
            var indexLocation = $"{Directory.GetParent(Environment.CurrentDirectory).Parent.FullName}Indexes\\{Path.GetFileName(directoryNameOrPath)}";

            var dir = FSDirectory.Open(indexLocation);

            DirectoryReader reader = MemoryCache.Default.GetCacheItem(indexLocation)?.Value as DirectoryReader;
            if (reader == null)
            {
                if (!DirectoryReader.IndexExists(dir)) throw new DirectoryNotFoundException();

                reader = DirectoryReader.Open(dir);
                MemoryCache.Default.Add(new CacheItem(indexLocation, reader), new CacheItemPolicy {});
            }

            // search with a phrase
            var keywordList = keyword.Split(' ').ToList();
            var phrase = new MultiPhraseQuery();
            foreach (var word in keywordList)
            {
                phrase.Add(new Term("text", word));
            }

            var searcher = new IndexSearcher(reader);
            var hits = searcher.Search(phrase, 20 /*, 20 top 20 */).ScoreDocs;
            var paths = new List<string>();
            foreach (var hit in hits)
            {
                var foundDoc = searcher.Doc(hit.Doc);
                paths.Add(foundDoc.GetField("path").GetStringValue());
                //add query for filename
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