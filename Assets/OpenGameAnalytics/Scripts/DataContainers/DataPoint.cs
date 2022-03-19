using System;

namespace OGA
{
    [Serializable]
    public class DataPoint
    {
        public string attribute_name, info;
        public enum measureType { action, independent, dependent, userinfo };
        public measureType type;

        public DataPoint(string nm, string data, string datatype) {
            attribute_name = nm;
            info = data;

            switch (datatype) {
                case "action":
                    type = measureType.action;
                    break;
                case "independent":
                    type = measureType.independent;
                    break;
                case "dependent":
                    type = measureType.dependent;
                    break;
                case "userinfo":
                    type = measureType.userinfo;
                    break;
            }
        }

        public override string ToString() {
            string to_out = "type: ";
            switch (type) {
                case measureType.action:
                    to_out += "action; ";
                    break;
                case measureType.independent:
                    to_out += "independent; ";
                    break;
                case measureType.dependent:
                    to_out += "dependent; ";
                    break;
                case measureType.userinfo:
                    to_out += "userinfo; ";
                    break;
            }

            to_out += "Attribute Name: " + attribute_name;
            to_out += "; Info: " + info + "; ";

            return to_out;
        }

    }
}
