using System;
using System.Collections.Generic;
using System.Threading;

namespace ELOsimulation
{
    class Program
    {
        static void Main(string[] args)
        {
            //可以开始了
        }
    }

    enum BattleResult
    {
        WIN,
        DRAW,
        LOSE
    }

    class Player
    {
        const int K = 60;
        const float S_WIN = 1f;
        const float S_DRAW = 0.5f;
        const float S_LOSE = 0f;
        const int BASELINE = 0;
        static int PlayerCount;
        static int highestRating;

        public static int HighestRating { get { return highestRating; } private set { highestRating = value; } }

        int rating;
        int pID;

        Thread tSearchGame;

        public Player(int rating)
        {
            AddPlayer();
            Rating = rating;
        }

        public Player()
        {
            AddPlayer();
        }

        void AddPlayer()
        {
            pID = ++PlayerCount; ;
        }

        public int Rating 
        { 
            get { return rating; } 
            private set 
            { 
                rating = value < 0 ? 0 : value;
                if (HighestRating < rating)
                {
                    HighestRating = rating;
                }
            } 
        }

        public int PID { get { return pID; } }
        public Thread TSearchGame { get { return tSearchGame; } set { tSearchGame = value; } }

        public int CalcRating(BattleResult br, int opponentRating)
        {
            Rating += CalcDelta(br, opponentRating);
            return Rating;
        }

        public int CalcRating(BattleResult br, Player p)
        {
            int delta = CalcDelta(br, p.Rating);
            Rating += delta;
            p.Rating -= delta;
            return Rating;
        }

        public int CalcDeltaRating(BattleResult br, int opponentRating)
        {
            int delta = CalcDelta(br, opponentRating);
            Rating += delta;
            return delta;
        }

        public int CalcDeltaRating(BattleResult br, Player p)
        {
            int delta = CalcDelta(br, p.Rating);
            Rating += delta;
            p.Rating -= delta;
            return delta;
        }

        int CalcDelta(BattleResult br, int opponentRating)
        {
            return (int)(K * (CalcScore(br) - CalcExpectation(opponentRating)));
        }

        public float CalcExpectation(int opponentRating)
        {
            return 1 / (1 + (float)Math.Pow(10, (double)((opponentRating - rating) / 400.0f)));
        }

        float CalcScore(BattleResult br)
        {
            switch (br)
            {
                case BattleResult.WIN:
                    return S_WIN;
                case BattleResult.DRAW:
                    return S_DRAW;
                case BattleResult.LOSE:
                default:
                    return S_LOSE;
            }
        }
    }

    class Battle
    {
        const int BATTLELIMIT = 1000;
        static int BattleCount;
        static Random RAN = new Random();

        public static bool DoMoreBattle { get { return BattleCount < BATTLELIMIT; } }

        Player p1;
        Player p2;

        public Battle(Player p1, Player p2)
        {
            this.p1 = p1;
            this.p2 = p2;
        }

        public BattleResult DoBattle()
        {
            BattleCount++;
            float e1 = p1.CalcExpectation(p2.Rating);
            if ((float)RAN.NextDouble() <= e1)
            {
                p1.CalcRating(BattleResult.WIN, p2);
                return BattleResult.WIN;
            }
            else
            {
                p1.CalcRating(BattleResult.LOSE, p2);
                return BattleResult.LOSE;
            }
        }
    }

    class Game
    {
        const int LOCKINTERVAL = 10;
        const int WAITINTERVAL = 1000;
        static readonly int[] SEARCHRANGE = new int[10]{ 30, 60, 90, 120, 150, 180, 210, 240, 270, 300 };
        static int[] RatingAxis = new int[100000];
        static bool TreeLock;
        static bool GameEnd;
        static int ThreadCount;
        static Dictionary<int, Player> playerDic = new Dictionary<int, Player>();
        static SegmentTree threadTree= new SegmentTree();

        void AbortAllThread()
        {
            foreach (var v in playerDic)
            {
                v.Value.TSearchGame.Abort();
            }
            Console.WriteLine("-----------------------------Game End!---------------------------------");
            Console.WriteLine(Player.HighestRating);
        }

        public void SearchGame(Player p)
        {
            p.TSearchGame = new Thread(new ParameterizedThreadStart(SearchGameThread));
            p.TSearchGame.IsBackground = true;
            playerDic.Add(p.PID, p);
            p.TSearchGame.Start(p);
            ThreadCount++;
        }

        void SearchGameThread(object obj)
        {
            Player p = (Player)obj;
            int range, lowerBound, upperBound;
            while (!GameEnd)
            {
                for (int i = 0; !GameEnd && i < SEARCHRANGE.Length; i++)
                {
                    range = SEARCHRANGE[i];
                    lowerBound = p.Rating - range < 0 ? 0 : p.Rating - range;
                    upperBound = p.Rating + range;
                    SegmentNode node = new SegmentNode(lowerBound, upperBound, p);

                    GetLock();

                    SegmentNode anotherNode = threadTree.AddNode(node);
                    if (anotherNode == null)
                    {
                        Unlock();
                        WaitForOpponent();
                    }
                    else
                    {
                        Player p2 = (Player)anotherNode.Value;
                        playerDic.Remove(p2.PID);
                        p2.TSearchGame.Abort();
                        p2.TSearchGame.Join();

                        if (!Battle.DoMoreBattle)
                        {
                            GameEnd = true;
                            AbortAllThread();
                            p.TSearchGame.Join();
                            break;
                        }

                        playerDic.Remove(p.PID);
                        Battle battle = new Battle(p, p2);
                        battle.DoBattle();
                        p.TSearchGame.Abort();
                        p.TSearchGame.Join();
                    }
                }
            }
        }

