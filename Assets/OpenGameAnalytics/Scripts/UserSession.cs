using System;

[Serializable]
public class UserSession
{
    public int play_session_id;
    public User user;

    public UserSession(User us, Session sess) {
        user = us;
        play_session_id = sess.play_session_id;
    }

    public override string ToString() {
        return "SESSION: " + play_session_id + ", " + user;
    }
}
