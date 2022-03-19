using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System;

namespace OGA
{
    public class OpenGameAnalytics : MonoBehaviour
    {
        public bool DEBUG_MODE = false;
        public bool LOG_ACTIONS = false;

        public static OpenGameAnalytics instance;
        public string API_URL = "http://127.0.0.1:5000";
        public int GAME_ID = 959742;
        public float session_continue_time = 15f;
        public bool poll_server_when_playing = false;

        User thisUser;
        Session thisSession;

        DataSaver saver;
        Queue<ServerAction> dataQueue;

        bool isSending = false;

        private void Awake() {
            if (instance != null) {
                Destroy(gameObject);
            } else {
                instance = this;
                DontDestroyOnLoad(gameObject);

                dataQueue = new Queue<ServerAction>();

                saver = new DataSaver();

                if (DEBUG_MODE) {
                    Debug.Log("DEBUG MODE ENABLED. Debug mode will delete player/session data at runtime and should never be enabled in production.");
                    if (LOG_ACTIONS) { Debug.Log("Deleting old queue"); }
                    saver.DeleteData("dataQueue");
                }

                object loadedData = saver.LoadData("dataQueue");
                if (loadedData != null) {
                    if (LOG_ACTIONS) { Debug.Log("Attempting queue load"); }
                    try {
                        dataQueue = (Queue<ServerAction>)loadedData;
                    } catch (Exception e) {
                        Debug.Log("Problem casting loaded data to queue.\n" + e.StackTrace);
                    }
                }

                InvokeRepeating("SendDataPoints", 20f, 20f);
            }
        }

        private void Start() {
            if (DEBUG_MODE) {
                if (LOG_ACTIONS) { Debug.Log("DELETING PLAYER PREFS"); }
                PlayerPrefs.DeleteAll();
            }

            if (!LoadUser()) {
                dataQueue.Enqueue(new ServerAction(ServerAction.ActionType.CreateUser));
            }

            dataQueue.Enqueue(new ServerAction(ServerAction.ActionType.StartSession));
            saver.SaveData(dataQueue, "dataQueue");
            SendDataPoints();

            if (poll_server_when_playing) {
                InvokeRepeating("SessionContinue", session_continue_time, session_continue_time);
            }
        }

        private void SessionContinue() {
            dataQueue.Enqueue(new ServerAction(ServerAction.ActionType.ContinueSession));
            SendDataPoints();
        }

        private void OnApplicationQuit() {
            dataQueue.Enqueue(new ServerAction(ServerAction.ActionType.EndSession));
            SendDataPoints();
        }

        /// <summary>
        /// Saves information about the user that isn't related to any experiment (i.e. neither an independent nor dependent variable).
        /// </summary>
        /// <param name="attributeName">The name of the attribute you're planning to save.</param>
        /// <param name="info">The content of the attribute you're saving.</param>
        public void SaveUserInfo(string attributeName, string info) {
            DataPoint point = new DataPoint(attributeName, info, "userinfo");
            dataQueue.Enqueue(new ServerAction(ServerAction.ActionType.PostData, point));
            SendDataPoints();
        }

        /// <summary>
        /// Saves an action the user has performed in the game.
        /// </summary>
        /// <param name="actionName">Name of the kind of action the player performed.</param>
        /// <param name="info">Content of the action they performed.</param>
        public void SaveUserAction(string actionName, string info) {
            DataPoint point = new DataPoint(actionName, info, "action");
            dataQueue.Enqueue(new ServerAction(ServerAction.ActionType.PostData, point));
            SendDataPoints();
        }

        /// <summary>
        /// Saves when the user has been assigned to an experimental group (i.e. the independent variable).
        /// </summary>
        /// <param name="attributeName">Name of the thing being modified by the experimental group.</param>
        /// <param name="info">The particular condition that the user has been assigned.</param>
        public void AssignCondition(string attributeName, string info) {
            DataPoint point = new DataPoint(attributeName, info, "independent");
            dataQueue.Enqueue(new ServerAction(ServerAction.ActionType.PostData, point));
            SendDataPoints();
        }

        /// <summary>
        /// Saves a piece of information that we expect to differ based on experimental group (i.e. the dependent variable). 
        /// </summary>
        /// <param name="attributeName">Name of the kind of data this represents.</param>
        /// <param name="info">Content of the data point collected.</param>
        public void SaveStudyEndpoint(string attributeName, string info) {
            DataPoint point = new DataPoint(attributeName, info, "dependent");
            dataQueue.Enqueue(new ServerAction(ServerAction.ActionType.PostData, point));
            SendDataPoints();
        }

        bool LoadUser() {
            if (!PlayerPrefs.HasKey("username") || !PlayerPrefs.HasKey("token")) {
                return false;
            }

            Debug.Log("Loading old user");
            thisUser = new User(PlayerPrefs.GetString("username"), PlayerPrefs.GetString("token"));

            if (PlayerPrefs.HasKey("session")) {
                if (LOG_ACTIONS) { Debug.Log("Loading old session"); }
                thisSession = new Session(PlayerPrefs.GetInt("session"));
            }
            
            return true;
        }

