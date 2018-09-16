using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Titanium.Web.Proxy.Examples.Wpf.Models
{
    class FilterMatchFinder
    {
        Dictionary<string, FilterModel> _filters;
        List<string> _notMatchedStrings;

        public IReadOnlyCollection<string> NotMatchedStrings
        {
            get
            {
                List<string> res = new List<string>();
                lock (_notMatchedStrings)
                {
                    res.AddRange(_notMatchedStrings);
                }
                return res;
            }
        }

        public string FiltersInfo
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                foreach(var k in _filters.Keys)
                {
                    sb.AppendFormat("{0} - matches: {1}\r\n", k, _filters[k].MatchCount);
                }

                sb.AppendLine("\r\nHas no matchings:");
                foreach (var i in NotMatchedStrings.Distinct().OrderBy((s) => s))
                {
                    sb.AppendLine(i);
                }
                return sb.ToString();
            }
        }
        public FilterMatchFinder(string fileFilterSettings)
        {
            _filters = new Dictionary<string, Models.FilterModel>();
            using(StreamReader strm = new StreamReader(fileFilterSettings))
            {
                string s = strm.ReadLine();
                while(true)
                {
                    s = s.Trim().ToLowerInvariant();
                    
                    if (!string.IsNullOrEmpty(s) && !string.IsNullOrWhiteSpace(s) && !s.StartsWith("#") && !_filters.ContainsKey(s))
                    {
                        _filters.Add(s, new FilterModel(s));
                    }
                    if (strm.EndOfStream) break;
                    s = strm.ReadLine();
                }
                
            }

            _notMatchedStrings = new List<string>();
        }

        public MatchResult HasMatches(string inString)
        {
            string src = inString.ToLowerInvariant();

            foreach(var k in _filters.Keys)
            {
                if (_filters[k].IsMatch(src))
                {
                    return new MatchResult(inString, k, true);
                }
            }
            AddNotMatched(src);
            return new MatchResult(inString, null, false);
        }

        public void AddNotMatched(string inString)
        {
            lock (_notMatchedStrings)
            {
                _notMatchedStrings.Add(inString);
            }
        }
    }

    class FilterModel
    {
        string[] _segments;
        int[] _matches;
        bool _isRefularExpression = false;

        public int MatchCount { get; private set; }
        public bool IsRegularExpression => _isRefularExpression;
        public FilterModel(string s)
        {
            if (s.StartsWith("@"))
            {
                _segments = new string[1];
                _segments[0] = s.Substring(1);
                _isRefularExpression = true;
                _matches = new int[1];
            }
            else
            {
                _segments = s.Split(new char[] { '*' });
                _matches = new int[_segments.Length];
            }
        }
        public bool IsMatch(string inString)
        {
            if (IsRegularExpression)
            {
                return IsMatchRegEx(inString);
            }
            else
            {
                return IsMatchNoRegEx(inString);
            }
        }
        public bool IsMatchNoRegEx(string inString)
        {
            int prev = 0;
            for(int i = 0; i < _segments.Length; i++)
            {
                if (string.IsNullOrEmpty(_segments[i]) && prev == 0)
                    continue;
                else if (string.IsNullOrEmpty(_segments[i]))
                    continue;

                _matches[i] = inString.IndexOf(_segments[i], prev);
                if(_matches[i] >= 0)
                    prev = _matches[i] + _segments[i].Length;

                //String has more text after last matchet segment of filter with word
                if (i + 1 >= _segments.Length && prev < inString.Length)
                    return false;
            }
            for (int i = 0; i < _segments.Length; i++)
            {
                //one of segments not have match
                if (_matches[i] < 0 && !string.IsNullOrEmpty(_segments[i]))
                    return false;

                if (i == 0)
                {
                    //First word segment not start from begin
                    if (!string.IsNullOrEmpty(_segments[i]) && _matches[i] != 0)
                        return false;
                }
                if(i == _segments.Length - 1)
                {
                    //Last word segments is not in end
                    if (!string.IsNullOrEmpty(_segments[i]) && _matches[i] + _segments[i].Length < inString.Length) return false;
                }
            }
            lock (_matches)
            {
                MatchCount++;
            }
            return true;
        }

        public bool IsMatchRegEx(string inString)
        {
            int prev = 0;
            if(!System.Text.RegularExpressions.Regex.IsMatch(inString, _segments[0], System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            {
                return false;
            }
            lock (_matches)
            {
                MatchCount++;
            }
            return true;
        }
    }

    class MatchResult
    {
        public MatchResult(string inString, string wildcard, bool result)
        {
            ProcessingString = inString;
            MatchedWildCard = wildcard;
            IsMatch = result;
        }
        public bool IsMatch { get; private set; }
        public string ProcessingString { get; private set; }
        public string MatchedWildCard { get; private set; }
    }
}
