using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerNetwork : NetworkBehaviour
{
    private NetworkVariable<MyCustomData> randomNumber = new NetworkVariable<MyCustomData>(new MyCustomData{_int = 56,_bool = true,}, NetworkVariableReadPermission.Everyone,NetworkVariableWritePermission.Owner);
    float moveSpeed = 3f;
    Vector3 moveDir;

    public struct MyCustomData:INetworkSerializable
    {
        public int _int;
        public bool _bool;
        public FixedString128Bytes message;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref _int);
            serializer.SerializeValue(ref _bool);
            serializer.SerializeValue(ref message);
        }
    }

    private void Update()
    {
        if (IsOwner)
        {
            Vector3 prevMoveDir = moveDir;
            HandleMovementInput();
            if (prevMoveDir != moveDir)
            {
                SetMoveDirServerRpc(moveDir);
            }
        }

        if (IsServer)
        {
            if (moveDir != Vector3.zero)
            {
                Move(moveDir);
            }
        }

    }

    private void HandleMovementInput()
    {
        moveDir = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) moveDir.z = 1f;
        if (Input.GetKey(KeyCode.S)) moveDir.z = -1f;
        if (Input.GetKey(KeyCode.A)) moveDir.x = -1f;
        if (Input.GetKey(KeyCode.D)) moveDir.x = 1f;
    }

    [ServerRpc]
    private void SetMoveDirServerRpc(Vector3 moveDir)
    {
        this.moveDir = moveDir;
    }

    private void Move(Vector3 moveDir, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        // Update the player's position on the server
        Transform playerTransform = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.transform;
        playerTransform.position += moveDir * moveSpeed * Time.deltaTime;

        // Broadcast the updated position to all clients
        UpdateClientPositionClientRpc(playerTransform.position);
    }

    [ClientRpc]
    private void UpdateClientPositionClientRpc(Vector3 updatedPosition, ClientRpcParams rpcParams = default)
    {
        // Update the player's position on the clients
        transform.position = updatedPosition;
    }
}