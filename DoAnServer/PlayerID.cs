using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DoAnServer
{
    public class PlayerID
    {
       // private const int _cnstQuestion = 20;
        public int _ID;
        public string _Name = string.Empty;
      
        private Stack<Answer> a_Stack = new Stack<Answer>();
        private Stack<Question> q_Stack = new Stack<Question>();
        private int _iPoints = 0;
        
        private int iRight = 0;
        private int iAnswred = 0;
        public PlayerID(){}
        public PlayerID(string playerName, int id)
        {
            _Name = playerName;
            _ID = id;
        }
//         public int TotalQuestions
//         {
//             get
//             {
//                 return _cnstQuestion;
//             }
//         }
        public int RightAnswers
        {
            get
            {
                return iRight;
            }
            set
            {
                iRight = value;
            }
        }
        public int AnsweredQuestions
        {
            get
            {
                return iAnswred;
            }
            set
            {
                iAnswred = value;
            }
        }
        public int POINTS
        {
            get
            {
                return _iPoints;
            }
            set
            {
                _iPoints = value;
            }
        }
        public bool CheckRightAns(Answer ans)
        {
                a_Stack.Push(ans);
                Question anwsred_question = q_Stack.Pop();

                if (anwsred_question.IsRight(ans))
                {
                    this.iRight++;
                    return true;
                }
                else return false;
        }
        public void Push(Question q)
        {
            q_Stack.Push(q);
        }
       
        


        
    }
}
