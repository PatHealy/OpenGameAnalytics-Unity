using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class GameActionAPI : MonoBehaviour
{
    public static GameActionAPI instance;
    public string API_URL = "http://127.0.0.1:5000";
    public float session_continue_time = -1f;
    public bool DEBUG = false;

    User thisUser;
    Session thisSession;

    private void Awake() {
        if (instance != null) {
            Destroy(gameObject);
        } else {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }    
    }

    private void Start() {
        if (DEBUG) {
            PlayerPrefs.DeleteAll();
        }
        StartCoroutine(StartSession());
    }

    IEnumerator GetRequest(string uri) {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(uri)) {
            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();

            if (webRequest.isHttpError) {
                Debug.LogError("Error from server: " + webRequest.responseCode);
            } else {
                Debug.Log(webRequest.downloadHandler.text);
            }
        }
    }

    private IEnumerator GetUser() {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(API_URL + "/user")) {
            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();

            if (webRequest.isHttpError) {
                Debug.LogError("Error from server: " + webRequest.responseCode);
            } else {
                string res = webRequest.downloadHandler.text;
                thisUser = JsonUtility.FromJson<User>(res);
                Debug.Log(thisUser);
                SaveUser();
            }
        }
    }

    bool LoadUser() {
        if (!PlayerPrefs.HasKey("username") || !PlayerPrefs.HasKey("token")) {
            return false;
        }
        thisUser = new User(PlayerPrefs.GetString("username"), PlayerPrefs.GetString("token"));
        return true;
    }

    void SaveUser() {
        PlayerPrefs.SetString("username", thisUser.username);
        PlayerPrefs.SetString("token", thisUser.token);
    }

    private IEnumerator PostUserInfo() {
        yield return null;
    }

    private IEnumerator GetSession() {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(API_URL + "/session")) {
            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();

            if (webRequest.isHttpError) {
                Debug.LogError("Error from server: " + webRequest.responseCode);
            } else {
                string res = webRequest.downloadHandler.text;
                thisSession = JsonUtility.FromJson<Session>(res);
                Debug.Log(thisSession);
            }
        }
    }

    private IEnumerator AssociateSession() {
        // POST /session/user

        // TODO

        yield return null;
    }

    
    private IEnumerator StartSession() {
        // Tries to load user from saved data. If can't, call Get User
        if (!LoadUser()) {
            yield return GetUser();
        }

        if (thisUser == null) {
            Debug.LogError("USER FAILED TO LOAD");
            yield break;
        }

        //Both GET /session and POST /session/user
        yield return GetSession();

        yield return AssociateSession();

        yield return null;
    }

    private IEnumerator PostSessionContinue() {
        yield return null;
    }

    private IEnumerator PostSessionEnd() {
        yield return null;
    }

    private IEnumerator PostAction() {
        yield return null;
    }

    private IEnumerator PostIndependent() {
        yield return null;
    }

    private IEnumerator PostDependent() {
        yield return null;
    }

}
