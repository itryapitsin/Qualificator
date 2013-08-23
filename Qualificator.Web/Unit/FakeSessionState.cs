using System.Collections.Generic;
using System.Web;

namespace Qualificator.Web.Unit
{
    public class FakeSessionState : HttpSessionStateBase
    {
        private readonly Dictionary<string, object> _items = new Dictionary<string, object>();

        public override object this[string name]
        {
            get { return _items.ContainsKey(name) ? _items[name] : null; }
            set { _items[name] = value; }
        }

        public override void Add(string name, object value)
        {
            _items.Add(name, value);
        }

        public override void Clear()
        {
            _items.Clear();
        }

        public override void RemoveAll()
        {
            _items.Clear();
        }

        public override void Remove(string name)
        {
            _items.Remove(name);
        }
    }
}