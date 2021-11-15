using System;

[Serializable]
public class UserSession
{
    public int play_session_id;
    public User user;

    public override string ToString() {
        return "SESSION: " + play_session_id + ", " + user;
    }
}
