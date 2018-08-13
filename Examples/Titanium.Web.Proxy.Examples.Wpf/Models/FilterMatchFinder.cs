﻿using System;
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

        public string FiltersInfo
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                foreach(var k in _filters.Keys)
                {
                    sb.AppendLine(k);
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
        }

        public MatchResult HasMatches(string inString)
        {
            foreach(var k in _filters.Keys)
            {
                if (_filters[k].IsMatch(inString))
                {
                    return new MatchResult(inString, k, true);
                }
            }
            return new MatchResult(inString, null, false);
        }
    }

    class FilterModel
    {
        string[] _segments;
        int[] _matches;
        public FilterModel(string s)
        {
            _segments = s.Split(new char[] { '*' });
            _matches = new int[_segments.Length];
        }

        public bool IsMatch(string inString)
        {
            int prev = 0;
            for(int i = 0; i < _segments.Length; i++)
            {
                if (string.IsNullOrEmpty(_segments[i]) && prev == 0)
                    continue;
                else if (string.IsNullOrEmpty(_segments[i]))
                    continue;

                _matches[i] = inString.IndexOf(_segments[i], prev);
                prev = _matches[i] + _segments[i].Length;

                //String has more text after last matchet segment of filter
                if (i + 1 >= _segments.Length && prev < inString.Length)
                    return false;
            }
            for (int i = 0; i < _segments.Length; i++)
            {
                if (_matches[i] < 0 && !string.IsNullOrEmpty( _segments[i]))
                    return false;
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