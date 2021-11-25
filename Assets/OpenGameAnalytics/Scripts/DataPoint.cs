using System;

[Serializable]

public class DataPoint
{
    public string attribute_name, info;
    public enum measureType {action, independent, dependent, userinfo};
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

}
