using System;

namespace OGA
{
    [Serializable]
    public class ServerAction
    {
        public enum ActionType {CreateUser, StartSession, ContinueSession, EndSession, PostData};
        public ActionType action;
        public string created_at;
        public DataPoint data;

        public ServerAction(ActionType action_type) {
            action = action_type;
            created_at = DateTime.Now.ToString();
        }

        public ServerAction(ActionType action_type, DataPoint data_point) {
            action = action_type;
            created_at = DateTime.Now.ToString();
            data = data_point;
        }

        public override string ToString() {
            string to_return = "Action: ";
            switch (action) {
                case ActionType.StartSession:
                    to_return += "StartSession; ";
                    break;
                case ActionType.ContinueSession:
                    to_return += "ContinueSession; ";
                    break;
                case ActionType.EndSession:
                    to_return += "EndSession; ";
                    break;
                case ActionType.CreateUser:
                    to_return += "CreateUser; ";
                    break;
                case ActionType.PostData:
                    to_return += "PostData; ";
                    break;
            }

            to_return += "Created at: " + created_at.ToString() + "; ";

            if (action == ActionType.PostData) {
                to_return += "Data: {" + data.ToString() + "}; ";
            }

            return to_return;
        }

    }
}
