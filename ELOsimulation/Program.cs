using System;
using System.Collections.Generic;

namespace ELOsimulation
{
    class Program
    {
        static void Main(string[] args)
        {
            Player p = new Player(1595);
            p.CalcRating(BattleResult.LOSE, 1595);
            Console.WriteLine(p.Rating);
        }
    }

    class Player
    {
        const int K = 64;
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
            Rating += (int)(K * (CalcScore(br) - CalcExpectation(opponentRating)));
            return Rating;
        }

        float CalcExpectation(int opponentRating)
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

    enum BattleResult
    {
        WIN,
        DRAW,
        LOSE
    }

}
