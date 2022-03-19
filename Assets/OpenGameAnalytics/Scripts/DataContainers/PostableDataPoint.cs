using System;

namespace OGA
{
    [Serializable]
    public class PostableDataPoint
    {
        public User user;
        public int play_session_id;
        public DataPoint data_point;
        public string created_at;

        public PostableDataPoint(User usr, int session_id, DataPoint dp, string creation) {
            user = new User(usr.username, usr.token);
            play_session_id = session_id;
            data_point = dp;
            created_at = creation;
        }
    }
}
