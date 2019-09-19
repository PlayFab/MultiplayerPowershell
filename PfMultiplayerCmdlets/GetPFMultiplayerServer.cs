namespace PFMultiplayerCmdlets
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Management.Automation;
    using PlayFab;
    using PlayFab.MultiplayerModels;

    [Cmdlet(VerbsCommon.Get, "PFMultiplayerServer")]
    [OutputType(typeof(MultiplayerServerSummary))]
    public class GetPFMultiplayerServer : PageableCmdlet
    {
        [Parameter]
        public string BuildName { get; set; }

        [Parameter]
        public Guid? BuildId { get; set; }

        [Parameter]
        public List<AzureRegion> Regions { get; set; }

        [Parameter]
        public SwitchParameter AllRegions { get; set; }

        protected override void ProcessRecord()
        {
            if (Regions?.Count > 0 && AllRegions)
            {
                throw new ArgumentException("Exactly one of Regions, AllRegions should be specified.");
            }

            if ((Regions == null || Regions.Count == 0) && !AllRegions)
            {
                throw new ArgumentException("Exactly one of Regions, AllRegions should be specified.");
            }

            string buildIdString = new NewPFMultiplayerServer { ProductionEnvironmentUrl = ProductionEnvironmentUrl }.GetBuildId(BuildName, BuildId);

            HashSet<AzureRegion> regionsList = new HashSet<AzureRegion>();
            if (AllRegions)
            {
                GetBuildResponse response = Instance.GetBuildAsync(new GetBuildRequest() { BuildId = buildIdString }).Result.Result;
                response.RegionConfigurations.ForEach(x => regionsList.Add(x.Region.Value));
            }
            else
            {
                Regions.ForEach(x => regionsList.Add(x));
            }

            List<MultiplayerServerSummary> serverSummaries = new List<MultiplayerServerSummary>();
            foreach (AzureRegion region in regionsList)
            {
                serverSummaries.AddRange(GetMultiplayerServers(buildIdString, region));
            }

            WriteObject(serverSummaries);
        }

        private List<MultiplayerServerSummary> GetMultiplayerServers(string buildId, AzureRegion region)
        {
            List<MultiplayerServerSummary> summaries = new List<MultiplayerServerSummary>();
            ListMultiplayerServersResponse response = CallPlayFabApi(() => Instance
                .ListMultiplayerServersAsync(new ListMultiplayerServersRequest() { PageSize = DefaultPageSize, Region = region, BuildId = buildId }));
            summaries.AddRange(response.MultiplayerServerSummaries ?? Enumerable.Empty<MultiplayerServerSummary>());
            if (All)
            {
                while (!string.IsNullOrEmpty(response.SkipToken))
                {
                    response = CallPlayFabApi(() => Instance
                        .ListMultiplayerServersAsync(new ListMultiplayerServersRequest()
                        {
                            BuildId = buildId,
                            PageSize = DefaultPageSize,
                            SkipToken = response.SkipToken,
                            Region = region
                        }));
                    summaries.AddRange(response.MultiplayerServerSummaries ?? Enumerable.Empty<MultiplayerServerSummary>());
                }
            }

            return summaries;
        }
    }
}