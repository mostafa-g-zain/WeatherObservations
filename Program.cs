using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        string stationId = "41112";
        string baseFolder = "SoundingData";
        Directory.CreateDirectory(baseFolder);

        using HttpClient client = new HttpClient();
        // Adding a user agent is good practice when scraping academic servers
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

        DateTime startDate = new DateTime(2024, 01, 01);
        DateTime endDate = new DateTime(2024, 12, 31);

        Console.WriteLine($"Starting download for station {stationId}...");

        for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
        {
            // The URL expects 'yyyy-MM-dd 00:00:00', with %20 for the space
            // The URL expects 'yyyy-MM-dd 12:00:00', with %20 for the space
            string url_00 = $"https://weather.uwyo.edu/wsgi/sounding?datetime={date:yyyy-MM-dd}%2000:00:00&id={stationId}&type=TEXT:CSV&src=FM35";
            string url_12 = $"https://weather.uwyo.edu/wsgi/sounding?datetime={date:yyyy-MM-dd}%2012:00:00&id={stationId}&type=TEXT:CSV&src=FM35";

            try
            {
                HttpResponseMessage response_00 = await client.GetAsync(url_00);
                if (response_00.IsSuccessStatusCode)
                {
                    string content = await response_00.Content.ReadAsStringAsync();
                    string fileName_00 = Path.Combine(baseFolder, $"Sounding_{stationId}_{date:yyyyMMdd}_00.CSV");
                    await File.WriteAllTextAsync(fileName_00, content);
                    Console.WriteLine($"[Success] Saved {date:yyyy-MM-dd} 00:00");
                }
                else
                {
                    Console.WriteLine($"[Failed] {date:yyyy-MM-dd} 00:00 - Server returned: {response_00.StatusCode}");
                }

                HttpResponseMessage response_12 = await client.GetAsync(url_12);
                if (response_12.IsSuccessStatusCode)
                {
                    string content = await response_12.Content.ReadAsStringAsync();
                    string fileName_12 = Path.Combine(baseFolder, $"Sounding_{stationId}_{date:yyyyMMdd}_12.CSV");
                    await File.WriteAllTextAsync(fileName_12, content);
                    Console.WriteLine($"[Success] Saved {date:yyyy-MM-dd} 12:00");
                }
                else
                {
                    Console.WriteLine($"[Failed] {date:yyyy-MM-dd} 12:00 - Server returned: {response_12.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] {date:yyyy-MM-dd} - {ex.Message}");
            }

            // CRITICAL: 2-second delay to prevent the Wyoming server from temporarily IP-banning you for spamming requests.
            //await Task.Delay(2000);
        }
        Console.WriteLine("All downloads completed.");
    }
}

/*
 https://weather.uwyo.edu/wsgi/sounding?datetime=2024-12-11%2000:00:00&id=41112&type=TEXT:CSV&src=FM35
 https://weather.uwyo.edu/wsgi/sounding?datetime=2024-12-22%2012:00:00&id=41112&type=TEXT:CSV&src=FM35
====

 */