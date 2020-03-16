using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using PlayFab.MultiplayerModels;

namespace PFMultiplayerCmdlets
{
    [Cmdlet(VerbsCommon.Get, "PFMultiplayerContainerImages")]
    [OutputType(typeof(List<string>))]
    public class GetPFMultiplayerContainerImages : PageableCmdlet
    {
        protected override void ProcessRecord()
        {
            var images = new List<string>();
            ListContainerImagesResponse response = CallPlayFabApi(() => Instance
                .ListContainerImagesAsync(new ListContainerImagesRequest() { PageSize = DefaultPageSize }));
            
            images.AddRange(response.Images);
            if (All)
            {
                while (!string.IsNullOrEmpty(response.SkipToken))
                {
                    string skipToken = response.SkipToken;
                    response = CallPlayFabApi(() => Instance
                        .ListContainerImagesAsync(new ListContainerImagesRequest() { PageSize = DefaultPageSize, SkipToken = skipToken }));
                    images.AddRange(response.Images ?? Enumerable.Empty<string>());
                }
            }
            
            WriteObject(images);
        }
    }
}