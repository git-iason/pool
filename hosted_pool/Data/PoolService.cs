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

        public Task<Pool> Get(string user, out int userIndex)
        {
            var pool = GetPool();
            var picks = GetPicks(user, out userIndex);
            if(picks != null) pool.LoadPicks(picks);
            return Task.FromResult(pool);
        }

        private Dictionary<string, string> GetPicks(string user, out int userIndex)
        {
            IList<IList<object>> values = null;

            SpreadsheetsResource.ValuesResource _googleSheetValues = _service.Spreadsheets.Values;
            var range = $"picks!R1C1:R80C50";


            var request = _googleSheetValues.Get(_docId, range);
            var response = request.Execute();
            values = response.Values;
            var res = GetPicksDictionary(values, user, out userIndex);
            return res;
        }

        private Pool GetPool()
        {
            IList<IList<object>> values = null;

            SpreadsheetsResource.ValuesResource _googleSheetValues = _service.Spreadsheets.Values;
            var range = $"nfl!R1C1:R20C24";


            var request = _googleSheetValues.Get(_docId, range);
            var response = request.Execute();
            values = response.Values;
            var res = FromSheetsValues(values);
            return res;
        }

        public void Put(Pool pool, string user, int userIndex)
        {
            var res = ToSheetsValues(pool, user, userIndex);
            Console.WriteLine(res);

            SpreadsheetsResource.ValuesResource _googleSheetValues = _service.Spreadsheets.Values;

            var vr = new ValueRange();
            vr.Values = res;



            SpreadsheetsResource.ValuesResource.UpdateRequest request = _service.Spreadsheets.Values.Update(vr, _docId, $"picks!R1C{userIndex+1}:R80C{userIndex + 2}");



            request.ValueInputOption = UpdateRequest.ValueInputOptionEnum.USERENTERED;

            // To execute asynchronously in an async method, replace `request.Execute()` as shown:
            var response = request.Execute();
            Console.WriteLine(response);

        }

        private static Pool FromSheetsValues(IList<IList<object>> values)
        {
            var res = new Pool();

            var pickNum = 0;
            foreach (var v in values)
            {
                if (v.Count() == 0) continue;
                var rnd = new Round { name = v[0].ToString() };
                var game = new Game { };
                if (v[0].ToString().Contains("Tiebreaker"))
                {
                    res.AddTiebreaker(v[0].ToString(), v[1].ToString());
                }
                else
                {
                    foreach (var l in v.Skip(1))
                    {
                        var val = l.ToString();
                        if (val != "")
                        {
                            game.AddPosssible(new Pick { name = val });
                        }
                        else
                        {
                            rnd.AddGame(game);
                            game = new Game { };
                        }
                    }
                    rnd.AddGame(game);
                    res.AddRound(rnd);
                }
            }

           

            return res;
        }

        private static Dictionary<string, string> GetPicksDictionary(IList<IList<object>> values, string user, out int userIndex)
        {
            var res = new Dictionary<string, string>();
            if(values == null || values.Count<=0)
            {
                userIndex = 0;
                return null;
            }
            userIndex = values[0].Count;
            // get user index
           for(var c = 0; c< values[0].Count(); c++)
           {
                if (values[0][c].ToString() == user)
                {
                    userIndex = c;
                    break;
                }
           }
           if (userIndex == values[0].Count) return null;

           foreach(var pick in values.Skip(1))
           {
                res.Add(pick[userIndex].ToString(), pick[userIndex + 1].ToString());
           }



            return res;
        }
        private static List<IList<object>> ToSheetsValues(Pool pool, string user, int userIndex)
        {
            var res = new List<IList<object>>();
            var inner = new List<object> { user, "confidence" };
            res.Add(inner);
            foreach (var r in pool.rounds)
            {
                foreach(var g in r.games)
                {
                    foreach(var t in g.possibleWinners)
                    {
                        inner = new List<object> {t.id, t.confidencePick };
                        res.Add(inner);
                    }
                }
            }
            foreach(var t in pool.tiebreakers)
            {
                inner = new List<object> { t.name, t.answer };
                res.Add(inner);
            }

            return res;
        }
    }
}

