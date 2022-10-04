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
            var res = FromSheetsValues(values);
            return Task.FromResult(res);
        }


        public void Put(Pool pool)
        {
            var res = ToSheetsValues(pool);
            Console.WriteLine(res);

            SpreadsheetsResource.ValuesResource _googleSheetValues = _service.Spreadsheets.Values;

            var vr = new ValueRange();
            vr.Values = res;

            SpreadsheetsResource.ValuesResource.UpdateRequest request = _service.Spreadsheets.Values.Update(vr, _docId, $"picks!A1:X100");
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
                var rnd = new Round { name = v[0].ToString() };
                var game = new Game {  };
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
                        game = new Game {};
                    }
                }
                rnd.AddGame(game);
                res.AddRound(rnd);
            }

           

            return res;
        }

        private static List<IList<object>> ToSheetsValues(Pool pool)
        {
            var res = new List<IList<object>>();
            foreach (var r in pool.rounds)
            {
                foreach(var g in r.games)
                {
                    foreach(var t in g.possibleWinners)
                    {
                        var inner = new List<object> { t.name, t.confidencePick };
                        res.Add(inner);
                    }
                    res.Add(new List<object> { "--", "--" });
                }
            }


            return res;
        }
    }
}

