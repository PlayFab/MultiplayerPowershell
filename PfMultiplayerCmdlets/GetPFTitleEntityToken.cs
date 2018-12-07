namespace PFMultiplayerCmdlets
{
    using System.Reflection;
    using PlayFab;
    using PlayFab.AuthenticationModels;
    using System;
    using System.Management.Automation;
    using BindingFlags = System.Reflection.BindingFlags;

    [Cmdlet(VerbsCommon.Get, "PFTitleEntityToken")]
    public class GetPFTitleEntityToken : Cmdlet
    {
        private static DateTime TokenRefreshTime { get; set; }

        private const int TokenRefreshIntervalInHours = 12;

        [Parameter(Mandatory = true)]
        public string SecretKey { get; set; }

        [Parameter(Mandatory = true)]
        public string TitleId { get; set; }

        protected override void ProcessRecord()
        {
            if (TokenRefreshTime < DateTime.UtcNow)
            {
                GetEntityTokenRequest request = new GetEntityTokenRequest();

                // Using reflection here since the property has an internal setter. Clearing it is essential, to force,
                // the SDK to use the secret key (and not send a potentially expired token).
                FieldInfo fieldInfo = typeof(PlayFabSettings).GetField("EntityToken", BindingFlags.Static | BindingFlags.NonPublic);
                fieldInfo.SetValue(null, null);

                PlayFabSettings.TitleId = TitleId;
                PlayFabSettings.DeveloperSecretKey = SecretKey;

                // The SDK sets the entity token as part of response evaluation.
                PlayFabAuthenticationAPI.GetEntityTokenAsync(request).Wait();
                TokenRefreshTime = DateTime.UtcNow.AddHours(TokenRefreshIntervalInHours);
                WriteVerbose("Entity token retrieved.");
            }
        }
    }
}