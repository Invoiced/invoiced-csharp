using System.Collections.Generic;

namespace Invoiced
{
    public class EntityList<T> : List<T> where T : AbstractEntity<T>
    {
        public Dictionary<string, string> LinkURLS { get; set; }
        public int TotalCount { get; set; }

        public string GetNextURL()
        {
            return GetURL("next");
        }

        public string GetSelfURL()
        {
            return GetURL("self");
        }

        public string GetLastURL()
        {
            return GetURL("last");
        }

        private string GetURL(string key)
        {
            var value = "";

            if (LinkURLS == null || !LinkURLS.ContainsKey(key)) return value;

            LinkURLS.TryGetValue(key, out value);
            return value;
        }
    }
}
