using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PA2
{
    public class Node
    {
        public char c;
        public Dictionary<char, Node> children;
        public Boolean isLeaf;

        public Node()
        {
            isLeaf = false;
            children = new Dictionary<char, Node>();
        }       

        public Node(char cIn)
        {
            isLeaf = false;
            children = new Dictionary<char, Node>();
            this.c = cIn;
        }
    }
}