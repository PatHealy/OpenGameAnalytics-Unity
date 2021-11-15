using System;

[Serializable]
public class User
{
    public string username;
    public string token;

    public User(string us, string to) {
        username = us;
        token = to;
    }

    public override string ToString() {
        return username + ": " + token;
    }
}
