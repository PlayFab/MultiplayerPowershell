using System.Management.Automation;
using PlayFab.MultiplayerModels;

namespace PFMultiplayerCmdlets
{
    [Cmdlet(VerbsCommon.Remove, "PFMultiplayerContainerImageTag")]
    public class RemovePFMultiplayerContainerImageTag : PFBaseCmdlet
    {
        [Parameter(Mandatory = true)]
        public string ImageName { get; set; }
        
        [Parameter(Mandatory = true)]
        public string Tag { get; set; }

        protected override void ProcessRecord()
        {
            CallPlayFabApi(() => Instance.UntagContainerImageAsync(new UntagContainerImageRequest() {ImageName = ImageName, Tag = Tag}));
            WriteVerbose($"Completed removing Image {ImageName}:{Tag}.");
        }
    }
}