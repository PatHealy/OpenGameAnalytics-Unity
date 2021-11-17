using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;

public class GameActionAPI : MonoBehaviour
{
    public static GameActionAPI instance;
    public string API_URL = "http://127.0.0.1:5000";
    public int GAME_ID = 959742;
    public float session_continue_time = 15f;
    public bool poll_server_when_playing = false;
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
        Debug.Log("Starting");
        if (DEBUG) {
            Debug.Log("DELETING PLAYER PREFS");
            PlayerPrefs.DeleteAll();
        }
        Debug.Log("Starting session");
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
        using (UnityWebRequest webRequest = UnityWebRequest.Get(API_URL + "/user/" + GAME_ID)) {
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
        using (UnityWebRequest webRequest = UnityWebRequest.Get(API_URL + "/session/" + GAME_ID)) {
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
        using (UnityWebRequest www = new UnityWebRequest(API_URL + "/session/user", "POST")) {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(JsonUtility.ToJson(new UserSession(thisUser, thisSession)));
            www.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            yield return www.SendWebRequest();

            if (www.isHttpError) {
                Debug.LogError("Error from server: " + www.responseCode);
            } else {
                Debug.Log("Session associated: " + www.responseCode);
            }
        }

        if (poll_server_when_playing) {
            StartCoroutine(PostSessionContinue());
        }
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
    }

    private IEnumerator PostSessionContinue() {
        while (true) {
            yield return new WaitForSeconds(session_continue_time);

            using (UnityWebRequest www = new UnityWebRequest(API_URL + "/session/continue", "POST")) {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(JsonUtility.ToJson(new UserSession(thisUser, thisSession)));
                www.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
                www.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");
                yield return www.SendWebRequest();

                if (www.isHttpError) {
                    Debug.LogError("Error from server: " + www.responseCode);
                } else {
                    Debug.Log("Session associated: " + www.responseCode);
                }
            }
        }
    }

    private void OnApplicationQuit() {
        StartCoroutine(PostSessionEnd());
    }

    private IEnumerator PostSessionEnd() {
        using (UnityWebRequest www = new UnityWebRequest(API_URL + "/session/end", "POST")) {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(JsonUtility.ToJson(new UserSession(thisUser, thisSession)));
            www.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            yield return www.SendWebRequest();

            if (www.isHttpError) {
                Debug.LogError("Error from server: " + www.responseCode);
            } else {
                Debug.Log("Session associated: " + www.responseCode);
            }
        }
    }

    private IEnumerator PostAction(string action_name, string action_data) {
        yield return null;
    }

    private IEnumerator PostIndependent() {
        yield return null;
    }

    private IEnumerator PostDependent() {
        yield return null;
    }

}
