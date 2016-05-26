using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;

namespace WebRole1
{
    public class Trie
    {
        public Node root;
        public List<string> outT;

        // CPU
        private PerformanceCounter theCPUCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        //this.theCPUCounter.NextValue();

        // RAM
        private PerformanceCounter theMemCounter = new PerformanceCounter("Memory", "Available MBytes");
        //this.theMemCounter.NextValue();

        public Trie()
        {
            root = new Node();
        }

        public void addTitle(string titleIn)
        {
            Node curr = root;
            for (int i = 0; i < titleIn.Length; i++)
            {
                if (!curr.children.ContainsKey(titleIn[i]))
                {
                    curr.children.Add(titleIn[i], new Node(titleIn[i]));
                }
                curr.children.TryGetValue(titleIn[i], out curr);
            }
            curr.isLeaf = true;
        }

        public string searchForPrefix(string query)
        {
            Node curr = root;
            for (int i = 0; i < query.Length; i++)
            {
                if (curr.children.ContainsKey(query[i]))
                {
                    curr.children.TryGetValue(query[i], out curr);
                }
                else
                {
                    return "";
                }
            }

            outT = new List<string>();
            searchHelp(curr, query);

            JavaScriptSerializer ts = new JavaScriptSerializer();

            return ts.Serialize(outT);
        }

        private void searchHelp(Node curr, string currString)
        {
            if (outT.Count < 10)
            {
                if (curr.isLeaf)
                {
                    outT.Add(currString);
                }

                if (curr.children.Count > 0)
                {
                    foreach (KeyValuePair<char, Node> entry in curr.children)
                    {
                        searchHelp(entry.Value, currString + entry.Key);
                    }
                }
            }
        }
    }
}