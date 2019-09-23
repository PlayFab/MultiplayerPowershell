namespace PFMultiplayerCmdlets
{
    using PlayFab.MultiplayerModels;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Management.Automation;

    [Cmdlet(VerbsCommon.New, "PFMultiplayerServer")]
    public class NewPFMultiplayerServer : PFBaseCmdlet
    {
        [Parameter]
        public string BuildName { get; set; }

        [Parameter]
        public Guid? BuildId { get; set; }

        [Parameter(Mandatory = true)]
        public Guid SessionId { get; set; }

        [Parameter]
        public string SessionCookie { get; set; }

        [Parameter(Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public List<AzureRegion> PreferredRegions { get; set; }

        internal string GetBuildId(string buildName, Guid? buildId)
        {
            ValidateBuildArguments(buildName, buildId);
            if (!string.IsNullOrEmpty(buildName))
            {
                List<BuildSummary> buildSummaries = new GetPFMultiplayerBuild { ProductionEnvironmentUrl = ProductionEnvironmentUrl }.GetBuildSummaries(all: true);
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

        protected override void ProcessRecord()
        {
            string buildIdString = GetBuildId(BuildName, BuildId);

            RequestMultiplayerServerResponse response = CallPlayFabApi(() => Instance.RequestMultiplayerServerAsync(new RequestMultiplayerServerRequest()
            {
                BuildId = buildIdString,
                PreferredRegions = PreferredRegions,
                SessionCookie = SessionCookie,
                SessionId = SessionId.ToString()
            }));

            WriteObject(response);
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