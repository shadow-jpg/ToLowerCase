using System.Text;
using System.Text.RegularExpressions;

namespace MyApp // Note: actual namespace depends on the project name.
{
    class Program
    {
        public static int Main()
        {
            Regex Already_done = new Regex(@".*Removed.txt");


            string folder = "C:\\Reports\\ToLower\\"; // папка откуда брать + \\ чтобы туда зайти и сохранять файлы
            string[] filenames = Directory.GetFiles(folder);
            foreach (string filename in filenames)
            {
                flagsAndInput _flag = new flagsAndInput();
                if (Already_done.IsMatch(filename))
                    continue;
                string?[] file = new string[2500];
                string newFileName = filename.Substring(0, filename.IndexOf(".txt")) + "Removed.txt";
                //StreamReader sr = new StreamReader(filename);

                using(StreamReader sr = new StreamReader(filename))//C:\\reports\\ToLower.txt
                {
                    int rows = 0;
                    while (sr.Peek() >= 0)
                    {
                        //if(_flag.IsTable)
                        file[rows] = StringRemove(sr.ReadLine().ToLower(), _flag);
                        //StringRemove(sr.ReadLine().ToLower(), schema);
                        rows++;
                    }
                    writeToFile(newFileName, file, rows,_flag);
                }
            }
            return 0;
        }
        async public static void writeToFile(string filename, string[] result, int rows,flagsAndInput _flag)
        {
            if (File.Exists(filename))
            {
                File.Delete(filename);
            }

            StreamWriter fileNew = new(filename);
            for (int i = 0; i <= rows; i++)
            {
                if (!("change_on_deploy" == result[i]))
                    fileNew.WriteLine(result[i]);
            }
            if(!_flag.IsTable&&!_flag.IsType)
                fileNew.WriteLine($"ALTER FUNCTION {_flag.nameToChange } (json, character varying)\n"+
                    $"OWNER TO {_flag.owner};");
            else if(_flag.IsType)
                fileNew.WriteLine($"ALTER TABLE IF EXISTS {_flag.nameToChange}\nOWNER TO {_flag.owner};");
            else if(_flag.IsTable)
                fileNew.WriteLine($"ALTER TYPE {_flag.nameToChange}\nOWNER TO {_flag.owner};");

            fileNew.Close();
        }

