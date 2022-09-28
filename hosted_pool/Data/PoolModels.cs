using System;
namespace hosted_pool.Data
{
    public class Pick
    {
        public string name { get; set; } = "";
        public int confidencePick { get; set; }
        public string confidencePickStr
        {
            get { return confidencePick.ToString(); }
            set {
                confidencePick = Convert.ToInt32(value);
            }
        }
    }
    public class Game
    {
        public List<Pick> possibleWinners { get; set; } = new List<Pick>();

    }
    public class Round
    {
        public string name { get; set; } = "";
        public List<Game> games { get; set; } = new List<Game>();
    }
    public class Pool
    {
        public string name { get; set; } = "";
        public List<Round> rounds { get; set; } = new List<Round>();

        public void SetPick(int round, int game, int pick, int conf)
        {
            rounds[round].games[game].possibleWinners[pick].confidencePick = conf;
        }
    }

}

