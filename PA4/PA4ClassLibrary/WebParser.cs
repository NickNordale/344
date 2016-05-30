using HtmlAgilityPack;
using Microsoft.WindowsAzure.Storage.Queue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace PA4ClassLibrary
{
    public class WebParser
    {
        public static HashSet<string> crawledUrls = new HashSet<string>();
        public static HashSet<string> crawledXmls = new HashSet<string>();

        public static HashSet<string> disallowed;

        public WebParser() { }

        public void crawlXml(string xmlPassed)
        {
            if (!crawledXmls.Contains(xmlPassed))
            {
                crawledXmls.Add(xmlPassed);
                XNamespace ns = xmlPassed;
                XmlDocument xDoc = new XmlDocument();
                xDoc.Load(xmlPassed);

                foreach (XmlNode node in xDoc.DocumentElement.ChildNodes)
                {
                    if (node["loc"].InnerText.EndsWith(".xml"))
                    {
                        if (node["lastmod"] != null)
                        {
                            string nodeLastMod = node["lastmod"].InnerText;
                            int year = Int32.Parse(nodeLastMod.Substring(0, 4));
                            int month = Int32.Parse(nodeLastMod.Substring(5, 2));
                            int day = Int32.Parse(nodeLastMod.Substring(8, 2));
                            DateTime dt = new DateTime(year, month, day);
                            if ((DateTime.Compare(dt, new DateTime(2016, 4, 1))) > 0)
                            {
                                crawlXml(node["loc"].InnerText);
                            }
                        }
                        else
                        {
                            crawlXml(node["loc"].InnerText);
                        }
                    }
                    else
                    {
                        CloudQueueMessage newLinkMsg = new CloudQueueMessage(node["loc"].InnerText);
                        Storage.UrlQueue.AddMessage(newLinkMsg);
                    }
                }
            }
        }

        public string[] crawlHTML(string crawlPassed)
        {
            if (!crawledUrls.Contains(crawlPassed))
            {
                crawledUrls.Add(crawlPassed);

                try
                {
                    HtmlWeb web = new HtmlWeb();
                    HtmlDocument doc = web.Load(crawlPassed);

                    if (doc.DocumentNode.SelectNodes("//a[@href]") != null)
                    {
                        foreach (HtmlNode link in doc.DocumentNode.SelectNodes("//a[@href]"))
                        {
                            string absUrl = GetAbsoluteUrlString(crawlPassed, link.Attributes["href"].Value);
                            if (checkLink(absUrl))
                            {
                                CloudQueueMessage newMessage = new CloudQueueMessage(absUrl);
                                Storage.UrlQueue.AddMessage(newMessage);
                            }
                        }
                    }

                    var title = doc.DocumentNode.SelectSingleNode("//title[1]");
                    string titleString = "";
                    if (title != null)
                    {
                        titleString = title.InnerText;
                    }
                    else
                    {
                        throw new System.ArgumentException("Couldn't get page title.", "crawlPassed");
                    }

                    string timeString = "";

                    if (crawlPassed.Contains("cnn.com") && doc.DocumentNode.SelectSingleNode("//meta[@property='og:pubdate'][1]") != null &&
                        doc.DocumentNode.SelectSingleNode("//meta[@property='og:pubdate'][1]").Attributes["content"] != null)
                    {
                        timeString = doc.DocumentNode.SelectSingleNode("//meta[@property='og:pubdate'][1]").Attributes["content"].Value;
                    }
                    else if (crawlPassed.Contains("bleacherreport.com") && doc.DocumentNode.SelectSingleNode("//meta[@name='pubdate'][1]") != null &&
                        doc.DocumentNode.SelectSingleNode("//meta[@name='pubdate'][1]").Attributes["content"] != null)
                    {
                        timeString = doc.DocumentNode.SelectSingleNode("//meta[@name='pubdate'][1]").Attributes["content"].Value;
                    }

                    if (timeString != null && timeString != "")
                    {
                        DateTime pageDT = Convert.ToDateTime(timeString);
                        timeString = pageDT.ToString();
                    }
                    else
                    {
                        timeString = DateTime.Now.ToString();
                    }
                    return new string[] { crawlPassed, titleString, timeString };
                }
                catch
                {
                    // If error occurs, crawlHTML will return null and a the error will
                    //   be handled in WorkerRole.cs
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        public Boolean checkLink(string checkUrl)
        {
            // check http and contains either cnn or br domain
            if (!checkUrl.StartsWith("http://") || !(checkUrl.Contains("cnn.com") || checkUrl.Contains("bleacherreport.com/articles/")))
            {
                return false;
            }

            // check if disallowed
            string relativePath = checkUrl.Substring(checkUrl.IndexOf(".com") + 4);
            string startDir = "";
            if (relativePath.Substring(1).Contains("/"))
            {
                startDir = relativePath.Substring(0, (relativePath.Substring(1).IndexOf("/") + 1));
            }
            else
            {
                startDir = relativePath;
            }

            if (disallowed.Contains(startDir))
            {
                return false;
            }

            // check if already visited
            if (crawledUrls.Contains(checkUrl))
            {
                return false;
            }

            // passed all tests
            return true;
        }

        static string GetAbsoluteUrlString(string baseUrl, string url)
        {
            var uri = new Uri(url, UriKind.RelativeOrAbsolute);
            if (!uri.IsAbsoluteUri)
                uri = new Uri(new Uri(baseUrl), uri);
            return uri.ToString();
        }
    }
}
