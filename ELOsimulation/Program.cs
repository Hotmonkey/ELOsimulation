using System;
using System.Collections.Generic;
using System.Threading;

namespace ELOsimulation
{
    class Program
    {
        static void Main(string[] args)
        {
            Player p1 = new Player(1900);
            Player p2 = new Player(1900);
            Battle ba = new Battle(p1, p2);
            for (int i = 0; i < 10; i++)
            {
                Console.WriteLine(ba.DoBattle().ToString());
            }
            Console.WriteLine(p1.Rating);
            Console.WriteLine(p2.Rating);
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

        static bool DoMoreBattle { get { return BattleCount < BATTLELIMIT; } }

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
        const int AXISBOUND = 99999;
        static readonly int[] SEARCHRANGE = new int[10]{ 30, 60, 90, 120, 150, 180, 210, 240, 270, 300 };
        static int[] RatingAxis = new int[100000];
        static bool AxisLock;
        static bool GameEnd;
        static int ThreadCount;
        static Dictionary<int, Player> playerDic = new Dictionary<int, Player>();

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
                    for (int j = lowerBound; j <= upperBound; j++)
                    {
                        if (j > AXISBOUND)
                        {
                            GameEnd = true;
                            AbortAllThread();
                            p.TSearchGame.Join();
                            break;
                        }

                        while (AxisLock)
                        {
                            Thread.Sleep(10);
                        }

                        AxisLock = true;
                        if (RatingAxis[j] != 0)
                        {
                            //清理现场
                            //等待停止线程后比赛
                            //停止线程
                        }
                        else
                        {
                            RatingAxis[j] = p.PID;
                        }
                    }
                }
            }
        }

        
    }

}