        public static string StringRemove(string data,  flagsAndInput _flag)
        {
            Regex dbo_function = new Regex(@".*procedure.*(?:\[|\]|)dbo(?:\[|\]|).(?:\[|\]|)([^\s\]]{1,})(?:\[|\]|)");
            Regex dbo_table = new Regex(@".*table.*(?:\[|\]|)dbo(?:\[|\]|).(?:\[|\]|)([^\s\]]{1,})(?:\[|\]|)(?:(\()|)");
            Regex dbo_type  = new Regex(@".*type.*(?:\[|\]|)dbo(?:\[|\]|).(?:\[|\]|)([^\s\]]{1,})(?:\[|\]|)(?:(\()|)\s*as\s*table(.*)");
            Regex header = new Regex(@"(?:\s*as|.*\@.*output|.*@source.*)");
            Regex begin = new Regex(@"\s*begin");
            Regex tempTableOnCreate = new Regex(@".*declare\s*@(.*(?:temp|))\s+as\s*table\(\s*");
            Regex tempTableOnInsert = new Regex(@"(.*insert\s*into\s*)@([a-z0-9_\-)]*)(?:(.*)|)");
            Regex @source = new Regex(@"(.*)@([^\[\]\s]*)(.*)");
            Regex temp = new Regex(@".*(?:_temp|temp).*");
            Regex[] types=new Regex[_flag.DictionaryTypes.Count()];//в создаваемых столах
            Regex variables = new Regex(@"(?:(,)|)\s*\[([^\s]*)\]\s*(?:(,)|)"); // ,[var], с любыми запятыми
            int tempTypes = 0;
            foreach (string item in _flag.DictionaryTypes.Keys)
            {
                //string newType = ;
                types[tempTypes++]=new Regex(@$"\s*\t*(?:\[|\]|)([^\[\]\s]*)(?:\[|\]|)\s*(?:\[|\]|)({item})(?:\[|\]|)\s*([^\[\]\s,]*)\s*(?:(identity\s*[^\s]*|))\s*(?:(not null)|(null)|)(?:(,)|)");
                //группа 1-,(если в начале)
                //группа 2- название переменной
                //группа 3- тип переменной
                //группа 4- если varchar то(число символов), если нет то "null" если идет после переменной
                //группа 5-  identity(num,num)
                //группа 6- "not null" если есть identity(num,num)
                //группа 7-"null"  если есть identity(num,num)
                //группа 8- ,(если в конце)
            }
            //insert into @table 1-вся строка включая и до @table 2-@table 3 -null
            //insert into @table ( 1-вся строка включая и до @table 2-@table 3 -null
            //string[] words = data.Split(' ');
            if (!_flag.flagBeginSuprassed&&dbo_function.IsMatch(data))
            {
                _flag.flagBeginSuprassed = true;
                _flag.nameToChange  = _flag.schema + dbo_function.Match(data).Groups[1].ToString();
                return $"CREATE OR REPLACE FUNCTION {_flag.schema}{dbo_function.Match(data).Groups[1].ToString()}\n" +
                    "v_source json,\nINOUT v_result character varying)\nRETURNS character varying\nLANGUAGE 'plpgsql'\n" +
                    "COST 100\nVOLATILE PARALLEL UNSAFE\nAS $BODY$\nDECLARE\nbegin\n";
            }else if (!_flag.flagBeginSuprassed && dbo_table.IsMatch(data))
            {
                _flag.flagBeginSuprassed = true;
                _flag.IsTable = true;
                _flag.nameToChange = dbo_table.Match(data).Groups[1].ToString();
                return $"CREATE TABLE IF NOT EXISTS {_flag.schema}{dbo_table.Match(data).Groups[1].ToString()} {dbo_table.Match(data).Groups[2].ToString()}";

            }
            else if (!_flag.flagBeginSuprassed && dbo_type.IsMatch(data))
            {
                _flag.flagBeginSuprassed = true;
                _flag.IsType = true;
                _flag.nameToChange = dbo_type.Match(data).Groups[1].ToString();
                return $"DROP TYPE IF EXISTS {_flag.schema}{dbo_function.Match(data).Groups[1].ToString()};" +
                    $"\nCREATE TYPE {_flag.schema}{dbo_function.Match(data).Groups[1].ToString()}  {dbo_table.Match(data).Groups[2].ToString()} AS (";

            }
            else if (!_flag.flagIF && header.IsMatch(data))
            {
                return "change_on_deploy";
            } else if (!_flag.flagIF && begin.IsMatch(data))
            {
                _flag.flagIF = true;
                return "change_on_deploy";
            }
            else if (tempTableOnCreate.IsMatch(data))
            {
                var Groups = tempTableOnCreate.Match(data).Groups;
                if (string.IsNullOrEmpty(Groups[2].ToString()))
                {
                    return "DROP TABLE IF EXISTS " + Groups[1].ToString() + "_temp CASCADE;\n" +
                    "CREATE TEMP TABLE " + Groups[1].ToString() + "_temp (\n";
                }
                else return $"DROP TABLE IF EXISTS {Groups[1].ToString()} CASCADE;\n" +
                    $"CREATE TEMP TABLE {Groups[1].ToString()} (\n";
            } else if (temp.IsMatch(data))
            {
                if (temp.Match(data).Groups[1].ToString() == "temp")
                {
                    data = Regex.Replace(data, @"temp", "_temp");
                }
            }
            else if (@source.IsMatch(data))
            {
                /*
              * ПРИ ЗАМЕНЕ НА ЦИКЛ ИСПРАВИТЬ
              * 
              */
                if(data.Contains("declare"))
                    return data;
                if (data.Contains("set"))
                    return data;
                if (data.Contains("select"))
                    return data;
                //if (data.Contains("delete"))
                //    data=data+"";
                if (data.Contains("while"))
                    return data;
                if (temp.Match(data).Groups[1].ToString() == "temp")
                {
                    data = Regex.Replace(data, @"temp", "_temp");
                    var Groups = source.Match(data).Groups;
                    return data = $"{@source.Match(data).Groups[1].ToString()} {Regex.Replace(@source.Match(data).Groups[2].ToString(), "\\s*", "")}";
                }
                else
                {
                    var Groups = source.Match(data).Groups;
                    return data = $"{Groups[1].ToString()} {Regex.Replace(Groups[2].ToString(),"\\s*","")}_temp {Groups[3].ToString()}";
                }
                   
                
            }
            else if (tempTableOnInsert.IsMatch(data))
            {

                var Groups = tempTableOnInsert.Match(data).Groups;
                return Groups[1].ToString() + Groups[2].ToString() + Groups[3].ToString();
                //var GroupsOnNoTemp = tempTableOnInsert.Match(data).Groups;
                //return GroupsOnNoTemp[1].ToString() + GroupsOnNoTemp[2].ToString() + "_temp" + GroupsOnNoTemp[3].ToString();
            } else
                for (int i = 0; i < types.Length; i++){
                    if (types[i].IsMatch(data))
                    {
                        bool ID = false;
                        if (_flag.IsTable&&data.Contains("identity"))
                            ID=true;//.Groups;
                        data = _flag.FormIdentity(types[i].Match(data).Groups, _flag.DictionaryTypes, ID);
                    }
                }
            if (variables.IsMatch(data))
            {
                var Groups = variables.Match(data).Groups;
                if (!string.IsNullOrEmpty(Groups[1].ToString()) && string.IsNullOrEmpty(Groups[1].ToString()))// , [переменная]
                    return $"\t{Groups[1].ToString()} { Groups[2].ToString()}";
                else if (!string.IsNullOrEmpty(Groups[1].ToString()) == null && !string.IsNullOrEmpty(Groups[1].ToString()))// [переменная], 
                    return $"\t{Groups[2].ToString()} {Groups[3].ToString()}";
                else if (string.IsNullOrEmpty(Groups[1].ToString()) && string.IsNullOrEmpty(Groups[1].ToString()))//, [переменная],
                    return $"\t{Groups[1].ToString()} {Groups[2].ToString()} {Groups[3].ToString()}";

            }


            return data;
        }
        public class flagsAndInput
        {
            public bool IsTable = false;
            public bool IsType = false;
            public bool flagBeginSuprassed = false;
            public bool flagIF = false;
            public string nameToChange = null;//название стола, функции , типа
            public string schema = "es.";
            public string owner = "emas";

