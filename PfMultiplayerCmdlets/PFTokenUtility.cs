namespace PFMultiplayerCmdlets
{
    using System;
    using System.Reflection;
    using System.Threading.Tasks;
    using PlayFab;
    using PlayFab.AuthenticationModels;
    using BindingFlags = System.Reflection.BindingFlags;

    internal class PFTokenUtility
    {
        public static PFTokenUtility Instance { get; } = new PFTokenUtility();

        private DateTime _tokenRefreshTime;

        private const int TokenRefreshIntervalInHours = 12;

        public string TitleId { get; set; }

        public string _secretKey;

        private PFTokenUtility()
        {
        }

        public void GetPFTitleEntityToken()
        {
            if (_tokenRefreshTime < DateTime.UtcNow)
            {
                GetEntityTokenRequest request = new GetEntityTokenRequest();

                // Using reflection here since the property has an internal setter. Clearing it is essential, to force,
                // the SDK to use the secret key (and not send a potentially expired token). If the SDK behavior changes to use the secret key (if available),
                // then all this code can be removed
                FieldInfo fieldInfo = typeof(PlayFabSettings).GetField("staticPlayer", BindingFlags.Static | BindingFlags.NonPublic);
                
                PlayFabAuthenticationContext context = (PlayFabAuthenticationContext)fieldInfo.GetValue(null);
                
                fieldInfo = typeof(PlayFabAuthenticationContext).GetField("EntityToken", BindingFlags.Instance | BindingFlags.Public);
                fieldInfo.SetValue(context, null);
                
                PlayFabSettings.TitleId = TitleId;
                PlayFabSettings.DeveloperSecretKey = _secretKey;

                // The SDK sets the entity token as part of response evaluation.
                PlayFabAuthenticationAPI.GetEntityTokenAsync(request).Wait();
                _tokenRefreshTime = DateTime.UtcNow.AddHours(TokenRefreshIntervalInHours);
            }
        }

        public void SetTitleSecretKey(string titleId, string secret)
        {
            if (!string.Equals(TitleId, titleId, StringComparison.OrdinalIgnoreCase))
            {
                TitleId = titleId;
                _secretKey = secret;
                _tokenRefreshTime = DateTime.MinValue;
            }
        }
    }
}