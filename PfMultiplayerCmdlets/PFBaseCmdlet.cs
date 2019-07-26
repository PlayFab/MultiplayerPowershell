namespace PFMultiplayerCmdlets
{
    using System;
    using System.Management.Automation;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using PlayFab;
    using PlayFab.Internal;

    public class PFBaseCmdlet : PSCmdlet
    {
        protected override void BeginProcessing()
        {
            if (string.IsNullOrEmpty(PFTokenUtility.Instance.TitleId))
            {
                throw new Exception("Run Set-PfTitle before running any cmdlet.");
            }
        }

        protected static T CallPlayFabApi<T>(Func<Task<PlayFabResult<T>>> apiCall) where T: PlayFabResultCommon
        {
            try
            {
                PFTokenUtility.Instance.GetPFTitleEntityToken();
            }
            catch (Exception e)
            {
                throw new Exception(e.ToString());
            }
            
            PlayFabResult<T> result = apiCall().Result;
            if (result.Error != null)
            {
                throw new Exception($"Error occurred while calling the api. {JsonConvert.SerializeObject(result.Error)}.");
            }

            return result.Result;
        }
    }
}