            public Dictionary<string, string> DictionaryTypes = new Dictionary<string, string>()
            {
                {"int","bigint" },
                {"varchar","character varying" },
                {"datetime","timestamp with time zone" },
                {"float","float" },
                {"real","double precision"}//,
                //{"","" },
                //{"","" },
                //{"","" },
                //{"","" },
                //{"","" },
                //{"","" },
                //{"","" }
            };
            public string FormIdentity(GroupCollection Groups, Dictionary<string, string> types, bool ID)
            {
                string typeOfIncomevariable;
                if (ID)
                {
                    typeOfIncomevariable = "bigserial";
                }
                else typeOfIncomevariable = types[Groups[2].ToString()];

                if (Groups[7].ToString() == ",")
                    return $"\t{Groups[1].ToString()} {typeOfIncomevariable} {Groups[3].ToString()} {Groups[4].ToString()} {Groups[6].ToString()} {Groups[7].ToString()} ,";
                //для всех записей до последней
                else if (Groups[1].ToString() == ",")
                    return $"\t,{Groups[1].ToString()} {typeOfIncomevariable} {Groups[3].ToString()} {Groups[4].ToString()} {Groups[6].ToString()} {Groups[7].ToString()}";
                else if (!(Groups[1].ToString() == ",") && !(Groups[7].ToString() == ","))
                    return $"\t{Groups[1].ToString()} {typeOfIncomevariable} {Groups[3].ToString()} {Groups[4].ToString()} {Groups[6].ToString()} {Groups[7].ToString()}";
                else
                    return $"\t,{Groups[1].ToString()} {typeOfIncomevariable} {Groups[3].ToString()} {Groups[4].ToString()} {Groups[6].ToString()} {Groups[7].ToString()},";
            }
        }

    }
}




