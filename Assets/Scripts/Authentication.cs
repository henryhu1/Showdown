#if UNITY_ANDROID
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif
using System.Threading.Tasks;
using System;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using TMPro;

public class Authentication : MonoBehaviour
{
    public string Token;
    public string Error;
#if DEBUG
    public TextMeshProUGUI m_statusTextTemp;
#endif

    void Start()
    {
        Authenticate();
    }

    async void Authenticate()
    {
#if DEBUG
        m_statusTextTemp.text = "Authenticating...";
#endif
        try
        {
            await UnityServices.InitializeAsync();

#if DEBUG
            AuthenticationService.Instance.SignedIn += () => {
                m_statusTextTemp.text = "Signed in! " + AuthenticationService.Instance.PlayerId;
            };
#endif
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
#if UNITY_ANDROID
            PlayGamesPlatform.Activate();
            PlayGamesPlatform.DebugLogEnabled = true;
            if (ShouldLinkAccount())
            {
#if DEBUG
                m_statusTextTemp.text = "Linking to Google Play Games...";
#endif
                LinkGooglePlayGames();
            }
            else
            {
#if DEBUG
                m_statusTextTemp.text = "Logging in to Google Play Games...";
#endif
                LoginGooglePlayGames();
            }
#endif
        }
        catch (Exception e)
        {
#if DEBUG
            m_statusTextTemp.text = e.Message;
#endif
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
       PlayGamesPlatform.Instance.Authenticate((success) =>
        {
            if (success == SignInStatus.Success)
            {
#if DEBUG
                m_statusTextTemp.text = "Login with Google Play games successful.";
#endif

                PlayGamesPlatform.Instance.RequestServerSideAccess(true, async code =>
                {
#if DEBUG
                    Debug.Log("Authorization code: " + code);
#endif
                    Token = code;
                    await SignInWithGooglePlayGamesAsync(Token);
                });
            }
            else
            {
#if DEBUG
                m_statusTextTemp.text = "Failed to retrieve Google play games authorization code.";
#endif
            }
        });
    }

    async Task SignInWithGooglePlayGamesAsync(string authCode)
    {
        try
        {
            await AuthenticationService.Instance.SignInWithGooglePlayGamesAsync(authCode);
#if DEBUG
            m_statusTextTemp.text = "SignIn is successful.";
#endif
        }
        catch (AuthenticationException ex)
        {
            // Compare error code to AuthenticationErrorCodes
            // Notify the player with the proper error message
#if DEBUG
            m_statusTextTemp.text = ex.Message;
#endif
        }
        catch (RequestFailedException ex)
        {
            // Compare error code to CommonErrorCodes
            // Notify the player with the proper error message
#if DEBUG
            m_statusTextTemp.text = ex.Message;
#endif
        }
    }

    public void LinkGooglePlayGames()
    {
        PlayGamesPlatform.Instance.Authenticate((status) =>
        {
            if (status == SignInStatus.Success)
            {
#if DEBUG
                m_statusTextTemp.text = "Google Play Games authentication successful.";
#endif

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
#if DEBUG
                m_statusTextTemp.text = "Failed to link Google Play Games.";
#endif
            }
        });
    }

    async Task LinkWithGooglePlayGamesAsync(string authCode)
    {
        try
        {
            await AuthenticationService.Instance.LinkWithGooglePlayGamesAsync(authCode);
#if DEBUG
            m_statusTextTemp.text = "Link is successful.";
#endif
        }
        catch (AuthenticationException ex) when (ex.ErrorCode == AuthenticationErrorCodes.AccountAlreadyLinked)
        {
            // Prompt the player with an error message.
#if DEBUG
            m_statusTextTemp.text = "This user is already linked with another account. Log in instead.";
#endif
        }

        catch (AuthenticationException ex)
        {
            // Compare error code to AuthenticationErrorCodes
            // Notify the player with the proper error message
#if DEBUG
            m_statusTextTemp.text = ex.Message;
#endif
        }
        catch (RequestFailedException ex)
        {
            // Compare error code to CommonErrorCodes
            // Notify the player with the proper error message
#if DEBUG
            m_statusTextTemp.text = ex.Message;
#endif
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
#if DEBUG
            m_statusTextTemp.text = "Cannot unlink Google Play Games because the user is not signed in.";
#endif
        }
    }

    async Task UnlinkGooglePlayGamesAsync()
    {
        try
        {
            await AuthenticationService.Instance.UnlinkGooglePlayGamesAsync();
#if DEBUG
            m_statusTextTemp.text = "Unlink is successful.";
#endif
        }
        catch (AuthenticationException ex)
        {
            // Compare error code to AuthenticationErrorCodes
            // Notify the player with the proper error message
#if DEBUG
            m_statusTextTemp.text = ex.Message;
#endif
        }
        catch (RequestFailedException ex)
        {
            // Compare error code to CommonErrorCodes
            // Notify the player with the proper error message
#if DEBUG
            m_statusTextTemp.text = ex.Message;
#endif
        }
    }
#endif
}
