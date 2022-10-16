using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;


using static Google.Apis.Sheets.v4.SpreadsheetsResource.ValuesResource;

namespace hosted_pool.Data
{
    public class PoolService
	{
        private SheetsService _service = null;
        private const string _docId = "16wm1twZlW0WMp5X6CUVRTFHp_H6NHULO-RaO-z6h8Rs";
        public string FullName { get; set; } = "";
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
            var pickSet = "";
            userIndex = -1;
            if (user != "")
            {
                var picks = GetPicks(user, out userIndex);
                if (picks != null)
                {
                    pool.LoadPicks(picks);
                }
            }
            return Task.FromResult(pool);
        }

        private string GetPicks(string user, out int userIndex)
        {
            IList<IList<object>> values = null;

            SpreadsheetsResource.ValuesResource _googleSheetValues = _service.Spreadsheets.Values;
            var range = $"nflpicks";


            var request = _googleSheetValues.Get(_docId, range);
            var response = request.Execute();
            values = response.Values;
            var res = GetPicksJson(values, user, out userIndex);
            return res;
        }

        private Pool GetPool()
        {
            IList<IList<object>> values = null;

            SpreadsheetsResource.ValuesResource _googleSheetValues = _service.Spreadsheets.Values;
            var range = $"nfl!R1C1:R25C25";


            var request = _googleSheetValues.Get(_docId, range);
            var response = request.Execute();
            values = response.Values;
            var res = FromSheetsValues(values);
            res.pickSet = FullName;
            return res;
        }
        public void PutProjection(Pool pool, string user)
        {
            var c1 = ToProjectionRowHeader(pool);
            var pvr = new ValueRange();
            pvr.Values = c1;
            var creq = _service.Spreadsheets.Values.Update(pvr, _docId, $"nfl_overview!R1C1:R80C1");
            creq.ValueInputOption = UpdateRequest.ValueInputOptionEnum.USERENTERED;
            var cresp = creq.Execute();
            Console.WriteLine(cresp);

            IList<IList<object>> values = null;

            SpreadsheetsResource.ValuesResource _googleSheetValues = _service.Spreadsheets.Values;
            var range = $"nfl_overview!R1C1:R80C50";


            var request = _googleSheetValues.Get(_docId, range);
            var response = request.Execute();
            values = response.Values;


            var userIndex = GetUserColumnProjection(values, user);
            var res = ToProjectionSheetValues(pool, user);


            var vr = new ValueRange();
            vr.Values = res;

            var put_req = _service.Spreadsheets.Values.Update(vr, _docId, $"nfl_overview!R1C{userIndex + 1}:R80C{userIndex + 2}");
            put_req.ValueInputOption = UpdateRequest.ValueInputOptionEnum.USERENTERED;
            var put_resp = put_req.Execute();
            Console.WriteLine(put_resp);

        }
        public void Put(Pool pool, string user, int userIndex)
        {
            var res = ToSheetsValues(pool, user, userIndex);
            Console.WriteLine(res);

            SpreadsheetsResource.ValuesResource _googleSheetValues = _service.Spreadsheets.Values;

            var vr = new ValueRange();
            vr.Values = res;



            SpreadsheetsResource.ValuesResource.UpdateRequest request = _service.Spreadsheets.Values.Update(vr, _docId, $"nflpicks!R{userIndex + 1}C1:R{userIndex + 1}C2");



            request.ValueInputOption = UpdateRequest.ValueInputOptionEnum.USERENTERED;

            // To execute asynchronously in an async method, replace `request.Execute()` as shown:
            var response = request.Execute();
            Console.WriteLine(response);

            PutProjection(pool, user);

        }

      
        public void PutUserData(string name, string user_name, string email)
        {
            FullName = name;
        }
        private static Pool FromSheetsValues(IList<IList<object>> values)
        {
            var res = new Pool();

            if(values.Count() >= 2)
            {
                res.welcomeStr = values[0][1].ToString();
                //res.poolName = values[1][1].ToString();
            }


            var pickNum = 0;
            foreach (var v in values.Skip(2))
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

        private static string GetPicksJson(IList<IList<object>> values, string user, out int userRow)
        {
            var res = "";
            if(values == null || values.Count<=0)
            {
                userRow = 0;
                return null;
            }
            userRow = values.Count;
            // get user row
           for(var c = 0; c< values.Count(); c++)
           {
                if (values[c][0].ToString() == user)
                {
                    userRow = c;
                    break;
                }
           }
           if (userRow == values.Count) return null;

           res = values[userRow][1].ToString();
           
           return res;
        }
        private static List<IList<object>> ToSheetsValues(Pool pool, string user, int userIndex)
        {
            var res = new List<IList<object>>();
            var inner = new List<object> { user, pool.GetPicksJson()};
            res.Add(inner);
            return res;
        }

        private int GetUserColumnProjection(IList<IList<object>> values, string user)
        {
            var userIndex = 0;
            if (values == null || values.Count <= 0) { 

            }
            else{
                userIndex = values[0].Count;
                // get user index
                for (var c = 0; c < values[0].Count(); c++)
                {
                    if (values[0][c].ToString() == user)
                    {
                        userIndex = c;
                        break;
                    }
                }
            }
            return userIndex;
        }

        private static List<IList<object>> ToProjectionSheetValues(Pool pool, string user)
        {
            var res = new List<IList<object>>();
            var inner = new List<object> { user};
            res.Add(inner);
            inner = new List<object> { pool.pickSet };
            res.Add(inner);
            inner = new List<object> { "No" };
            res.Add(inner);
            foreach (var r in pool.rounds)
            {
                foreach (var g in r.games)
                {
                    foreach (var t in g.possibleWinners)
                    {
                        inner = new List<object> {t.confidencePick};
                        res.Add(inner);
                    }
                }
            }
            foreach (var t in pool.tiebreakers)
            {
                inner = new List<object> { t.answer };
                res.Add(inner);
            }

            return res;
        }

        private static List<IList<object>> ToProjectionRowHeader(Pool pool)
        {
            var res = new List<IList<object>>();
            var inner = new List<object> { "User" };
            res.Add(inner);
            inner = new List<object> { "Set Name"};
            res.Add(inner);
            inner = new List<object> { "Paid" };
            res.Add(inner);
            foreach (var r in pool.rounds)
            {
                foreach (var g in r.games)
                {
                    foreach (var t in g.possibleWinners)
                    {
                        inner = new List<object> { t.name};
                        res.Add(inner);
                    }
                }
            }
            foreach (var t in pool.tiebreakers)
            {
                inner = new List<object> { t.question };
                res.Add(inner);
            }

            return res;
        }
    }
}

