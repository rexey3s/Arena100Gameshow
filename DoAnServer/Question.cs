using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace DoAnServer
{
    public enum Answer {
        A, B, C, D,
        NONE 
    }
    public enum Level {
        Easy, Hard ,
        None
    }
    public enum Category
    {
        Science = 0, Sport = 1, Geo_His = 2, Culture = 3, Art = 4, None = -1 
    }
    
    public class Question
    {
        public int ID;
        public int Point = 1000;
        public Answer right_ans = Answer.NONE;

        public Category cat = Category.None;

        public Level level = Level.None;
        
        private StringBuilder sb = new StringBuilder();

        public int catid;
        public Question(){}
        public Question(XmlNode content_node)
        {
            XmlNodeList node = content_node.ChildNodes;
            if (content_node.ParentNode.InnerText.Contains("Easy"))
            {
                sb.AppendLine("Easy");
                level = (Level)Enum.Parse(typeof(Level), "Easy");
            }
            if (content_node.ParentNode.InnerText.Contains("Hard"))
            {
                sb.AppendLine("Hard");
                level = (Level)Enum.Parse(typeof(Level),"Hard");
            }
            string catname=content_node.ParentNode.ParentNode.FirstChild.InnerText.Substring(0, content_node.ParentNode.ParentNode.FirstChild.InnerText.LastIndexOf("\r"));
            sb.AppendLine(catname);
           
            switch (catname)
            {
                case "Science":{catid=0;break;}
                case "Sport":{catid=1;break;}
                case "Geo_His":{catid=2;break;}
                case "Culture":{catid=3;break;}
                case "Art":{catid=4;break;}
            }
            cat = (Category)Enum.Parse(typeof(Category), catid.ToString());
                for (int i = 0; i < node.Count; i++)
                {
                   
                    if (node[i].Name == "id")
                    {
                        ID = Int32.Parse(node[i].InnerText);
                    }
                    if (node[i].Name == "question")
                    {
                        sb.AppendLine(node[i].InnerText);
                    }
                    if (node[i].Name == "answer")
                    {
                        foreach (XmlNode sub_node in node[i])
                        {
                            sb.AppendLine(sub_node.InnerText);
                        }
                    }
                    if (node[i].Name == "result")
                    {
                        right_ans = (Answer)Enum.Parse(typeof(Answer), node[i].InnerText.ToUpper());
                    }
                    
                }
           
        }
        public bool IsRight(Answer ans)
        {
            return ans == right_ans ? true : false;
        }

        public StringBuilder strContent
        {
            get
            {
                return sb;
            }
            set
            {
                sb = value;
            }
        }
        
    }
}
