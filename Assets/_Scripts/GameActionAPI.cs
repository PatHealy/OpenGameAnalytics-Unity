using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System;

public class GameActionAPI : MonoBehaviour
{
    bool DEBUG = false;

    public static GameActionAPI instance;
    public string API_URL = "http://127.0.0.1:5000";
    public int GAME_ID = 959742;
    public float session_continue_time = 15f;
    public bool poll_server_when_playing = false;

    User thisUser;
    Session thisSession;

    Queue<DataPoint> dataQueue;

    private void Awake() {
        if (instance != null) {
            Destroy(gameObject);
        } else {
            instance = this;
            DontDestroyOnLoad(gameObject);
            dataQueue = new Queue<DataPoint>();
            InvokeRepeating("SendDataPoints", 20f, 20f);
        }
    }

    private void Start() {
        Debug.Log("Starting");
        if (DEBUG) {
            Debug.Log("DELETING PLAYER PREFS");
            PlayerPrefs.DeleteAll();
            Debug.Log("TESTING SAVE DATA");
            TestSaveData();
        }
        Debug.Log("Starting session");
        StartCoroutine(StartSession());
    }

    private void TestSaveData() {
        // Saves this user's age as 32
        GameActionAPI.instance.SaveUserInfo("age", "32");

        // Saves that this user's study ID is 00012
        GameActionAPI.instance.SaveUserInfo("study_id", "00012");

        // Saves that the user choose option #4 during choice #2
        GameActionAPI.instance.SaveUserAction("choice2", "4");

        // Saves that the user was randomly assigned to the 1st person condition
        GameActionAPI.instance.AssignCondition("perspective", "1st-person");

        // Saves that the user chose answer 'A' for the first question of some quiz
        GameActionAPI.instance.SaveStudyEndpoint("QuizQuestion1", "A");
        // ^^^ Would only use this if there's something in the game itself to measure an endpoint
        // i.e. something we may expect to be changed by the game
    }

    /// <summary>
    /// Saves information about the user that isn't related to any experiment (i.e. neither an independent nor dependent variable).
    /// </summary>
    /// <param name="attributeName">The name of the attribute you're planning to save.</param>
    /// <param name="info">The content of the attribute you're saving.</param>
    public void SaveUserInfo(string attributeName, string info) {
        DataPoint point = new DataPoint(attributeName, info, "userinfo");
        dataQueue.Enqueue(point);
        SendDataPoints();
    }

    /// <summary>
    /// Saves an action the user has performed in the game.
    /// </summary>
    /// <param name="actionName">Name of the kind of action the player performed.</param>
    /// <param name="info">Content of the action they performed.</param>
    public void SaveUserAction(string actionName, string info) {
        DataPoint point = new DataPoint(actionName, info, "action");
        dataQueue.Enqueue(point);
        SendDataPoints();
    }

    /// <summary>
    /// Saves when the user has been assigned to an experimental group (i.e. the independent variable).
    /// </summary>
    /// <param name="attributeName">Name of the thing being modified by the experimental group.</param>
    /// <param name="info">The particular condition that the user has been assigned.</param>
    public void AssignCondition(string attributeName, string info) {
        DataPoint point = new DataPoint(attributeName, info, "independent");
        dataQueue.Enqueue(point);
        SendDataPoints();
    }

    /// <summary>
    /// Saves a piece of information that we expect to differ based on experimental group (i.e. the dependent variable). 
    /// </summary>
    /// <param name="attributeName">Name of the kind of data this represents.</param>
    /// <param name="info">Content of the data point collected.</param>
    public void SaveStudyEndpoint(string attributeName, string info) {
        DataPoint point = new DataPoint(attributeName, info, "dependent");
        dataQueue.Enqueue(point);
        SendDataPoints();
    }

