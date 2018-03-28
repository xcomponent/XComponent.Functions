using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XComponent.Functions.Core
{
    public class KeyValuePairSettingsItem
    {
        public string ComponentName { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }

        public bool Equals(KeyValuePairSettingsItem other)
        {
            return string.Equals(ComponentName, other.ComponentName) && string.Equals(Key, other.Key) && string.Equals(Value, other.Value);
        }
    }
}
