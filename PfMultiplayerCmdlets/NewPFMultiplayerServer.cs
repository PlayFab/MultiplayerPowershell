namespace PFMultiplayerCmdlets
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Management.Automation;
    using PlayFab;
    using PlayFab.MultiplayerModels;

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

        internal static string GetBuildId(Cmdlet cmdlet, string buildName, Guid? buildId)
        {
            ValidateBuildArguments(cmdlet, buildName, buildId);
            if (!string.IsNullOrEmpty(buildName))
            {
                List<BuildSummary> buildSummaries = GetPFMultiplayerBuild.GetBuildSummaries(all: true);
                buildSummaries = buildSummaries.Where(x => x.BuildName.IndexOf(buildName, StringComparison.OrdinalIgnoreCase) > -1).ToList();

                if (buildSummaries.Count == 0)
                {
                    cmdlet.ThrowTerminatingError(new ErrorRecord(new Exception($"Build {buildName} not found."), "BuildNotFound", ErrorCategory.InvalidArgument, null));
                }

                if (buildSummaries.Count > 1)
                {
                    cmdlet.ThrowTerminatingError(new ErrorRecord(new Exception($"More than one build matched {buildName}."), "MultipleBuildsFound", ErrorCategory.InvalidArgument, null));
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
            string buildIdString = GetBuildId(this, BuildName, BuildId);

            RequestMultiplayerServerResponse response = CallPlayFabApi(() =>PlayFabMultiplayerAPI.RequestMultiplayerServerAsync(new RequestMultiplayerServerRequest()
            {
                BuildId = buildIdString,
                PreferredRegions = PreferredRegions,
                SessionCookie = SessionCookie,
                SessionId = SessionId.ToString()
            })).Result.Result;

            WriteObject(response);
        }

        internal static void ValidateBuildArguments(Cmdlet cmdlet, string buildName, Guid? buildId)
        {
            if (!string.IsNullOrEmpty(buildName) && buildId.HasValue)
            {
                cmdlet.ThrowTerminatingError(
                    new ErrorRecord(new ArgumentException("Exactly one of BuildName, BuildId should be specified."),
                        "InvalidArgument",
                        ErrorCategory.InvalidArgument, null));
            }

            if (string.IsNullOrEmpty(buildName) && !buildId.HasValue)
            {
               cmdlet. ThrowTerminatingError(
                    new ErrorRecord(new ArgumentException("Exactly one of BuildName, BuildId should be specified."),
                        "InvalidArgument",
                        ErrorCategory.InvalidArgument, null));
            }
        }
    }
}