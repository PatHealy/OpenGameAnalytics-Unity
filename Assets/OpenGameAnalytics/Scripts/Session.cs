using System;
namespace OGA
{
    [Serializable]
    public class Session
    {
        public int play_session_id;

        public Session(int id) {
            play_session_id = id;
        }

        public override string ToString() {
            return "SESSION: " + play_session_id;
        }
    }
}
