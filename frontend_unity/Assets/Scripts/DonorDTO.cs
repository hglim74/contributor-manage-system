using System;

[Serializable]
public class DonorData
{
    public string name;
    public int amount;
    public string grade; // VVIP, GOLD, SILVER
    public string message;
}

[Serializable]
public class SocketPayload
{
    public string type;
    public DonorData payload;
}