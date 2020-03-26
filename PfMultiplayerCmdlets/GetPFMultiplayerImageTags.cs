using System.Collections.Generic;
using System.Management.Automation;
using PlayFab.MultiplayerModels;

namespace PFMultiplayerCmdlets
{
    [Cmdlet(VerbsCommon.Get, "PFMultiplayerContainerImageTags")]
    [OutputType(typeof(List<string>))]
    public class GetPfMultiplayerContainerImageTags : PageableCmdlet
    {
        [Parameter(Mandatory = true)]
        public string ImageName { get; set; }
        
        protected override void ProcessRecord()
        {
            ListContainerImageTagsResponse response = CallPlayFabApi(() => Instance
                .ListContainerImageTagsAsync(new ListContainerImageTagsRequest() { ImageName = ImageName }));
            
            WriteObject(response.Tags);
        }
    }
}