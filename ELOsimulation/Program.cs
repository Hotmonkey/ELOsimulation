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
        int battleCount;

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
            pID = PlayerCount++;
        }

        public int Rating
        {
            get { return rating; }
            private set
            {
                rating = value < 0 ? 0 : value;
                BattleCount++;
                if (HighestRating < rating)
                {
                    HighestRating = rating;
                }
            }
        }

        public int BattleCount { get { return battleCount; } private set { battleCount = value; } }

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
        const int BATTLELIMIT = 5000;
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
        const int PLAYERCOUNT = 50;
        const int LOCKINTERVAL = 10;
        const int WAITINTERVAL = 1000;
        static readonly int[] SEARCHRANGE = new int[10] { 30, 60, 90, 120, 150, 180, 210, 240, 270, 300 };
        static bool TreeLock;
        static bool GameEnd;
        static Player[] AllPlayers = new Player[PLAYERCOUNT];
        static Queue<Player> players = new Queue<Player>();
        static bool[] playerState = new bool[PLAYERCOUNT];
        static SegmentTree threadTree = new SegmentTree();
        static readonly Random random = new Random();
        static int LockID = -1;

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
                while (!GameEnd && players.Count > 0)
                {
                    //Console.WriteLine("----------------------------------------Going to dequeue:::" + players.Peek().PID);
                    tempPlayer = players.Dequeue();
                    if (tempPlayer == null)
                    {
                        continue;
                    }
                    if ((tempPlayer.TSearchGame != null && tempPlayer.TSearchGame.IsAlive) || playerState[tempPlayer.PID])
                    {
                        //Console.WriteLine("hehe--------------------" + tempPlayer.PID);
                        tempPlayer.TSearchGame.Abort();
                        playerState[tempPlayer.PID] = false;
                        players.Enqueue(tempPlayer);
                    }
                    else
                    {
                        SearchGame(tempPlayer);
                    }
                }
                if (tempPlayer == null)
                {
                    if (players.Count > 0)
                    {
                        Console.WriteLine("QQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQ:::");
                        Console.WriteLine(players.Peek() == null);
                    }
                    Console.WriteLine("EEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEE:::");
                    GameEnd = true;
                }
            }

            AbortAllThread();
        }

        void AbortAllThread()
        {
            int aliveCount = 0;
            for (int i = 0; i < PLAYERCOUNT; i++)
            {
                if (playerState[i])
                {
                    aliveCount++;
                    AllPlayers[i].TSearchGame.Abort();
                    //AllPlayers[i].TSearchGame.Join();
                }
            }
            Console.WriteLine("-----------------------------Game End!---------------------------------");
            Console.WriteLine("Battle Count ---------------------------- " + Battle.BATTLECOUNT);
            Console.WriteLine("Alive Count ---------------------------- " + aliveCount);
            for (int i = 0; i < PLAYERCOUNT / 5; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    if (AllPlayers[i * 5 + j] == null)
                    {
                        Console.Write("E:" + (i * 5 + j));
                        continue;
                    }
                    Console.Write(AllPlayers[i * 5 + j].Rating + ":" + AllPlayers[i * 5 + j].BattleCount + "\t");
                }
                Console.Write("\n\r");
            }
            Console.WriteLine("High:::" + Player.HighestRating);
        }

        void SearchGame(Player p)
        {
            if (GameEnd)
            {
                return;
            }

            Console.WriteLine("Search Game ------------------- Player " + p.PID);
            p.TSearchGame = null;
            p.TSearchGame = new Thread(new ParameterizedThreadStart(SearchGameThread));
            p.TSearchGame.IsBackground = true;
            //if (p.TSearchGame.IsAlive || playerState[p.PID])
            //{
            //    p.TSearchGame.Abort();
            //    playerState[p.PID] = false;
            //    players.Enqueue(p);
            //    return;
            //}
            playerState[p.PID] = true;
            p.TSearchGame.Start(p);
        }

        void SearchGameThread(object obj)
        {
            WaitRandomTime();
            Player p = (Player)obj;

            if (GameEnd)
            {
                playerState[p.PID] = false;
                return;
            }

            Console.WriteLine("Start Thread ---------------------- PID ::: " + p.PID);
            int range, lowerBound, upperBound;
            SegmentNode node = new SegmentNode();
            for (int i = 0; !GameEnd && i < SEARCHRANGE.Length; i++)
            {
                if (GameEnd)
                {
                    playerState[p.PID] = false;
                    return;
                }
                Console.WriteLine("Search Range ---------------------- " + SEARCHRANGE[i]);
                range = SEARCHRANGE[i];
                lowerBound = p.Rating - range < 0 ? 0 : p.Rating - range;
                upperBound = p.Rating + range;
                node = new SegmentNode(lowerBound, upperBound, p);

                GetLock(p.PID);

                SegmentNode anotherNode = threadTree.AddNode(node);

                Unlock();

                if (anotherNode == null)
                {
                    WaitForOpponent();

                    GetLock(p.PID);

                    threadTree.RemoveNode(node);

                    Unlock();
                }
                else
                {
                    Player p2 = (Player)anotherNode.Value;
                    if (p2.TSearchGame.IsAlive)
                    {
                        if (LockID == p2.PID)
                        {
                            Unlock();
                        }
                        p2.TSearchGame.Abort();
                    }
                    playerState[p2.PID] = false;

                    if (!Battle.DoMoreBattle)
                    {
                        GameEnd = true;
                        playerState[p.PID] = false;
                        //p.TSearchGame.Abort();
                        return;
                    }

                    playerState[p.PID] = false;
                    Battle battle = new Battle(p, p2);
                    battle.DoBattle();
                    players.Enqueue(p);
                    players.Enqueue(p2);
                    //p.TSearchGame.Abort();
                    return;
                }
            }
            playerState[p.PID] = false;
            players.Enqueue(p);
        }

        void GetLock(int pid)
        {
            while (TreeLock)
            {
                Console.WriteLine("haha");
                Thread.Sleep(LOCKINTERVAL);
            }
            TreeLock = true;
            LockID = pid;
        }

        void Unlock()
        {
            LockID = -1;
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
