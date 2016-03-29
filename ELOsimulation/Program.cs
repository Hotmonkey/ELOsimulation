using System;
using System.Collections.Generic;

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

        int rating;

        public Player(int rating)
        {
            Rating = rating;
        }

        public int Rating { get { return rating; } private set { rating = value < 0 ? 0 : value; } }

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
        static Random RAN = new Random();

        Player p1;
        Player p2;

        public Battle(Player p1, Player p2)
        {
            this.p1 = p1;
            this.p2 = p2;
        }

        public BattleResult DoBattle()
        {
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
        
    }

}
