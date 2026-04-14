using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace WheatherObservations
{
    class Program
    {
        static async Task Main()
        {
            string stationId = "41112";
            string baseFolder = "SoundingData";
            Directory.CreateDirectory(baseFolder);

            string combinedFile = Path.Combine(baseFolder, $"Sounding_{stationId}_2024_ALL.CSV");

            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

            DateTime startDate = new DateTime(2024, 01, 01);
            DateTime endDate = new DateTime(2024, 01, 10);

            bool combinedHeaderWritten = false;

            await using StreamWriter combinedWriter = new StreamWriter(combinedFile, append: false);

            Console.WriteLine($"Starting download for station {stationId}...");

            for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
            {
                string url00 = $"https://weather.uwyo.edu/wsgi/sounding?datetime={date:yyyy-MM-dd}%2000:00:00&id={stationId}&type=TEXT:CSV&src=FM35";
                string url12 = $"https://weather.uwyo.edu/wsgi/sounding?datetime={date:yyyy-MM-dd}%2012:00:00&id={stationId}&type=TEXT:CSV&src=FM35";

                await DownloadAndAppendAsync(date, "00", url00);
                await DownloadAndAppendAsync(date, "12", url12);

                // Keep this delay to avoid rate-limiting / temporary IP blocks.
                //await Task.Delay(2000);
            }

            Console.WriteLine($"All downloads completed. Combined file: {combinedFile}");

            async Task DownloadAndAppendAsync(DateTime date, string hour, string url)
            {
                try
                {
                    HttpResponseMessage response = await client.GetAsync(url);
                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"[Failed] {date:yyyy-MM-dd} {hour}:00 - Server returned: {response.StatusCode}");
                        return;
                    }

                    string content = await response.Content.ReadAsStringAsync();

                    // Optional: keep individual files too
                    string separateFile = Path.Combine(baseFolder, $"Sounding_{stationId}_{date:yyyyMMdd}_{hour}.CSV");
                    await File.WriteAllTextAsync(separateFile, content);
                    Console.WriteLine($"[Success] Saved {date:yyyy-MM-dd} {hour}:00");

                    string[] lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                    if (lines.Length == 0)
                    {
                        return;
                    }

                    int startIndex = 0;

                    // Write combined header once, with an extra datetime column
                    if (!combinedHeaderWritten)
                    {
                        await combinedWriter.WriteLineAsync("ObservationDateTimeUtc," + lines[0]);
                        combinedHeaderWritten = true;
                        startIndex = 1;
                    }
                    else if (lines[0].Contains(","))
                    {
                        // Skip repeated source header in subsequent files
                        startIndex = 1;
                    }

                    string timestamp = $"{date:yyyy-MM-dd} {hour}:00:00";
                    for (int i = startIndex; i < lines.Length; i++)
                    {
                        await combinedWriter.WriteLineAsync($"{timestamp},{lines[i]}");
                    }

                    await combinedWriter.FlushAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Error] {date:yyyy-MM-dd} {hour}:00 - {ex.Message}");
                }
            }
        }
    }
}

/*
 https://weather.uwyo.edu/wsgi/sounding?datetime=2024-12-11%2000:00:00&id=41112&type=TEXT:CSV&src=FM35
 https://weather.uwyo.edu/wsgi/sounding?datetime=2024-12-22%2012:00:00&id=41112&type=TEXT:CSV&src=FM35
====

 */