        void GetLock()
        {
            while (TreeLock)
            {
                Thread.Sleep(LOCKINTERVAL);
            }
            TreeLock = true;
        }

        void Unlock()
        {
            TreeLock = false;
        }

        void WaitForOpponent()
        {
            Thread.Sleep(WAITINTERVAL);
        }

    }

    class SegmentTree
    {
        SegmentNode root = null;
        SegmentNode Root { get { return root; } set { root = value; } }

        int count;
        public int Count { get { return count; } private set { count = value; } }

        public void ShowTree()
        {
            Queue<SegmentNode> queue = new Queue<SegmentNode>();
            SegmentNode temp;
            queue.Enqueue(Root);
            while (queue.Count > 0)
            {
                int i = 0, qCount = queue.Count;
                while (i++ < qCount)
                {
                    temp = queue.Dequeue();
                    if (temp.LeftNode != null)
                    {
                        queue.Enqueue(temp.LeftNode);
                    }
                    if (temp.RightNode != null)
                    {
                        queue.Enqueue(temp.RightNode);
                    }
                    Console.Write("[" + temp.LowerBound + "," + temp.UpperBound + "]\t");
                }
                Console.Write("\n\r");
            }
        }

        public void BuildTree(params SegmentNode[] args)
        {
            foreach (SegmentNode node in args)
            {
                AddNode(node);
            }
        }

        public SegmentNode AddNode(SegmentNode arg)
        {
            if (Root == null)
            {
                Root = arg;
                Count++;
                return null;
            }

            SegmentNode currentNode = Root, parentNode = null;
            while (true)
            {
                if (arg.UpperBound < currentNode.LowerBound)
                {
                    parentNode = currentNode;
                    currentNode = currentNode.LeftNode;
                    if (currentNode == null)
                    {
                        parentNode.LeftNode = arg;
                        break;
                    }
                }
                else if (arg.LowerBound > currentNode.UpperBound)
                {
                    parentNode = currentNode;
                    currentNode = currentNode.RightNode;
                    if (currentNode == null)
                    {
                        parentNode.RightNode = arg;
                        break;
                    }
                }
                else
                {
                    SegmentNode Result = currentNode;
                    RemoveNode(currentNode, parentNode);
                    return currentNode;
                }
            }
            Count++;
            return null;
        }

        public bool RemoveNode(SegmentNode arg)
        {
            if (arg == null)
            {
                return true;
            }
            if (Root == null)
            {
                return false;
            }

            SegmentNode currentNode = Root, parentNode = null;
            while (currentNode != null)
            {
                if (arg.UpperBound < currentNode.LowerBound)
                {
                    parentNode = currentNode;
                    currentNode = currentNode.LeftNode;
                }
                else if (arg.LowerBound > currentNode.UpperBound)
                {
                    parentNode = currentNode;
                    currentNode = currentNode.RightNode;
                }
                else
                {
                    if (arg == currentNode)
                    {
                        RemoveNode(currentNode, parentNode);
                        return true;
                    }
                    return false;
                }
            }
            return false;
        }

        void RemoveNode(SegmentNode arg, SegmentNode parent)
        {
            Count--;
            if (arg == Root)
            {
                if (arg.LeftNode != null)
                {
                    Root = arg.LeftNode;
                }
                else
                {
                    Root = arg.RightNode;
                    return;
                }
            }
            if (parent != null)
            {
                parent.LeftNode = arg.LeftNode;
            }
            if (arg.RightNode == null)
            {
                arg.LeftNode = null;
                return;
            }

            SegmentNode currentNode = arg.LeftNode, parentNode = parent;
            while (currentNode != null)
            {
                parentNode = currentNode;
                currentNode = currentNode.RightNode;
            }
            parentNode.RightNode = arg.RightNode;
            arg.LeftNode = arg.RightNode = null;
        }

    }

    class SegmentNode
    {
        static int snID;
        public static int SNID { get { return snID; } private set { snID = value; } }

        int lowerBound;
        int upperBound;
        SegmentNode leftNode = null;
        SegmentNode rightNode = null;
        Object value = null;

        public int LowerBound { get { return lowerBound; } private set { lowerBound = value; } }
        public int UpperBound { get { return upperBound; } private set { upperBound = value; } }

        public SegmentNode LeftNode { get { return leftNode; } set { leftNode = value; } }
        public SegmentNode RightNode { get { return rightNode; } set { rightNode = value; } }

        public Object Value { get { return value; } }

        public SegmentNode()
        {
            SNID++;
        }

        public SegmentNode(int lower, int upper, Object obj)
        {
            LowerBound = lower;
            UpperBound = upper;
            value = obj;
            SNID++;
        }
    }

}
