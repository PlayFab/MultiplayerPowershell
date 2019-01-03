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
                throw new Exception("Run Get-PFTitleEntityToken before running any cmdlet.");
            }
        }

        protected static T CallPlayFabApi<T>(Func<Task<PlayFabResult<T>>> apiCall) where T: PlayFabResultCommon
        {
            // BeginProcessing verifies that TitleId is set (and Get-PFTitleEntityToken has been run before).
            new GetPFTitleEntityToken() {TitleId = PlayFabSettings.TitleId, SecretKey = PlayFabSettings.DeveloperSecretKey}.Invoke();
            PlayFabResult<T> result = apiCall().Result;
            if (result.Error != null)
            {
                throw new Exception($"Error occurred while calling the api. {JsonConvert.SerializeObject(result.Error)}.");
            }

            return result.Result;
        }
    }
}