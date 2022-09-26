using System;
using static Google.Apis.Sheets.v4.SpreadsheetsResource.ValuesResource.GetRequest;
using System.IO;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4.Data;
using static Google.Apis.Sheets.v4.SpreadsheetsResource.ValuesResource;
using System.Reflection.Metadata;

namespace hosted_pool.Data
{
	public class PoolService
	{
        private SheetsService _service = null;
        private const string _docId = "1o8I88rUZBz9cEOSk9EJNuz654Yl8Ne4qLGS52-ejDMI";
		public PoolService()
		{
            var jsonCredsContent = System.IO.File.ReadAllText("choice_secret.json");
            string[] Scopes = { SheetsService.Scope.Spreadsheets };
            var credential = GoogleCredential.FromJson(jsonCredsContent).CreateScoped(Scopes);
            _service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "choicer"
            });
        }

        public Task<Pool> Get()
        {
            IList<IList<object>> values = null;

            SpreadsheetsResource.ValuesResource _googleSheetValues = _service.Spreadsheets.Values;
            var range = $"nfl!A1:X4";


            var request = _googleSheetValues.Get(_docId, range);
            var response = request.Execute();
            values = response.Values;


            var res = new Pool();


            foreach (var v in values)
            {
                var rnd = new Round { name = v[0].ToString(), games = new List<Game>() };
                var game = new Game { possibleWinners = new List<Team>() };
                foreach (var l in v.Skip(1))
                {
                    var val = l.ToString();
                    if (val != "")
                    {
                        game.possibleWinners.Add(new Team { name = val });
                    }
                    else
                    {
                        rnd.games.Add(game);
                        game = new Game { possibleWinners = new List<Team>() };
                    }
                }
                rnd.games.Add(game);
                res.rounds.Add(rnd);
            }

            foreach (var r in res.rounds)
            {
                foreach (var g in r.games)
                {
                    foreach (var w in g.possibleWinners)
                    {
                        Console.WriteLine(w.name);
                    }
                }
            }
            return Task.FromResult(res);
        }
	}
}

