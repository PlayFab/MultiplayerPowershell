namespace PFMultiplayerCmdlets
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Management.Automation;
    using PlayFab;
    using PlayFab.MultiplayerModels;

    [Cmdlet(VerbsCommon.Get, "PFMultiplayerAsset")]
    [OutputType(typeof(List<AssetSummary>))]
    public class GetPFMultiplayerAsset : PageableCmdlet
    {
        protected override void ProcessRecord()
        {
            List<AssetSummary> summaries = new List<AssetSummary>();
            ListAssetSummariesResponse response = PlayFabMultiplayerAPI
                .ListAssetSummariesAsync(new ListAssetSummariesRequest() {PageSize = DefaultPageSize}).Result.Result;
            summaries.AddRange(response.AssetSummaries ?? Enumerable.Empty<AssetSummary>());
            if (All)
            {
                while (!string.IsNullOrEmpty(response.SkipToken))
                {
                    response = CallPlayFabApi(() => PlayFabMultiplayerAPI
                        .ListAssetSummariesAsync(new ListAssetSummariesRequest() {PageSize = DefaultPageSize, SkipToken = response.SkipToken}));
                    summaries.AddRange(response.AssetSummaries ?? Enumerable.Empty<AssetSummary>());
                }
            }

            WriteObject(summaries);
        }
    }
}