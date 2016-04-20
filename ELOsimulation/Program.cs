using System;
using System.Collections.Generic;
using System.Threading;

namespace ELOsimulation
{
    class Program
    {
        static void Main(string[] args)
        {
            Game.GetInstance().StartGame();
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
            pID = ++PlayerCount;
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
        public static int BATTLECOUNT { get { return BattleCount; } }

        Player p1;
        Player p2;

        public Battle(Player p1, Player p2)
        {
            this.p1 = p1;
            this.p2 = p2;
        }

        public BattleResult DoBattle()
        {
            Console.WriteLine("Do Battle ----------------- " + p1.PID + " VS " + p2.PID);
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
        static Player[] AllPlayers = new Player[50];
        static Queue<Player> players = new Queue<Player>();
        static Dictionary<int, Player> playerDic = new Dictionary<int, Player>();
        static SegmentTree threadTree= new SegmentTree();
        static readonly Random random = new Random();

        static Game instance;

        public static Game GetInstance()
        {
            if (instance == null)
            {
                instance = new Game();
            }
            return instance;
        }

        public void StartGame()
        {
            for (int i = 0; i < 50; i++)
            {
                AllPlayers[i] = new Player();
                players.Enqueue(AllPlayers[i]);
            }

            Player tempPlayer = AllPlayers[0];
            while (!GameEnd)
            {
                while (players.Count > 0)
                {
                    tempPlayer = players.Dequeue();
                    if (tempPlayer == null)
                    {
                        continue;
                    }
                    if (tempPlayer.TSearchGame != null && tempPlayer.TSearchGame.IsAlive)
                    {
                        players.Enqueue(tempPlayer);
                    }
                    else
                    {
                        SearchGame(tempPlayer);
                    }
                }
                if (tempPlayer == null)
                {
                    Console.WriteLine("EEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEE:::");
                    GameEnd = true;
                }
            }

            AbortAllThread();
        }


        void AbortAllThread()
        {
            foreach (var v in playerDic)
            {
                if (v.Value.TSearchGame.IsAlive)
                {
                    ThreadCount--;
                    v.Value.TSearchGame.Abort();
                    v.Value.TSearchGame.Join();
                }
            }
            Console.WriteLine("-----------------------------Game End!---------------------------------");
            Console.WriteLine("Battle Count ---------------------------- " + Battle.BATTLECOUNT);
            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    if (AllPlayers[i * 5 + j] == null)
                    {
                        Console.Write("E:" + (i * 5 + j));
                        continue;
                    }
                    Console.Write(AllPlayers[i * 5 + j].Rating + "\t");
                }
                Console.Write("\n\r");
            }
            Console.WriteLine("High:::" + Player.HighestRating);
        }

        void SearchGame(Player p)
        {
            Console.WriteLine("Search Game ------------------- Player " + p.PID);
            p.TSearchGame = new Thread(new ParameterizedThreadStart(SearchGameThread));
            p.TSearchGame.IsBackground = true;
            playerDic.Add(p.PID, p);
            p.TSearchGame.Start(p);
            ThreadCount++;
        }

        void SearchGameThread(object obj)
        {
            WaitRandomTime();

            Player p = (Player)obj;
            Console.WriteLine("Start Thread ---------------------- PID ::: " + p.PID);
            int range, lowerBound, upperBound;
            SegmentNode node = new SegmentNode();
            for (int i = 0; !GameEnd && i < SEARCHRANGE.Length; i++)
            {
                if (GameEnd)
                {
                    break;
                }
                Console.WriteLine("Search Range ---------------------- " + SEARCHRANGE[i]);
                if (i > 0)
                {
                    threadTree.RemoveNode(node);
                }
                range = SEARCHRANGE[i];
                lowerBound = p.Rating - range < 0 ? 0 : p.Rating - range;
                upperBound = p.Rating + range;
                node = new SegmentNode(lowerBound, upperBound, p);

                GetLock();

                SegmentNode anotherNode = threadTree.AddNode(node);

                Unlock();

                if (anotherNode == null)
                {
                    WaitForOpponent();
                }
                else
                {
                    Player p2 = (Player)anotherNode.Value;
                    playerDic.Remove(p2.PID);
                    if (p2.TSearchGame.IsAlive)
                    {
                        ThreadCount--;
                        p2.TSearchGame.Abort();
                        p2.TSearchGame.Join();
                    }

                    if (!Battle.DoMoreBattle)
                    {
                        GameEnd = true;
                        p.TSearchGame.Join();
                        break;
                    }

                    playerDic.Remove(p.PID);
                    Battle battle = new Battle(p, p2);
                    battle.DoBattle();
                    players.Enqueue(p);
                    players.Enqueue(p2);
                    ThreadCount--;
                    p.TSearchGame.Abort();
                    p.TSearchGame.Join();
                }
            }
            ThreadCount--;
        }

        void GetLock()
        {
            while (TreeLock)
            {
                Console.WriteLine("haha");
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

        void WaitRandomTime()
        {
            Thread.Sleep((int)(random.NextDouble() * 2000) + 1000);
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
            if (Root == null)
            {
                Console.WriteLine("SegmentTree is empty!");
                return;
            }

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

        public void ShowNode()
        {
            Console.WriteLine("(" + LowerBound + ":::" + UpperBound + ")");
        }

        ~SegmentNode()
        {
            SNID--;
        }
    }

}
