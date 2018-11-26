namespace PFMultiplayerCmdlets
{
    using System;
    using System.Management.Automation;
    using PlayFab;
    using PlayFab.MultiplayerModels;

    [Cmdlet(VerbsCommon.Remove, "PFMultiplayerBuild")]
    public class RemovePFMultiplayerBuild : PFBaseCmdlet
    {
        [Parameter(Mandatory = true)]
        public Guid? BuildId { get; set; }

        protected override void ProcessRecord()
        {
            PlayFabMultiplayerAPI.DeleteBuildAsync(new DeleteBuildRequest() {BuildId = BuildId.Value.ToString()}).Wait();
            WriteObject($"Deleted build {BuildId.Value}");
        }
    }
}