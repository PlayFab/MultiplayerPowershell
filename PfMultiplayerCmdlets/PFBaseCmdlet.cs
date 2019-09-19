namespace PFMultiplayerCmdlets
{
    using Newtonsoft.Json;
    using PlayFab;
    using PlayFab.Internal;
    using System;
    using System.Management.Automation;
    using System.Threading.Tasks;

    public class PFBaseCmdlet : PSCmdlet
    {
        protected PlayFabMultiplayerInstanceAPI Instance { get; private set; }

        [Parameter]
        public string ProductionEnvironmentUrl { get; set; }

        protected override void BeginProcessing()
        {
            if (string.IsNullOrEmpty(PFTokenUtility.Instance.TitleId))
            {
                throw new Exception("Run Set-PfTitle before running any cmdlet.");
            }

            Instance = new PlayFabMultiplayerInstanceAPI(new PlayFabApiSettings
            {
                ProductionEnvironmentUrl = ProductionEnvironmentUrl ?? PlayFabSettings.staticSettings.ProductionEnvironmentUrl,
                TitleId = PlayFabSettings.staticSettings.TitleId,
                VerticalName = PlayFabSettings.staticSettings.VerticalName,
                AdvertisingIdType = PlayFabSettings.staticSettings.AdvertisingIdType,
                AdvertisingIdValue = PlayFabSettings.staticSettings.AdvertisingIdValue,
                DeveloperSecretKey = PlayFabSettings.staticSettings.DeveloperSecretKey,
                DisableAdvertising = PlayFabSettings.staticSettings.DisableAdvertising
            }, GetAuthenticationContext());
        }

        private PlayFabAuthenticationContext GetAuthenticationContext()
        {
            // Using reflection here since the property has an internal getter.
            var fieldInfo = typeof(PlayFabSettings).GetField("staticPlayer", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            var context = (PlayFabAuthenticationContext)fieldInfo.GetValue(null);
            return context;
        }

        protected static T CallPlayFabApi<T>(Func<Task<PlayFabResult<T>>> apiCall) where T : PlayFabResultCommon
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