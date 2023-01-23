using System;
using System.Xml.Linq;
using hosted_pool.Pages;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
//using Newtonsoft.Json;

namespace hosted_pool.Data
{
    public class Pick
    {
        public string name { get; set; } = "";
        public int confidencePick { get; set; }
        [JsonIgnore]
        public Game? associatedGame { get; set; } = null;
        public string confidencePickStr
        {
            get { return confidencePick.ToString(); }
            set
            {
                Console.WriteLine(value);
                associatedGame?.UpdatePicks(value);
                confidencePick = Convert.ToInt32(value);
            }
        }

        public string id
        {
            get { return associatedGame.id + name; }
        }

        public int num { get; set; } = 0;

        public bool eliminated { get; set; } = false;
    }
    public class Game
    {
        private List<Pick> _possibleWinners = new List<Pick>();
        public IReadOnlyCollection<Pick> possibleWinners => _possibleWinners.AsReadOnly();
        [JsonIgnore]
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

        public void RemovePossible(string team)
        {
            _possibleWinners.RemoveAll(x => x.name == team);
        }

        public List<string> Choices(string currentTeam)
        {

            List<string> list = Enumerable.Range(1, _possibleWinners.Count).Select(x => x.ToString()).ToList();

            for (var c = 0; c < list.Count; c++)
            {
                for (var i = 0; i < possibleWinners.Count; i++)
                {
                    var pick = _possibleWinners[i].confidencePickStr;
                    if (_possibleWinners[i].confidencePickStr == "0")
                        continue;

                    if (_possibleWinners[i].name.Equals(currentTeam))
                        continue;

                    if (list[c].Equals(_possibleWinners[i].confidencePickStr))
                    {
                        list[c] = $"{list[c]} - {_possibleWinners[i].name}";
                    }

                }
            }

            return list;

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

        public void UpdateResult(List<string> results)
        {
            foreach (var res in results)
                possibleWinners.Where(x => x.name.Equals(res)).FirstOrDefault().eliminated = true;
        }

        public int max_points
        {
            get {
                
                return _possibleWinners.Where(x => !x.eliminated).Max(x=>x.confidencePick);

            }
        }
    }
    public class Round

    {
        public string name { get; set; } = "";
        private List<Game> _games = new List<Game>();
        public IReadOnlyCollection<Game> games => _games.AsReadOnly();
        public List<List<string>> Results = new List<List<string>>();
        public Pool? associatedPool { get; set; } = null;
        public void AddGame(Game game)
        {
            game.associatedRound = this;
            game.num = _games.Count() + 1;
            _games.Add(game);
        }

        public void UpdateGameResults()
        {
            for(var c = 0; c<_games.Count; c++)
            {
                var res = Results[c];

                 _games[c].UpdateResult(res);
                
            }
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

        public int max_points
        {
            get
            {
                return _games.Sum(g => g.max_points);
            }
        }

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
    [Serializable]
    public class Pool
    {
        public DateTime start { get; set; } = new DateTime();
        public DateTime end { get; set; } = new DateTime();

        public string poolName { get; set; } = "";
        public string welcomeStr { get; set; } = "";
        public string pickSet { get; set; } = "";
        public string pickSetAbbr
        {
            get
            {
                var res = "";
                var split = pickSet.Split(" ");
                if (split.Length >=1)
                {
                    //res += split[0][0];
                    //res += split[0][split[0].Length - 1];
                    res += split[0];
                    if (split.Length >= 2)
                    {
                        res += " ";
                        res += split[1][0];
                    }
                }
                return res;
            }
        }
        public List<string> eliminatedTeams = new List<string>();
        private List<Round> _rounds = new List<Round>();
        public IReadOnlyCollection<Round> rounds => _rounds.AsReadOnly();

        private List<TieBreaker> _tiebreakers = new List<TieBreaker>();
        public IReadOnlyCollection<TieBreaker> tiebreakers => _tiebreakers.AsReadOnly();

        public bool ShowGroup { get; set; } = false;
        public Pool()
        {
            start = DateTime.Now.AddHours(-1);
            end = DateTime.Now.AddHours(+1);
        }
        
        public string Serialize() => JsonSerializer.Serialize(this);
        public static Pool Deserailize(string pool) => JsonSerializer.Deserialize<Pool>(pool);

        public void AddRound(Round round)
        {
            round.associatedPool = this;
            round.num = _rounds.Count + 1;
            _rounds.Add(round);
        }

        public void AddTiebreaker(string name, string question)
        {
            _tiebreakers.Add(new TieBreaker { name = name, question = question });
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


        public string GetPicksJson() => JsonSerializer.Serialize(GetPicks());

        public Dictionary<string, string> GetPicks()
        {
            var res = new Dictionary<string, string>();

            foreach (var round in rounds)
            {
                foreach (var game in round.games)
                {
                    foreach (var possible in game.possibleWinners)
                    {
                        res.Add(possible.id, possible.confidencePickStr);
                    }
                }
            }
            foreach (var t in tiebreakers)
            {
                res.Add(t.name, t.answer);
            }
            res.Add("name", pickSet);
            return res;
        }
        public void LoadPicks(string picksJson)
        {
            var picksDictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(picksJson);
            LoadPicks(picksDictionary);
        }
        public void LoadPicks(Dictionary<string, string> picks)
        {
            if (picks == null) return;

            foreach (var round in rounds)
            {
                foreach (var game in round.games)
                {
                    foreach (var possible in game.possibleWinners)
                    {
                        string val = "0";
                        var success = picks.TryGetValue(possible.id, out val);
                        possible.confidencePickStr = val;
                    }
                }
            }
            foreach (var t in tiebreakers)
            {
                string val = "";
                var success = picks.TryGetValue(t.name, out val);
                t.answer = val;
            }
            string set = "";
            picks.TryGetValue("name", out set);
            pickSet = set;

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

        public bool isLive(out string message)
        {
            var res = false;
            message = "Boom";

            var adjustedEndDate = GetAdjustEndTime().ToShortDateString();
            var adjustedEndTime = GetAdjustEndTime().ToShortTimeString();
            if (DateTime.Now < start)
            {
                message = $"The pool is not open yet, check back on {adjustedEndDate} at {adjustedEndTime} ET";
            }
            else if (DateTime.Now > end)
            {
                message = $"Sorry, the pool closed as of {adjustedEndDate} at {adjustedEndTime} ET";
            }
            else { res = true; }
            return res;
        }

        private DateTime GetAdjustedTime(DateTime dt, string tz = "Eastern Standard Time")
        {
            TimeZoneInfo originalTimeZone = TimeZoneInfo.Local;
            TimeZoneInfo newTimeZone = TimeZoneInfo.FindSystemTimeZoneById(tz);

            var originalTimeOffset = new DateTimeOffset(dt, originalTimeZone.GetUtcOffset(dt));
            var newTimeOffset = TimeZoneInfo.ConvertTime(originalTimeOffset, newTimeZone);

            return newTimeOffset.DateTime;
        }

        public DateTime GetAdjustStartTime(string tz = "Eastern Standard Time")
        {
            return GetAdjustedTime(start, tz);
        }

        public DateTime GetAdjustEndTime(string tz = "Eastern Standard Time")
        {
            return GetAdjustedTime(end, tz);
        }

        public void UpdateResults()
        {
            foreach (var r in _rounds)
                r.UpdateGameResults();
        }

        public int max_points
        {
            get
            {
                return _rounds.Sum(r => r.max_points);
            }
        }

    }


}