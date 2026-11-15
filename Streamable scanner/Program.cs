using System.Collections.Concurrent;

HttpClient httpClient = new(); // Shared across threads

// Ensure files exist
if (!File.Exists("FoundLinks.txt"))
    File.WriteAllText("FoundLinks.txt", "Links:\n");
if (!File.Exists("attemptedids.txt"))
    File.WriteAllText("attemptedids.txt", "");

// Thread-safe ID storage
ConcurrentDictionary<string, bool> attemptedIds = new();
foreach (var id in File.ReadAllLines("attemptedids.txt"))
    attemptedIds.TryAdd(id, true);

int attempts = 0;
int validvids = 0;
int invalidvids = 0;
object consoleLock = new();
object fileLock = new();

async Task WorkerAsync(int workerId)
{
    string videoID;
    Random rng = new();
    while (true)
    {
        int length = rng.Next(5, 7); 

        videoID = new string(Enumerable.Range(0, length)
            .Select(_ => "0123456789abcdefghijklmnopqrstuvwxyz"[rng.Next(36)]).ToArray());

        if (!attemptedIds.TryAdd(videoID, true))
            continue; // skip duplicates quickly

        Interlocked.Increment(ref attempts);
        lock (fileLock)
        {
            File.AppendAllText("attemptedids.txt", videoID + "\n");
        }
        Console.Title = $"Attempts: {attempts} | ✅ {validvids} ❌ {invalidvids}";
        //Console.WriteLine($"[Worker {workerId}] trying {videoID}");

        try
        {
            string response = await httpClient.GetStringAsync($"https://streamable.com/{videoID}");
            var htmlDoc = new HtmlAgilityPack.HtmlDocument();
            htmlDoc.LoadHtml(response);

            var metaTag = htmlDoc.DocumentNode.SelectSingleNode("//meta[@name='description']");

            if (metaTag != null)
            {
                string metaContent = metaTag.GetAttributeValue("content", "");

                if (metaContent.Contains("This video is unavailable.") ||
                    metaContent.Contains("This video is private") ||
                    metaContent.Contains("Video is uploading"))
                {
                    Interlocked.Increment(ref invalidvids);
                    lock (consoleLock)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        Console.WriteLine($"[Worker {workerId}] Unavailable: {videoID}");
                        Console.ResetColor();
                    }
                    continue;
                }
                else if (metaContent.Contains("This video is restricted"))
                {
                    Interlocked.Increment(ref invalidvids);
                    lock (consoleLock)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"[Worker {workerId}] Restricted: {videoID}");
                        Console.ResetColor();
                    }
                    continue;
                }
            }

            string title = htmlDoc.DocumentNode.SelectSingleNode("//h1")?.InnerText ?? "Unknown title";
            Interlocked.Increment(ref validvids);
            lock (consoleLock)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[Worker {workerId}] Found: https://streamable.com/{videoID} | {title}");
                Console.ResetColor();
            }


            lock (fileLock)
            {
                File.AppendAllText("attemptedids.txt", videoID + "\n");
                File.AppendAllText("FoundLinks.txt", $"https://streamable.com/{videoID} | {title}\n");
            }

            var message = new
            {
                content = $"Found new video https://streamable.com/{videoID}",
                username = "Streamable Scanner"
            };

            
        }
        catch 
        {
            lock (consoleLock)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                //Console.WriteLine($"[Worker {workerId}] Network error: {ex.Message.Split('\n')[0]}");
                Console.ResetColor();
            }
        }
    }
}

var workers = new[]
{
    Task.Run(() => WorkerAsync(1)),
    //Task.Run(() => WorkerAsync(2))
    
};
//To add more workers copy line 120 and add a new worker number in the WorkerAsync method, for example: Task.Run(() => WorkerAsync(3)) and so on. Example already made for 2 but commented out since streamable has a rate limit and too many workers will get you ratelimited.
//Even with with 1 worker it will block you after awhile but not as fast as having 2 if you can edit this to proxy the different workers good for you but i have no clue on how to do that :P



await Task.WhenAll(workers);



//Comments are made by ChatGPT (Apart from 124 and 125) since i had no clue on how to make multiple threads run this