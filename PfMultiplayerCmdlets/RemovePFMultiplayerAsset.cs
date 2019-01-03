namespace PFMultiplayerCmdlets
{
    using System.Management.Automation;
    using PlayFab;
    using PlayFab.MultiplayerModels;

    [Cmdlet(VerbsCommon.Remove, "PFMultiplayerAsset")]
    public class RemovePFMultiplayerAsset : PFBaseCmdlet
    {
        [Parameter(Mandatory = true)]
        public string FileName { get; set; }

        protected override void ProcessRecord()
        {
            CallPlayFabApi(() => PlayFabMultiplayerAPI.DeleteAssetAsync(new DeleteAssetRequest() {FileName = FileName}));
            WriteVerbose($"Completed removing asset {FileName}.");
        }
    }
}