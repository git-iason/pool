using System;
using System.Xml.Linq;

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
            set
            {
                associatedGame?.UpdatePicks(value);
                confidencePick = Convert.ToInt32(value);
            }
        }

        public string id
        {
            get { return associatedGame.id + "." + name; }
        }
    }
    public class Game
    {
        private List<Pick> _possibleWinners = new List<Pick>();
        public IReadOnlyCollection<Pick> possibleWinners => _possibleWinners.AsReadOnly();
        public Round? associatedRound { get; set; } = null;
        public void AddPosssible(Pick pick)
        {
            pick.associatedGame = this;
            _possibleWinners.Add(pick);
        }
        public void UpdatePicks(string conf)
        {
            if (conf == "0") return;
            foreach (var p in possibleWinners)
                if (p.confidencePickStr == conf)
                    p.confidencePickStr = "0";
        }

        public override string ToString()
        {
            var res = "";
            foreach (var p in possibleWinners)
            {
                res += $"{p.name}\n";
            }
            return res;
        }

        public string id
        {
            get { return associatedRound.id + "." + possibleWinners.Aggregate<Pick, string>("", (r, x) => r += $".{x.name}"); }
        }
    }
    public class Round
    {
        public string name { get; set; } = "";
        private List<Game> _games = new List<Game>();
        public IReadOnlyCollection<Game> games => _games.AsReadOnly();
        public Pool? associatedPool { get; set; } = null;
        public void AddGame(Game game)
        {
            game.associatedRound = this;
            _games.Add(game);
        }

        public override string ToString()
        {
            var res = "";
            foreach (var g in games)
            {
                res += g.ToString();
            }
            return res;
        }

        public string id
        {
            get
            {
                return associatedPool.name + "." + name;
            }
        }

    }
    public class Pool
    {
        public string name { get; set; } = "";
        private List<Round> _rounds = new List<Round>();
        public IReadOnlyCollection<Round> rounds => _rounds.AsReadOnly();

        public void AddRound(Round round)
        {
            round.associatedPool = this;
            _rounds.Add(round);
        }

        public override string ToString()
        {
            var res = "";
            foreach (var r in rounds)
            {
                res += r.ToString();
            }
            return res;
        }
    }
}