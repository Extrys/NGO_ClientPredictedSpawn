using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class MonoPredictedSpawnerEasy : NetworkBehaviour, INetworkPrefabInstanceHandler
{
	[SerializeField] NetworkObject prefab;
	Queue<NetworkObject> queuedIntances = new();

	private void Update()
	{
		if (UnityEngine.InputSystem.Mouse.current.backButton.wasPressedThisFrame)
			Spawn(Camera.main.transform.position + Camera.main.transform.forward * 3, transform.rotation);
	}

	public void Spawn(Vector3 position, Quaternion orientation)
	{
		var spawned = Instantiate(prefab, position, orientation);
		spawned.SetSceneObjectStatus(false);
		queuedIntances.Enqueue(spawned);
		SpawnServerRPC(NetworkManager.LocalClientId, position, orientation);
	}

	public override void OnNetworkSpawn()
	{
		NetworkManager.PrefabHandler.AddHandler(prefab, this);
		List<NetworkObject> offlineInstances = queuedIntances.ToList();
		foreach (var instance in offlineInstances)
			SpawnServerRPC(NetworkManager.LocalClientId, instance.transform.position, instance.transform.rotation);
	}

	public override void OnNetworkDespawn() => NetworkManager.PrefabHandler.RemoveHandler(prefab);

	[ServerRpc(RequireOwnership = false)] //El instantiate realmente esta siendo overriden por el metodo de abajo
	void SpawnServerRPC(ulong clientId, Vector3 position, Quaternion orientation) => NetworkManager.SpawnManager.InstantiateAndSpawn(prefab, clientId, false, false, false, position, orientation);

	NetworkObject INetworkPrefabInstanceHandler.Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
	{
		return NetworkManager.LocalClientId == ownerClientId ? queuedIntances.Dequeue() : Instantiate(prefab, position, rotation);
	}
	void INetworkPrefabInstanceHandler.Destroy(NetworkObject networkObject) => Destroy(networkObject);
}