using System.Collections.Generic;
using System.Text;

namespace UrbanizationIndex
{
    public class CsvHelper
    {
        public static List<string> Separate(string line, char separator)
        {
            List<string> data = new List<string>();
            int quot = -1;

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == '"')
                {
                    if (quot != -1 && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++;
                    }
                    else
                        quot = (quot == -1) ? i : -1;
                }
                else if (quot == -1 && line[i] == separator)
                {
                    data.Add(sb.ToString());
                    sb.Length = 0;
                }
                else
                    sb.Append(line[i]);
            }

            data.Add(sb.ToString());

            return data;
        }

        public static string ToCsvData(string data)
        {
            StringBuilder sb = new StringBuilder();
            int semiCol = data.IndexOf(';');
            int quot = data.IndexOf('"');
            if (semiCol != -1 || quot != -1)
                sb.Append('"');

            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] == '"')
                    sb.Append("\"\"");
                else
                    sb.Append(data[i]);
            }

            if (semiCol != -1 || quot != -1)
                sb.Append('"');

            return sb.ToString();
        }
    }
}
