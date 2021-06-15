using System;
using System.Collections.Generic;
using System.Linq;

namespace Slacek.Server
{
    public class Headers : Dictionary<string, string>
    {
        private const string CRLF = "\r\n";
        public string Method { get; set; }
        public string StatusCode { get; set; }


        public static Headers Parse(string parsed)
        {
            string[] rows = parsed.Split(CRLF, StringSplitOptions.RemoveEmptyEntries);
            string[] firstLine = rows[0].Split(' ');
            if(firstLine.Length != 2)
                return null;
            Headers headers = new Headers
            {
                Method = firstLine[0],
                StatusCode = firstLine[1]
            };
            foreach(string row in rows.Skip(1))
            {
                string[] keyValuePair = row.Split(":");
                if(keyValuePair.Length != 2)
                    return null;
                string key = keyValuePair[0].Trim();
                string value = keyValuePair[1].Trim();
                headers[key] = value;
            }
            return headers;
        }

        public override string ToString()
        {
            string result = Method + " " + StatusCode + CRLF;;
            foreach((string key, string value) in this)
            {
                result += $"{key}: {value}" + CRLF;
            }
            return result;
        }
    }
}
