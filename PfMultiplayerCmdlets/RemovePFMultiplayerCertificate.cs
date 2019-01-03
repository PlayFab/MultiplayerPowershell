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
            CallPlayFabApi(() => PlayFabMultiplayerAPI.DeleteCertificateAsync(new DeleteCertificateRequest {Name = Name}));
            WriteVerbose($"Completed removing certificate {Name}.");
        }
    }
}