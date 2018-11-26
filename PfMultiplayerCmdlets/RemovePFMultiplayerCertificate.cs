using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PFMultiplayerCmdlets
{
    using System.Management.Automation;
    using PlayFab;
    using PlayFab.MultiplayerModels;

    [Cmdlet(VerbsCommon.Remove, "PFMultiplayerCertificate")]
    public class RemovePFMultiplayerCertificate : PFBaseCmdlet
    {
        [Parameter(Mandatory = true)]
        public string Name { get; set; }

        protected override void ProcessRecord()
        {
            PlayFabMultiplayerAPI.DeleteCertificateAsync(new DeleteCertificateRequest() { Name = Name }).Wait();
            WriteObject($"Completed removing certificate {Name}.");
        }
    }
}
