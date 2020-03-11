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

            string buildIdString = GetBuildId(BuildName, BuildId);

            HashSet<AzureRegion> regionsList = new HashSet<AzureRegion>();
            if (AllRegions)
            {
                GetBuildResponse response = Instance.GetBuildAsync(new GetBuildRequest() { BuildId = buildIdString }).Result.Result;
                response.RegionConfigurations.ForEach(x => regionsList.Add((AzureRegion)Enum.Parse(typeof(AzureRegion), x.Region, true)));
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
                .ListMultiplayerServersAsync(new ListMultiplayerServersRequest() { PageSize = DefaultPageSize, Region = region.ToString(), BuildId = buildId }));
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
                            Region = region.ToString()
                        }));
                    summaries.AddRange(response.MultiplayerServerSummaries ?? Enumerable.Empty<MultiplayerServerSummary>());
                }
            }

            return summaries;
        }

        internal string GetBuildId(string buildName, Guid? buildId)
        {
            ValidateBuildArguments(buildName, buildId);
            if (!string.IsNullOrEmpty(buildName))
            {
                List<BuildSummary> buildSummaries = GetBuildSummaries(all: true);
                buildSummaries = buildSummaries.Where(x => x.BuildName.IndexOf(buildName, StringComparison.OrdinalIgnoreCase) > -1).ToList();

                if (buildSummaries.Count == 0)
                {
                    throw new Exception($"Build {buildName} not found.");
                }

                if (buildSummaries.Count > 1)
                {
                    throw new Exception($"More than one build matched {buildName}.");
                }

                return buildSummaries[0].BuildId;
            }
            else
            {
                return buildId.Value.ToString();
            }
        }

        internal List<BuildSummary> GetBuildSummaries(bool all)
        {
            List<BuildSummary> summaries = new List<BuildSummary>();
            ListBuildSummariesResponse response = CallPlayFabApi(() => Instance
                .ListBuildSummariesAsync(new ListBuildSummariesRequest() { PageSize = DefaultPageSize }));
            summaries.AddRange(response.BuildSummaries ?? Enumerable.Empty<BuildSummary>());
            if (all)
            {
                while (!string.IsNullOrEmpty(response.SkipToken))
                {
                    response = CallPlayFabApi(() => Instance
                        .ListBuildSummariesAsync(new ListBuildSummariesRequest() { PageSize = DefaultPageSize, SkipToken = response.SkipToken }));
                    summaries.AddRange(response.BuildSummaries ?? Enumerable.Empty<BuildSummary>());
                }
            }

            return summaries;
        }

        internal static void ValidateBuildArguments(string buildName, Guid? buildId)
        {
            if (!string.IsNullOrEmpty(buildName) && buildId.HasValue)
            {
                throw new ArgumentException("Exactly one of BuildName, BuildId should be specified.");
            }

            if (string.IsNullOrEmpty(buildName) && !buildId.HasValue)
            {
                throw new ArgumentException("Exactly one of BuildName, BuildId should be specified.");
            }
        }
    }
}