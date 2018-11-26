namespace PFMultiplayerCmdlets
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Management.Automation;
    using PlayFab;
    using PlayFab.MultiplayerModels;

    [Cmdlet(VerbsCommon.Get, "PFMultiplayerBuild")]
    [OutputType(typeof(GetBuildResponse))]
    public class GetPFMultiplayerBuild : PageableCmdlet
    {
        [Parameter]
        public string BuildName { get; set; }

        [Parameter]
        public Guid? BuildId { get; set; }

        [Parameter]
        public SwitchParameter Detailed { get; set; }

        protected override void ProcessRecord()
        {
            if (Convert.ToInt32(!string.IsNullOrEmpty(BuildName)) + Convert.ToInt32(BuildId.HasValue) + Convert.ToInt32(All) > 1)
            {
                ThrowTerminatingError(
                    new ErrorRecord(new ArgumentException("Exactly one of BuildName, BuildId, All should be specified."),
                    "InvalidArgument",
                    ErrorCategory.InvalidArgument, null));
            }

            List<BuildSummary> buildSummaries = GetBuildSummaries(All);
            if (!string.IsNullOrEmpty(BuildName))
            {
                buildSummaries = buildSummaries.Where(x => x.BuildName.IndexOf(BuildName, StringComparison.OrdinalIgnoreCase) > -1).ToList();
            }
            else if (BuildId.HasValue)
            {
                // This list will have only one value, It is retained as a list for convenience and consistent behavior.
                buildSummaries = buildSummaries.Where(x => string.Equals(x.BuildId, BuildId.Value.ToString(), StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (Detailed)
            {
                List<GetBuildResponse> buildDetails = new List<GetBuildResponse>();
                buildDetails.AddRange(buildSummaries.Select(x => GetBuildDetails(x.BuildId)));
                WriteObject(buildDetails);
            }
            else
            {

                WriteObject(buildSummaries.Select(ToGetBuildResponse));
            }
        }

        private GetBuildResponse ToGetBuildResponse(BuildSummary buildSummary)
        {
            return new GetBuildResponse()
            {
                BuildId = buildSummary.BuildId,
                BuildName = buildSummary.BuildName,
                CreationTime = buildSummary.CreationTime,
                Metadata = buildSummary.Metadata
            };
        }

        private GetBuildResponse GetBuildDetails(string buildId)
        {
            return PlayFabMultiplayerAPI.GetBuildAsync(new GetBuildRequest() {BuildId = buildId}).Result.Result;
        }

        internal static List<BuildSummary> GetBuildSummaries(bool all)
        {
            List<BuildSummary> summaries = new List<BuildSummary>();
            ListBuildSummariesResponse response = PlayFabMultiplayerAPI
                .ListBuildSummariesAsync(new ListBuildSummariesRequest() { PageSize = DefaultPageSize }).Result.Result;
            summaries.AddRange(response.BuildSummaries ?? Enumerable.Empty<BuildSummary>());
            if (all)
            {
                while (!string.IsNullOrEmpty(response.SkipToken))
                {
                    response = PlayFabMultiplayerAPI
                        .ListBuildSummariesAsync(new ListBuildSummariesRequest() { PageSize = DefaultPageSize, SkipToken = response.SkipToken }).Result.Result;
                    summaries.AddRange(response.BuildSummaries ?? Enumerable.Empty<BuildSummary>());
                }
            }

            return summaries;
        }
    }
}