    private IEnumerator GetUser() {
        // If request fails, continue to poll every 15 seconds
        bool userAcquired = false;
        while (!userAcquired) {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(API_URL + "/user/" + GAME_ID)) {
                // Request and wait for the desired page.
                yield return webRequest.SendWebRequest();

                if (webRequest.isNetworkError || webRequest.isHttpError) {
                    Debug.LogError("Error from server when getting user: " + webRequest.responseCode);
                    yield return new WaitForSeconds(15f);
                } else {
                    string res = webRequest.downloadHandler.text;
                    thisUser = JsonUtility.FromJson<User>(res);
                    Debug.Log(thisUser);
                    SaveUser();
                    userAcquired = true;
                }
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

    private IEnumerator GetSession() {
        bool retrievedSession = false;
        while (!retrievedSession) {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(API_URL + "/session/" + GAME_ID)) {
                // Request and wait for the desired page.
                yield return webRequest.SendWebRequest();

                if (webRequest.isNetworkError || webRequest.isHttpError) {
                    Debug.LogError("Error from server when getting session: " + webRequest.responseCode);
                    yield return new WaitForSeconds(15f);
                } else {
                    string res = webRequest.downloadHandler.text;
                    thisSession = JsonUtility.FromJson<Session>(res);
                    Debug.Log(thisSession);
                    retrievedSession = true;
                }
            }
        }
    }

    private IEnumerator AssociateSession() {
        // POST /session/user
        bool associatedSession = false;
        while (!associatedSession) {
            using (UnityWebRequest www = new UnityWebRequest(API_URL + "/session/user", "POST")) {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(JsonUtility.ToJson(new UserSession(thisUser, thisSession)));
                www.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
                www.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");
                yield return www.SendWebRequest();

                if (www.isNetworkError || www.isHttpError) {
                    Debug.LogError("Error from server: " + www.responseCode);
                    yield return new WaitForSeconds(15f);
                } else {
                    Debug.Log("Session associated: " + www.responseCode);
                    associatedSession = true;
                }
            }
        }

        if (poll_server_when_playing) {
            InvokeRepeating("LogSessionContinue", session_continue_time, session_continue_time);
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

    public void LogSessionContinue() {
        StartCoroutine(PostSessionContinue());
    }

    private IEnumerator PostSessionContinue() {
        if (thisSession == null) {
            yield break;
        }
        using (UnityWebRequest www = new UnityWebRequest(API_URL + "/session/continue", "POST")) {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(JsonUtility.ToJson(new UserSession(thisUser, thisSession)));
            www.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError) {
                Debug.LogError("Error from server: " + www.responseCode);
            } else {
                Debug.Log("Session continue: " + www.responseCode);
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

            if (www.isNetworkError || www.isHttpError) {
                Debug.LogError("Error from server: " + www.responseCode);
            } else {
                Debug.Log("Session end: " + www.responseCode);
            }
        }
    }


    bool isSending = false;

    private void SendDataPoints() {
        if (isSending || dataQueue.Count < 1 || thisUser == null || thisSession == null) {
            Debug.Log("Not sending! " + dataQueue.Count + " in the queue.");
            return;
        } else {
            isSending = true;
            StartCoroutine(PostDataPoints());
        }
    }



    private IEnumerator PostDataPoints() {
        while(dataQueue.Count > 0) {
            DataPoint currentPoint = dataQueue.Peek();
            string postURL = API_URL;

            switch (currentPoint.type) {
                case DataPoint.measureType.action:
                    postURL += "/session/action";
                    break;
                case DataPoint.measureType.independent:
                    postURL += "/experiment/independent";
                    break;
                case DataPoint.measureType.dependent:
                    postURL += "/experiment/dependent";
                    break;
                case DataPoint.measureType.userinfo:
                    postURL += "/user/info";
                    break;
            }

            using (UnityWebRequest www = new UnityWebRequest(postURL, "POST")) {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(JsonUtility.ToJson(new PostableDataPoint(thisUser, thisSession.play_session_id, currentPoint)));
                www.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
                www.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");
                yield return www.SendWebRequest();

                if (www.isNetworkError || www.isHttpError) {
                    Debug.LogError("Error from server: " + www.responseCode);
                    isSending = false;
                    yield break;
                } else {
                    Debug.Log("Posted data point: " + www.responseCode);
                    dataQueue.Dequeue();
                }
            }
        }

        yield return null;
        isSending = false;
    }

}
