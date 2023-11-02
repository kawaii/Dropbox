using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dropbox
{
    [Serializable]
    public class TradeQueueEntry
    {
        [NonSerialized] internal string GUID = Guid.NewGuid().ToString();
        public string Player = "";
        public int Gil = 0;
    }
}
