namespace PFMultiplayerCmdlets
{
    using System;
    using System.Management.Automation;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using PlayFab;
    using PlayFab.Internal;

    public class PFBaseCmdlet : Cmdlet
    {
        protected override void BeginProcessing()
        {
            if (string.IsNullOrEmpty(PlayFabSettings.TitleId))
            {
                ThrowTerminatingError(new ErrorRecord(new Exception("Run Get-PFTitleEntityToken before running any cmdlet."), "EntityTokenMissing",
                    ErrorCategory.AuthenticationError, null));
            }
        }

        protected async Task<PlayFabResult<T>>CallPlayFabApi<T>(Func<Task<PlayFabResult<T>>> apiCall) where T: PlayFabResultCommon
        {
            PlayFabResult<T> result = await apiCall();
            WriteObject(result.Error?.HttpStatus ?? "null");
            if (result.Error != null)
            {
                ThrowTerminatingError(new ErrorRecord(new Exception($"Error occurred while calling the api. {JsonConvert.SerializeObject(result.Error)}."),
                    "APIError", ErrorCategory.InvalidResult, null));
            }

            return result;
        }
    }
}