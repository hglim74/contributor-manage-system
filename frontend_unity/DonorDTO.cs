[System.Serializable]
public class DonorData {
    public string name;
    public int amount;
    public string grade;
    public string message;
}

[System.Serializable]
public class SocketPayload {
    public string type;
    public DonorData payload;
}