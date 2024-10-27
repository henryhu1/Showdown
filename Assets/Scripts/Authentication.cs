#if UNITY_ANDROID
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif
using System.Threading.Tasks;
using System;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

public class Authentication : MonoBehaviour
{
    public string Token;
    public string Error;

    async void Awake()
    {
        await Authenticate();
    }

    public async Task Authenticate()
    {
        Debug.Log("authenticating");
        try
        {
            await UnityServices.InitializeAsync();

            AuthenticationService.Instance.SignedIn += () => {
                Debug.Log("Signed in! " + AuthenticationService.Instance.PlayerId);
            };
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
#if UNITY_ANDROID
            PlayGamesPlatform.Activate();
            PlayGamesPlatform.DebugLogEnabled = true;
            if (ShouldLinkAccount())
            {
                LinkGooglePlayGames();
            }
            else
            {
                LoginGooglePlayGames();
            }
#endif
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
        }
    }

#if UNITY_ANDROID
    private bool ShouldLinkAccount()
    {
        // Example condition: Check if the user is signed in anonymously
        return AuthenticationService.Instance.IsSignedIn && AuthenticationService.Instance.IsAuthorized &&
               AuthenticationService.Instance.PlayerInfo.GetUnityId() != null;
    }

    public void LoginGooglePlayGames()
    {
        Debug.Log("login google play games");
        PlayGamesPlatform.Instance.Authenticate((success) =>
        {
            if (success == SignInStatus.Success)
            {
                Debug.Log("Login with Google Play games successful.");

                PlayGamesPlatform.Instance.RequestServerSideAccess(true, async code =>
                {
                    Debug.Log("Authorization code: " + code);
                    Token = code;
                    await SignInWithGooglePlayGamesAsync(Token);
                });
            }
            else
            {
                Error = "Failed to retrieve Google play games authorization code";
                Debug.Log("Login Unsuccessful");
            }
        });
    }

    async Task SignInWithGooglePlayGamesAsync(string authCode)
    {
        try
        {
            await AuthenticationService.Instance.SignInWithGooglePlayGamesAsync(authCode);
            Debug.Log("SignIn is successful.");
        }
        catch (AuthenticationException ex)
        {
            // Compare error code to AuthenticationErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
        }
        catch (RequestFailedException ex)
        {
            // Compare error code to CommonErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
        }
    }

    public void LinkGooglePlayGames()
    {
        Debug.Log("link google play games");
        PlayGamesPlatform.Instance.Authenticate((status) =>
        {
            if (status == SignInStatus.Success)
            {
                Debug.Log("Google Play Games authentication successful.");

                // Request server-side access to get the authorization code
                PlayGamesPlatform.Instance.RequestServerSideAccess(true, async (authCode) =>
                {
                    Debug.Log("Authorization code obtained: " + authCode);

                    // Link the current Unity Authentication session with Google Play Games
                    await LinkWithGooglePlayGamesAsync(authCode);
                });
            }
            else
            {
                Debug.LogError("Failed to authenticate with Google Play Games.");
            }
        });
    }

    async Task LinkWithGooglePlayGamesAsync(string authCode)
    {
        try
        {
            await AuthenticationService.Instance.LinkWithGooglePlayGamesAsync(authCode);
            Debug.Log("Link is successful.");
        }
        catch (AuthenticationException ex) when (ex.ErrorCode == AuthenticationErrorCodes.AccountAlreadyLinked)
        {
            // Prompt the player with an error message.
            Debug.LogError("This user is already linked with another account. Log in instead.");
        }

        catch (AuthenticationException ex)
        {
            // Compare error code to AuthenticationErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
        }
        catch (RequestFailedException ex)
        {
            // Compare error code to CommonErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
        }
    }

    public async void UnlinkGooglePlayGames()
    {
        // First, ensure the user is signed in with Google Play Games
        if (PlayGamesPlatform.Instance.IsAuthenticated())
        {
            await UnlinkGooglePlayGamesAsync();
        }
        else
        {
            Debug.LogWarning("Cannot unlink Google Play Games because the user is not signed in.");
        }
    }

    async Task UnlinkGooglePlayGamesAsync()
    {
        try
        {
            await AuthenticationService.Instance.UnlinkGooglePlayGamesAsync();
            Debug.Log("Unlink is successful.");
        }
        catch (AuthenticationException ex)
        {
            // Compare error code to AuthenticationErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
        }
        catch (RequestFailedException ex)
        {
            // Compare error code to CommonErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
        }
    }
#endif
}