        void SaveUser() {
            PlayerPrefs.SetString("username", thisUser.username);
            PlayerPrefs.SetString("token", thisUser.token);
        }

        private void SendDataPoints() {
            saver.SaveData(dataQueue, "dataQueue");
            if (isSending || dataQueue.Count < 1) {
                if (LOG_ACTIONS) { Debug.Log("Not sending! " + dataQueue.Count + " in the queue."); }
                return;
            } else {
                isSending = true;
                StartCoroutine(PostData());
            }
        }


        private string GetURL(ServerAction sa) {
            switch (sa.action) {
                case ServerAction.ActionType.CreateUser:
                    return "/user/" + GAME_ID;
                case ServerAction.ActionType.StartSession:
                    return "/session/" + GAME_ID;
                case ServerAction.ActionType.ContinueSession:
                    return "/session/continue";
                case ServerAction.ActionType.EndSession:
                    return "/session/end";
            }

            switch (sa.data.type) {
                case DataPoint.measureType.action:
                    return "/session/action";
                case DataPoint.measureType.independent:
                    return "/experiment/independent";
                case DataPoint.measureType.dependent:
                    return "/experiment/dependent";
                case DataPoint.measureType.userinfo:
                    return "/user/info";
            }
            return null;
        }

        private string GetMethod(ServerAction sa) {
            if (sa.action == ServerAction.ActionType.CreateUser) {
                return "GET";
            }
            return "POST";
        }

        private byte[] PackData(ServerAction sa) {
            switch (sa.action) {
                case ServerAction.ActionType.CreateUser:
                    return null;
                case ServerAction.ActionType.StartSession:
                    return Encoding.UTF8.GetBytes(JsonUtility.ToJson(new UserSession(thisUser, sa.created_at)));
                case ServerAction.ActionType.ContinueSession:
                    return Encoding.UTF8.GetBytes(JsonUtility.ToJson(new UserSession(thisUser, thisSession, sa.created_at)));
                case ServerAction.ActionType.EndSession:
                    return Encoding.UTF8.GetBytes(JsonUtility.ToJson(new UserSession(thisUser, thisSession, sa.created_at)));
                case ServerAction.ActionType.PostData:
                    return Encoding.UTF8.GetBytes(JsonUtility.ToJson(new PostableDataPoint(thisUser, thisSession.play_session_id, sa.data, sa.created_at)));
            }

            return null;
        }

        private void HandleResponse(ServerAction sa, string response) {
            switch (sa.action) {
                case ServerAction.ActionType.CreateUser:
                    thisUser = JsonUtility.FromJson<User>(response);
                    if (LOG_ACTIONS) { Debug.Log(thisUser); }
                    SaveUser();
                    break;
                case ServerAction.ActionType.StartSession:
                    thisSession = JsonUtility.FromJson<Session>(response);
                    PlayerPrefs.SetInt("session", thisSession.play_session_id);
                    if (LOG_ACTIONS) { Debug.Log(thisSession); }
                    break;
                case ServerAction.ActionType.ContinueSession:
                    if (LOG_ACTIONS) { Debug.Log("Session continued"); }
                    break;
                case ServerAction.ActionType.EndSession:
                    if (LOG_ACTIONS) { Debug.Log("Session ended"); }
                    break;
                case ServerAction.ActionType.PostData:
                    if (LOG_ACTIONS) { Debug.Log("Data posted"); }
                    break;
            }
        }

        private IEnumerator PostData() {
            while (dataQueue.Count > 0) {
                ServerAction currentPoint = dataQueue.Peek();

                bool shouldDequeue = true;
                if (thisUser == null && currentPoint.action != ServerAction.ActionType.CreateUser) {
                    currentPoint = new ServerAction(ServerAction.ActionType.CreateUser);
                    shouldDequeue = false;
                } else if (thisUser != null && thisSession == null && currentPoint.action != ServerAction.ActionType.StartSession) {
                    currentPoint = new ServerAction(ServerAction.ActionType.StartSession);
                    shouldDequeue = false;
                }

                if (LOG_ACTIONS) { Debug.Log("Attempting to send: " + currentPoint.ToString()); }

                string postURL = API_URL + GetURL(currentPoint);
                string method = GetMethod(currentPoint);

                using (UnityWebRequest www = new UnityWebRequest(postURL, method)) {
                    if (method == "POST") {
                        byte[] bodyRaw = PackData(currentPoint);
                        www.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
                    }
                    www.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
                    www.SetRequestHeader("Content-Type", "application/json");
                    yield return www.SendWebRequest();

                    if (www.isNetworkError || www.isHttpError) {
                        if (LOG_ACTIONS) { Debug.LogError("Error from server: " + www.responseCode); }
                        isSending = false;
                        yield break;
                    } else {
                        if (LOG_ACTIONS) { Debug.Log("Success with action (" + postURL + ": " + www.responseCode); }
                        HandleResponse(currentPoint, www.downloadHandler.text);
                        if (shouldDequeue) {
                            dataQueue.Dequeue();
                        }
                    }
                }

                saver.SaveData(dataQueue, "dataQueue");
            }

            yield return null;
            isSending = false;
        }

    }
}
