using HtmlAgilityPack;
int attempts = 0;
if (!File.Exists("FoundLinks.txt"))
{
    File.WriteAllText("FoundLinks.txt", "Links:\n");
}
while (true)
{
    string videoID = new string(Enumerable.Range(0, 6).Select(_ => "0123456789abcdefghijklmnopqrstuvwxyz"[new Random().Next(36)]).ToArray());
    try
    {
        string response = new HttpClient().GetStringAsync($"https://streamable.com/{videoID}").Result;
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(response);
        var metaTag = htmlDoc.DocumentNode.SelectSingleNode("//meta[@name='description']");
        if (metaTag != null && metaTag.GetAttributeValue("content", "").Contains("This video is unavailable."))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(($"https://streamable.com/{videoID} | Video is unavailable"));
            Console.ResetColor();
        }
        else
        {
            Console.WriteLine($"https://streamable.com/{videoID} | " + htmlDoc.DocumentNode.SelectSingleNode("//h1").InnerText);
            using (StreamWriter streamwriter = File.AppendText("FoundLinks.txt"))
            {
                streamwriter.WriteLine($"https://streamable.com/{videoID} | " + htmlDoc.DocumentNode.SelectSingleNode("//h1").InnerText);
            }
        }
    }
    catch { }
    attempts++;
    Console.Title = $"Attempts {attempts}";
}