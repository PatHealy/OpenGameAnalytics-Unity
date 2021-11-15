using System;

[Serializable]
public class Session
{
    public int play_session_id;

    public override string ToString() {
        return "SESSION: " + play_session_id;
    }
}
