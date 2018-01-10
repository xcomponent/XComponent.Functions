using System.Collections.Generic;
using XComponent.Functions.Core.Senders;

namespace XComponent.Functions.Core
{
    public class FunctionResult
    {
        public string ComponentName { get; set; }
        public string StateMachineName { get; set; }
        public object PublicMember { get; set; }
        public object InternalMember { get; set; }
        public List<SenderResult> Senders { get; set; }
        public string RequestId { get; set; }
        public string ErrorMessage { get; set; }
        public bool Error { get; set; }
    }
}
