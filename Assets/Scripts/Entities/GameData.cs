using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameData : INetworkSerializable
{
    private Dictionary<ulong, PlayerGameData> m_PlayerGameData;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
    }
}
