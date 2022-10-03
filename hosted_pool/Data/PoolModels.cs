using System;
namespace hosted_pool.Data
{
    public class Pick
    {
        public string name { get; set; } = "";
        public int confidencePick { get; set; }
        public Game? associatedGame { get; set; } = null;
        public string confidencePickStr
        {
            get { return confidencePick.ToString(); }
            set {
                associatedGame?.UpdatePicks(value);
                confidencePick = Convert.ToInt32(value);
            }
        }
    }
    public class Game
    {
        public List<Pick> possibleWinners { get; set; } = new List<Pick>();

        public void AddWinner(Pick pick)
        {
            pick.associatedGame = this;
            possibleWinners.Add(pick);
        }
        public void UpdatePicks(string conf)
        {
            if (conf == "0") return;
            foreach (var p in possibleWinners)
                if (p.confidencePickStr == conf)
                    p.confidencePickStr = "0";
        }
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
    }

}

