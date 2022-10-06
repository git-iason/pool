using System;
using System.Xml.Linq;
using hosted_pool.Pages;

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
            get { return associatedGame.id + name; }
        }

        public int num { get; set; } = 0;
    }
    public class Game
    {
        private List<Pick> _possibleWinners = new List<Pick>();
        public IReadOnlyCollection<Pick> possibleWinners => _possibleWinners.AsReadOnly();
        public Round? associatedRound { get; set; } = null;
        public void AddPosssible(Pick pick)
        {
            pick.associatedGame = this;
            pick.num = _possibleWinners.Count() + 1;
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
            get { return associatedRound.id + "." + possibleWinners.Aggregate<Pick, string>("", (r, x) => r += $"{x.name}."); }
        }

        public int num { get; set; } = 0;
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
            game.num = _games.Count() + 1;
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
                return associatedPool.poolName + "." + name;
            }
        }

        public int num { get; set; } = 0;

    }
    public class TieBreaker
    {
        public string name { get; set; } = "";
        public string question { get; set; } = "";
        private string _answer = "";
        public string answer
        {
            get { return _answer; }
            set { _answer = value; }
        }
    }
    public class Pool
    {
        public string poolName { get; set; } = "";
        public string welcomeStr { get; set; } = "";
        public string pickSet { get; set; } = "";

        private List<Round> _rounds = new List<Round>();
        public IReadOnlyCollection<Round> rounds => _rounds.AsReadOnly();

        private List<TieBreaker> _tiebreakers = new List<TieBreaker>();
        public IReadOnlyCollection<TieBreaker> tiebreakers => _tiebreakers.AsReadOnly();
        public void AddRound(Round round)
        {
            round.associatedPool = this;
            round.num = _rounds.Count + 1;
            _rounds.Add(round);
        }

        public void AddTiebreaker(string name, string question)
        {
            _tiebreakers.Add(new TieBreaker { name=name, question = question});
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

        public void LoadPicks(Dictionary<string,string> picks)
        {
            foreach(var round in rounds)
            {
                foreach(var game in round.games)
                {
                    foreach(var possible in game.possibleWinners)
                    {
                        string val = "0";
                        var success = picks.TryGetValue(possible.id, out val);
                        possible.confidencePickStr = val;
                    }
                }
            }
            foreach(var t in tiebreakers)
            {
                string val = "";
                var success = picks.TryGetValue(t.name, out val);
                t.answer = val;
            }
        }

        public bool IsComplete()
        {
            foreach (var round in rounds)
            {
                foreach (var game in round.games)
                {
                    foreach (var possible in game.possibleWinners)
                    {
                        if (possible.confidencePick == 0) return false;
                    }
                }
            }
            foreach (var t in tiebreakers)
            {
                if (t.answer == "") return false;
            }

            return true;
        }
    }

   
}