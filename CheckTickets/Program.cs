using System;
using System.Collections.Generic;
using System.Json;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CheckTickets
{
  class Program
  {
    private static Dictionary<string, string> snapshot;

    protected static void sendMail(string subj, string msg)
    {
      try
      {
        string emailMessage = msg;
        MailMessage mailMsg = new MailMessage();
        mailMsg.From = new MailAddress("@gmail.com");
        mailMsg.To.Add(new MailAddress("@gmail.com"));
        mailMsg.Subject = "uz.gov.ua :: " + subj;
        mailMsg.Body = string.Format(emailMessage);
        mailMsg.IsBodyHtml = false;

        SmtpClient client = new SmtpClient("smtp.gmail.com");
        client.Port = 587;
        client.Credentials = new System.Net.NetworkCredential("x", "y");
        client.EnableSsl = true;
        client.Send(mailMsg);
      }
      catch (Exception)
      {
        Console.WriteLine("unable to send mail!!");
      }
    }

    protected static void MakeRequest(string date, string from, string to, bool beep)
    {
      try
      {
        WebClient webClient = new WebClient();
        webClient.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
        webClient.Headers["GV-Unique-Host"] = "1";
        webClient.Headers["GV-Ajax"] = "1";
        webClient.Headers["GV-Screen"] = "1600x900";
        webClient.Headers["GV-Token"] = "d079bd9cd0d961bf68bd7b029f4363e3";
        webClient.Headers["GV-Referer"] = "http://booking.uz.gov.ua/";

        webClient.Headers[HttpRequestHeader.Cookie] = @"_gv_sessid=lmo8rrco2sn2ned2dh8v9r0564; HTTPSERVERID=server2; _gv_lang=uk; __utma=31515437.524370422.1428527902.1428527902.1428527902.1; __utmb=31515437.2.10.1428527902; __utmc=31515437; __utmz=31515437.1428527902.1.1.utmcsr=(direct)|utmccn=(direct)|utmcmd=(none)";

        Console.WriteLine("...sending" + ": " + "From: " + from + "To: " + to + " Date: " + date);
        var data = "station_id_from=" + from + "&station_id_till=" + to + "&date_dep=" + date + "&time_dep=00%3A00&search=";
        dynamic result = JsonValue.Parse(webClient.UploadString(@"http://booking.uz.gov.ua/purchase/search/", data));

        if (result.error)
        {
          Console.WriteLine(DateTime.Now + ": " + result.value);

          if (beep)
          {
            if (!snapshot.ContainsKey(from + to + date))
            {
              snapshot.Add(from + to + date, string.Empty);
              Console.WriteLine(DateTime.Now + ": " + "added to cache for further checking");
            }
          }
        }
        else
        {
          Console.WriteLine(DateTime.Now + ": " + "found!");

          var summary = "From: " + from + "To: " + to + " Date: " + date + "\r\n";
          foreach (var v in result.value)
          {
            string types = "[";
            foreach (var t in v.types)
            {
              types += t.letter.ToString() + "=" + t.places.ToString() + ",";
            }
            types += "]";

            var outdata = "Num=" + v.num + ", Types: " + types;

            summary += outdata + "\r\n";

            Console.WriteLine(DateTime.Now + ": " + outdata);
          }

          if (beep)
          {
            if (snapshot.ContainsKey(from + to + date))
            {
              Console.WriteLine(DateTime.Now + ": " + "checking for beep...");

              var old = snapshot[from + to + date];
              if (!string.Equals(old, summary, StringComparison.InvariantCultureIgnoreCase))
              {
                snapshot[from + to + date] = summary;
                sendMail(date + ":" + from + " -> " + to, summary);

                Console.WriteLine(DateTime.Now + ": " + "mail sent");
              }
            }
            else
            {
              snapshot.Add(from+to+date, summary);
              Console.WriteLine(DateTime.Now + ": " + "added to cache for further checking");
            }
          }
        }
      }
      catch (Exception e)
      {
        Console.WriteLine(e.Message + "\r\n" + e.StackTrace);
      }
    }

    static void Main(string[] args)
    {
      Random rnd = new Random(DateTime.Now.Millisecond);

      Console.WriteLine("Start...");
      snapshot = new Dictionary<string, string>();

      DateTime lastCheck = DateTime.Now.AddMinutes(-1);

      while (true)
      {
        if (lastCheck.AddMinutes(1) < DateTime.Now)
        {
          MakeRequest("30.04.2015", "2210700", "2218000", true);
          Thread.Sleep(2000);
          MakeRequest("01.05.2015", "2210700", "2218000", true);
          // Thread.Sleep(1500);
          // MakeRequest("05.05.2013", "2218000", "2210700", true);
          Console.WriteLine("-----------");
          lastCheck = DateTime.Now;
        }
        Thread.Sleep(5000);
      }

      Console.ReadKey();
    }
  }
}
