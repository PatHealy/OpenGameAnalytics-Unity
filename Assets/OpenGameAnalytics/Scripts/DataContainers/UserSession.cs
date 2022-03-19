using System;

namespace OGA
{
    [Serializable]
    public class UserSession
    {
        public int play_session_id;
        public User user;
        public string created_at;

        public UserSession(User us, Session sess, string creation_time) {
            user = us;
            play_session_id = sess.play_session_id;
            created_at = creation_time;
        }

        public UserSession(User us, string creation_time) {
            user = us;
            created_at = creation_time;
            play_session_id = -1;
        }

        public override string ToString() {
            return "SESSION: " + play_session_id + ", " + user;
        }
    }
}
