using System;
namespace hosted_pool.Data
{
    public class Team
    {
        public string name { get; set; } = "";
        public int confidencePick { get; set; } = 0;
    }
    public class Game
    {
        public List<Team> possibleWinners { get; set; } = new List<Team>();

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

