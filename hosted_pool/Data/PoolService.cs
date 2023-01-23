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

      
        public Task<Pool> Get(string pool_name, string user, out int userIndex)
        {
            var pool = GetPool(pool_name);
            var pickSet = "";
            userIndex = -1;
            if (user != "" && pool != null)
            {
                var picks = GetPicks(pool_name, user, out userIndex);
                if (picks != null)
                {
                    pool.LoadPicks(picks);
                }
            }
            return Task.FromResult(pool);
        }

        public Task<Dictionary<string,string>> GetGroupPicks(string pool_name)
        {
            IList<IList<object>> values = null;

            SpreadsheetsResource.ValuesResource _googleSheetValues = _service.Spreadsheets.Values;
            var range = $"{pool_name}_picks";


            var request = _googleSheetValues.Get(_docId, range);
            var response = request.Execute();
            values = response.Values;
            var res = GetGroupPicksJson(values);
            return Task.FromResult(res);
        }
        private string GetPicks(string pool_name, string user, out int userIndex)
        {
            IList<IList<object>> values = null;

            SpreadsheetsResource.ValuesResource _googleSheetValues = _service.Spreadsheets.Values;
            var range = $"{pool_name}_picks";


            var request = _googleSheetValues.Get(_docId, range);
            var response = request.Execute();
            values = response.Values;
            var res = GetPicksJson(values, user, out userIndex);
            return res;
        }


        
        private Pool GetPool(string pool_name)
        {
            IList<IList<object>> values = null;

            SpreadsheetsResource.ValuesResource _googleSheetValues = _service.Spreadsheets.Values;
            var range = $"{pool_name}";


            var request = _googleSheetValues.Get(_docId, range);

            try
            {
                var response = request.Execute();
                values = response.Values;
                var res = FromSheetsValues(values);
                res.pickSet = FullName;
                return res;
            }
            catch(Google.GoogleApiException gae)
            {
                return new Pool { welcomeStr="<b>Pool not found</b><br>"};
            }
            
        }
        public void PutProjection(string pool_name, Pool pool, string user)
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
            var range = $"{pool_name}_overview";


            AddColumns(_service, _docId, range);

            var request = _googleSheetValues.Get(_docId, range);
            var response = request.Execute();
            values = response.Values;

            string paid = "No";
            var userIndex = GetUserColumnProjection(values, user, out paid);
            var res = ToProjectionSheetValues(pool, user, paid);
            
            var vr = new ValueRange();
            vr.Values = res;
            
            var put_req = _service.Spreadsheets.Values.Update(vr, _docId, $"{pool_name}_overview!R1C{userIndex + 1}:R80C{userIndex + 2}");
            put_req.ValueInputOption = UpdateRequest.ValueInputOptionEnum.USERENTERED;
        
            var put_resp = put_req.Execute();
            Console.WriteLine(put_resp);

        }
        public void Put(string pool_name, Pool pool, string user, int userIndex)
        {
            var res = ToSheetsValues(pool, user, userIndex);
            Console.WriteLine(res);

            SpreadsheetsResource.ValuesResource _googleSheetValues = _service.Spreadsheets.Values;

            var vr = new ValueRange();
            vr.Values = res;



            SpreadsheetsResource.ValuesResource.UpdateRequest request = _service.Spreadsheets.Values.Update(vr, _docId, $"{pool_name}_picks!R{userIndex + 1}C1:R{userIndex + 1}C2");



            request.ValueInputOption = UpdateRequest.ValueInputOptionEnum.USERENTERED;

            // To execute asynchronously in an async method, replace `request.Execute()` as shown:
            var response = request.Execute();
            Console.WriteLine(response);

            PutProjection(pool_name, pool, user);

        }

      
        public void PutUserData(string name, string user_name, string email)
        {
            FullName = name;
        }
        private static Pool FromSheetsValues(IList<IList<object>> values)
        {
            var res = new Pool();

            var stopString = "rounds";
            var c = 0;
            var val = values[0];
            var fieldName = val.Count > 0 ? val[0].ToString().ToLower() : "";


            while (!fieldName.Equals(stopString))
            {

                if (fieldName.Equals("title"))
                {
                    res.poolName = val[1].ToString();
                }
                if (fieldName.Equals("welcome"))
                {
                    res.welcomeStr = val[1].ToString();
                }
                if (fieldName.Equals("start"))
                {
                    var timeString = val[1].ToString();
                    res.start= DateTime.Parse(timeString);
                }
                if (fieldName.Equals("lock"))
                {
                    var timeString = val[1].ToString();
                    res.end = DateTime.Parse(timeString);
                }
                if (fieldName.Equals("showgroup"))
                {
                    var temp = false;
                    Boolean.TryParse(val[1].ToString(), out temp );
                    res.ShowGroup = temp;
                }

                val = values[++c];
                
                fieldName = val.Count > 0?val[0].ToString().ToLower():"";
            }
            var roundDictionary = new Dictionary<string, Round>();
            foreach(var round in val.Skip(1))
            {
                var roundName = round.ToString();
                var rnd = new Round { name = roundName };
                roundDictionary.Add(rnd.name.ToLower(), rnd);
                res.AddRound(rnd);
            }
            stopString = "tiebreakers";
            val = values[++c];
            fieldName = val.Count > 0 ? val[0].ToString().ToLower() : "";
            while (!fieldName.Equals(stopString))
            {
                Round round;
                var found = roundDictionary.TryGetValue(fieldName, out round);
                if (found)
                {
                    var game = new Game();
                    foreach(var p in val.Skip(1))
                    {
                        game.AddPosssible(new Pick { name = p.ToString()});
                    }
                    round.AddGame(game);
                }
                val = values[++c];
                fieldName = val.Count > 0 ? val[0].ToString().ToLower() : "";
            }

            stopString = "results";
            val = values[++c];
            fieldName = val.Count > 0 ? val[0].ToString().ToLower() : "";
            while (!fieldName.Equals(stopString))
            {
                if(val.Count >= 2)
                    res.AddTiebreaker(fieldName, val[1].ToString());

                if (c + 1 >= values.Count) break;
                val = values[++c];
                fieldName = val.Count > 0 ? val[0].ToString().ToLower() : "";
            }

            stopString = "";
            val = values[++c];
            fieldName = val.Count > 0 ? val[0].ToString().ToLower() : "";
            int game_index = 0;
            while (!fieldName.Equals(stopString))
            {
                Round round;
                var found = roundDictionary.TryGetValue(fieldName, out round);
                var games = new List<string>();
                foreach (var v in val.Skip(1))
                    games.Add(v.ToString());

                round.Results.Add(games);

                if (c + 1 >= values.Count) break;
                val = values[++c];
                fieldName = val.Count > 0 ? val[0].ToString().ToLower() : "";
            }
            res.UpdateResults();
            return res;
        }

        private static Dictionary<string, string> GetGroupPicksJson(IList<IList<object>> values)
        {
            var res = new Dictionary<string, string>();
            if (values == null || values.Count <= 0)
            {
                return null;
            }
            foreach(var v in values)
            {
                res.Add(v[0].ToString(), v[1].ToString());
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

        private int GetUserColumnProjection(IList<IList<object>> values, string user, out string paid)
        {
            var userIndex = 0;
            paid = "No";
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
                        paid = values[2][c].ToString();
                        break;
                    }
                }
            }
            return userIndex;
        }

        private static List<IList<object>> ToProjectionSheetValues(Pool pool, string user, string paid)
        {
            var res = new List<IList<object>>();
            var inner = new List<object> { user};
            res.Add(inner);
            inner = new List<object> { pool.pickSet };
            res.Add(inner);
            inner = new List<object> { paid };
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

        private Sheet GetSheet(SheetsService service, string spreadSheetId, string spreadSheetName)
        {
            var spreadsheet = service.Spreadsheets.Get(spreadSheetId).Execute();
            var sheet = spreadsheet.Sheets.FirstOrDefault(s => s.Properties.Title == spreadSheetName);
            return sheet;
        }

        private void AddColumns(SheetsService service, string spreadSheetId, string spreadSheetName)
        {
            var sheet = GetSheet(service, spreadSheetId, spreadSheetName);
            if (sheet == null) return;

            int sheetId = (int)sheet.Properties.SheetId;
            int columnCount = (int)sheet.Properties.GridProperties.ColumnCount;
            DimensionRange dr1 = new DimensionRange
            {
                SheetId = sheetId,
                Dimension = "COLUMNS",
                StartIndex = columnCount - 1,
                EndIndex = columnCount
            };

            var request1 = new Request { InsertDimension = new InsertDimensionRequest { Range = dr1, InheritFromBefore = false } };

            var requests = new BatchUpdateSpreadsheetRequest { Requests = new List<Request> { request1 } };
            service.Spreadsheets.BatchUpdate(requests, spreadSheetId).Execute();
        }
    }
}

